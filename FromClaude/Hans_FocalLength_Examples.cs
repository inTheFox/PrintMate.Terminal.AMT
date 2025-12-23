using System;
using System.Collections.Generic;
using System.Linq;

namespace PrintMateMC.HansFinal
{
    /// <summary>
    /// Примеры использования focalLengthMm
    /// </summary>
    public class HansFocalLengthExamples
    {
        public class BeamConfig
        {
            public double MinBeamDiameterMicron { get; set; }
            public double WavelengthNano { get; set; }
            public double RayleighLengthMicron { get; set; }
            public double M2 { get; set; }
            public double FocalLengthMm { get; set; }  // ← ИСПОЛЬЗУЕМ!

            public float CalculateZOffset(double targetDiameterMicron)
            {
                if (targetDiameterMicron <= MinBeamDiameterMicron)
                    return 0.0f;

                double ratio = targetDiameterMicron / MinBeamDiameterMicron;
                double z_micron = RayleighLengthMicron * Math.Sqrt(ratio * ratio - 1.0);
                return (float)(z_micron / 1000.0);
            }
        }

        public class ScannerConfig
        {
            public double FieldSizeX { get; set; }
            public double FieldSizeY { get; set; }
        }

        /// <summary>
        /// Валидатор координат с использованием focalLengthMm
        /// </summary>
        public class ScannerValidator
        {
            private readonly double focalLengthMm;
            private readonly double fieldSizeX;
            private readonly double fieldSizeY;

            public ScannerValidator(BeamConfig beamConfig, ScannerConfig scannerConfig)
            {
                this.focalLengthMm = beamConfig.FocalLengthMm;
                this.fieldSizeX = scannerConfig.FieldSizeX;
                this.fieldSizeY = scannerConfig.FieldSizeY;
            }

            /// <summary>
            /// Проверить, что точка в пределах поля
            /// </summary>
            public bool IsPointValid(float x, float y, bool printWarning = true)
            {
                bool valid = true;

                if (Math.Abs(x) > fieldSizeX / 2.0)
                {
                    if (printWarning)
                        Console.WriteLine($"  ⚠️ X={x:F1} mm вне поля (max ±{fieldSizeX / 2.0:F1} mm)");
                    valid = false;
                }

                if (Math.Abs(y) > fieldSizeY / 2.0)
                {
                    if (printWarning)
                        Console.WriteLine($"  ⚠️ Y={y:F1} mm вне поля (max ±{fieldSizeY / 2.0:F1} mm)");
                    valid = false;
                }

                return valid;
            }

            /// <summary>
            /// Рассчитать угол отклонения для координаты
            /// </summary>
            public double CalculateAngle(float coordinate_mm)
            {
                return coordinate_mm / focalLengthMm;  // радианы
            }

            /// <summary>
            /// Рассчитать максимальный угол отклонения
            /// </summary>
            public double GetMaxAngle()
            {
                return (fieldSizeX / 2.0) / focalLengthMm;
            }

            /// <summary>
            /// Рассчитать теоретическое разрешение системы
            /// </summary>
            public double CalculateResolution(int galvoBits = 16)
            {
                double theta_max = GetMaxAngle();
                int steps = (int)Math.Pow(2, galvoBits);
                double theta_min = theta_max / steps;
                double resolution = focalLengthMm * theta_min;
                return resolution * 1000.0;  // μm
            }

            /// <summary>
            /// Проверить все точки геометрии
            /// </summary>
            public bool ValidateGeometry(List<CliPoint> points)
            {
                bool allValid = true;
                int invalidCount = 0;

                foreach (var point in points)
                {
                    if (!IsPointValid(point.X, point.Y, false))
                    {
                        invalidCount++;
                        allValid = false;
                    }
                }

                if (!allValid)
                {
                    Console.WriteLine($"  ❌ {invalidCount}/{points.Count} точек вне поля!");
                }

                return allValid;
            }
        }

        /// <summary>
        /// Расширенная проверка BeamConfig с использованием focalLengthMm
        /// </summary>
        public class AdvancedBeamConfig : BeamConfig
        {
            /// <summary>
            /// Рассчитать теоретическую Rayleigh length
            /// </summary>
            public double CalculateTheoreticalRayleighLength()
            {
                double lambda_micron = WavelengthNano / 1000.0;
                double zR = Math.PI * Math.Pow(MinBeamDiameterMicron, 2) * M2
                            / (4.0 * lambda_micron);
                return zR;
            }

