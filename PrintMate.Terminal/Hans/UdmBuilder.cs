using Hans.NET.libs;
using Hans.NET.Models;
using ImTools;
using Newtonsoft.Json;
using PrintMate.Terminal.Parsers.Shared;
using ProjectParserTest.Parsers.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PrintMate.Terminal.Parsers.Shared.Models;
using ProjectParserTest.Parsers.Shared.Enums;
using static Hans.NET.libs.HM_UDM_DLL;
using RegionModel = ProjectParserTest.Parsers.Shared.Models.Region;

namespace PrintMate.Terminal.Hans
{
    public class UdmBuilder
    {
        private ScanatorConfiguration _scanConfig;
        private readonly float _currentZOffset = 0.0f;

        private readonly string LogsPathDirectory;
        private readonly string BinPathsDirectory;

        public UdmBuilder(ScanatorConfiguration scanConfig)
        {
            _scanConfig = scanConfig;

            LogsPathDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UdmLogs");
            BinPathsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UdmBinFiles");

            if (!Directory.Exists(LogsPathDirectory)) Directory.CreateDirectory(LogsPathDirectory);
            if (!Directory.Exists(BinPathsDirectory)) Directory.CreateDirectory(BinPathsDirectory);
        }

        private void RemovePreviewRegions(Layer layer)
        {
            // Удаляем превью
            layer.Regions.RemoveAll(p =>
                p.GeometryRegion == GeometryRegion.InfillRegionPreview ||
                p.GeometryRegion == GeometryRegion.UpskinRegionPreview ||
                p.GeometryRegion == GeometryRegion.DownskinRegionPreview);
        }

        public string BuildLayer(Layer layer)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();

                // Указываем путь к bin файлу
                string udmFile = System.IO.Path.Combine(BinPathsDirectory, $"{_scanConfig.CardInfo.SeqIndex}____{Guid.NewGuid().ToString()}.bin");

                // Создаем новый файл
                if (UDM_NewFile() != 0)
                    throw new InvalidOperationException("Failed to create UDM file");

                // Начинаем 
                UDM_Main();
                UDM_SkyWriting(1);
                double[] paraK = _scanConfig.ThirdAxisConfig.CorrectionPolynomial;
                int correctionResult = UDM_Set3dCorrectionPara((float)_scanConfig.BeamConfig.FocalLengthMm, paraK, paraK.Length);
                ApplyScannerConfig(_scanConfig.ScannerConfig);

