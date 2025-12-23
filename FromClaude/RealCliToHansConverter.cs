using System;
using System.Collections.Generic;
using System.Text.Json;
using Hans.NET;

namespace PrintMateMC.ScannerConfig
{
    /// <summary>
    /// РЕАЛЬНАЯ конвертация CLI → Hans на основе JAVA кода из PrintMateMC
    ///
    /// Этот код основан на ФАКТИЧЕСКОЙ реализации из:
    /// - src/jobparser/JobBuilder.java - парсинг _laser_beam_diameter
    /// - src/jobparser/JobParameter.java - создание DiameterOperation
    /// - libs/Scanner/Hans/Hans4Java - библиотека DiameterOperation
    ///
    /// В Java версии используется DiameterOperation из Hans4Java, которая
    /// ВНУТРИ конвертирует diameter в Z. В C# мы делаем это вручную.
    /// </summary>
    public class RealCliToHansConverter
    {
        // ═══════════════════════════════════════════════════════════════════════
        // КАЛИБРОВОЧНЫЕ КОНСТАНТЫ
        // Эти значения должны быть откалиброваны для вашей оптики!
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Номинальный диаметр пучка при Z=0 (микроны)
        ///
        /// В Java коде это захардкожено в Hans4Java библиотеке.
        /// Для вашей системы используйте beamConfig.minBeamDiameterMicron
        /// или калиброванное значение.
        ///
        /// Из вашего JSON: 48.141 μm (карта 0), 53.872 μm (карта 1)
        /// </summary>
        private readonly double NOMINAL_DIAMETER_UM;

        /// <summary>
        /// Коэффициент конвертации Z → diameter (мм на 10 микрон)
        ///
        /// Формула: Z = (diameter - nominalDiameter) / 10.0 × zCoefficient
        ///
        /// В Java коде это также захардкожено в Hans4Java.
        /// Вычисляется из beamConfig.rayleighLengthMicron или из калибровки.
        ///
        /// Из вашего JSON (вычисленное): 0.343 (карта 0), 0.389 (карта 1)
        /// </summary>
        private readonly double Z_COEFFICIENT;

        private readonly ScannerCardConfiguration _config;

        // ═══════════════════════════════════════════════════════════════════════
        // КОНСТРУКТОР
        // ═══════════════════════════════════════════════════════════════════════

