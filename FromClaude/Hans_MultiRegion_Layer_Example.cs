using System;
using System.Collections.Generic;
using System.Linq;
using Hans.NET;

namespace PrintMateMC.HansFinal
{
    /// <summary>
    /// ПОЛНЫЙ ПРИМЕР: Как формировать UDM файл с множеством регионов в слое
    /// </summary>
    public class HansMultiRegionLayerExample
    {
        public class BeamConfig
        {
            public double MinBeamDiameterMicron { get; set; }
            public double RayleighLengthMicron { get; set; }
            public double FocalLengthMm { get; set; }

            public float CalculateZOffset(double targetDiameterMicron)
            {
                if (targetDiameterMicron <= MinBeamDiameterMicron)
                    return 0.0f;

                double ratio = targetDiameterMicron / MinBeamDiameterMicron;
                double z_micron = RayleighLengthMicron * Math.Sqrt(ratio * ratio - 1.0);
                return (float)(z_micron / 1000.0);
            }
        }

        public class ThirdAxisConfig
        {
            public double Bfactor { get; set; }
            public double Cfactor { get; set; }
            public double Afactor { get; set; }

            public float CalculateFieldCorrection(float x, float y)
            {
                double r = Math.Sqrt(x * x + y * y);
                return (float)(Afactor * r * r + Bfactor * r + Cfactor);
            }
        }

        public class SpeedConfig
        {
            public int MarkSpeed { get; set; }
            public int JumpSpeed { get; set; }
            public int PolygonDelay { get; set; }
            public int JumpDelay { get; set; }
            public int MarkDelay { get; set; }
            public double LaserOnDelay { get; set; }
            public double LaserOffDelay { get; set; }
            public double LaserOnDelayForSkyWriting { get; set; }
            public double LaserOffDelayForSkyWriting { get; set; }
            public bool SWEnable { get; set; }
        }

        public class LaserConfig
        {
            public BeamConfig BeamConfig { get; set; }
            public ThirdAxisConfig ThirdAxisConfig { get; set; }
            public double StaticOffsetZ { get; set; }
            public List<SpeedConfig> SpeedConfigs { get; set; }
            public double MaxPower { get; set; }

            public SpeedConfig FindSpeedConfig(int markSpeed)
            {
                var exact = SpeedConfigs.FirstOrDefault(c => c.MarkSpeed == markSpeed);
                if (exact != null) return exact;

                return SpeedConfigs
                    .Where(c => c.MarkSpeed <= markSpeed)
                    .OrderByDescending(c => c.MarkSpeed)
                    .FirstOrDefault() ?? SpeedConfigs.First();
            }
        }

        /// <summary>
        /// CLI Регион - один тип геометрии в слое
        /// </summary>
        public class CliRegion
        {
            public string Name { get; set; }              // "edges", "infill_hatch", etc.
            public bool SkyWritingEnabled { get; set; }
            public int MarkSpeed { get; set; }
            public double LaserPower { get; set; }
            public double BeamDiameter { get; set; }
            public List<CliPolyline> Polylines { get; set; }

            // Для удобства отладки
            public override string ToString()
            {
                int totalPoints = Polylines?.Sum(p => p.Points.Count) ?? 0;
                return $"{Name}: {Polylines?.Count ?? 0} polylines, {totalPoints} points, " +
                       $"SW={SkyWritingEnabled}, speed={MarkSpeed}, power={LaserPower}W, d={BeamDiameter}μm";
            }
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
        /// КЛЮЧЕВОЙ КЛАСС: Конвертер с поддержкой множества регионов в слое
        /// </summary>
        public class MultiRegionLayerConverter
        {
            private readonly LaserConfig laserConfig;

            public MultiRegionLayerConverter(LaserConfig config)
            {
                this.laserConfig = config;
            }

