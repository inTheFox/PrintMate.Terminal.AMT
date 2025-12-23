using System;
using System.Collections.Generic;
using System.Linq;
using Hans.NET;

namespace PrintMateMC.HansFinal
{
    /// <summary>
    /// ПОЛНЫЙ ПРИМЕР: от CLI JSON до Hans .bin файла
    /// С правильным расчетом Z-offset на основе beamConfig
    /// </summary>
    public class HansCliCompleteExample
    {
        /// <summary>
        /// Конфигурация оптики (из scanner config JSON -> beamConfig)
        /// </summary>
        public class BeamConfig
        {
            public double MinBeamDiameterMicron { get; set; } = 48.141;
            public double WavelengthNano { get; set; } = 1070.0;
            public double RayleighLengthMicron { get; set; } = 1426.715;
            public double M2 { get; set; } = 1.127;
            public double FocalLengthMm { get; set; } = 538.46;

            /// <summary>
            /// Рассчитать Z-offset (mm) для заданного целевого диаметра (μm)
            /// Формула дефокусировки Гауссова луча
            /// </summary>
            public float CalculateZOffset(double targetDiameterMicron)
            {
                if (targetDiameterMicron < MinBeamDiameterMicron)
                {
                    Console.WriteLine($"⚠️ WARNING: Target diameter {targetDiameterMicron:F1} μm " +
                                    $"is less than minimum {MinBeamDiameterMicron:F1} μm. Using Z=0.");
                    return 0.0f;
                }

                if (Math.Abs(targetDiameterMicron - MinBeamDiameterMicron) < 0.001)
                {
                    return 0.0f;  // Точно в фокусе
                }

                // z = z_R × sqrt((d_target / d₀)² - 1)
                double ratio = targetDiameterMicron / MinBeamDiameterMicron;
                double z_micron = RayleighLengthMicron * Math.Sqrt(ratio * ratio - 1.0);

                // Преобразовать μm -> mm
                return (float)(z_micron / 1000.0);
            }
        }

        /// <summary>
        /// Конфигурация процесса для конкретной скорости
        /// (из scanner config JSON -> processVariablesMap.markSpeed[i])
        /// </summary>
        public class SpeedConfig
        {
            public int MarkSpeed { get; set; }
            public bool SWEnable { get; set; }
            public double Umax { get; set; }

            // Задержки для обычного режима
            public double LaserOnDelay { get; set; }
            public double LaserOffDelay { get; set; }
            public int MarkDelay { get; set; }
            public int JumpDelay { get; set; }
            public int PolygonDelay { get; set; }

            // Специальные задержки для SkyWriting
            public double LaserOnDelayForSkyWriting { get; set; }
            public double LaserOffDelayForSkyWriting { get; set; }
            public int MarkDelayForSkyWriting { get; set; }

            // Другие параметры
            public int JumpSpeed { get; set; }
            public double CurPower { get; set; }
            public double CurBeamDiameterMicron { get; set; }
        }

        /// <summary>
        /// Полная конфигурация лазера
        /// </summary>
        public class LaserConfig
        {
            public string IpAddress { get; set; }
            public int SeqIndex { get; set; }
            public BeamConfig BeamConfig { get; set; }
            public List<SpeedConfig> SpeedConfigs { get; set; }
        }

        /// <summary>
        /// Регион из CLI файла
        /// Соответствует одному типу геометрии (edges, infill, supports, etc.)
        /// </summary>
        public class CliRegion
        {
            public string Name { get; set; }              // "edges", "downskin_hatch", etc.
            public bool SkyWritingEnabled { get; set; }   // edge_skywriting = "1"
            public int MarkSpeed { get; set; }            // laser_scan_speed (mm/s)
            public double LaserPower { get; set; }        // laser_power (W)
            public double BeamDiameter { get; set; }      // laser_beam_diameter (μm)
            public List<CliPolyline> Polylines { get; set; }  // Геометрия региона
        }

        public class CliPolyline
        {
            public List<CliPoint> Points { get; set; }
        }

        public class CliPoint
        {
            public float X { get; set; }
            public float Y { get; set; }
        }

        /// <summary>
        /// Конвертер CLI → Hans с правильным расчетом Z-offset
        /// </summary>
        public class CliToHansConverter
        {
            private readonly LaserConfig laserConfig;

            public CliToHansConverter(LaserConfig config)
            {
                this.laserConfig = config;
            }