        public RealCliToHansConverter(
            ScannerCardConfiguration config,
            double? nominalDiameterOverride = null,
            double? zCoefficientOverride = null)
        {
            _config = config;

            // Берем значения из beamConfig или используем переопределенные
            NOMINAL_DIAMETER_UM = nominalDiameterOverride ??
                                  config.BeamConfig.MinBeamDiameterMicron;

            // Вычисляем или используем переопределенный zCoefficient
            if (zCoefficientOverride.HasValue)
            {
                Z_COEFFICIENT = zCoefficientOverride.Value;
            }
            else
            {
                // Вычисляем из Rayleigh length (как в моих предыдущих примерах)
                double zRayleighMm = config.BeamConfig.RayleighLengthMicron / 1000.0;
                double diameterAtRayleigh = NOMINAL_DIAMETER_UM * Math.Sqrt(2);
                double deltaDiameter = diameterAtRayleigh - NOMINAL_DIAMETER_UM;
                Z_COEFFICIENT = zRayleighMm / (deltaDiameter / 10.0);
            }

            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  REAL CLI → Hans Converter (на основе Java кода)         ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine($"Номинальный диаметр: {NOMINAL_DIAMETER_UM:F3} μm");
            Console.WriteLine($"Z коэффициент:       {Z_COEFFICIENT:F3} мм/10μm");
            Console.WriteLine();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CORE: Конвертация diameter → Z (как в DiameterOperation)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Конвертирует diameter из CLI в Z-offset для Hans scanner
        ///
        /// Это C# реализация логики из Hans4Java DiameterOperation.
        ///
        /// Формула: Z = (diameter - nominalDiameter) / 10.0 × zCoefficient
        ///
        /// Пример:
        ///   diameter = 80 μm, nominal = 48.141 μm, coeff = 0.343
        ///   Z = (80 - 48.141) / 10 × 0.343 = 1.093 мм
        /// </summary>
        public float CalculateZFromDiameter(double diameterMicrons)
        {
            double z = (diameterMicrons - NOMINAL_DIAMETER_UM) / 10.0 * Z_COEFFICIENT;
            return (float)z;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CLI REGION → Hans Layer (точно как в Java CommandManager)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Конвертирует CLI регион в Hans layer
        ///
        /// Эквивалент Java кода:
        ///   JobParameter param = laser.getParameter(region, LASER_PARAM.FOCUS);
        ///   IOperation op = param.getScanOperation(); // → DiameterOperation
        ///   scannator.addOperation(op);
        /// </summary>
        public void ConvertCliRegionToHans(
            string regionName,
            double diameterMicrons,
            double powerWatts,
            double speedMmPerSec,
            List<(double x, double y)> geometry,
            int layerIndex)
        {
            Console.WriteLine($"\n━━━ Конвертация региона: {regionName} ━━━");

            // 1. DIAMETER → Z (как DiameterOperation)
            float z = CalculateZFromDiameter(diameterMicrons);

            Console.WriteLine($"  Diameter:  {diameterMicrons:F1} μm");
            Console.WriteLine($"  Z-offset:  {z:F3} мм");
            Console.WriteLine($"  Power:     {powerWatts:F1} W");
            Console.WriteLine($"  Speed:     {speedMmPerSec:F0} mm/s");

            // 2. POWER коррекция (если включена)
            double correctedPower = powerWatts;
            if (_config.FunctionSwitcherConfig.EnablePowerCorrection)
            {
                correctedPower = CorrectLaserPower(powerWatts);
                Console.WriteLine($"  Power (скорректированная): {correctedPower:F1} W");
            }

            // 3. Найти ProcessVariables для этой скорости
            var processVars = SelectProcessVariables(speedMmPerSec);

            // 4. Создать MarkParameter для Hans
            MarkParameter param = new MarkParameter
            {
                MarkSpeed = (uint)speedMmPerSec,
                JumpSpeed = (uint)processVars.JumpSpeed,
                PolygonDelay = (uint)processVars.PolygonDelay,
                JumpDelay = (uint)processVars.JumpDelay,
                MarkDelay = (uint)processVars.MarkDelay,
                LaserOnDelay = (float)processVars.LaserOnDelay,
                LaserOffDelay = (float)processVars.LaserOffDelay,

                // Конвертируем мощность из Ватт в проценты
                LaserPower = (float)(correctedPower / _config.LaserPowerConfig.MaxPower * 100.0),

                Frequency = 30.0f,  // Можно взять из processVars если есть
                DutyCycle = 0.5f
            };

            HM_UDM_DLL.UDM_SetLayersPara(new[] { param }, 1);

            // 5. Добавить геометрию с Z-offset
            Console.WriteLine($"  Геометрия: {geometry.Count} точек");

            structUdmPos[] points = new structUdmPos[geometry.Count];
            for (int i = 0; i < geometry.Count; i++)
            {
                // Применяем координатные трансформации
                var (transX, transY, transZ) = TransformCoordinates(
                    geometry[i].x,
                    geometry[i].y,
                    z);

                points[i] = new structUdmPos
                {
                    x = (float)transX,
                    y = (float)transY,
                    z = (float)transZ  // ← ВОТ ОН! Z-offset для diameter!
                };
            }

            // 6. Отправить в Hans (как в Java: scannator.addOperation())
            HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, layerIndex);

            Console.WriteLine($"  ✓ Регион отправлен в Hans scanner");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════════

        private ProcessVariables SelectProcessVariables(double cliSpeed)
        {
            ProcessVariables selected = null;
            double minDiff = double.MaxValue;

            foreach (var vars in _config.ProcessVariablesMap.MarkSpeed)
            {
                double diff = Math.Abs(vars.MarkSpeed - cliSpeed);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    selected = vars;
                }
            }

            return selected ?? _config.ProcessVariablesMap.MarkSpeed[0];
        }

        private double CorrectLaserPower(double requestedPower)
        {
            var table = _config.LaserPowerConfig.ActualPowerCorrectionValue;
            double maxPower = _config.LaserPowerConfig.MaxPower;

            // Линейная интерполяция
            double normalizedPower = requestedPower / maxPower;
            int steps = table.Count - 1;
            double stepSize = 1.0 / steps;

            for (int i = 0; i < steps; i++)
            {
                double lower = i * stepSize;
                double upper = (i + 1) * stepSize;

                if (normalizedPower >= lower && normalizedPower <= upper)
                {
                    double t = (normalizedPower - lower) / stepSize;
                    double interpolated = table[i] + t * (table[i + 1] - table[i]);

                    // Применяем offset
                    if (_config.FunctionSwitcherConfig.EnablePowerOffset)
                    {
                        double offset = _config.LaserPowerConfig.PowerOffsetKFactor * interpolated +
                                      _config.LaserPowerConfig.PowerOffsetCFactor;
                        return interpolated + offset;
                    }

                    return interpolated;
                }
            }

            return requestedPower;
        }

        private (double x, double y, double z) TransformCoordinates(double x, double y, double zFromDiameter)
        {
            // Применяем коррекцию кривизны поля
            double zFieldCorr = 0.0;
            if (_config.FunctionSwitcherConfig.EnableZCorrection)
            {
                double r = Math.Sqrt(x * x + y * y);
                zFieldCorr = _config.ThirdAxisConfig.AFactor * r * r +
                           _config.ThirdAxisConfig.BFactor * r +
                           _config.ThirdAxisConfig.CFactor;
            }

            // Применяем трансформации
            double transX = x * _config.ScannerConfig.ScaleX + _config.ScannerConfig.OffsetX;
            double transY = y * _config.ScannerConfig.ScaleY + _config.ScannerConfig.OffsetY;
            double transZ = zFromDiameter + zFieldCorr + _config.ScannerConfig.OffsetZ;

            return (transX, transY, transZ);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ПОЛНЫЙ ПРИМЕР: CLI файл → Hans .bin
        // ═══════════════════════════════════════════════════════════════════════

        public void ConvertFullCliFile(string outputPath)
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ПОЛНАЯ КОНВЕРТАЦИЯ CLI → Hans                           ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

            // Инициализация Hans (как в Java: HM_UDM_DLL.UDM_NewFile())
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1); // 3D режим (для Z-offset!)

            // Пример: Типичные CLI регионы из $PARAMETER_SET
            var regions = new[]
            {
                new {
                    Name = "edges",
                    Diameter = 65.0,  // edges_laser_beam_diameter
                    Power = 250.0,    // edges_laser_power
                    Speed = 800.0,    // edges_laser_scan_speed
                    LayerIndex = 0
                },
                new {
                    Name = "downskin_hatch",
                    Diameter = 80.0,  // downskin_hatch_laser_beam_diameter
                    Power = 280.0,
                    Speed = 800.0,
                    LayerIndex = 1
                },
                new {
                    Name = "upskin_contour",
                    Diameter = 70.0,
                    Power = 260.0,
                    Speed = 1000.0,
                    LayerIndex = 2
                },
                new {
                    Name = "infill_hatch",
                    Diameter = 90.0,
                    Power = 300.0,
                    Speed = 1250.0,
                    LayerIndex = 3
                },
                new {
                    Name = "support_hatch",
                    Diameter = 100.0,
                    Power = 200.0,
                    Speed = 2000.0,
                    LayerIndex = 4
                }
            };

            // Конвертируем каждый регион
            foreach (var region in regions)
            {
                // Тестовая геометрия (квадрат 10x10 мм)
                var geometry = new List<(double x, double y)>
                {
                    (0, 0), (10, 0), (10, 10), (0, 10), (0, 0)
                };

                ConvertCliRegionToHans(
                    region.Name,
                    region.Diameter,
                    region.Power,
                    region.Speed,
                    geometry,
                    region.LayerIndex
                );
            }

            // Финализация и сохранение
            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine($"\n✓ Файл сохранен: {outputPath}");
            Console.WriteLine("✓ Готово к печати на Hans scanner!");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ПРИМЕР ИСПОЛЬЗОВАНИЯ
    // ═══════════════════════════════════════════════════════════════════════

    public class RealConverterExample
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 1. Загрузить конфигурацию из JSON
            string configJson = File.ReadAllText("scanner_config.json");
            var configs = JsonSerializer.Deserialize<List<ScannerCardConfiguration>>(configJson);
            var config = configs[0]; // Карта 0

            // 2. Создать конвертер (АВТОМАТИЧЕСКИ вычисляет параметры из beamConfig)
            var converter = new RealCliToHansConverter(config);

            // ИЛИ с калиброванными значениями:
            // var converter = new RealCliToHansConverter(
            //     config,
            //     nominalDiameterOverride: 48.0,  // Из вашей калибровки
            //     zCoefficientOverride: 0.35      // Из вашей калибровки
            // );

            // 3. Конвертировать полный CLI файл
            converter.ConvertFullCliFile("output.bin");

            Console.WriteLine("\nНажмите Enter для выхода...");
            Console.ReadLine();
        }
    }
}