            /// <summary>
            /// ВАРИАНТ 1: Все регионы в ОДНОМ слое (рекомендуется!)
            /// </summary>
            public void ConvertMultipleRegionsToSingleLayer(List<CliRegion> regions, string outputFile)
            {
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  ВАРИАНТ 1: Все регионы в ОДНОМ слое                        ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

                HM_UDM_DLL.UDM_NewFile();
                HM_UDM_DLL.UDM_SetProtocol(0, 1);  // Protocol 0 (SPI), Mode 1 (3D)

                int layerIndex = 0;  // ← ВСЕ регионы идут в layer 0!

                foreach (var region in regions)
                {
                    Console.WriteLine($"Adding region: {region}");
                    AddRegionToLayer(region, layerIndex);
                    Console.WriteLine();
                }

                HM_UDM_DLL.UDM_Main();
                HM_UDM_DLL.UDM_SaveToFile(outputFile);
                HM_UDM_DLL.UDM_EndMain();

                Console.WriteLine($"✅ Saved: {outputFile}");
                Console.WriteLine($"   {regions.Count} regions in 1 layer\n");
            }

            /// <summary>
            /// ВАРИАНТ 2: Каждый регион в ОТДЕЛЬНОМ слое (если нужно)
            /// </summary>
            public void ConvertMultipleRegionsToSeparateLayers(List<CliRegion> regions, string outputFile)
            {
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  ВАРИАНТ 2: Каждый регион в ОТДЕЛЬНОМ слое                  ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

                HM_UDM_DLL.UDM_NewFile();
                HM_UDM_DLL.UDM_SetProtocol(0, 1);

                int layerIndex = 0;
                foreach (var region in regions)
                {
                    Console.WriteLine($"Layer {layerIndex}: {region}");
                    AddRegionToLayer(region, layerIndex);
                    layerIndex++;  // ← Следующий регион в следующий слой
                    Console.WriteLine();
                }

                HM_UDM_DLL.UDM_Main();
                HM_UDM_DLL.UDM_SaveToFile(outputFile);
                HM_UDM_DLL.UDM_EndMain();

                Console.WriteLine($"✅ Saved: {outputFile}");
                Console.WriteLine($"   {regions.Count} regions in {regions.Count} layers\n");
            }

            /// <summary>
            /// ВАРИАНТ 3: Группировать регионы по параметрам
            /// (регионы с одинаковыми параметрами → один слой)
            /// </summary>
            public void ConvertRegionsGroupedByParameters(List<CliRegion> regions, string outputFile)
            {
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  ВАРИАНТ 3: Группировка по параметрам                       ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

                // Группировать регионы по (SkyWriting, Speed, Power, Diameter)
                var grouped = regions.GroupBy(r => new
                {
                    r.SkyWritingEnabled,
                    r.MarkSpeed,
                    r.LaserPower,
                    r.BeamDiameter
                }).ToList();

                Console.WriteLine($"Найдено {grouped.Count} уникальных групп параметров:\n");

                HM_UDM_DLL.UDM_NewFile();
                HM_UDM_DLL.UDM_SetProtocol(0, 1);

                int layerIndex = 0;
                foreach (var group in grouped)
                {
                    var key = group.Key;
                    var regionsInGroup = group.ToList();

                    Console.WriteLine($"Layer {layerIndex}: SW={key.SkyWritingEnabled}, " +
                                    $"speed={key.MarkSpeed}, power={key.LaserPower}W, d={key.BeamDiameter}μm");
                    Console.WriteLine($"  Regions: {string.Join(", ", regionsInGroup.Select(r => r.Name))}");

                    foreach (var region in regionsInGroup)
                    {
                        AddRegionToLayer(region, layerIndex);
                    }

                    layerIndex++;
                    Console.WriteLine();
                }

                HM_UDM_DLL.UDM_Main();
                HM_UDM_DLL.UDM_SaveToFile(outputFile);
                HM_UDM_DLL.UDM_EndMain();

                Console.WriteLine($"✅ Saved: {outputFile}");
                Console.WriteLine($"   {regions.Count} regions in {layerIndex} layers\n");
            }

