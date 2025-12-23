using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hans.NET;

namespace PrintMateMC.ScannerConfig
{
    /// <summary>
    /// ПОЛНЫЙ ПРИМЕР ИСПОЛЬЗОВАНИЯ JSON КОНФИГУРАЦИИ СКАНЕРА
    ///
    /// Этот файл демонстрирует применение ВСЕХ параметров из вашей конфигурации
    /// для конвертации CLI файлов в Hans .bin файлы с полными коррекциями.
    /// </summary>
    public class FullScannerConfigExample
    {
        #region Data Classes (из вашей конфигурации)

        public class ScannerCardConfiguration
        {
            [JsonPropertyName("cardInfo")]
            public CardInfo CardInfo { get; set; }

            [JsonPropertyName("beamConfig")]
            public BeamConfig BeamConfig { get; set; }

            [JsonPropertyName("functionSwitcherConfig")]
            public FunctionSwitcherConfig FunctionSwitcherConfig { get; set; }

            [JsonPropertyName("laserPowerConfig")]
            public LaserPowerConfig LaserPowerConfig { get; set; }

            [JsonPropertyName("processVariablesMap")]
            public ProcessVariablesMap ProcessVariablesMap { get; set; }

            [JsonPropertyName("scannerConfig")]
            public ScannerConfig ScannerConfig { get; set; }

            [JsonPropertyName("thirdAxisConfig")]
            public ThirdAxisConfig ThirdAxisConfig { get; set; }
        }

        public class CardInfo
        {
            [JsonPropertyName("ipAddress")]
            public string IpAddress { get; set; }

            [JsonPropertyName("seqIndex")]
            public int SeqIndex { get; set; }
        }

        public class BeamConfig
        {
            [JsonPropertyName("focalLengthMm")]
            public double FocalLengthMm { get; set; }

            [JsonPropertyName("m2")]
            public double M2 { get; set; }

            [JsonPropertyName("minBeamDiameterMicron")]
            public double MinBeamDiameterMicron { get; set; }

            [JsonPropertyName("rayleighLengthMicron")]
            public double RayleighLengthMicron { get; set; }

            [JsonPropertyName("wavelengthNano")]
            public double WavelengthNano { get; set; }
        }

        public class FunctionSwitcherConfig
        {
            [JsonPropertyName("enableDiameterChange")]
            public bool EnableDiameterChange { get; set; }

            [JsonPropertyName("enableDynamicChangeVariables")]
            public bool EnableDynamicChangeVariables { get; set; }

            [JsonPropertyName("enablePowerCorrection")]
            public bool EnablePowerCorrection { get; set; }

            [JsonPropertyName("enablePowerOffset")]
            public bool EnablePowerOffset { get; set; }

            [JsonPropertyName("enableVariableJumpDelay")]
            public bool EnableVariableJumpDelay { get; set; }

            [JsonPropertyName("enableZCorrection")]
            public bool EnableZCorrection { get; set; }

            [JsonPropertyName("limitVariablesMaxPoint")]
            public bool LimitVariablesMaxPoint { get; set; }

            [JsonPropertyName("limitVariablesMinPoint")]
            public bool LimitVariablesMinPoint { get; set; }
        }

        public class LaserPowerConfig
        {
            [JsonPropertyName("actualPowerCorrectionValue")]
            public List<double> ActualPowerCorrectionValue { get; set; }

            [JsonPropertyName("maxPower")]
            public double MaxPower { get; set; }

            [JsonPropertyName("powerOffsetCFactor")]
            public double PowerOffsetCFactor { get; set; }

            [JsonPropertyName("powerOffsetKFactor")]
            public double PowerOffsetKFactor { get; set; }
        }

        public class ProcessVariablesMap
        {
            [JsonPropertyName("markSpeed")]
            public List<ProcessVariables> MarkSpeed { get; set; }

            [JsonPropertyName("nonDepends")]
            public List<ProcessVariables> NonDepends { get; set; }
        }

        public class ProcessVariables
        {
            [JsonPropertyName("curBeamDiameterMicron")]
            public double CurBeamDiameterMicron { get; set; }

            [JsonPropertyName("curPower")]
            public double CurPower { get; set; }

            [JsonPropertyName("jumpDelay")]
            public int JumpDelay { get; set; }

            [JsonPropertyName("jumpMaxLengthLimitMm")]
            public double JumpMaxLengthLimitMm { get; set; }

            [JsonPropertyName("jumpSpeed")]
            public int JumpSpeed { get; set; }

            [JsonPropertyName("laserOffDelay")]
            public double LaserOffDelay { get; set; }

            [JsonPropertyName("laserOffDelayForSkyWriting")]
            public double LaserOffDelayForSkyWriting { get; set; }

            [JsonPropertyName("laserOnDelay")]
            public double LaserOnDelay { get; set; }

            [JsonPropertyName("laserOnDelayForSkyWriting")]
            public double LaserOnDelayForSkyWriting { get; set; }

            [JsonPropertyName("markDelay")]
            public int MarkDelay { get; set; }

            [JsonPropertyName("markSpeed")]
            public int MarkSpeed { get; set; }

            [JsonPropertyName("minJumpDelay")]
            public int MinJumpDelay { get; set; }

            [JsonPropertyName("polygonDelay")]
            public int PolygonDelay { get; set; }

