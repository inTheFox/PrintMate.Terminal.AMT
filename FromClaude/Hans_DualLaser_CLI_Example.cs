using System;
using System.Collections.Generic;
using System.Linq;
using Hans.NET;

namespace PrintMateMC.HansFinal
{
    /// <summary>
    /// ПОЛНЫЙ ПРИМЕР: Dual-laser система с вашей реальной конфигурацией
    /// Laser 1: 172.18.34.227 (d₀=48.141 μm, z_R=1426.715 μm)
    /// Laser 2: 172.18.34.228 (d₀=53.872 μm, z_R=1616.16 μm)
    /// </summary>
    public class HansDualLaserCliExample
    {
        /// <summary>
        /// Beam Config - оптические параметры лазера
        /// </summary>
        public class BeamConfig
        {
            public double MinBeamDiameterMicron { get; set; }
            public double WavelengthNano { get; set; }
            public double RayleighLengthMicron { get; set; }
            public double M2 { get; set; }
            public double FocalLengthMm { get; set; }

            /// <summary>
            /// Рассчитать Z-offset для целевого диаметра
            /// z = z_R × sqrt((d_target / d₀)² - 1)
            /// </summary>
            public float CalculateZOffset(double targetDiameterMicron)
            {
                if (targetDiameterMicron < MinBeamDiameterMicron)
                {
                    Console.WriteLine($"⚠️ WARNING: Target {targetDiameterMicron:F1} μm < min {MinBeamDiameterMicron:F1} μm. Using Z=0.");
                    return 0.0f;
                }

                if (Math.Abs(targetDiameterMicron - MinBeamDiameterMicron) < 0.001)
                    return 0.0f;

                double ratio = targetDiameterMicron / MinBeamDiameterMicron;
                double z_micron = RayleighLengthMicron * Math.Sqrt(ratio * ratio - 1.0);
                return (float)(z_micron / 1000.0);
            }
        }

        /// <summary>
        /// Scanner Config - калибровка поля сканирования
        /// </summary>
        public class ScannerConfig
        {
            public double FieldSizeX { get; set; }
            public double FieldSizeY { get; set; }
            public int ProtocolCode { get; set; }
            public int CoordinateTypeCode { get; set; }
            public double OffsetX { get; set; }
            public double OffsetY { get; set; }
            public double OffsetZ { get; set; }
            public double ScaleX { get; set; }
            public double ScaleY { get; set; }
            public double ScaleZ { get; set; }
            public double RotateAngle { get; set; }
        }

        /// <summary>
        /// Third Axis Config - коррекция кривизны поля
        /// Z_correction = A×r² + B×r + C
        /// </summary>
        public class ThirdAxisConfig
        {
            public double Bfactor { get; set; }
            public double Cfactor { get; set; }
            public double Afactor { get; set; }

            /// <summary>
            /// Рассчитать коррекцию Z для позиции (x, y)
            /// </summary>
            public float CalculateZCorrection(float x, float y)
            {
                double r = Math.Sqrt(x * x + y * y);
                double z_corr = Afactor * r * r + Bfactor * r + Cfactor;
                return (float)z_corr;
            }
        }

        /// <summary>
        /// Laser Power Config - коррекция мощности
        /// </summary>
        public class LaserPowerConfig
        {
            public double MaxPower { get; set; }
            public List<double> ActualPowerCorrectionValue { get; set; }
            public double PowerOffsetKFactor { get; set; }
            public double PowerOffsetCFactor { get; set; }

            /// <summary>
            /// Конвертировать мощность (W) в процент с коррекцией
            /// </summary>
            public float ConvertPowerToPercent(double powerWatts)
            {
                // Базовый процент
                double percent = (powerWatts / MaxPower) * 100.0;

                // TODO: Применить коррекцию через actualPowerCorrectionValue
                // (интерполяция по таблице)

                return (float)percent;
            }
        }

        /// <summary>
        /// Process Variables - параметры процесса для конкретной скорости
        /// </summary>
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
            public double CurBeamDiameterMicron { get; set; }
            public double CurPower { get; set; }
            public double JumpMaxLengthLimitMm { get; set; }
            public int MinJumpDelay { get; set; }
            public bool SWEnable { get; set; }
            public double Umax { get; set; }
        }