            /// <summary>
            /// Добавить регион в указанный слой
            /// </summary>
            private void AddRegionToLayer(CliRegion region, int layerIndex)
            {
                // 1. Найти конфигурацию скорости
                SpeedConfig speedConfig = laserConfig.FindSpeedConfig(region.MarkSpeed);

                // 2. Установить параметры слоя (делается ПЕРЕД добавлением геометрии)
                SetLayerParameters(region, speedConfig);

                // 3. Рассчитать Z-offset для диаметра
                float z_diameter = laserConfig.BeamConfig.CalculateZOffset(region.BeamDiameter);

                // 4. Добавить геометрию
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
                            float x = polyline.Points[i].X;
                            float y = polyline.Points[i].Y;

                            // Z = z_diameter + z_field + z_static
                            float z_field = laserConfig.ThirdAxisConfig.CalculateFieldCorrection(x, y);
                            float z_total = z_diameter + z_field + (float)laserConfig.StaticOffsetZ;

                            points[i] = new structUdmPos
                            {
                                x = x,
                                y = y,
                                z = z_total
                            };
                        }

                        HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, layerIndex);
                        totalPoints += points.Length;
                    }
                }

                Console.WriteLine($"  Added {totalPoints} points in {region.Polylines?.Count ?? 0} polylines");
            }

            /// <summary>
            /// Установить параметры слоя
            /// </summary>
            private void SetLayerParameters(CliRegion region, SpeedConfig speedConfig)
            {
                // 1. Установить SkyWriting
                HM_UDM_DLL.UDM_SkyWriting(region.SkyWritingEnabled ? 1 : 0);

                // 2. Установить параметры слоя
                MarkParameter[] layers = new MarkParameter[1];
                layers[0] = new MarkParameter
                {
                    MarkSpeed = (uint)region.MarkSpeed,
                    JumpSpeed = (uint)speedConfig.JumpSpeed,
                    LaserPower = (float)(region.LaserPower / laserConfig.MaxPower * 100.0),
                    MarkCount = 1
                };

                // 3. Задержки в зависимости от SkyWriting
                if (region.SkyWritingEnabled)
                {
                    layers[0].JumpDelay = 0;       // ← КРИТИЧНО: 0 для SkyWriting!
                    layers[0].PolygonDelay = 0;    // ← КРИТИЧНО: 0 для SkyWriting!
                    layers[0].MarkDelay = (uint)speedConfig.MarkDelay;
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
            }
        }

        /// <summary>
        /// ПРИМЕР: Реальный слой с множеством регионов
        /// </summary>
        public static void Example_RealWorldLayer()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример: Реальный слой с множеством регионов                ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            // Конфигурация лазера
            LaserConfig laserConfig = new LaserConfig
            {
                BeamConfig = new BeamConfig
                {
                    MinBeamDiameterMicron = 48.141,
                    RayleighLengthMicron = 1426.715,
                    FocalLengthMm = 538.46
                },
                ThirdAxisConfig = new ThirdAxisConfig
                {
                    Afactor = 0.0,
                    Bfactor = 0.013944261,
                    Cfactor = -7.5056114
                },
                StaticOffsetZ = -0.001,
                MaxPower = 500.0,
                SpeedConfigs = new List<SpeedConfig>
                {
                    new SpeedConfig
                    {
                        MarkSpeed = 800,
                        JumpSpeed = 25000,
                        PolygonDelay = 385,
                        JumpDelay = 40000,
                        MarkDelay = 470,
                        LaserOnDelay = 420.0,
                        LaserOffDelay = 490.0,
                        LaserOnDelayForSkyWriting = 600.0,
                        LaserOffDelayForSkyWriting = 730.0,
                        SWEnable = true
                    },
                    new SpeedConfig
                    {
                        MarkSpeed = 1250,
                        JumpSpeed = 25000,
                        PolygonDelay = 465,
                        JumpDelay = 40000,
                        MarkDelay = 496,
                        LaserOnDelay = 375.0,
                        LaserOffDelay = 500.0,
                        LaserOnDelayForSkyWriting = 615.0,
                        LaserOffDelayForSkyWriting = 725.0,
                        SWEnable = true
                    },
                    new SpeedConfig
                    {
                        MarkSpeed = 2000,
                        JumpSpeed = 25000,
                        PolygonDelay = 600,
                        JumpDelay = 40000,
                        MarkDelay = 540,
                        LaserOnDelay = 330.0,
                        LaserOffDelay = 530.0,
                        LaserOnDelayForSkyWriting = 630.0,
                        LaserOffDelayForSkyWriting = 720.0,
                        SWEnable = true
                    }
                }
            };

            // Создать слой с множеством регионов (типичный слой из CLI)
            List<CliRegion> layerRegions = new List<CliRegion>
            {
                // 1. Edges - внешний контур детали
                new CliRegion
                {
                    Name = "edges",
                    SkyWritingEnabled = true,
                    MarkSpeed = 800,
                    LaserPower = 140.0,
                    BeamDiameter = 80.0,
                    Polylines = new List<CliPolyline>
                    {
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = 0, Y = 0 },
                                new CliPoint { X = 50, Y = 0 },
                                new CliPoint { X = 50, Y = 50 },
                                new CliPoint { X = 0, Y = 50 },
                                new CliPoint { X = 0, Y = 0 }
                            }
                        }
                    }
                },

                // 2. Downskin border - граница нижней поверхности
                new CliRegion
                {
                    Name = "downskin_border",
                    SkyWritingEnabled = true,
                    MarkSpeed = 800,
                    LaserPower = 150.0,
                    BeamDiameter = 90.0,
                    Polylines = new List<CliPolyline>
                    {
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = 5, Y = 5 },
                                new CliPoint { X = 45, Y = 5 },
                                new CliPoint { X = 45, Y = 45 },
                                new CliPoint { X = 5, Y = 45 },
                                new CliPoint { X = 5, Y = 5 }
                            }
                        }
                    }
                },

                // 3. Downskin hatch - штриховка нижней поверхности
                new CliRegion
                {
                    Name = "downskin_hatch",
                    SkyWritingEnabled = true,
                    MarkSpeed = 1250,
                    LaserPower = 180.0,
                    BeamDiameter = 95.0,
                    Polylines = GenerateHatchLines(7, 7, 43, 43, 2.0f, 0)  // Вертикальные линии
                },

                // 4. Infill border - граница заполнения
                new CliRegion
                {
                    Name = "infill_border",
                    SkyWritingEnabled = true,
                    MarkSpeed = 800,
                    LaserPower = 160.0,
                    BeamDiameter = 85.0,
                    Polylines = new List<CliPolyline>
                    {
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = 10, Y = 10 },
                                new CliPoint { X = 40, Y = 10 },
                                new CliPoint { X = 40, Y = 40 },
                                new CliPoint { X = 10, Y = 40 },
                                new CliPoint { X = 10, Y = 10 }
                            }
                        }
                    }
                },

                // 5. Infill hatch - штриховка заполнения
                new CliRegion
                {
                    Name = "infill_hatch",
                    SkyWritingEnabled = true,
                    MarkSpeed = 1250,
                    LaserPower = 220.0,
                    BeamDiameter = 100.0,
                    Polylines = GenerateHatchLines(12, 12, 38, 38, 1.5f, 90)  // Горизонтальные линии
                },

                // 6. Upskin border - граница верхней поверхности
                new CliRegion
                {
                    Name = "upskin_border",
                    SkyWritingEnabled = true,
                    MarkSpeed = 800,
                    LaserPower = 155.0,
                    BeamDiameter = 88.0,
                    Polylines = new List<CliPolyline>
                    {
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = 15, Y = 15 },
                                new CliPoint { X = 35, Y = 15 },
                                new CliPoint { X = 35, Y = 35 },
                                new CliPoint { X = 15, Y = 35 },
                                new CliPoint { X = 15, Y = 15 }
                            }
                        }
                    }
                },

                // 7. Upskin hatch - штриховка верхней поверхности
                new CliRegion
                {
                    Name = "upskin_hatch",
                    SkyWritingEnabled = true,
                    MarkSpeed = 1250,
                    LaserPower = 200.0,
                    BeamDiameter = 95.0,
                    Polylines = GenerateHatchLines(17, 17, 33, 33, 1.5f, 45)  // Диагональные линии
                },

                // 8. Support border - граница поддержек (БЕЗ SkyWriting!)
                new CliRegion
                {
                    Name = "support_border",
                    SkyWritingEnabled = false,  // ← БЕЗ SkyWriting
                    MarkSpeed = 2000,
                    LaserPower = 280.0,
                    BeamDiameter = 110.0,
                    Polylines = new List<CliPolyline>
                    {
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = -10, Y = -10 },
                                new CliPoint { X = -5, Y = -10 },
                                new CliPoint { X = -5, Y = -5 },
                                new CliPoint { X = -10, Y = -5 },
                                new CliPoint { X = -10, Y = -10 }
                            }
                        }
                    }
                },

                // 9. Support hatch - штриховка поддержек (БЕЗ SkyWriting!)
                new CliRegion
                {
                    Name = "support_hatch",
                    SkyWritingEnabled = false,  // ← БЕЗ SkyWriting
                    MarkSpeed = 2000,
                    LaserPower = 320.0,
                    BeamDiameter = 120.0,
                    Polylines = GenerateHatchLines(-9, -9, -6, -6, 1.0f, 0)
                }
            };

            Console.WriteLine($"Создан слой с {layerRegions.Count} регионами:\n");
            foreach (var region in layerRegions)
            {
                Console.WriteLine($"  • {region}");
            }
            Console.WriteLine();

            // Создать конвертер
            MultiRegionLayerConverter converter = new MultiRegionLayerConverter(laserConfig);

            Console.WriteLine("─────────────────────────────────────────────────────────────────\n");

            // ВАРИАНТ 1: Все в один слой (рекомендуется!)
            converter.ConvertMultipleRegionsToSingleLayer(layerRegions, "layer_single.bin");

            Console.WriteLine("─────────────────────────────────────────────────────────────────\n");

            // ВАРИАНТ 2: Каждый регион в отдельный слой
            converter.ConvertMultipleRegionsToSeparateLayers(layerRegions, "layer_separate.bin");

            Console.WriteLine("─────────────────────────────────────────────────────────────────\n");

            // ВАРИАНТ 3: Группировка по параметрам
            converter.ConvertRegionsGroupedByParameters(layerRegions, "layer_grouped.bin");
        }

        /// <summary>
        /// Helper: Сгенерировать штриховку
        /// </summary>
        private static List<CliPolyline> GenerateHatchLines(
            float x1, float y1, float x2, float y2, float spacing, float angleDegrees)
        {
            List<CliPolyline> polylines = new List<CliPolyline>();

            // Простая реализация: вертикальные, горизонтальные или диагональные линии
            if (angleDegrees == 0)  // Вертикальные
            {
                for (float x = x1; x <= x2; x += spacing)
                {
                    polylines.Add(new CliPolyline
                    {
                        Points = new List<CliPoint>
                        {
                            new CliPoint { X = x, Y = y1 },
                            new CliPoint { X = x, Y = y2 }
                        }
                    });
                }
            }
            else if (angleDegrees == 90)  // Горизонтальные
            {
                for (float y = y1; y <= y2; y += spacing)
                {
                    polylines.Add(new CliPolyline
                    {
                        Points = new List<CliPoint>
                        {
                            new CliPoint { X = x1, Y = y },
                            new CliPoint { X = x2, Y = y }
                        }
                    });
                }
            }
            else  // Диагональные (упрощенно)
            {
                for (float offset = -(x2 - x1); offset <= (x2 - x1); offset += spacing)
                {
                    polylines.Add(new CliPolyline
                    {
                        Points = new List<CliPoint>
                        {
                            new CliPoint { X = x1, Y = y1 + offset },
                            new CliPoint { X = x2, Y = y2 + offset }
                        }
                    });
                }
            }

            return polylines;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Hans UDM: Множество регионов в слое                        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            Example_RealWorldLayer();

            Console.WriteLine("\n" + new string('═', 65));
            Console.WriteLine("\n💡 РЕКОМЕНДАЦИЯ:");
            Console.WriteLine("   Используйте ВАРИАНТ 1 (все регионы в один слой)");
            Console.WriteLine("   Это самый простой и естественный способ.\n");

            Console.WriteLine("   Hans UDM автоматически:");
            Console.WriteLine("   • Применяет правильные параметры для каждого региона");
            Console.WriteLine("   • Переключает SkyWriting между регионами");
            Console.WriteLine("   • Оптимизирует траектории\n");

            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }
    }
}