            [JsonPropertyName("swenable")]
            public bool SWEnable { get; set; }

            [JsonPropertyName("umax")]
            public double Umax { get; set; }
        }

        public class ScannerConfig
        {
            [JsonPropertyName("coordinateTypeCode")]
            public int CoordinateTypeCode { get; set; }

            [JsonPropertyName("fieldSizeX")]
            public double FieldSizeX { get; set; }

            [JsonPropertyName("fieldSizeY")]
            public double FieldSizeY { get; set; }

            [JsonPropertyName("offsetX")]
            public double OffsetX { get; set; }

            [JsonPropertyName("offsetY")]
            public double OffsetY { get; set; }

            [JsonPropertyName("offsetZ")]
            public double OffsetZ { get; set; }

            [JsonPropertyName("protocolCode")]
            public int ProtocolCode { get; set; }

            [JsonPropertyName("rotateAngle")]
            public double RotateAngle { get; set; }

            [JsonPropertyName("scaleX")]
            public double ScaleX { get; set; }

            [JsonPropertyName("scaleY")]
            public double ScaleY { get; set; }

            [JsonPropertyName("scaleZ")]
            public double ScaleZ { get; set; }
        }

        public class ThirdAxisConfig
        {
            [JsonPropertyName("afactor")]
            public double AFactor { get; set; }

            [JsonPropertyName("bfactor")]
            public double BFactor { get; set; }

            [JsonPropertyName("cfactor")]
            public double CFactor { get; set; }
        }

        #endregion

        #region CLI Region Data (примеры параметров из CLI)

        public class CliRegion
        {
            public string Name { get; set; }
            public double LaserBeamDiameter { get; set; }  // μm
            public double LaserPower { get; set; }         // W
            public double ScanSpeed { get; set; }          // mm/s
            public List<CliPoint> Geometry { get; set; }
        }

        public class CliPoint
        {
            public double X { get; set; }  // mm
            public double Y { get; set; }  // mm
        }

        #endregion

        // ═══════════════════════════════════════════════════════════════════════
        // ОСНОВНОЙ КЛАСС КОНВЕРТЕРА С ПОЛНЫМИ КОРРЕКЦИЯМИ
        // ═══════════════════════════════════════════════════════════════════════

        public class CliToHansConverterWithFullCorrections
        {
            private ScannerCardConfiguration _config;

            // Калибровочные константы - вычисляются из beamConfig
            private readonly double NOMINAL_DIAMETER_UM;  // μm при Z=0 (из beamConfig.minBeamDiameterMicron)
            private readonly double Z_COEFFICIENT;         // мм/10μm (вычисляется из Rayleigh length)

            public CliToHansConverterWithFullCorrections(ScannerCardConfiguration config)
            {
                _config = config;

                // Берем номинальный диаметр из конфигурации
                NOMINAL_DIAMETER_UM = _config.BeamConfig.MinBeamDiameterMicron;

                // Вычисляем Z_COEFFICIENT из Rayleigh length
                // На расстоянии z_R диаметр увеличивается в √2 раза
                double zRayleighMm = _config.BeamConfig.RayleighLengthMicron / 1000.0;
                double diameterAtRayleigh = NOMINAL_DIAMETER_UM * Math.Sqrt(2);
                double deltaDiameter = diameterAtRayleigh - NOMINAL_DIAMETER_UM;

                // Z = (Δd / 10) × k  =>  k = Z / (Δd / 10)
                Z_COEFFICIENT = zRayleighMm / (deltaDiameter / 10.0);

                Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  КАЛИБРОВОЧНЫЕ ПАРАМЕТРЫ (из beamConfig)                 ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
                Console.WriteLine($"Номинальный диаметр (d₀):     {NOMINAL_DIAMETER_UM:F3} μm");
                Console.WriteLine($"Rayleigh length (z_R):        {_config.BeamConfig.RayleighLengthMicron:F3} μm ({zRayleighMm:F3} мм)");
                Console.WriteLine($"Диаметр на z_R:               {diameterAtRayleigh:F3} μm");
                Console.WriteLine($"Вычисленный zCoefficient:     {Z_COEFFICIENT:F3} мм/10μm");
                Console.WriteLine();
                Console.WriteLine($"ВАЖНО: Это теоретическое значение из Гауссовой оптики.");
                Console.WriteLine($"       Реальное значение может отличаться из-за:");
                Console.WriteLine($"       - Аберраций F-theta линзы");
                Console.WriteLine($"       - Термических эффектов");
                Console.WriteLine($"       Рекомендуется провести калибровку!");
                Console.WriteLine();
            }

            // Геттеры для калибровочных параметров
            public double GetNominalDiameter() => NOMINAL_DIAMETER_UM;
            public double GetZCoefficient() => Z_COEFFICIENT;

            // Метод для переопределения zCoefficient из внешней калибровки
            private double _calibratedZCoeff = double.NaN;
            public void SetCalibratedZCoefficient(double zCoeff)
            {
                _calibratedZCoeff = zCoeff;
                Console.WriteLine($"⚠ Применен калиброванный zCoefficient: {zCoeff:F3} мм/10μm");
                Console.WriteLine($"  (вместо вычисленного: {Z_COEFFICIENT:F3} мм/10μm)");
            }