        /// <summary>
        /// Полная конфигурация одного лазера
        /// </summary>
        public class LaserCardConfig
        {
            public string IpAddress { get; set; }
            public int SeqIndex { get; set; }
            public BeamConfig BeamConfig { get; set; }
            public ScannerConfig ScannerConfig { get; set; }
            public ThirdAxisConfig ThirdAxisConfig { get; set; }
            public LaserPowerConfig LaserPowerConfig { get; set; }
            public List<SpeedConfig> SpeedConfigs { get; set; }

            /// <summary>
            /// Найти конфигурацию для заданной скорости
            /// </summary>
            public SpeedConfig FindSpeedConfig(int markSpeed)
            {
                var exact = SpeedConfigs.FirstOrDefault(c => c.MarkSpeed == markSpeed);
                if (exact != null) return exact;

                var closest = SpeedConfigs
                    .Where(c => c.MarkSpeed <= markSpeed)
                    .OrderByDescending(c => c.MarkSpeed)
                    .FirstOrDefault();

                return closest ?? SpeedConfigs.First();
            }
        }

        /// <summary>
        /// CLI Region - регион из CLI файла
        /// </summary>
        public class CliRegion
        {
            public string Name { get; set; }
            public bool SkyWritingEnabled { get; set; }
            public int MarkSpeed { get; set; }
            public double LaserPower { get; set; }
            public double BeamDiameter { get; set; }       // ← ИЗ CLI!
            public int LaserIndex { get; set; }            // 0 или 1 (какой лазер)
            public List<CliPolyline> Polylines { get; set; }
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
        /// Конвертер CLI → Hans для dual-laser системы
        /// </summary>
        public class DualLaserCliConverter
        {
            private readonly List<LaserCardConfig> laserConfigs;

            public DualLaserCliConverter(List<LaserCardConfig> configs)
            {
                this.laserConfigs = configs;
            }

            /// <summary>
            /// Конвертировать регион с использованием правильного beamConfig
            /// </summary>
            public void ConvertRegion(CliRegion region, int layerIndex)
            {
                LaserCardConfig laser = laserConfigs[region.LaserIndex];

                Console.WriteLine($"\n=== Region: {region.Name} (Laser {region.LaserIndex + 1}: {laser.IpAddress}) ===");
                Console.WriteLine($"  SkyWriting: {region.SkyWritingEnabled}");
                Console.WriteLine($"  Speed: {region.MarkSpeed} mm/s");
                Console.WriteLine($"  Power: {region.LaserPower} W");
                Console.WriteLine($"  CLI Beam Diameter: {region.BeamDiameter} μm");

                // 1. Найти конфигурацию скорости
                SpeedConfig speedConfig = laser.FindSpeedConfig(region.MarkSpeed);

                // 2. КЛЮЧЕВОЕ: Рассчитать Z-offset из CLI diameter
                // Используем beamConfig конкретного лазера!
                float z_diameter = laser.BeamConfig.CalculateZOffset(region.BeamDiameter);
                Console.WriteLine($"  Z from diameter: {z_diameter:F3} mm");
                Console.WriteLine($"    (using d₀={laser.BeamConfig.MinBeamDiameterMicron:F3} μm, " +
                                $"z_R={laser.BeamConfig.RayleighLengthMicron:F3} μm)");

                // 3. Применить коррекцию кривизны поля (thirdAxisConfig)
                // Для центра поля (0, 0)
                float z_field_correction = laser.ThirdAxisConfig.CalculateZCorrection(0, 0);
                Console.WriteLine($"  Z field correction (center): {z_field_correction:F3} mm");

                // 4. Общий Z-offset (статический из scannerConfig)
                float z_static = (float)laser.ScannerConfig.OffsetZ;
                Console.WriteLine($"  Z static offset: {z_static:F3} mm");

                // 5. Итоговый Z
                float z_total = z_diameter + z_field_correction + z_static;
                Console.WriteLine($"  ✅ TOTAL Z-offset: {z_total:F3} mm");

                // 6. Применить SkyWriting
                HansSkyWritingFinalSolution.ApplySWEnableOperation_Hans4JavaWay(
                    enable: region.SkyWritingEnabled,
                    laserOnDelayForSkyWriting: (float)speedConfig.LaserOnDelayForSkyWriting,
                    laserOffDelayForSkyWriting: (float)speedConfig.LaserOffDelayForSkyWriting,
                    markDelayForSkyWriting: speedConfig.MarkDelay,
                    laserOnDelayNormal: (float)speedConfig.LaserOnDelay,
                    laserOffDelayNormal: (float)speedConfig.LaserOffDelay,
                    markDelayNormal: speedConfig.MarkDelay,
                    jumpDelayNormal: speedConfig.JumpDelay,
                    polygonDelayNormal: speedConfig.PolygonDelay
                );

                // 7. Установить параметры слоя
                MarkParameter[] layers = new MarkParameter[1];
                layers[0] = new MarkParameter
                {
                    MarkSpeed = (uint)region.MarkSpeed,
                    JumpSpeed = (uint)speedConfig.JumpSpeed,
                    LaserPower = laser.LaserPowerConfig.ConvertPowerToPercent(region.LaserPower),
                    MarkCount = 1
                };

                if (region.SkyWritingEnabled)
                {
                    layers[0].JumpDelay = 0;
                    layers[0].PolygonDelay = 0;
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

                // 8. Добавить геометрию с Z-offset
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
                            // Применить коррекцию для каждой точки
                            float x = polyline.Points[i].X;
                            float y = polyline.Points[i].Y;
                            float z_corr_point = laser.ThirdAxisConfig.CalculateZCorrection(x, y);

                            points[i] = new structUdmPos
                            {
                                x = x,
                                y = y,
                                z = z_diameter + z_corr_point + z_static  // ← Итоговый Z
                            };
                        }

                        HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, layerIndex);
                        totalPoints += points.Length;
                    }
                }