            /// <summary>
            /// Проверить соответствие теории
            /// </summary>
            public void ValidateRayleighLength()
            {
                double theoretical = CalculateTheoreticalRayleighLength();
                double configured = RayleighLengthMicron;
                double difference = configured - theoretical;
                double percentDiff = (difference / theoretical) * 100.0;

                Console.WriteLine($"Rayleigh Length Validation:");
                Console.WriteLine($"  Theoretical: {theoretical:F1} μm");
                Console.WriteLine($"  Configured:  {configured:F1} μm");
                Console.WriteLine($"  Difference:  {difference:F1} μm ({percentDiff:+F1}%)");

                if (Math.Abs(percentDiff) > 30)
                {
                    Console.WriteLine($"  ⚠️ WARNING: Large difference! Check calibration.");
                }
                else
                {
                    Console.WriteLine($"  ✅ OK: Within reasonable range (experimental calibration)");
                }
            }

            /// <summary>
            /// Рассчитать эффективную Rayleigh length с учетом угла
            /// (для точек далеко от центра)
            /// </summary>
            public double CalculateEffectiveRayleighLength(float x, float y)
            {
                // Расстояние от центра
                double r = Math.Sqrt(x * x + y * y);

                // Угол падения луча
                double theta = r / FocalLengthMm;

                // Коррекционный фактор (приближенная формула)
                // Луч падает под углом → увеличивается эффективная z_R
                double correction = 1.0 + 0.5 * Math.Pow(theta, 2);

                return RayleighLengthMicron * correction;
            }
        }

        public class CliPoint
        {
            public float X { get; set; }
            public float Y { get; set; }
        }

        /// <summary>
        /// ПРИМЕР 1: Валидация координат
        /// </summary>
        public static void Example1_ValidateCoordinates()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример 1: Валидация координат с focalLengthMm             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            BeamConfig beamConfig = new BeamConfig
            {
                MinBeamDiameterMicron = 48.141,
                RayleighLengthMicron = 1426.715,
                FocalLengthMm = 538.46  // ← ИСПОЛЬЗУЕМ!
            };

            ScannerConfig scannerConfig = new ScannerConfig
            {
                FieldSizeX = 400.0,
                FieldSizeY = 400.0
            };

            ScannerValidator validator = new ScannerValidator(beamConfig, scannerConfig);

            Console.WriteLine($"Конфигурация:");
            Console.WriteLine($"  Focal length: {beamConfig.FocalLengthMm:F2} mm");
            Console.WriteLine($"  Field size: {scannerConfig.FieldSizeX:F0} × {scannerConfig.FieldSizeY:F0} mm");
            Console.WriteLine($"  Max angle: {validator.GetMaxAngle():F4} rad ({validator.GetMaxAngle() * 180 / Math.PI:F1}°)");
            Console.WriteLine($"  Resolution (16-bit): {validator.CalculateResolution(16):F2} μm\n");

            // Тестовые точки
            (float x, float y, string desc)[] testPoints = new[]
            {
                (0f, 0f, "Центр поля"),
                (100f, 100f, "Внутри поля"),
                (200f, 0f, "Край поля (X)"),
                (0f, 200f, "Край поля (Y)"),
                (250f, 0f, "ВНЕ поля (X)"),
                (141f, 141f, "Угол поля (r=200)"),
                (150f, 150f, "ВНЕ поля (угол)")
            };

            Console.WriteLine("Проверка точек:\n");
            Console.WriteLine("┌──────────────┬────────────┬──────────────┬─────────┐");
            Console.WriteLine("│ Position     │ r (mm)     │ Angle (°)    │ Valid?  │");
            Console.WriteLine("├──────────────┼────────────┼──────────────┼─────────┤");

            foreach (var (x, y, desc) in testPoints)
            {
                double r = Math.Sqrt(x * x + y * y);
                double angle_x = validator.CalculateAngle(x);
                double angle_y = validator.CalculateAngle(y);
                double angle_total = Math.Sqrt(angle_x * angle_x + angle_y * angle_y) * 180 / Math.PI;
                bool valid = validator.IsPointValid(x, y, false);

                string validStr = valid ? "✅" : "❌";

                Console.WriteLine($"│ ({x,4:F0}, {y,4:F0}) │ {r,6:F1}     │ {angle_total,8:F2}     │ {validStr,3}     │");
            }

            Console.WriteLine("└──────────────┴────────────┴──────────────┴─────────┘\n");
        }