            private double GetActiveZCoefficient()
            {
                return double.IsNaN(_calibratedZCoeff) ? Z_COEFFICIENT : _calibratedZCoeff;
            }

            // ═══════════════════════════════════════════════════════════════════
            // 1. ВЫБОР ПАРАМЕТРОВ ПРОЦЕССА ПО СКОРОСТИ
            // ═══════════════════════════════════════════════════════════════════

            public ProcessVariables SelectProcessVariables(double cliSpeed)
            {
                ProcessVariables selected = null;
                double minDifference = double.MaxValue;

                foreach (var vars in _config.ProcessVariablesMap.MarkSpeed)
                {
                    double diff = Math.Abs(vars.MarkSpeed - cliSpeed);

                    if (diff < minDifference)
                    {
                        minDifference = diff;
                        selected = vars;
                    }
                }

                Console.WriteLine($"Выбран набор параметров для скорости {selected.MarkSpeed} мм/с");
                Console.WriteLine($"  (запрошено: {cliSpeed} мм/с, разница: {minDifference} мм/с)");

                return selected;
            }

            // ═══════════════════════════════════════════════════════════════════
            // 2. РАСЧЕТ Z-OFFSET ИЗ ДИАМЕТРА ПУЧКА
            // ═══════════════════════════════════════════════════════════════════

            public float CalculateZFromDiameter(double cliDiameter)
            {
                double activeZCoeff = GetActiveZCoefficient();

                // Формула: Z = (diameter - nominalDiameter) / 10.0 × coefficient
                float z = (float)((cliDiameter - NOMINAL_DIAMETER_UM) / 10.0 * activeZCoeff);

                Console.WriteLine($"Расчет Z из диаметра:");
                Console.WriteLine($"  Диаметр CLI: {cliDiameter} μm");
                Console.WriteLine($"  Номинальный диаметр: {NOMINAL_DIAMETER_UM:F3} μm");
                Console.WriteLine($"  Коэффициент: {activeZCoeff:F3} мм/10μm");
                Console.WriteLine($"  Z-offset: {z:F3} мм");

                return z;
            }

            // Обратный расчет: Z → диаметр
            public double CalculateDiameterFromZ(float z)
            {
                double activeZCoeff = GetActiveZCoefficient();
                return NOMINAL_DIAMETER_UM + (z / activeZCoeff * 10.0);
            }

            // Расчет реального диаметра с учетом оптики (формула Гауссова луча)
            public double CalculateRealDiameter(double zOffsetMm)
            {
                double d0 = _config.BeamConfig.MinBeamDiameterMicron;
                double zR = _config.BeamConfig.RayleighLengthMicron;
                double zOffsetUm = zOffsetMm * 1000.0;

                // d(z) = d₀ × sqrt(1 + (z / z_R)²)
                double diameter = d0 * Math.Sqrt(1 + Math.Pow(zOffsetUm / zR, 2));

                return diameter;
            }

            // ═══════════════════════════════════════════════════════════════════
            // 3. КОРРЕКЦИЯ КРИВИЗНЫ ПОЛЯ (Third Axis Config)
            // ═══════════════════════════════════════════════════════════════════

            public float ApplyFieldCurvatureCorrection(double x, double y)
            {
                if (!_config.FunctionSwitcherConfig.EnableZCorrection)
                {
                    return 0.0f; // Коррекция выключена
                }

                // Расстояние от центра поля
                double r = Math.Sqrt(x * x + y * y);

                // Z_correction = A×r² + B×r + C
                double A = _config.ThirdAxisConfig.AFactor;
                double B = _config.ThirdAxisConfig.BFactor;
                double C = _config.ThirdAxisConfig.CFactor;

                double zCorr = A * r * r + B * r + C;

                return (float)zCorr;
            }

            // ═══════════════════════════════════════════════════════════════════
            // 4. КОРРЕКЦИЯ МОЩНОСТИ ЛАЗЕРА
            // ═══════════════════════════════════════════════════════════════════

            public double CorrectLaserPower(double requestedPower)
            {
                double power = requestedPower;

                // ШАГ 1: Интерполяция по таблице коррекции
                if (_config.FunctionSwitcherConfig.EnablePowerCorrection)
                {
                    power = InterpolatePowerCorrection(power);
                }

                // ШАГ 2: Применение смещения мощности
                if (_config.FunctionSwitcherConfig.EnablePowerOffset)
                {
                    double kFactor = _config.LaserPowerConfig.PowerOffsetKFactor;
                    double cFactor = _config.LaserPowerConfig.PowerOffsetCFactor;
                    double offset = kFactor * power + cFactor;
                    power += offset;
                }

                // ШАГ 3: Ограничение
                power = Math.Max(0, Math.Min(power, _config.LaserPowerConfig.MaxPower));

                return power;
            }

            private double InterpolatePowerCorrection(double requestedPower)
            {
                double maxPower = _config.LaserPowerConfig.MaxPower;
                var table = _config.LaserPowerConfig.ActualPowerCorrectionValue;

                // Нормализация (0.0 - 1.0)
                double normalized = requestedPower / maxPower;

                // Индекс в таблице (6 точек: 0%, 20%, 40%, 60%, 80%, 100%)
                double index = normalized * (table.Count - 1);
                int lowerIdx = (int)Math.Floor(index);
                int upperIdx = Math.Min((int)Math.Ceiling(index), table.Count - 1);
                double fraction = index - lowerIdx;

                // Линейная интерполяция
                double lowerValue = table[lowerIdx];
                double upperValue = table[upperIdx];
                double correctedPower = lowerValue + (upperValue - lowerValue) * fraction;

                return correctedPower;
            }