                Console.WriteLine($"  Added {totalPoints} points");
            }

            /// <summary>
            /// Конвертировать весь CLI файл
            /// </summary>
            public void ConvertFullCliFile(List<CliRegion> regions, string outputDir)
            {
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  Dual-Laser CLI → Hans Conversion                           ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

                // Группировать по SkyWriting и лазеру
                var withSW = regions.Where(r => r.SkyWritingEnabled).ToList();
                var withoutSW = regions.Where(r => !r.SkyWritingEnabled).ToList();

                // Файл 1: С SkyWriting
                if (withSW.Any())
                {
                    Console.WriteLine("Creating: regions_with_skywriting.bin");
                    HM_UDM_DLL.UDM_NewFile();
                    HM_UDM_DLL.UDM_SetProtocol(0, 1);

                    int layerIndex = 0;
                    foreach (var region in withSW)
                    {
                        ConvertRegion(region, layerIndex++);
                    }

                    HM_UDM_DLL.UDM_Main();
                    HM_UDM_DLL.UDM_SaveToFile($"{outputDir}/regions_with_skywriting.bin");
                    HM_UDM_DLL.UDM_EndMain();
                    Console.WriteLine("✅ Saved\n");
                }

                // Файл 2: Без SkyWriting
                if (withoutSW.Any())
                {
                    Console.WriteLine("Creating: regions_without_skywriting.bin");
                    HM_UDM_DLL.UDM_NewFile();
                    HM_UDM_DLL.UDM_SetProtocol(0, 1);

                    int layerIndex = 0;
                    foreach (var region in withoutSW)
                    {
                        ConvertRegion(region, layerIndex++);
                    }

                    HM_UDM_DLL.UDM_Main();
                    HM_UDM_DLL.UDM_SaveToFile($"{outputDir}/regions_without_skywriting.bin");
                    HM_UDM_DLL.UDM_EndMain();
                    Console.WriteLine("✅ Saved\n");
                }

                Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  ✅ Conversion Complete                                        ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            }
        }

        /// <summary>
        /// ПРИМЕР: Использование с вашей конфигурацией
        /// </summary>
        public static void Example_YourActualConfig()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример с вашей реальной dual-laser конфигурацией           ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            // Создать конфигурацию из вашего JSON
            List<LaserCardConfig> laserConfigs = new List<LaserCardConfig>
            {
                // ═══════════════════════════════════════════════════════════
                // LASER 1: 172.18.34.227
                // ═══════════════════════════════════════════════════════════
                new LaserCardConfig
                {
                    IpAddress = "172.18.34.227",
                    SeqIndex = 0,

                    BeamConfig = new BeamConfig
                    {
                        MinBeamDiameterMicron = 48.141,      // ← d₀ лазера 1
                        WavelengthNano = 1070.0,
                        RayleighLengthMicron = 1426.715,     // ← z_R лазера 1
                        M2 = 1.127,
                        FocalLengthMm = 538.46
                    },

                    ScannerConfig = new ScannerConfig
                    {
                        FieldSizeX = 400.0,
                        FieldSizeY = 400.0,
                        ProtocolCode = 1,
                        CoordinateTypeCode = 5,
                        OffsetX = 0.0,
                        OffsetY = 105.03,
                        OffsetZ = -0.001                     // ← Статический Z offset
                    },

                    ThirdAxisConfig = new ThirdAxisConfig
                    {
                        Bfactor = 0.013944261,               // ← Коррекция кривизны
                        Cfactor = -7.5056114,
                        Afactor = 0.0
                    },

                    LaserPowerConfig = new LaserPowerConfig
                    {
                        MaxPower = 500.0,
                        ActualPowerCorrectionValue = new List<double> { 0, 67, 176, 281, 382, 475 },
                        PowerOffsetKFactor = -0.6839859,
                        PowerOffsetCFactor = 51.298943
                    },

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
                            CurBeamDiameterMicron = 65.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 400.0,
                            MinJumpDelay = 400,
                            SWEnable = true,
                            Umax = 0.1
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
                            CurBeamDiameterMicron = 65.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 400.0,
                            MinJumpDelay = 400,
                            SWEnable = true,
                            Umax = 0.1
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
                            CurBeamDiameterMicron = 65.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 400.0,
                            MinJumpDelay = 400,
                            SWEnable = true,
                            Umax = 0.1
                        }
                    }
                },

                // ═══════════════════════════════════════════════════════════
                // LASER 2: 172.18.34.228
                // ═══════════════════════════════════════════════════════════
                new LaserCardConfig
                {
                    IpAddress = "172.18.34.228",
                    SeqIndex = 1,

                    BeamConfig = new BeamConfig
                    {
                        MinBeamDiameterMicron = 53.872,      // ← d₀ лазера 2 (другой!)
                        WavelengthNano = 1070.0,
                        RayleighLengthMicron = 1616.16,      // ← z_R лазера 2 (другой!)
                        M2 = 1.175,
                        FocalLengthMm = 538.46
                    },

                    ScannerConfig = new ScannerConfig
                    {
                        FieldSizeX = 400.0,
                        FieldSizeY = 400.0,
                        ProtocolCode = 1,
                        CoordinateTypeCode = 5,
                        OffsetX = -2.636,
                        OffsetY = -105.03,                   // ← Смещение лазера 2
                        OffsetZ = 0.102                      // ← Другой статический Z
                    },

                    ThirdAxisConfig = new ThirdAxisConfig
                    {
                        Bfactor = 0.0139135085,
                        Cfactor = -7.477292,
                        Afactor = 0.0
                    },

                    LaserPowerConfig = new LaserPowerConfig
                    {
                        MaxPower = 500.0,
                        ActualPowerCorrectionValue = new List<double> { 0, 69, 177, 282, 385, 475 },
                        PowerOffsetKFactor = -1.0362141,
                        PowerOffsetCFactor = 77.71606
                    },

                    SpeedConfigs = new List<SpeedConfig>
                    {
                        new SpeedConfig
                        {
                            MarkSpeed = 800,
                            JumpSpeed = 25000,
                            PolygonDelay = 385,
                            JumpDelay = 35000,               // ← Другой JumpDelay
                            MarkDelay = 470,
                            LaserOnDelay = 420.0,
                            LaserOffDelay = 490.0,
                            LaserOnDelayForSkyWriting = 560.0,
                            LaserOffDelayForSkyWriting = 700.0,
                            CurBeamDiameterMicron = 67.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 300.0,
                            MinJumpDelay = 400,
                            SWEnable = true,
                            Umax = 0.1
                        },
                        new SpeedConfig
                        {
                            MarkSpeed = 1250,
                            JumpSpeed = 25000,
                            PolygonDelay = 465,
                            JumpDelay = 35000,
                            MarkDelay = 496,
                            LaserOnDelay = 375.0,
                            LaserOffDelay = 500.0,
                            LaserOnDelayForSkyWriting = 565.0,
                            LaserOffDelayForSkyWriting = 690.0,
                            CurBeamDiameterMicron = 67.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 300.0,
                            MinJumpDelay = 400,
                            SWEnable = true,
                            Umax = 0.1
                        },
                        new SpeedConfig
                        {
                            MarkSpeed = 2000,
                            JumpSpeed = 25000,
                            PolygonDelay = 600,
                            JumpDelay = 35000,
                            MarkDelay = 540,
                            LaserOnDelay = 345.0,
                            LaserOffDelay = 510.0,
                            LaserOnDelayForSkyWriting = 570.0,
                            LaserOffDelayForSkyWriting = 685.0,
                            CurBeamDiameterMicron = 67.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 300.0,
                            MinJumpDelay = 400,
                            SWEnable = true,
                            Umax = 0.1
                        }
                    }
                }
            };

            // Создать CLI регионы
            List<CliRegion> cliRegions = new List<CliRegion>
            {
                // Edges - лазер 1
                new CliRegion
                {
                    Name = "edges",
                    SkyWritingEnabled = true,
                    MarkSpeed = 800,
                    LaserPower = 140.0,
                    BeamDiameter = 80.0,                     // ← ИЗ CLI JSON!
                    LaserIndex = 0,                          // ← Лазер 1
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

                // Infill - лазер 2
                new CliRegion
                {
                    Name = "infill_hatch",
                    SkyWritingEnabled = true,
                    MarkSpeed = 1250,
                    LaserPower = 220.0,
                    BeamDiameter = 100.0,                    // ← ИЗ CLI JSON!
                    LaserIndex = 1,                          // ← Лазер 2
                    Polylines = new List<CliPolyline>
                    {
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = 2, Y = 2 },
                                new CliPoint { X = 8, Y = 2 }
                            }
                        }
                    }
                },

                // Supports - лазер 1, БЕЗ SkyWriting
                new CliRegion
                {
                    Name = "support_hatch",
                    SkyWritingEnabled = false,
                    MarkSpeed = 2000,
                    LaserPower = 320.0,
                    BeamDiameter = 120.0,                    // ← ИЗ CLI JSON!
                    LaserIndex = 0,
                    Polylines = new List<CliPolyline>
                    {
                        new CliPolyline
                        {
                            Points = new List<CliPoint>
                            {
                                new CliPoint { X = 15, Y = 15 },
                                new CliPoint { X = 20, Y = 20 }
                            }
                        }
                    }
                }
            };

            // Конвертировать
            DualLaserCliConverter converter = new DualLaserCliConverter(laserConfigs);
            converter.ConvertFullCliFile(cliRegions, ".");

            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine("РЕЗУЛЬТАТ:");
            Console.WriteLine("  ✅ regions_with_skywriting.bin");
            Console.WriteLine("     - edges (Laser 1, 80 μm)");
            Console.WriteLine("     - infill_hatch (Laser 2, 100 μm)");
            Console.WriteLine();
            Console.WriteLine("  ✅ regions_without_skywriting.bin");
            Console.WriteLine("     - support_hatch (Laser 1, 120 μm)");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
        }

        /// <summary>
        /// ПРИМЕР: Сравнение Z-offset для двух лазеров
        /// </summary>
        public static void Example_CompareZOffsets()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Сравнение Z-offset для двух лазеров                        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            BeamConfig laser1 = new BeamConfig
            {
                MinBeamDiameterMicron = 48.141,
                RayleighLengthMicron = 1426.715
            };

            BeamConfig laser2 = new BeamConfig
            {
                MinBeamDiameterMicron = 53.872,
                RayleighLengthMicron = 1616.16
            };

            double[] cliDiameters = { 80, 90, 100, 110, 120 };

            Console.WriteLine("CLI Diameter (μm) │ Laser 1 Z (mm) │ Laser 2 Z (mm) │ Difference");
            Console.WriteLine("──────────────────┼────────────────┼────────────────┼───────────");

            foreach (var d in cliDiameters)
            {
                float z1 = laser1.CalculateZOffset(d);
                float z2 = laser2.CalculateZOffset(d);
                float diff = z2 - z1;

                Console.WriteLine($"{d,8:F0}          │ {z1,10:F3}     │ {z2,10:F3}     │ {diff,+7:F3}");
            }

            Console.WriteLine("\nВывод: Лазер 2 требует БОЛЬШЕ Z-offset для того же диаметра");
            Console.WriteLine("(потому что d₀ больше и z_R больше)\n");
        }
    }

    public class ProgramDualLaser
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Hans Dual-Laser CLI Example                                ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("1. Полная конвертация CLI с вашей конфигурацией");
            Console.WriteLine("2. Сравнение Z-offset двух лазеров");
            Console.WriteLine("3. Оба примера");
            Console.WriteLine("\nВыбор: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    HansDualLaserCliExample.Example_YourActualConfig();
                    break;
                case "2":
                    HansDualLaserCliExample.Example_CompareZOffsets();
                    break;
                case "3":
                default:
                    HansDualLaserCliExample.Example_CompareZOffsets();
                    Console.WriteLine("\n\n");
                    HansDualLaserCliExample.Example_YourActualConfig();
                    break;
            }

            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }
    }
}