                MarkParameter[] parameters = new MarkParameter[layer.Regions.Count];
                for (int i = 0; i < layer.Regions.Count; i++)
                {
                    var region = layer.Regions[i];
                    //region.Parameters.LaserBeamDiameter = 55;
                    
                    float powerWatts = (float)region.Parameters.LaserPower;
                    float originalWatts = powerWatts;

                    // Применяем коррекцию к ваттам (если включена)
                    if (_scanConfig.FunctionSwitcherConfig.EnablePowerCorrection)
                    {
                        powerWatts = _scanConfig.LaserPowerConfig.GetCorrectPowerWatts(powerWatts);
                        //Console.WriteLine($"[UdmBuilder] Power correction: {originalWatts}W -> {powerWatts}W");
                    }

                    // Конвертируем скорректированные ватты в проценты
                    float powerPercent = _scanConfig.LaserPowerConfig.ConvertPower(powerWatts);

                    // Ограничиваем диапазон 0-100%
                    powerPercent = Math.Max(20, Math.Min(100f, powerPercent));

                    //Console.WriteLine($"[UdmBuilder] Power final: {originalWatts}W -> {powerWatts}W -> {powerPercent}%");

                    // Предупреждение о низкой мощности (только для первого региона слоя)
                    if (i == 0 && originalWatts < 100)
                    {
                        //Console.WriteLine($"⚠️ [UdmBuilder] WARNING: Very low power from CLI!");
                        //Console.WriteLine($"  Original power: {originalWatts}W (from CLI)");
                        //Console.WriteLine($"  After correction: {powerWatts}W");
                        //Console.WriteLine($"  Percentage: {powerPercent}% of {_scanConfig.LaserPowerConfig.MaxPower}W max");
                        //Console.WriteLine($"  This may be insufficient for metal melting!");
                        //Console.WriteLine($"  Recommended: 150-300W for typical metal powder");
                    }

                    // processVariables
                    var processVariables = _scanConfig.ProcessVariablesMap.NonDepends.First();
                    if (_scanConfig.FunctionSwitcherConfig.EnableDynamicChangeVariables)
                    {
                        ProcessVariables closest = _scanConfig.ProcessVariablesMap.MarkSpeed[0];
                        int minDifference = Math.Abs(_scanConfig.ProcessVariablesMap.MarkSpeed[0].MarkSpeed - (int)region.Parameters.LaserSpeed);

                        foreach (var config in _scanConfig.ProcessVariablesMap.MarkSpeed)
                        {
                            int difference = Math.Abs(config.MarkSpeed - (int)region.Parameters.LaserSpeed);
                            if (difference < minDifference)
                            {
                                minDifference = difference;
                                processVariables = config;
                            }
                        }
                    }

                    parameters[i] = new MarkParameter();
                    parameters[i].MarkSpeed = (uint)processVariables.MarkSpeed;
                    parameters[i].JumpSpeed = (uint)processVariables.JumpSpeed;
                    parameters[i].MarkDelay = (uint)processVariables.MarkDelay;
                    parameters[i].JumpDelay = (uint)processVariables.JumpDelay;
                    parameters[i].PolygonDelay = (uint)processVariables.PolygonDelay;
                    //parameters[i].LaserPower = powerPercent;
                    parameters[i].LaserPower = 60;

                    parameters[i].AnalogMode = 1;
                    parameters[i].MarkCount = 1;
                    //parameters[i].MarkCount = 0; 


                    if (processVariables.Swenable)
                    {
                        parameters[i].LaserOnDelay = (float)processVariables.LaserOnDelay;
                        parameters[i].LaserOffDelay = (float)processVariables.LaserOffDelay;
                    }
                    else
                    {
                        parameters[i].LaserOnDelay = (float)processVariables.LaserOnDelay;
                        parameters[i].LaserOffDelay = (float)processVariables.LaserOffDelay;
                    }


                    stringBuilder.AppendLine($"Начало настроек нового региона.");
                    stringBuilder.AppendLine($"Region {region.GeometryRegion}, Type: {region.Type == BlockType.Hatch}, LaserNum: {region.LaserNum}");
                    stringBuilder.AppendLine($"Region params: SpeedCLI: {region.Parameters.LaserSpeed}, LaserPowerCLI: {region.Parameters.LaserPower} W");
                    stringBuilder.AppendLine($"MaxPower: {_scanConfig.LaserPowerConfig.MaxPower} W");
                    stringBuilder.AppendLine($"Converted power: {powerPercent}% (после ConvertPower и ApplyOffsetCorrection)");
                    stringBuilder.AppendLine($"Region params: SkywritingCLI: {region.Parameters.Skywriting}, SkyWritting from settings: {processVariables.Swenable},  LaserBeamDiameterCLI: {region.Parameters.LaserBeamDiameter}");
                    stringBuilder.AppendLine($"Преобразованные настройки: ");
                    stringBuilder.AppendLine(JsonConvert.SerializeObject(parameters[i], Formatting.Indented));
                }
                UDM_SetLayersPara(parameters, parameters.Length);