            // ═══════════════════════════════════════════════════════════════════
            // 5. ТРАНСФОРМАЦИЯ КООРДИНАТ
            // ═══════════════════════════════════════════════════════════════════

            public (float x, float y, float z) TransformCoordinates(
                double cliX, double cliY, float zFromDiameter)
            {
                var sc = _config.ScannerConfig;

                // ШАГ 1: Масштабирование
                double scaledX = cliX * sc.ScaleX;
                double scaledY = cliY * sc.ScaleY;

                // ШАГ 2: Поворот
                double angleRad = sc.RotateAngle * Math.PI / 180.0;
                double rotatedX = scaledX * Math.Cos(angleRad) - scaledY * Math.Sin(angleRad);
                double rotatedY = scaledX * Math.Sin(angleRad) + scaledY * Math.Cos(angleRad);

                // ШАГ 3: Смещения XY
                double finalX = rotatedX + sc.OffsetX;
                double finalY = rotatedY + sc.OffsetY;

                // ШАГ 4: Коррекция кривизны поля
                float zFieldCorr = ApplyFieldCurvatureCorrection(finalX, finalY);

                // ШАГ 5: Финальная Z
                float finalZ = zFromDiameter + zFieldCorr + (float)sc.OffsetZ;

                return ((float)finalX, (float)finalY, finalZ);
            }

            // ═══════════════════════════════════════════════════════════════════
            // 6. КОНВЕРТАЦИЯ CLI РЕГИОНА В HANS С ПОЛНЫМИ КОРРЕКЦИЯМИ
            // ═══════════════════════════════════════════════════════════════════

