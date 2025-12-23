using System;
using System.Collections.Generic;
using System.Linq;
using Hans.NET;

namespace PrintMateMC.HansFinal
{
    /// <summary>
    /// Полная интеграция: от CLI JSON до Hans .bin файла
    /// Использует ФИНАЛЬНОЕ РЕШЕНИЕ на основе декомпилированного Hans4Java
    /// </summary>
    public class CompleteCliToHansIntegration
    {
        /// <summary>
        /// Конфигурация лазера (из вашего scanner config JSON)
        /// </summary>
        public class LaserConfig
        {
            public string IpAddress { get; set; }
            public int SeqIndex { get; set; }

            // Параметры для разных скоростей
            public List<SpeedConfig> SpeedConfigs { get; set; }
        }

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
        /// Регион из CLI файла
        /// </summary>
        public class CliRegion
        {
            public string Name { get; set; }              // "edges", "downskin_hatch", etc.
            public bool SkyWritingEnabled { get; set; }   // Из CLI JSON: edge_skywriting = "1"
            public int MarkSpeed { get; set; }            // laser_scan_speed
            public double LaserPower { get; set; }        // laser_power (W)
            public double BeamDiameter { get; set; }      // laser_beam_diameter (μm)
            public List<CliPoint> Geometry { get; set; }  // Геометрия региона
        }

        public class CliPoint
        {
            public float X { get; set; }
            public float Y { get; set; }
        }

        /// <summary>
        /// Конвертер CLI -> Hans с использованием финального решения
        /// </summary>
        public class CliToHansConverter
        {
            private readonly LaserConfig laserConfig;
            private readonly double nominalDiameter = 120.0; // μm (из калибровки)
            private readonly double zCoefficient = 0.3;      // mm/10μm (из калибровки)

            public CliToHansConverter(LaserConfig config)
            {
                this.laserConfig = config;
            }

            /// <summary>
            /// Рассчитать Z-offset для заданного диаметра
            /// </summary>
            private float CalculateZOffset(double diameterMicrons)
            {
                return (float)((diameterMicrons - nominalDiameter) / 10.0 * zCoefficient);
            }

            /// <summary>
            /// Найти конфигурацию для заданной скорости
            /// </summary>
            private SpeedConfig FindSpeedConfig(int markSpeed)
            {
                // Найти точное совпадение или ближайшее
                var exact = laserConfig.SpeedConfigs.FirstOrDefault(c => c.MarkSpeed == markSpeed);
                if (exact != null)
                    return exact;

                // Если точного нет, найти ближайшую меньшую
                return laserConfig.SpeedConfigs
                    .Where(c => c.MarkSpeed <= markSpeed)
                    .OrderByDescending(c => c.MarkSpeed)
                    .FirstOrDefault()
                    ?? laserConfig.SpeedConfigs.First();
            }

            /// <summary>
            /// Конвертировать один регион CLI в Hans API вызовы
            /// ИСПОЛЬЗУЕТ ФИНАЛЬНОЕ РЕШЕНИЕ от Hans4Java
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

                // 2. Применить SkyWriting ТОЧНО КАК Hans4Java
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

                // 3. Рассчитать Z-offset для диаметра
                float z = CalculateZOffset(region.BeamDiameter);
                Console.WriteLine($"  Calculated Z offset: {z:F3} mm");

                // 4. Установить параметры слоя
                MarkParameter[] layers = new MarkParameter[1];
                layers[0] = new MarkParameter
                {
                    MarkSpeed = (uint)region.MarkSpeed,
                    JumpSpeed = (uint)speedConfig.JumpSpeed,
                    LaserPower = (float)(region.LaserPower / 500.0 * 100.0), // W -> %
                    MarkCount = 1
                };

                // Задержки УЖЕ установлены в ApplySWEnableOperation_Hans4JavaWay,
                // но для ясности продублируем логику:
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
                if (region.Geometry != null && region.Geometry.Count > 0)
                {
                    structUdmPos[] points = new structUdmPos[region.Geometry.Count];
                    for (int i = 0; i < region.Geometry.Count; i++)
                    {
                        points[i] = new structUdmPos
                        {
                            x = region.Geometry[i].X,
                            y = region.Geometry[i].Y,
                            z = z  // Применить Z-offset для управления диаметром
                        };
                    }

                    HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, layerIndex);
                    Console.WriteLine($"  Added {points.Length} points");
                }

                Console.WriteLine("✅ Region converted successfully\n");
            }