                for (int regionIndex = 0; regionIndex < layer.Regions.Count; regionIndex++)
                {
                    var region = layer.Regions[regionIndex];
                    stringBuilder.AppendLine($"\n\nRegionId: {region.Part.Id}, Region: {region.GeometryRegion}, Type: {region.Type}");
                    //UDM_SkyWriting((int)region.Parameters.Skywriting);

                    // Skip empty regions
                    if (region.PolyLines == null || region.PolyLines.Count == 0)
                        continue;

                    // Вычисляем мощность для этого региона (нужна для power offset)
                    float currentPowerWatts = (float)region.Parameters.LaserPower;
                    if (_scanConfig.FunctionSwitcherConfig.EnablePowerCorrection)
                    {
                        currentPowerWatts = _scanConfig.LaserPowerConfig.GetCorrectPowerWatts(currentPowerWatts);
                    }

                    // Z coordinate from beam diameter
                    // Если диаметр не задан или равен 0, используем минимальный диаметр (фокус)
                    double targetDiameter = region.Parameters.LaserBeamDiameter;
                    if (targetDiameter <= 0 || targetDiameter < _scanConfig.BeamConfig.MinBeamDiameterMicron)
                    {
                        targetDiameter = _scanConfig.BeamConfig.MinBeamDiameterMicron;
                        //Console.WriteLine($"[UdmBuilder] Beam diameter not set, using min diameter (focus): {targetDiameter} μm");
                    }

                    float zOffset = _scanConfig.BeamConfig.CalculateZOffset(targetDiameter);
                    stringBuilder.AppendLine($"ZOFFSET BEAM: {zOffset}");
                    //Console.WriteLine($"[UdmBuilder] Region {regionIndex}: Beam diameter {targetDiameter} μm -> Z offset {zOffset} mm");

                    // Предупреждение о влиянии расфокусировки на интенсивность (только для первого региона)
                    if (regionIndex == 0 && targetDiameter > _scanConfig.BeamConfig.MinBeamDiameterMicron * 1.5)
                    {
                        double ratio = targetDiameter / _scanConfig.BeamConfig.MinBeamDiameterMicron;
                        double intensityLoss = ratio * ratio; // I₀/I = (d/d₀)²

                        //Console.WriteLine($"\n⚠️ [UdmBuilder] DEFOCUS WARNING:");
                        //Console.WriteLine($"  Beam diameter: {targetDiameter:F1} μm (vs minimum {_scanConfig.BeamConfig.MinBeamDiameterMicron:F1} μm)");
                        //Console.WriteLine($"  Defocus ratio: {ratio:F2}x");
                        //Console.WriteLine($"  Intensity reduction: {intensityLoss:F2}x (loses {(1 - 1/intensityLoss) * 100:F1}% intensity)");
                        //Console.WriteLine($"  Z-offset: {zOffset:F3} mm from focus");
                        //Console.WriteLine($"  Effective power density is significantly reduced!");
                        //Console.WriteLine($"  Consider: reducing beam diameter OR increasing laser power\n");
                    }

                    // Power offset correction (в микронах)
                    float powerOffsetMicrons = 0;
                    if (_scanConfig.FunctionSwitcherConfig.EnablePowerOffset)
                    {
                        powerOffsetMicrons = _scanConfig.BeamConfig.GetPowerOffset(
                            currentPowerWatts,
                            _scanConfig.LaserPowerConfig.MaxPower
                        );
                        //Console.WriteLine($"[UdmBuilder] Power offset: {currentPowerWatts}W -> {powerOffsetMicrons} μm");
                    }

                    for (int polyLineIndex = 0; polyLineIndex < region.PolyLines.Count; polyLineIndex++)
                    {
                        var polyLine = region.PolyLines[polyLineIndex];
                        structUdmPos[] hansPoints = new structUdmPos[polyLine.Points.Count];

                        for (int pointIndex = 0; pointIndex < polyLine.Points.Count; pointIndex++)
                        {
                            var point = polyLine.Points[pointIndex];

                            // Алгоритм как в Java BeamConfig.getCorrectZValue():
                            // 1. Вычисляем текущее фокусное расстояние от точки (x,y,0) до объектива
                            //    focalLength = sqrt(x² + y² + (baseFocal + z)²)
                            //    Для z=0: focalLength = sqrt(x² + y² + baseFocal²)
                            double focalLengthMm = Math.Sqrt(
                                point.X * point.X +
                                point.Y * point.Y +
                                Math.Pow(_scanConfig.ThirdAxisConfig.BaseFocal, 2)
                            );
                            double focalLengthMicron = focalLengthMm * 1000.0;

                            // 2. Добавляем offset от диаметра луча (если включено)
                            if (_scanConfig.FunctionSwitcherConfig.EnableDiameterChange)
                            {
                                focalLengthMicron += zOffset * 1000.0;  // мм -> μm
                            }

                            // 3. Вычитаем power offset (если включен)
                            if (_scanConfig.FunctionSwitcherConfig.EnablePowerOffset)
                            {
                                focalLengthMicron -= powerOffsetMicrons;
                            }

                            // 4. Преобразуем обратно через полином: Z = a*f² + b*f + c
                            float zFinal;
                            if (_scanConfig.FunctionSwitcherConfig.EnableZCorrection)
                            {
                                double f = focalLengthMicron / 1000.0;  // μm -> mm
                                zFinal = (float)(
                                    _scanConfig.ThirdAxisConfig.Afactor * f * f +
                                    _scanConfig.ThirdAxisConfig.Bfactor * f +
                                    _scanConfig.ThirdAxisConfig.Cfactor
                                );
                            }
                            else
                            {
                                // Если коррекция отключена, просто возвращаем 0
                                zFinal = 0.0f;
                            }

                            hansPoints[pointIndex] = new structUdmPos
                            {
                                x = point.X,
                                y = point.Y,
                                z = zFinal,
                                //z = UDM_GetZvalue(point.X, point.Y, (float)CorrectDiameterPrecise(region.Parameters.LaserBeamDiameter)),
                                a = 0  // Обычно не используется
                            };

                            // Логируем первую точку каждого региона для отладки
                            if (polyLineIndex == 0 && pointIndex == 0)
                            {
                                //Console.WriteLine($"[UdmBuilder] First point Z calculation (Java algorithm):");
                                //Console.WriteLine($"  Point (X,Y): ({point.X:F2}, {point.Y:F2}) mm");
                                //Console.WriteLine($"  baseFocal: {_scanConfig.ThirdAxisConfig.BaseFocal:F2} mm");
                                //Console.WriteLine($"  focalLength: {focalLengthMm:F4} mm = {focalLengthMicron:F1} μm");
                                if (_scanConfig.FunctionSwitcherConfig.EnableDiameterChange)
                                {
                                    double afterDiameter = focalLengthMicron - (zOffset * 1000.0);
                                    //Console.WriteLine($"  + diameterOffset: {zOffset * 1000.0:F1} μm (from {targetDiameter:F1} μm beam)");
                                }
                                if (_scanConfig.FunctionSwitcherConfig.EnablePowerOffset)
                                {
                                    //Console.WriteLine($"  - powerOffset: {powerOffsetMicrons:F1} μm (from {currentPowerWatts:F1}W)");
                                }
                                //Console.WriteLine($"  Final focalLength: {focalLengthMicron:F1} μm = {focalLengthMicron / 1000.0:F4} mm");
                                //Console.WriteLine($"  Polynomial: Z = {_scanConfig.ThirdAxisConfig.Afactor}*f² + {_scanConfig.ThirdAxisConfig.Bfactor:F6}*f + {_scanConfig.ThirdAxisConfig.Cfactor:F6}");
                                //Console.WriteLine($"  zFinal: {zFinal:F6} mm");

                                // Проверка диапазона
                                if (zFinal < 0 || zFinal > 1)
                                {
                                    //Console.WriteLine($"  ⚠️ WARNING: Z = {zFinal:F6} mm is OUT OF RANGE [0, 1] mm!");
                                }
                            }
                        }
                        stringBuilder.AppendLine($"UDM_AddPolyline3D({JsonConvert.SerializeObject(hansPoints)})");
                        UDM_AddPolyline3D(hansPoints, hansPoints.Length, regionIndex);
                    }
                }