            /// <summary>
            /// Найти конфигурацию для заданной скорости
            /// </summary>
            private SpeedConfig FindSpeedConfig(int markSpeed)
            {
                // Найти точное совпадение
                var exact = laserConfig.SpeedConfigs.FirstOrDefault(c => c.MarkSpeed == markSpeed);
                if (exact != null)
                    return exact;

                // Если точного нет, найти ближайшую меньшую
                var closest = laserConfig.SpeedConfigs
                    .Where(c => c.MarkSpeed <= markSpeed)
                    .OrderByDescending(c => c.MarkSpeed)
                    .FirstOrDefault();

                return closest ?? laserConfig.SpeedConfigs.First();
            }

            /// <summary>
            /// Конвертировать один регион CLI в Hans API вызовы
            /// </summary>
            public void ConvertRegion(CliRegion region, int layerIndex)
            {
                Console.WriteLine($"\n=== Converting Region: {region.Name} ===");
                Console.WriteLine($"  SkyWriting: {region.SkyWritingEnabled}");
                Console.WriteLine($"  Speed: {region.MarkSpeed} mm/s");
                Console.WriteLine($"  Power: {region.LaserPower} W");
                Console.WriteLine($"  Beam Diameter: {region.BeamDiameter} μm");

                // 1. Найти конфигурацию для этой скорости
                SpeedConfig speedConfig = FindSpeedConfig(region.MarkSpeed);

                // 2. Рассчитать Z-offset для диаметра (используя beamConfig)
                float z = laserConfig.BeamConfig.CalculateZOffset(region.BeamDiameter);
                Console.WriteLine($"  Calculated Z offset: {z:F3} mm");

                // 3. Применить SkyWriting ТОЧНО КАК Hans4Java
                HansSkyWritingFinalSolution.ApplySWEnableOperation_Hans4JavaWay(
                    enable: region.SkyWritingEnabled,
                    // Задержки для SkyWriting
                    laserOnDelayForSkyWriting: (float)speedConfig.LaserOnDelayForSkyWriting,
                    laserOffDelayForSkyWriting: (float)speedConfig.LaserOffDelayForSkyWriting,
                    markDelayForSkyWriting: speedConfig.MarkDelayForSkyWriting,
                    // Обычные задержки
                    laserOnDelayNormal: (float)speedConfig.LaserOnDelay,
                    laserOffDelayNormal: (float)speedConfig.LaserOffDelay,
                    markDelayNormal: speedConfig.MarkDelay,
                    jumpDelayNormal: speedConfig.JumpDelay,
                    polygonDelayNormal: speedConfig.PolygonDelay
                );

                // 4. Установить параметры слоя
                MarkParameter[] layers = new MarkParameter[1];
                layers[0] = new MarkParameter
                {
                    MarkSpeed = (uint)region.MarkSpeed,
                    JumpSpeed = (uint)speedConfig.JumpSpeed,
                    LaserPower = (float)(region.LaserPower / 500.0 * 100.0), // W -> % (500W max)
                    MarkCount = 1
                };

                // Задержки УЖЕ установлены в ApplySWEnableOperation_Hans4JavaWay
                if (region.SkyWritingEnabled)
                {
                    layers[0].JumpDelay = 0;       // ← КРИТИЧНО: 0 для SkyWriting!
                    layers[0].PolygonDelay = 0;    // ← КРИТИЧНО: 0 для SkyWriting!
                    layers[0].MarkDelay = (uint)speedConfig.MarkDelayForSkyWriting;
                    layers[0].LaserOnDelay = (float)speedConfig.LaserOnDelayForSkyWriting;
                    layers[0].LaserOffDelay = (float)speedConfig.LaserOffDelayForSkyWriting;
                }
                else
                {
                    layers[0].JumpDelay = (uint)speedConfig.JumpDelay;
                    layers[0].PolygonDelay = (uint)speedConfig.PolygonDelay;
                    layers[0].MarkDelay = (uint)speedConfig.MarkDelay;
                    layers[0].LaserOnDelay = (float)speedConfig.LaserOnDelay;
                    layers[0].LaserOffDelay = (float)speedConfig.LaserOffDelay;
                }

                HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

                // 5. Добавить геометрию с Z-offset
                int totalPoints = 0;
                if (region.Polylines != null)
                {
                    foreach (var polyline in region.Polylines)
                    {
                        if (polyline.Points == null || polyline.Points.Count == 0)
                            continue;

                        structUdmPos[] points = new structUdmPos[polyline.Points.Count];
                        for (int i = 0; i < polyline.Points.Count; i++)
                        {
                            points[i] = new structUdmPos
                            {
                                x = polyline.Points[i].X,
                                y = polyline.Points[i].Y,
                                z = z  // ← Применить рассчитанный Z-offset
                            };
                        }

                        HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, layerIndex);
                        totalPoints += points.Length;
                    }
                }