            /// <summary>
            /// Конвертировать весь CLI файл в Hans .bin файлы
            /// ВАЖНО: Создает ОТДЕЛЬНЫЕ файлы для регионов с разным SkyWriting
            /// </summary>
            public void ConvertFullCliFile(List<CliRegion> regions, string outputDirectory)
            {
                Console.WriteLine("=== Starting Full CLI to Hans Conversion ===\n");

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

                Console.WriteLine("=== Conversion Complete ===");
            }
        }

        /// <summary>
        /// Пример использования: Конвертация реального CLI файла
        /// </summary>
        public static void Example_RealWorldUsage()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Полная интеграция: CLI → Hans (финальное решение)          ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            // 1. Создать конфигурацию лазера из вашего JSON
            LaserConfig laser1Config = new LaserConfig
            {
                IpAddress = "172.18.34.227",
                SeqIndex = 0,
                SpeedConfigs = new List<SpeedConfig>
                {
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
                    }
                }
            };

            // 2. Создать регионы из CLI файла
            List<CliRegion> regions = new List<CliRegion>
            {
                // Edges - с SkyWriting
                new CliRegion
                {
                    Name = "edges",
                    SkyWritingEnabled = true,   // edge_skywriting = "1"
                    MarkSpeed = 800,
                    LaserPower = 140.0,
                    BeamDiameter = 80.0,
                    Geometry = new List<CliPoint>
                    {
                        new CliPoint { X = 0, Y = 0 },
                        new CliPoint { X = 10, Y = 0 },
                        new CliPoint { X = 10, Y = 10 },
                        new CliPoint { X = 0, Y = 10 },
                        new CliPoint { X = 0, Y = 0 }
                    }
                },

                // Infill hatch - с SkyWriting
                new CliRegion
                {
                    Name = "infill_hatch",
                    SkyWritingEnabled = true,   // infill_hatch_skywriting = "1"
                    MarkSpeed = 1250,
                    LaserPower = 220.0,
                    BeamDiameter = 100.0,
                    Geometry = new List<CliPoint>
                    {
                        new CliPoint { X = 2, Y = 2 },
                        new CliPoint { X = 8, Y = 2 },
                        new CliPoint { X = 8, Y = 8 },
                        new CliPoint { X = 2, Y = 8 }
                    }
                },

                // Support hatch - БЕЗ SkyWriting
                new CliRegion
                {
                    Name = "support_hatch",
                    SkyWritingEnabled = false,  // support_hatch_skywriting = "0"
                    MarkSpeed = 800,
                    LaserPower = 260.0,
                    BeamDiameter = 120.0,
                    Geometry = new List<CliPoint>
                    {
                        new CliPoint { X = 15, Y = 15 },
                        new CliPoint { X = 20, Y = 15 },
                        new CliPoint { X = 20, Y = 20 },
                        new CliPoint { X = 15, Y = 20 }
                    }
                }
            };

            // 3. Конвертировать
            CliToHansConverter converter = new CliToHansConverter(laser1Config);
            converter.ConvertFullCliFile(regions, ".");

            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine("РЕЗУЛЬТАТ:");
            Console.WriteLine("  ✅ regions_with_skywriting.bin - edges + infill_hatch");
            Console.WriteLine("  ✅ regions_without_skywriting.bin - support_hatch");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
        }

        /// <summary>
        /// Пример упрощенной интеграции для одного слоя
        /// </summary>
        public static void Example_SingleLayerQuickConversion()
        {
            Console.WriteLine("=== Quick Single Layer Conversion ===\n");

            // Конфигурация для скорости 800 mm/s
            SpeedConfig config = new SpeedConfig
            {
                MarkSpeed = 800,
                SWEnable = true,
                LaserOnDelayForSkyWriting = 600.0,
                LaserOffDelayForSkyWriting = 730.0,
                MarkDelayForSkyWriting = 470,
                LaserOnDelay = 420.0,
                LaserOffDelay = 490.0,
                MarkDelay = 470,
                JumpDelay = 40000,
                PolygonDelay = 385,
                JumpSpeed = 25000
            };

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Применить SkyWriting точно как Hans4Java
            HansSkyWritingFinalSolution.ApplySWEnableOperation_Hans4JavaWay(
                enable: true,
                laserOnDelayForSkyWriting: (float)config.LaserOnDelayForSkyWriting,
                laserOffDelayForSkyWriting: (float)config.LaserOffDelayForSkyWriting,
                markDelayForSkyWriting: config.MarkDelayForSkyWriting,
                laserOnDelayNormal: (float)config.LaserOnDelay,
                laserOffDelayNormal: (float)config.LaserOffDelay,
                markDelayNormal: config.MarkDelay,
                jumpDelayNormal: config.JumpDelay,
                polygonDelayNormal: config.PolygonDelay
            );

            // Добавить геометрию
            structUdmPos[] points = new structUdmPos[]
            {
                new structUdmPos { x = 0, y = 0, z = -1.2f },
                new structUdmPos { x = 10, y = 0, z = -1.2f },
                new structUdmPos { x = 10, y = 10, z = -1.2f },
                new structUdmPos { x = 0, y = 10, z = -1.2f },
                new structUdmPos { x = 0, y = 0, z = -1.2f }
            };
            HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, 0);

            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("quick_layer.bin");
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine("✅ Quick layer created: quick_layer.bin\n");
        }
    }

    /// <summary>
    /// Главный класс для запуска примеров
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Hans SkyWriting - Полная Интеграция (Final Solution)       ║");
            Console.WriteLine("║  На основе декомпилированного Hans4Java UdmProducer.class   ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            // Показать ключевые выводы
            Hans4JavaFindings.PrintFindings();

            Console.WriteLine("\nВыберите пример:");
            Console.WriteLine("1. Полная конвертация CLI файла (Real World Example)");
            Console.WriteLine("2. Быстрая конвертация одного слоя");
            Console.WriteLine("3. Базовые примеры из Hans_CSharp_Final_Solution");
            Console.WriteLine("\nВведите номер (1-3): ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    CompleteCliToHansIntegration.Example_RealWorldUsage();
                    break;
                case "2":
                    CompleteCliToHansIntegration.Example_SingleLayerQuickConversion();
                    break;
                case "3":
                    HansSkyWritingFinalSolution.Example1_WithYourConfig();
                    HansSkyWritingFinalSolution.Example2_SwitchingSkyWriting();
                    HansSkyWritingFinalSolution.Example3_SimplifiedVersion();
                    break;
                default:
                    Console.WriteLine("Запускаем все примеры...\n");
                    HansSkyWritingFinalSolution.Example1_WithYourConfig();
                    CompleteCliToHansIntegration.Example_SingleLayerQuickConversion();
                    CompleteCliToHansIntegration.Example_RealWorldUsage();
                    break;
            }

            Console.WriteLine("\n\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }
}