            public void ConvertRegionToHans(CliRegion region, int layerIndex)
            {
                Console.WriteLine($"\n╔═══════════════════════════════════════════════════════════╗");
                Console.WriteLine($"║  КОНВЕРТАЦИЯ РЕГИОНА: {region.Name,-37} ║");
                Console.WriteLine($"╚═══════════════════════════════════════════════════════════╝\n");

                // ═══════════════════════════════════════════════════════════════
                // ЭТАП 1: Выбор параметров процесса
                // ═══════════════════════════════════════════════════════════════

                ProcessVariables processVars = SelectProcessVariables(region.ScanSpeed);
                Console.WriteLine();

                // ═══════════════════════════════════════════════════════════════
                // ЭТАП 2: Установка параметров слоя
                // ═══════════════════════════════════════════════════════════════

                MarkParameter markParams = new MarkParameter
                {
                    // Скорости
                    MarkSpeed = (uint)processVars.MarkSpeed,
                    JumpSpeed = (uint)processVars.JumpSpeed,

                    // Задержки (наносекунды)
                    PolygonDelay = (uint)processVars.PolygonDelay,
                    JumpDelay = (uint)processVars.JumpDelay,
                    MarkDelay = (uint)processVars.MarkDelay,
                    LaserOnDelay = (float)processVars.LaserOnDelay,
                    LaserOffDelay = (float)processVars.LaserOffDelay,
                    LaserOnDelayForSkyWriting = (float)processVars.LaserOnDelayForSkyWriting,
                    LaserOffDelayForSkyWriting = (float)processVars.LaserOffDelayForSkyWriting,
                    MinJumpDelay = (uint)processVars.MinJumpDelay,

                    // Ограничения
                    JumpMaxLengthLimit = (float)processVars.JumpMaxLengthLimitMm,

                    // Режимы
                    SkyWritingEnable = processVars.SWEnable,
                    Umax = (float)processVars.Umax,

                    // Мощность (с коррекцией!)
                    LaserPower = 0.0f // Установим ниже
                };

                // Коррекция мощности
                double correctedPower = CorrectLaserPower(region.LaserPower);
                markParams.LaserPower = (float)(correctedPower / _config.LaserPowerConfig.MaxPower * 100.0);

                Console.WriteLine($"Коррекция мощности:");
                Console.WriteLine($"  CLI мощность: {region.LaserPower} Вт");
                Console.WriteLine($"  Скорректированная: {correctedPower:F2} Вт");
                Console.WriteLine($"  Процент для Hans: {markParams.LaserPower:F2}%");
                Console.WriteLine();

                // Установка параметров в Hans
                HM_UDM_DLL.UDM_SetLayersPara(new[] { markParams }, 1);

                // ═══════════════════════════════════════════════════════════════
                // ЭТАП 3: Обработка геометрии
                // ═══════════════════════════════════════════════════════════════

                Console.WriteLine($"Обработка геометрии ({region.Geometry.Count} точек):");

                // Расчет Z из диаметра
                float zFromDiameter = 0.0f;
                if (_config.FunctionSwitcherConfig.EnableDiameterChange)
                {
                    zFromDiameter = CalculateZFromDiameter(region.LaserBeamDiameter);
                }
                Console.WriteLine();

                // Преобразование точек
                structUdmPos[] hansPoints = new structUdmPos[region.Geometry.Count];

                for (int i = 0; i < region.Geometry.Count; i++)
                {
                    var cliPoint = region.Geometry[i];

                    // Полная трансформация с коррекциями
                    var (x, y, z) = TransformCoordinates(
                        cliPoint.X,
                        cliPoint.Y,
                        zFromDiameter
                    );

                    hansPoints[i] = new structUdmPos
                    {
                        x = x,
                        y = y,
                        z = z
                    };
                }

                // Вывод примера трансформации первой точки
                if (region.Geometry.Count > 0)
                {
                    var firstPoint = region.Geometry[0];
                    var firstHans = hansPoints[0];

                    Console.WriteLine($"Пример трансформации точки:");
                    Console.WriteLine($"  CLI: ({firstPoint.X:F3}, {firstPoint.Y:F3})");
                    Console.WriteLine($"  Hans: ({firstHans.x:F3}, {firstHans.y:F3}, {firstHans.z:F3})");

                    // Расчет коррекции кривизны для этой точки
                    double r = Math.Sqrt(firstHans.x * firstHans.x + firstHans.y * firstHans.y);
                    float fieldCorr = ApplyFieldCurvatureCorrection(firstHans.x, firstHans.y);
                    Console.WriteLine($"  Расстояние от центра: {r:F2} мм");
                    Console.WriteLine($"  Коррекция кривизны: {fieldCorr:F3} мм");
                    Console.WriteLine($"  Z (диаметр): {zFromDiameter:F3} мм");
                    Console.WriteLine($"  Z (итого): {firstHans.z:F3} мм");
                }
                Console.WriteLine();

                // Добавление геометрии в Hans
                HM_UDM_DLL.UDM_AddPolyline3D(hansPoints, hansPoints.Length, layerIndex);

                Console.WriteLine($"✓ Регион '{region.Name}' успешно конвертирован!");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ПРИМЕРЫ ИСПОЛЬЗОВАНИЯ
        // ═══════════════════════════════════════════════════════════════════════

        public static void Example1_SingleRegion()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("  ПРИМЕР 1: Конвертация одного региона CLI");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            // Загружаем ВАШУ реальную конфигурацию
            string configJson = @"
{
    ""cardInfo"": {
        ""ipAddress"": ""172.18.34.227"",
        ""seqIndex"": 0
    },
    ""beamConfig"": {
        ""minBeamDiameterMicron"": 48.141,
        ""wavelengthNano"": 1070.0,
        ""rayleighLengthMicron"": 1426.715,
        ""m2"": 1.127,
        ""focalLengthMm"": 538.46
    },
    ""processVariablesMap"": {
        ""markSpeed"": [
            {
                ""markSpeed"": 800,
                ""jumpSpeed"": 25000,
                ""polygonDelay"": 385,
                ""jumpDelay"": 40000,
                ""markDelay"": 470,
                ""laserOnDelay"": 420.0,
                ""laserOffDelay"": 490.0,
                ""laserOnDelayForSkyWriting"": 600.0,
                ""laserOffDelayForSkyWriting"": 730.0,
                ""curBeamDiameterMicron"": 65.0,
                ""curPower"": 50.0,
                ""jumpMaxLengthLimitMm"": 400.0,
                ""minJumpDelay"": 400,
                ""swenable"": true,
                ""umax"": 0.1
            },
            {
                ""markSpeed"": 1250,
                ""jumpSpeed"": 25000,
                ""polygonDelay"": 465,
                ""jumpDelay"": 40000,
                ""markDelay"": 496,
                ""laserOnDelay"": 375.0,
                ""laserOffDelay"": 500.0,
                ""laserOnDelayForSkyWriting"": 615.0,
                ""laserOffDelayForSkyWriting"": 725.0,
                ""curBeamDiameterMicron"": 65.0,
                ""curPower"": 50.0,
                ""jumpMaxLengthLimitMm"": 400.0,
                ""minJumpDelay"": 400,
                ""swenable"": true,
                ""umax"": 0.1
            },
            {
                ""markSpeed"": 2000,
                ""jumpSpeed"": 25000,
                ""polygonDelay"": 600,
                ""jumpDelay"": 40000,
                ""markDelay"": 540,
                ""laserOnDelay"": 330.0,
                ""laserOffDelay"": 530.0,
                ""laserOnDelayForSkyWriting"": 630.0,
                ""laserOffDelayForSkyWriting"": 720.0,
                ""curBeamDiameterMicron"": 65.0,
                ""curPower"": 50.0,
                ""jumpMaxLengthLimitMm"": 400.0,
                ""minJumpDelay"": 400,
                ""swenable"": true,
                ""umax"": 0.1
            }
        ],
        ""nonDepends"": []
    },
    ""scannerConfig"": {
        ""fieldSizeX"": 400.0,
        ""fieldSizeY"": 400.0,
        ""protocolCode"": 1,
        ""coordinateTypeCode"": 5,
        ""offsetX"": 0.0,
        ""offsetY"": 105.03,
        ""offsetZ"": -0.001,
        ""scaleX"": 1.0,
        ""scaleY"": 1.0,
        ""scaleZ"": 1.0,
        ""rotateAngle"": 0.0
    },
    ""laserPowerConfig"": {
        ""maxPower"": 500.0,
        ""actualPowerCorrectionValue"": [0.0, 67.0, 176.0, 281.0, 382.0, 475.0],
        ""powerOffsetKFactor"": -0.6839859,
        ""powerOffsetCFactor"": 51.298943
    },
    ""functionSwitcherConfig"": {
        ""enablePowerOffset"": true,
        ""enablePowerCorrection"": true,
        ""enableZCorrection"": true,
        ""enableDiameterChange"": true,
        ""enableDynamicChangeVariables"": true,
        ""limitVariablesMinPoint"": true,
        ""limitVariablesMaxPoint"": true,
        ""enableVariableJumpDelay"": true
    },
    ""thirdAxisConfig"": {
        ""bfactor"": 0.013944261,
        ""cfactor"": -7.5056114,
        ""afactor"": 0.0
    }
}";

            var config = JsonSerializer.Deserialize<ScannerCardConfiguration>(configJson);

            // Создаем конвертер
            var converter = new CliToHansConverterWithFullCorrections(config);

            // Создаем пример CLI региона (downskin_hatch)
            var downskinRegion = new CliRegion
            {
                Name = "downskin_hatch",
                LaserBeamDiameter = 80.0,  // μm
                LaserPower = 280.0,        // W
                ScanSpeed = 800.0,         // mm/s
                Geometry = new List<CliPoint>
                {
                    new CliPoint { X = -50, Y = -50 },
                    new CliPoint { X = 50, Y = -50 },
                    new CliPoint { X = 50, Y = 50 },
                    new CliPoint { X = -50, Y = 50 },
                    new CliPoint { X = -50, Y = -50 }
                }
            };

            // Инициализация Hans
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1); // 3D режим!