        /// <summary>
        /// ПРИМЕР 2: Проверка Rayleigh length
        /// </summary>
        public static void Example2_ValidateRayleighLength()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример 2: Проверка Rayleigh Length                         ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            // Laser 1
            Console.WriteLine("📍 LASER 1 (172.18.34.227):\n");
            AdvancedBeamConfig laser1 = new AdvancedBeamConfig
            {
                MinBeamDiameterMicron = 48.141,
                WavelengthNano = 1070.0,
                RayleighLengthMicron = 1426.715,
                M2 = 1.127,
                FocalLengthMm = 538.46
            };
            laser1.ValidateRayleighLength();

            // Эффективная z_R на разных расстояниях
            Console.WriteLine($"\n  Эффективная z_R:");
            float[] positions = { 0, 100, 200 };
            foreach (float r in positions)
            {
                double zR_eff = laser1.CalculateEffectiveRayleighLength(r, 0);
                double increase = ((zR_eff / laser1.RayleighLengthMicron) - 1.0) * 100;
                Console.WriteLine($"    r={r,3:F0} mm: {zR_eff:F1} μm (+{increase:F1}%)");
            }

            Console.WriteLine("\n" + new string('─', 65) + "\n");

            // Laser 2
            Console.WriteLine("📍 LASER 2 (172.18.34.228):\n");
            AdvancedBeamConfig laser2 = new AdvancedBeamConfig
            {
                MinBeamDiameterMicron = 53.872,
                WavelengthNano = 1070.0,
                RayleighLengthMicron = 1616.16,
                M2 = 1.175,
                FocalLengthMm = 538.46
            };
            laser2.ValidateRayleighLength();

            Console.WriteLine($"\n  Эффективная z_R:");
            foreach (float r in positions)
            {
                double zR_eff = laser2.CalculateEffectiveRayleighLength(r, 0);
                double increase = ((zR_eff / laser2.RayleighLengthMicron) - 1.0) * 100;
                Console.WriteLine($"    r={r,3:F0} mm: {zR_eff:F1} μm (+{increase:F1}%)");
            }