                // Завершаем построение и сохраняем файл
                UDM_Jump(0, 0, 0);
                UDM_EndMain();

                //Console.WriteLine($"UDM_FILE: {udmFile}");
                //Console.WriteLine($"LOG_FILE: {Path.Combine(LogsPathDirectory, udmFile.Replace(".bin", ".txt"))}");

                int saveResult = UDM_SaveToFile(udmFile);
                if (saveResult != 0)
                    throw new InvalidOperationException($"Failed to save UDM file. Error code: {saveResult}");

                File.WriteAllText(Path.Combine(LogsPathDirectory, $"MYBUILDER_{_scanConfig.CardInfo.IpAddress}_{Guid.NewGuid().ToString()}.txt"), stringBuilder.ToString());
                //Console.WriteLine("File successfull created !");
                return udmFile;
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                throw;
            }
            
        }

        public static double CorrectDiameterPrecise(double desiredDiameterMicron)
        {
            const double K = 1.19760479;   // 1 / 0.835
            const double B = -0.17964072;  // -0.15 / 0.835

            return K * desiredDiameterMicron + B;
        }

        private void ApplyScannerConfig(ScannerConfig config)
        {
            UDM_SetProtocol(1, 1);

            if (config.OffsetX != 0 ||
                config.OffsetY != 0 ||
                config.OffsetZ != 0)
            {
                UDM_SetOffset(
                    offsetX: config.OffsetX,
                    offsetY: config.OffsetY,
                    offsetZ: config.OffsetZ
                );
            }
            if (config.RotateAngle != 0)
                UDM_SetRotate(config.RotateAngle, 0, 0);
        }
    }
}