            // Конвертация с полными коррекциями
            converter.ConvertRegionToHans(downskinRegion, 0);

            // Генерация и сохранение
            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("downskin_with_corrections.bin");
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine("\n✓ Файл сохранен: downskin_with_corrections.bin");
        }

        public static void Example2_AllCliRegions()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("  ПРИМЕР 2: Все регионы из CLI (типичный слайсер)");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            // Загружаем конфигурацию
            string configJson = System.IO.File.ReadAllText("scanner_config.json");
            var config = JsonSerializer.Deserialize<ScannerCardConfiguration>(configJson);

            var converter = new CliToHansConverterWithFullCorrections(config);

            // Типичные регионы из слайсера Materialise Build Processor
            var cliRegions = new List<CliRegion>
            {
                new CliRegion
                {
                    Name = "edges",
                    LaserBeamDiameter = 65.0,   // μm
                    LaserPower = 240.0,         // W
                    ScanSpeed = 500.0,          // mm/s
                    Geometry = GenerateEdgeGeometry()
                },
                new CliRegion
                {
                    Name = "upskin_contour",
                    LaserBeamDiameter = 70.0,
                    LaserPower = 250.0,
                    ScanSpeed = 600.0,
                    Geometry = GenerateContourGeometry()
                },
                new CliRegion
                {
                    Name = "downskin_hatch",
                    LaserBeamDiameter = 80.0,
                    LaserPower = 280.0,
                    ScanSpeed = 800.0,
                    Geometry = GenerateHatchGeometry()
                },
                new CliRegion
                {
                    Name = "infill_hatch",
                    LaserBeamDiameter = 90.0,
                    LaserPower = 350.0,
                    ScanSpeed = 1400.0,
                    Geometry = GenerateInfillGeometry()
                },
                new CliRegion
                {
                    Name = "support_hatch",
                    LaserBeamDiameter = 100.0,
                    LaserPower = 320.0,
                    ScanSpeed = 1600.0,
                    Geometry = GenerateSupportGeometry()
                }
            };

            // Инициализация Hans
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Конвертация всех регионов
            for (int i = 0; i < cliRegions.Count; i++)
            {
                converter.ConvertRegionToHans(cliRegions[i], i);
            }

            // Генерация и сохранение
            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("complete_layer_all_regions.bin");
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine("\n✓ Все регионы успешно конвертированы!");
            Console.WriteLine("✓ Файл сохранен: complete_layer_all_regions.bin");
        }

        public static void Example3_CompareWithWithoutCorrections()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("  ПРИМЕР 3: Сравнение С и БЕЗ коррекций");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            string configJson = System.IO.File.ReadAllText("scanner_config.json");
            var config = JsonSerializer.Deserialize<ScannerCardConfiguration>(configJson);

            var converter = new CliToHansConverterWithFullCorrections(config);

            // Тестовая точка на краю поля
            double testX = 150.0;
            double testY = 150.0;
            double testDiameter = 80.0;
            double testPower = 280.0;

            Console.WriteLine($"Тестовая точка: ({testX}, {testY})");
            Console.WriteLine($"CLI диаметр: {testDiameter} μm");
            Console.WriteLine($"CLI мощность: {testPower} W");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════════════
            // БЕЗ КОРРЕКЦИЙ
            // ═══════════════════════════════════════════════════════════════

            Console.WriteLine("┌─────────────────────────────────────────────────────────┐");
            Console.WriteLine("│  БЕЗ КОРРЕКЦИЙ (простая конвертация)                   │");
            Console.WriteLine("└─────────────────────────────────────────────────────────┘");

            float zSimple = converter.CalculateZFromDiameter(testDiameter);
            float xSimple = (float)testX;
            float ySimple = (float)testY;
            float powerSimple = (float)(testPower / 500.0 * 100.0);