                Console.WriteLine($"  Added {totalPoints} points in {region.Polylines?.Count ?? 0} polylines");
                Console.WriteLine("✅ Region converted successfully\n");
            }

            /// <summary>
            /// Конвертировать весь CLI файл в Hans .bin файлы
            /// </summary>
            public void ConvertFullCliFile(List<CliRegion> regions, string outputDirectory)
            {
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  CLI → Hans Conversion (с beamConfig расчетом Z-offset)     ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

                // Группировать регионы по SkyWriting состоянию
                var withSkyWriting = regions.Where(r => r.SkyWritingEnabled).ToList();
                var withoutSkyWriting = regions.Where(r => !r.SkyWritingEnabled).ToList();

                Console.WriteLine($"Regions with SkyWriting: {withSkyWriting.Count}");
                Console.WriteLine($"Regions without SkyWriting: {withoutSkyWriting.Count}\n");

                // Файл 1: Регионы С SkyWriting
                if (withSkyWriting.Any())
                {
                    Console.WriteLine("Creating file: regions_with_skywriting.bin");
                    HM_UDM_DLL.UDM_NewFile();
                    HM_UDM_DLL.UDM_SetProtocol(0, 1); // Protocol 0 (SPI), Mode 1 (3D)

                    int layerIndex = 0;
                    foreach (var region in withSkyWriting)
                    {
                        ConvertRegion(region, layerIndex++);
                    }

                    HM_UDM_DLL.UDM_Main();
                    HM_UDM_DLL.UDM_SaveToFile($"{outputDirectory}/regions_with_skywriting.bin");
                    HM_UDM_DLL.UDM_EndMain();

                    Console.WriteLine("✅ File saved: regions_with_skywriting.bin\n");
                }

                // Файл 2: Регионы БЕЗ SkyWriting
                if (withoutSkyWriting.Any())
                {
                    Console.WriteLine("Creating file: regions_without_skywriting.bin");
                    HM_UDM_DLL.UDM_NewFile();
                    HM_UDM_DLL.UDM_SetProtocol(0, 1);

                    int layerIndex = 0;
                    foreach (var region in withoutSkyWriting)
                    {
                        ConvertRegion(region, layerIndex++);
                    }

                    HM_UDM_DLL.UDM_Main();
                    HM_UDM_DLL.UDM_SaveToFile($"{outputDirectory}/regions_without_skywriting.bin");
                    HM_UDM_DLL.UDM_EndMain();

                    Console.WriteLine("✅ File saved: regions_without_skywriting.bin\n");
                }

                Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  ✅ Conversion Complete                                        ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            }
        }

