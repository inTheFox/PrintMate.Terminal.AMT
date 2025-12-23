using Hans.NET.libs;
using Hans.NET.Models;
using LaserConfigurator.Models;
using PrintMate.Terminal.Parsers.Shared;
using PrintMate.Terminal.Parsers.Shared.Models;
using ProjectParserTest.Parsers.Shared.Enums;
using ProjectParserTest.Parsers.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Hans.NET.libs.HM_UDM_DLL;

namespace LaserConfigurator.Services
{
    /// <summary>
    /// Реализация сервиса генерации UDM файлов
    /// </summary>
    public class UdmService : IUdmService
    {
        public const float ScaleX = 0.8446f;
        public const float ScaleY = 0.9615f;


        public async Task<string> GenerateUdmDataAsync(
            ScanatorConfiguration config,
            List<(float x, float y)> points,
            ShapeParameters parameters)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Создаём слой (Layer) с одним регионом (Region)
                    // Преобразуем координаты из системы, где центр поля в (0,0),
                    // в систему сканатора (обычно 0..FieldSizeX, 0..FieldSizeY),
                    // если в конфигурации указаны размеры поля.
                    var layer = CreateLayerFromPoints(config, points, parameters);

                    // Используем UdmBuilderJavaPort - упрощённая версия без зависимости от всего PrintMate.Terminal
                    return GenerateUdmFromLayer(config, layer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating UDM: {ex.Message}");
                    return null;
                }
            });
        }

        public async Task<(string scanner1Data, string scanner2Data)> GenerateDualUdmDataAsync(
            ScanatorConfiguration scanner1Config,
            ScanatorConfiguration scanner2Config,
            List<(float x, float y)> part1Points,
            List<(float x, float y)> part2Points,
            ShapeParameters parameters)
        {
            // Hans SDK не потокобезопасен - генерируем UDM файлы последовательно
            string udm1 = null;
            string udm2 = null;

            if (part1Points != null && part1Points.Count >= 2)
            {
                udm1 = await GenerateUdmDataAsync(scanner1Config, part1Points, parameters);
            }

            if (part2Points != null && part2Points.Count >= 2)
            {
                udm2 = await GenerateUdmDataAsync(scanner2Config, part2Points, parameters);
            }

            return (udm1, udm2);
        }

        private Layer CreateLayerFromPoints(ScanatorConfiguration config, List<(float x, float y)> points, ShapeParameters parameters)
        {
            // Координаты уже заданы относительно центра поля (центр в (0,0)).
            // Hans сканатор работает в системе координат с центром в (0,0),
            // поэтому смещение не требуется.
            var polylinePoints = points.Select(p => new Point
            {
                X = p.x,
                Y = p.y
            }).ToList();

            // Создаём polyline
            var polyline = new PolyLine
            {
                Points = polylinePoints
            };

            // Создаём регион с параметрами
            var region = new Region
            {
                GeometryRegion = GeometryRegion.Contour,
                Type = BlockType.PolyLine,
                PolyLines = new List<PolyLine> { polyline },
                Parameters = new RegionParameters
                {
                    LaserSpeed = parameters.Speed,
                    LaserPower = parameters.Power,
                    LaserBeamDiameter = parameters.BeamDiameter  // Диаметр пучка
                }
            };

            // Создаём слой
            var layer = new Layer
            {
                Regions = new List<Region> { region }
            };

            return layer;
        }

        private string GenerateUdmFromLayer(ScanatorConfiguration config, Layer layer)
        {
            try
            {
                string udmFile = Path.Combine($"{config.CardInfo.SeqIndex}____{Guid.NewGuid()}.bin");

                // Инициализация UDM файла
                if (UDM_NewFile() != 0)
                {
                    Console.WriteLine("Failed to create UDM file");
                    return null;
                }

                UDM_Main();
                UDM_SkyWriting(1);
                UDM_SetProtocol(1, 1);

                if (config.ScannerConfig.OffsetX != 0 || config.ScannerConfig.OffsetY != 0 || config.ScannerConfig.OffsetZ != 0)
                {
                    UDM_SetOffset(
                        offsetX: config.ScannerConfig.OffsetX,
                        offsetY: config.ScannerConfig.OffsetY,
                        offsetZ: config.ScannerConfig.OffsetZ
                    );
                }
                if (config.ScannerConfig.RotateAngle != 0)
                {
                    UDM_SetRotate(config.ScannerConfig.RotateAngle, 0, 0);
                }

                // Смещение не применяем - фигуры рисуются относительно центра поля (0,0)
                // Если нужно смещение, оно уже учтено в координатах точек

                // Создание параметров маркировки
                var region = layer.Regions[0];
                var markParam = CreateMarkParameter(config, region);

                MarkParameter[] parameters = new[] { markParam };
                UDM_SetLayersPara(parameters, 1);

                // Обработка полилиний
                ProcessPolylines(config, region, 0);

                // Завершение
                UDM_SetAnalogValue(0, 0);
                UDM_Jump(0, 0, 0);
                UDM_EndMain();

                int saveResult = UDM_SaveToFile(udmFile);
                if (saveResult != 0)
                    throw new InvalidOperationException($"Failed to save UDM file. Error code: {saveResult}");

                return udmFile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateUdmFromLayer: {ex.Message}");
                return null;
            }
        }

        private MarkParameter CreateMarkParameter(ScanatorConfiguration config, Region region)
        {
            // Получаем параметры процесса (аналогично UdmBuilderJavaPort.cs строки 127-144)
            var processVars = config.ProcessVariablesMap.NonDepends.FirstOrDefault();

            // Если включена динамическая смена переменных в зависимости от скорости
            if (config.FunctionSwitcherConfig.EnableDynamicChangeVariables &&
                config.ProcessVariablesMap.MarkSpeed != null &&
                config.ProcessVariablesMap.MarkSpeed.Count > 0)
            {
                // Ищем ближайший набор параметров по скорости
                ProcessVariables closest = config.ProcessVariablesMap.MarkSpeed[0];
                int minDifference = Math.Abs(config.ProcessVariablesMap.MarkSpeed[0].MarkSpeed - (int)region.Parameters.LaserSpeed);

                foreach (var configVar in config.ProcessVariablesMap.MarkSpeed)
                {
                    int difference = Math.Abs(configVar.MarkSpeed - (int)region.Parameters.LaserSpeed);
                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        closest = configVar;
                    }
                }
                processVars = closest;
            }

            if (processVars == null)
            {
                // Создаём параметры по умолчанию
                processVars = new ProcessVariables
                {
                    MarkSpeed = (int)region.Parameters.LaserSpeed,
                    JumpSpeed = 5000,
                    MarkDelay = 50,
                    JumpDelay = 50,
                    PolygonDelay = 50,
                    LaserOnDelay = 100,
                    LaserOffDelay = 100
                };
            }

            // Расчёт мощности
            float powerWatts = (float)region.Parameters.LaserPower;
            if (config.FunctionSwitcherConfig.EnablePowerCorrection)
            {
                powerWatts = config.LaserPowerConfig.GetCorrectPowerWatts(powerWatts);
            }
            float powerPercent = config.LaserPowerConfig.ConvertPower(powerWatts);
            powerPercent = Math.Max(20, Math.Min(100f, powerPercent));

            return new MarkParameter
            {
                MarkSpeed = (uint)region.Parameters.LaserSpeed,
                JumpSpeed = (uint)processVars.JumpSpeed,
                MarkDelay = (uint)processVars.MarkDelay,
                JumpDelay = (uint)processVars.JumpDelay,
                PolygonDelay = (uint)processVars.PolygonDelay,
                LaserPower = 50,
                AnalogMode = 1,
                MarkCount = 1,
                LaserOnDelay = (float)processVars.LaserOnDelay,
                LaserOffDelay = (float)processVars.LaserOffDelay
            };
        }

        private void ProcessPolylines(ScanatorConfiguration config, Region region, int regionIndex)
        {
            foreach (var polyline in region.PolyLines)
            {
                if (polyline.Points == null || polyline.Points.Count < 2)
                    continue;

                // Создание 3D точек с коррекцией Z для диаметра пучка
                // Используем оригинальные точки без интерполяции
                var hansPoints = new List<structUdmPos>();
                double targetBeamDiameter = region.Parameters.LaserBeamDiameter;

                foreach (var point in polyline.Points)
                {
                    double correctedZ = 0.0;

                    // Если включена коррекция диаметра пучка
                    if (config.FunctionSwitcherConfig.EnableDiameterChange && targetBeamDiameter > 0)
                    {
                        correctedZ = CalculateZForBeamDiameter(config, targetBeamDiameter);
                    }


                    hansPoints.Add(new structUdmPos
                    {
                        x = point.X,
                        y = point.Y,
                        //z = (float)correctedZ,
                        a = 0
                    });

                    Console.WriteLine($"Point: X={point.X}, Y={point.Y}, Z={correctedZ}");
                }

                // Добавляем полилинию
                if (hansPoints.Count >= 2)
                {
                    UDM_AddPolyline3D(hansPoints.ToArray(), hansPoints.Count, regionIndex);
                }
            }
        }

        private double CalculateZForBeamDiameter(ScanatorConfiguration config, double targetDiameterMicrons)
        {
            // Упрощённая формула расчёта Z для достижения нужного диаметра пучка
            // Используется формула из UdmBuilderJavaPort
            double minDiameter = config.BeamConfig.MinBeamDiameterMicron;

            if (targetDiameterMicrons <= minDiameter)
                return 0.0;

            double rayleighLength = config.BeamConfig.RayleighLengthMicron;
            double zOffset = rayleighLength * Math.Sqrt(
                (Math.Pow(targetDiameterMicrons / 2, 2) / Math.Pow(minDiameter / 2, 2)) - 1
            );

            // Конвертируем из микрон в мм и применяем коррекцию
            return zOffset / 1000.0;
        }
    }
}