            Console.WriteLine($"  X: {xSimple:F3} мм (без трансформации)");
            Console.WriteLine($"  Y: {ySimple:F3} мм (без трансформации)");
            Console.WriteLine($"  Z: {zSimple:F3} мм (только диаметр)");
            Console.WriteLine($"  Мощность: {powerSimple:F2}% (простая конвертация)");
            Console.WriteLine();

            // ═══════════════════════════════════════════════════════════════
            // С ПОЛНЫМИ КОРРЕКЦИЯМИ
            // ═══════════════════════════════════════════════════════════════

            Console.WriteLine("┌─────────────────────────────────────────────────────────┐");
            Console.WriteLine("│  С ПОЛНЫМИ КОРРЕКЦИЯМИ                                  │");
            Console.WriteLine("└─────────────────────────────────────────────────────────┘");

            var (xCorrected, yCorrected, zCorrected) = converter.TransformCoordinates(
                testX, testY, zSimple
            );

            double powerCorrected = converter.CorrectLaserPower(testPower);
            float powerCorrectedPercent = (float)(powerCorrected / 500.0 * 100.0);

            Console.WriteLine($"  X: {xCorrected:F3} мм (с offsetX и scaleX)");
            Console.WriteLine($"  Y: {yCorrected:F3} мм (с offsetY и scaleY)");
            Console.WriteLine($"  Z: {zCorrected:F3} мм (диаметр + кривизна + offsetZ)");
            Console.WriteLine($"  Мощность: {powerCorrectedPercent:F2}% (с таблицей + offset)");
            Console.WriteLine();

            // Расчет разницы
            double r = Math.Sqrt(testX * testX + testY * testY);
            float fieldCorr = converter.ApplyFieldCurvatureCorrection(testX, testY);

            Console.WriteLine("┌─────────────────────────────────────────────────────────┐");
            Console.WriteLine("│  РАЗНИЦА (коррекции)                                    │");
            Console.WriteLine("└─────────────────────────────────────────────────────────┘");
            Console.WriteLine($"  ΔX: {xCorrected - xSimple:F3} мм");
            Console.WriteLine($"  ΔY: {yCorrected - ySimple:F3} мм (offsetY = 105.03 мм)");
            Console.WriteLine($"  ΔZ: {zCorrected - zSimple:F3} мм (коррекция кривизны)");
            Console.WriteLine($"  Расстояние от центра: {r:F2} мм");
            Console.WriteLine($"  Коррекция кривизны поля: {fieldCorr:F3} мм");
            Console.WriteLine($"  ΔПower: {powerCorrected - testPower:F2} W");
            Console.WriteLine($"  ΔPower %: {powerCorrectedPercent - powerSimple:F2}%");
            Console.WriteLine();

            Console.WriteLine("✓ Коррекции критически важны для точности!");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ГЕНЕРАЦИИ ГЕОМЕТРИИ
        // ═══════════════════════════════════════════════════════════════════════

        private static List<CliPoint> GenerateEdgeGeometry()
        {
            // Простой квадратный контур
            return new List<CliPoint>
            {
                new CliPoint { X = -40, Y = -40 },
                new CliPoint { X = 40, Y = -40 },
                new CliPoint { X = 40, Y = 40 },
                new CliPoint { X = -40, Y = 40 },
                new CliPoint { X = -40, Y = -40 }
            };
        }

        private static List<CliPoint> GenerateContourGeometry()
        {
            // Внутренний контур
            return new List<CliPoint>
            {
                new CliPoint { X = -35, Y = -35 },
                new CliPoint { X = 35, Y = -35 },
                new CliPoint { X = 35, Y = 35 },
                new CliPoint { X = -35, Y = 35 },
                new CliPoint { X = -35, Y = -35 }
            };
        }

        private static List<CliPoint> GenerateHatchGeometry()
        {
            // Штриховка (вертикальные линии)
            var points = new List<CliPoint>();
            for (double x = -30; x <= 30; x += 5)
            {
                points.Add(new CliPoint { X = x, Y = -30 });
                points.Add(new CliPoint { X = x, Y = 30 });
            }
            return points;
        }

        private static List<CliPoint> GenerateInfillGeometry()
        {
            // Заполнение (горизонтальные линии)
            var points = new List<CliPoint>();
            for (double y = -25; y <= 25; y += 5)
            {
                points.Add(new CliPoint { X = -25, Y = y });
                points.Add(new CliPoint { X = 25, Y = y });
            }
            return points;
        }