            Console.WriteLine("\n💡 ВЫВОД:");
            Console.WriteLine("   У края поля (r=200) эффективная z_R больше на ~5%");
            Console.WriteLine("   Это означает, что глубина фокуса немного увеличивается\n");
        }

        /// <summary>
        /// ПРИМЕР 3: Улучшенный расчет Z с учетом позиции
        /// </summary>
        public static void Example3_ImprovedZCalculation()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример 3: Улучшенный расчет Z с учетом focalLength        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            AdvancedBeamConfig beamConfig = new AdvancedBeamConfig
            {
                MinBeamDiameterMicron = 48.141,
                WavelengthNano = 1070.0,
                RayleighLengthMicron = 1426.715,
                M2 = 1.127,
                FocalLengthMm = 538.46
            };

            double cliDiameter = 80.0;  // μm

            Console.WriteLine($"CLI diameter: {cliDiameter} μm\n");

            // Точки на разных расстояниях от центра
            (float x, float y, string location)[] positions = new[]
            {
                (0f, 0f, "Центр"),
                (100f, 0f, "r=100"),
                (141f, 141f, "r=200 (угол)"),
                (200f, 0f, "r=200 (край)")
            };

            Console.WriteLine("┌──────────────┬────────────┬─────────────┬─────────────┬─────────────┐");
            Console.WriteLine("│ Position     │ r (mm)     │ z_R (μm)    │ z_R_eff (μm)│ Z-offset    │");
            Console.WriteLine("├──────────────┼────────────┼─────────────┼─────────────┼─────────────┤");

            foreach (var (x, y, location) in positions)
            {
                double r = Math.Sqrt(x * x + y * y);

                // Обычный расчет (без учета позиции)
                float z_standard = beamConfig.CalculateZOffset(cliDiameter);

                // Улучшенный расчет (с учетом эффективной z_R)
                double zR_eff = beamConfig.CalculateEffectiveRayleighLength(x, y);
                double ratio = cliDiameter / beamConfig.MinBeamDiameterMicron;
                double z_improved = (zR_eff * Math.Sqrt(ratio * ratio - 1.0)) / 1000.0;

                Console.WriteLine($"│ ({x,4:F0}, {y,4:F0}) │ {r,6:F1}     │ " +
                                $"{beamConfig.RayleighLengthMicron,7:F1}     │ " +
                                $"{zR_eff,7:F1}     │ " +
                                $"{z_improved:F3} mm    │");
            }

            Console.WriteLine("└──────────────┴────────────┴─────────────┴─────────────┴─────────────┘\n");

            Console.WriteLine("💡 ВЫВОД:");
            Console.WriteLine("   На краю поля Z-offset нужно увеличить на ~5%");
            Console.WriteLine("   (в реальности Hans firmware может это учитывать автоматически)\n");
        }

        /// <summary>
        /// ПРИМЕР 4: Практическое применение - валидация CLI файла
        /// </summary>
        public static void Example4_ValidateCliFile()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример 4: Валидация CLI файла                              ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            BeamConfig beamConfig = new BeamConfig
            {
                MinBeamDiameterMicron = 48.141,
                RayleighLengthMicron = 1426.715,
                FocalLengthMm = 538.46
            };

            ScannerConfig scannerConfig = new ScannerConfig
            {
                FieldSizeX = 400.0,
                FieldSizeY = 400.0
            };

            ScannerValidator validator = new ScannerValidator(beamConfig, scannerConfig);

            // Симуляция геометрии из CLI
            List<CliPoint> edgesGeometry = new List<CliPoint>
            {
                new CliPoint { X = 0, Y = 0 },
                new CliPoint { X = 180, Y = 0 },
                new CliPoint { X = 180, Y = 180 },
                new CliPoint { X = 0, Y = 180 },
                new CliPoint { X = 0, Y = 0 }
            };

            List<CliPoint> infillGeometry = new List<CliPoint>();
            for (int i = 0; i < 20; i++)
            {
                infillGeometry.Add(new CliPoint { X = 10 + i * 8, Y = 10 });
                infillGeometry.Add(new CliPoint { X = 10 + i * 8, Y = 170 });
            }

            Console.WriteLine("Валидация геометрии из CLI:\n");

            Console.WriteLine("1. Edges (контур):");
            Console.WriteLine($"   Точек: {edgesGeometry.Count}");
            bool edgesValid = validator.ValidateGeometry(edgesGeometry);
            Console.WriteLine($"   Результат: {(edgesValid ? "✅ OK" : "❌ FAILED")}\n");

            Console.WriteLine("2. Infill (заполнение):");
            Console.WriteLine($"   Точек: {infillGeometry.Count}");
            bool infillValid = validator.ValidateGeometry(infillGeometry);
            Console.WriteLine($"   Результат: {(infillValid ? "✅ OK" : "❌ FAILED")}\n");

            // Попробуем добавить точку вне поля
            List<CliPoint> invalidGeometry = new List<CliPoint>
            {
                new CliPoint { X = 0, Y = 0 },
                new CliPoint { X = 250, Y = 0 },  // ← ВНЕ ПОЛЯ!
                new CliPoint { X = 0, Y = 0 }
            };

            Console.WriteLine("3. Invalid geometry (точка вне поля):");
            Console.WriteLine($"   Точек: {invalidGeometry.Count}");
            validator.ValidateGeometry(invalidGeometry);
            Console.WriteLine($"   Результат: ❌ FAILED (как и ожидалось)\n");

            Console.WriteLine("✅ Валидация завершена!");
            Console.WriteLine("   Можно использовать focalLengthMm для проверки CLI перед конвертацией\n");
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Hans focalLengthMm - Примеры использования                 ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("Выберите пример:");
            Console.WriteLine("1. Валидация координат");
            Console.WriteLine("2. Проверка Rayleigh Length");
            Console.WriteLine("3. Улучшенный расчет Z");
            Console.WriteLine("4. Валидация CLI файла");
            Console.WriteLine("5. Все примеры");
            Console.WriteLine("\nВыбор: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Example1_ValidateCoordinates();
                    break;
                case "2":
                    Example2_ValidateRayleighLength();
                    break;
                case "3":
                    Example3_ImprovedZCalculation();
                    break;
                case "4":
                    Example4_ValidateCliFile();
                    break;
                case "5":
                default:
                    Example1_ValidateCoordinates();
                    Console.WriteLine("\n" + new string('═', 65) + "\n");
                    Example2_ValidateRayleighLength();
                    Console.WriteLine("\n" + new string('═', 65) + "\n");
                    Example3_ImprovedZCalculation();
                    Console.WriteLine("\n" + new string('═', 65) + "\n");
                    Example4_ValidateCliFile();
                    break;
            }

            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }
    }
}