        /// <summary>
        /// ПРИМЕР: Реальный CLI файл с вашей конфигурацией
        /// </summary>
        public static void Example_RealWorld_FullCliConversion()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример: Полная конвертация CLI файла                       ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            // 1. Создать конфигурацию лазера из вашего scanner config JSON
            LaserConfig laser1 = new LaserConfig
            {
                IpAddress = "172.18.34.227",
                SeqIndex = 0,

                // beamConfig - оптические параметры
                BeamConfig = new BeamConfig
                {
                    MinBeamDiameterMicron = 48.141,
                    WavelengthNano = 1070.0,
                    RayleighLengthMicron = 1426.715,
                    M2 = 1.127,
                    FocalLengthMm = 538.46
                },

                // processVariablesMap - параметры для разных скоростей
                SpeedConfigs = new List<SpeedConfig>
                {
                    // Скорость 800 mm/s
                    new SpeedConfig
                    {
                        MarkSpeed = 800,
                        SWEnable = true,
                        Umax = 0.1,
                        // Задержки для SkyWriting
                        LaserOnDelayForSkyWriting = 600.0,
                        LaserOffDelayForSkyWriting = 730.0,
                        MarkDelayForSkyWriting = 470,
                        // Обычные задержки
                        LaserOnDelay = 420.0,
                        LaserOffDelay = 490.0,
                        MarkDelay = 470,
                        JumpDelay = 40000,
                        PolygonDelay = 385,
                        // Другие параметры
                        JumpSpeed = 25000,
                        CurPower = 140.0,
                        CurBeamDiameterMicron = 80.0
                    },

                    // Скорость 1250 mm/s
                    new SpeedConfig
                    {
                        MarkSpeed = 1250,
                        SWEnable = true,
                        Umax = 0.1,
                        LaserOnDelayForSkyWriting = 700.0,
                        LaserOffDelayForSkyWriting = 830.0,
                        MarkDelayForSkyWriting = 370,
                        LaserOnDelay = 520.0,
                        LaserOffDelay = 590.0,
                        MarkDelay = 370,
                        JumpDelay = 35000,
                        PolygonDelay = 285,
                        JumpSpeed = 25000,
                        CurPower = 220.0,
                        CurBeamDiameterMicron = 100.0
                    },

                    // Скорость 2000 mm/s
                    new SpeedConfig
                    {
                        MarkSpeed = 2000,
                        SWEnable = true,
                        Umax = 0.1,
                        LaserOnDelayForSkyWriting = 800.0,
                        LaserOffDelayForSkyWriting = 930.0,
                        MarkDelayForSkyWriting = 270,
                        LaserOnDelay = 620.0,
                        LaserOffDelay = 690.0,
                        MarkDelay = 270,
                        JumpDelay = 30000,
                        PolygonDelay = 185,
                        JumpSpeed = 25000,
                        CurPower = 320.0,
                        CurBeamDiameterMicron = 120.0
                    }
                }
            };