        private static List<CliPoint> GenerateSupportGeometry()
        {
            // Поддержки (редкая сетка)
            var points = new List<CliPoint>();
            for (double x = -20; x <= 20; x += 10)
            {
                for (double y = -20; y <= 20; y += 10)
                {
                    points.Add(new CliPoint { X = x, Y = y });
                    points.Add(new CliPoint { X = x + 8, Y = y });
                }
            }
            return points;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ПРИМЕР 4: ИСПОЛЬЗОВАНИЕ КАЛИБРОВАННОГО zCoefficient
        // ═══════════════════════════════════════════════════════════════════════

        public static void Example4_WithCalibratedZCoefficient()
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ПРИМЕР 4: Калиброванный zCoefficient                    ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

            // Загрузка конфигурации
            string configJson = GetExampleConfigJson();
            var configs = JsonSerializer.Deserialize<List<ScannerCardConfiguration>>(configJson);
            var config = configs[0]; // Карта 0

            // Создаем конвертер (вычисляется теоретический zCoeff из Rayleigh)
            var converter = new CliToHansConverterWithFullCorrections(config);

            Console.WriteLine($"Вычисленный zCoefficient: {converter.GetZCoefficient():F3} мм/10μm");
            Console.WriteLine();

            // СИТУАЦИЯ: Вы провели калибровку и определили реальный zCoefficient
            // Например, напечатали тестовые линии и измерили под микроскопом

            Console.WriteLine("═══ РЕЗУЛЬТАТЫ КАЛИБРОВКИ ═══");
            Console.WriteLine("Вы напечатали тестовые линии:");
            Console.WriteLine("  Z = -1.2 мм  →  Измеренный диаметр: 80 μm");
            Console.WriteLine("  Z = +1.2 мм  →  Измеренный диаметр: 120 μm");
            Console.WriteLine();
            Console.WriteLine("Расчет калиброванного zCoefficient:");
            Console.WriteLine("  ΔZ = 2.4 мм");
            Console.WriteLine("  Δd = 40 μm");
            Console.WriteLine("  zCoeff = ΔZ / (Δd/10) = 2.4 / 4.0 = 0.6 мм/10μm");
            Console.WriteLine();

            double calibratedZCoeff = 0.6;  // Из вашей калибровки!

            // Применяем калиброванное значение
            converter.SetCalibratedZCoefficient(calibratedZCoeff);
            Console.WriteLine();

            // Теперь используем конвертер с калиброванным значением
            Console.WriteLine("═══ КОНВЕРТАЦИЯ С КАЛИБРОВАННЫМ zCoefficient ═══");

            double cliDiameter = 80.0; // μm из CLI

            // Вычисляем Z с калиброванным коэффициентом
            float zOffset = converter.CalculateZFromDiameter(cliDiameter);

            Console.WriteLine();
            Console.WriteLine($"✓ Для диаметра {cliDiameter} μm вычислен Z = {zOffset:F3} мм");
            Console.WriteLine($"  Это ТОЧНОЕ значение из вашей калибровки!");

            Console.WriteLine("\n═══ СРАВНЕНИЕ: Теоретический vs Калиброванный ═══");

            // Пересоздаем конвертер для сравнения
            var converterTheoretical = new CliToHansConverterWithFullCorrections(config);

            double[] testDiameters = { 40, 60, 80, 100, 120, 140 };

            Console.WriteLine("┌──────────────┬─────────────────┬────────────────────┬──────────────┐");
            Console.WriteLine("│ Диаметр (μm) │ Z теор. (мм)    │ Z калибр. (мм)     │ Разница (мм) │");
            Console.WriteLine("├──────────────┼─────────────────┼────────────────────┼──────────────┤");

            foreach (double d in testDiameters)
            {
                float zTheoretical = converterTheoretical.CalculateZFromDiameter(d);

                converter.SetCalibratedZCoefficient(calibratedZCoeff);
                float zCalibrated = converter.CalculateZFromDiameter(d);

                double difference = zCalibrated - zTheoretical;

                Console.WriteLine($"│ {d,12:F1} │ {zTheoretical,15:+0.000;-0.000;0.000} │ {zCalibrated,18:+0.000;-0.000;0.000} │ {difference,12:+0.000;-0.000;0.000} │");
            }

            Console.WriteLine("└──────────────┴─────────────────┴────────────────────┴──────────────┘");

            Console.WriteLine("\n💡 ВЫВОД:");
            Console.WriteLine("  Калиброванное значение учитывает реальные аберрации оптики");
            Console.WriteLine("  и дает ТОЧНЫЕ результаты для вашей конкретной системы!");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ГЛАВНАЯ ФУНКЦИЯ
        // ═══════════════════════════════════════════════════════════════════════

        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ПОЛНЫЕ ПРИМЕРЫ ИСПОЛЬЗОВАНИЯ JSON КОНФИГУРАЦИИ          ║");
            Console.WriteLine("║  Hans Scanner с ВСЕМИ коррекциями                        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");

            // Запуск примеров
            try
            {
                // ПРИМЕР 1: Один регион
                Example1_SingleRegion();

                Console.WriteLine("\n\nНажмите Enter для следующего примера...");
                Console.ReadLine();

                // ПРИМЕР 2: Все регионы CLI
                Example2_AllCliRegions();

                Console.WriteLine("\n\nНажмите Enter для следующего примера...");
                Console.ReadLine();

                // ПРИМЕР 3: Сравнение
                Example3_CompareWithWithoutCorrections();

                Console.WriteLine("\n\nНажмите Enter для следующего примера...");
                Console.ReadLine();

                // ПРИМЕР 4: Калиброванный zCoefficient
                Example4_WithCalibratedZCoefficient();

                Console.WriteLine("\n\n╔═══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  ✓ ВСЕ ПРИМЕРЫ УСПЕШНО ВЫПОЛНЕНЫ!                       ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✗ Ошибка: {ex.Message}");
                Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                Console.ResetColor();
            }

            Console.WriteLine("\n\nНажмите Enter для выхода...");
            Console.ReadLine();
        }
    }
}