            // 2. Создать регионы из CLI файла
            // В реальном коде вы парсите CLI JSON
            List<CliRegion> regions = new List<CliRegion>
            {
                // Edges - контуры детали
                new CliRegion
                {
                    Name = "edges",
                    SkyWritingEnabled = true,   // edge_skywriting = "1"
                    MarkSpeed = 800,            // laser_scan_speed
                    LaserPower = 140.0,         // laser_power
                    BeamDiameter = 80.0,        // edges_laser_beam_diameter
                    Polylines = new List<CliPolyline>
                    {
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = 0, Y = 0 },
                                new CliPoint { X = 10, Y = 0 },
                                new CliPoint { X = 10, Y = 10 },
                                new CliPoint { X = 0, Y = 10 },
                                new CliPoint { X = 0, Y = 0 }
                            }
                        }
                    }
                },

                // Downskin border - нижняя граница
                new CliRegion
                {
                    Name = "downskin_border",
                    SkyWritingEnabled = true,   // downskin_border_skywriting = "1"
                    MarkSpeed = 800,
                    LaserPower = 150.0,
                    BeamDiameter = 90.0,        // downskin_border_laser_beam_diameter
                    Polylines = new List<CliPolyline>
                    {
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = 1, Y = 1 },
                                new CliPoint { X = 9, Y = 1 },
                                new CliPoint { X = 9, Y = 9 },
                                new CliPoint { X = 1, Y = 9 },
                                new CliPoint { X = 1, Y = 1 }
                            }
                        }
                    }
                },

                // Infill hatch - заполнение
                new CliRegion
                {
                    Name = "infill_hatch",
                    SkyWritingEnabled = true,   // infill_hatch_skywriting = "1"
                    MarkSpeed = 1250,
                    LaserPower = 220.0,
                    BeamDiameter = 100.0,       // infill_hatch_laser_beam_diameter
                    Polylines = new List<CliPolyline>
                    {
                        // Hatch lines
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = 2, Y = 2 },
                                new CliPoint { X = 8, Y = 2 }
                            }
                        },
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = 2, Y = 3 },
                                new CliPoint { X = 8, Y = 3 }
                            }
                        },
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = 2, Y = 4 },
                                new CliPoint { X = 8, Y = 4 }
                            }
                        }
                    }
                },

                // Support hatch - поддержки (БЕЗ SkyWriting!)
                new CliRegion
                {
                    Name = "support_hatch",
                    SkyWritingEnabled = false,  // support_hatch_skywriting = "0"
                    MarkSpeed = 2000,
                    LaserPower = 320.0,
                    BeamDiameter = 120.0,       // support_hatch_laser_beam_diameter
                    Polylines = new List<CliPolyline>
                    {
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = 15, Y = 15 },
                                new CliPoint { X = 20, Y = 15 },
                                new CliPoint { X = 20, Y = 20 },
                                new CliPoint { X = 15, Y = 20 }
                            }
                        }
                    }
                }
            };

            // 3. Конвертировать
            CliToHansConverter converter = new CliToHansConverter(laser1);
            converter.ConvertFullCliFile(regions, ".");

            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine("РЕЗУЛЬТАТ:");
            Console.WriteLine("  ✅ regions_with_skywriting.bin");
            Console.WriteLine("     - edges (80 μm → Z=1.894 mm)");
            Console.WriteLine("     - downskin_border (90 μm → Z=2.224 mm)");
            Console.WriteLine("     - infill_hatch (100 μm → Z=2.522 mm)");
            Console.WriteLine();
            Console.WriteLine("  ✅ regions_without_skywriting.bin");
            Console.WriteLine("     - support_hatch (120 μm → Z=3.052 mm)");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
        }

        /// <summary>
        /// ПРИМЕР: Проверка расчета Z-offset
        /// </summary>
        public static void Example_TestZOffsetCalculation()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Тест: Расчет Z-offset из beamConfig                        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            BeamConfig beamConfig = new BeamConfig
            {
                MinBeamDiameterMicron = 48.141,
                RayleighLengthMicron = 1426.715,
                M2 = 1.127,
                WavelengthNano = 1070.0,
                FocalLengthMm = 538.46
            };

            Console.WriteLine($"Beam Config:");
            Console.WriteLine($"  Min diameter (d₀): {beamConfig.MinBeamDiameterMicron:F3} μm");
            Console.WriteLine($"  Rayleigh length (z_R): {beamConfig.RayleighLengthMicron:F3} μm");
            Console.WriteLine($"  Wavelength: {beamConfig.WavelengthNano:F1} nm");
            Console.WriteLine($"  M²: {beamConfig.M2:F3}");
            Console.WriteLine($"  Focal length: {beamConfig.FocalLengthMm:F2} mm\n");

            // CLI диаметры из вашего JSON
            double[] cliDiameters = { 80.0, 90.0, 100.0, 120.0, 140.0 };
            string[] regionNames = { "edges", "downskin_border", "infill_hatch", "support_hatch", "upskin" };

            Console.WriteLine("┌──────────────────────┬─────────────┬──────────────┐");
            Console.WriteLine("│ Region               │ Diameter    │ Z-offset     │");
            Console.WriteLine("├──────────────────────┼─────────────┼──────────────┤");

            for (int i = 0; i < cliDiameters.Length; i++)
            {
                float z = beamConfig.CalculateZOffset(cliDiameters[i]);
                Console.WriteLine($"│ {regionNames[i],-20} │ {cliDiameters[i],6:F1} μm │ {z,8:F3} mm │");
            }

            Console.WriteLine("└──────────────────────┴─────────────┴──────────────┘\n");

            // Таблица Z-offset для разных диаметров
            Console.WriteLine("Полная таблица Z-offset:");
            Console.WriteLine("┌─────────────┬──────────────┐");
            Console.WriteLine("│ Diameter    │ Z-offset     │");
            Console.WriteLine("├─────────────┼──────────────┤");

            double[] allDiameters = { 48.141, 50, 60, 70, 80, 90, 100, 110, 120, 140, 160, 200 };
            foreach (var d in allDiameters)
            {
                float z = beamConfig.CalculateZOffset(d);
                Console.WriteLine($"│ {d,6:F1} μm │ {z,8:F3} mm │");
            }

            Console.WriteLine("└─────────────┴──────────────┘\n");
        }
    }

    /// <summary>
    /// Главный класс для запуска примеров
    /// </summary>
    public class ProgramCliExample
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Hans CLI Complete Example - с beamConfig                   ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("Выберите пример:");
            Console.WriteLine("1. Полная конвертация CLI файла");
            Console.WriteLine("2. Тест расчета Z-offset");
            Console.WriteLine("3. Оба примера");
            Console.WriteLine("\nВведите номер (1-3): ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    HansCliCompleteExample.Example_RealWorld_FullCliConversion();
                    break;
                case "2":
                    HansCliCompleteExample.Example_TestZOffsetCalculation();
                    break;
                case "3":
                default:
                    HansCliCompleteExample.Example_TestZOffsetCalculation();
                    Console.WriteLine("\n\n");
                    HansCliCompleteExample.Example_RealWorld_FullCliConversion();
                    break;
            }

            Console.WriteLine("\n\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }
}
