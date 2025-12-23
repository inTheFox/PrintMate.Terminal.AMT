using System;
using Hans.NET;

namespace PrintMateMC.HansFinal
{
    /// <summary>
    /// ОБЪЯСНЕНИЕ: Как одновременно использовать Z для диаметра и Z для коррекции поля
    /// </summary>
    public class HansZCorrectionExplained
    {
        public class BeamConfig
        {
            public double MinBeamDiameterMicron { get; set; }
            public double RayleighLengthMicron { get; set; }

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
            public double Afactor { get; set; }
            public double Bfactor { get; set; }
            public double Cfactor { get; set; }

            /// <summary>
            /// Рассчитать коррекцию кривизны поля для точки (x, y)
            /// </summary>
            public float CalculateFieldCorrection(float x, float y)
            {
                double r = Math.Sqrt(x * x + y * y);
                double z_corr = Afactor * r * r + Bfactor * r + Cfactor;
                return (float)z_corr;
            }
        }

        /// <summary>
        /// ПРИМЕР 1: Визуализация - как складываются Z
        /// </summary>
        public static void Example1_VisualizeCombinedZ()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример 1: Как складываются Z-компоненты                    ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            // Конфигурация Laser 1
            BeamConfig beamConfig = new BeamConfig
            {
                MinBeamDiameterMicron = 48.141,
                RayleighLengthMicron = 1426.715
            };

            ThirdAxisConfig thirdAxis = new ThirdAxisConfig
            {
                Afactor = 0.0,
                Bfactor = 0.013944261,
                Cfactor = -7.5056114
            };

            double staticOffsetZ = -0.001;  // из scannerConfig

            // CLI параметр: нужен диаметр 80 μm
            double cliDiameter = 80.0;

            // 1. Рассчитать Z для диаметра
            float z_diameter = beamConfig.CalculateZOffset(cliDiameter);
            Console.WriteLine($"1️⃣  Z для диаметра {cliDiameter} μm:");
            Console.WriteLine($"    z_diameter = {z_diameter:F3} mm");
            Console.WriteLine($"    (это дефокусировка для получения нужного размера пятна)\n");

            // 2. Рассчитать Z коррекцию для разных точек поля
            Console.WriteLine($"2️⃣  Z коррекция поля (зависит от позиции):\n");

            float[][] testPoints = new float[][]
            {
                new float[] { 0, 0 },       // Центр
                new float[] { 100, 0 },     // Справа от центра
                new float[] { 200, 0 },     // Край справа
                new float[] { 0, 100 },     // Сверху от центра
                new float[] { 141, 141 },   // Угол (r=200)
                new float[] { -200, 0 }     // Край слева
            };

            Console.WriteLine("┌──────────────┬──────────────┬─────────────────┐");
            Console.WriteLine("│ Position     │ r (mm)       │ z_field (mm)    │");
            Console.WriteLine("├──────────────┼──────────────┼─────────────────┤");

            foreach (var point in testPoints)
            {
                float x = point[0];
                float y = point[1];
                float r = (float)Math.Sqrt(x * x + y * y);
                float z_field = thirdAxis.CalculateFieldCorrection(x, y);

                Console.WriteLine($"│ ({x,4:F0}, {y,4:F0}) │ {r,8:F1}     │ {z_field,11:F3}     │");
            }

            Console.WriteLine("└──────────────┴──────────────┴─────────────────┘\n");

            Console.WriteLine("    📊 Видно: чем дальше от центра, тем меньше z_field (меньше отрицательное)\n");

            // 3. Статический offset
            Console.WriteLine($"3️⃣  Статический Z offset:");
            Console.WriteLine($"    z_static = {staticOffsetZ:F3} mm");
            Console.WriteLine($"    (калибровочное смещение для всей системы)\n");

            // 4. ИТОГОВЫЙ Z для каждой точки
            Console.WriteLine($"4️⃣  ИТОГОВЫЙ Z = z_diameter + z_field + z_static:\n");

            Console.WriteLine("┌──────────────┬─────────────┬─────────────┬────────────┬──────────────┐");
            Console.WriteLine("│ Position     │ z_diameter  │ z_field     │ z_static   │ Z TOTAL      │");
            Console.WriteLine("├──────────────┼─────────────┼─────────────┼────────────┼──────────────┤");

            foreach (var point in testPoints)
            {
                float x = point[0];
                float y = point[1];
                float z_field = thirdAxis.CalculateFieldCorrection(x, y);
                float z_total = z_diameter + z_field + (float)staticOffsetZ;

                Console.WriteLine($"│ ({x,4:F0}, {y,4:F0}) │ {z_diameter,7:F3}     │ {z_field,7:F3}     │ {staticOffsetZ,6:F3}     │ {z_total,8:F3}     │");
            }

            Console.WriteLine("└──────────────┴─────────────┴─────────────┴────────────┴──────────────┘\n");

            Console.WriteLine("✅ ВЫВОД: Каждая точка имеет СВОЙ итоговый Z!");
            Console.WriteLine("   - z_diameter одинаковый для всех точек региона (фиксированный диаметр)");
            Console.WriteLine("   - z_field разный для каждой точки (коррекция кривизны)");
            Console.WriteLine("   - z_static одинаковый для всей системы\n");
        }

        /// <summary>
        /// ПРИМЕР 2: Реальный код - как применить в Hans API
        /// </summary>
        public static void Example2_RealCodeUsage()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример 2: Реальный код для Hans API                        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            // Конфигурация
            BeamConfig beamConfig = new BeamConfig
            {
                MinBeamDiameterMicron = 48.141,
                RayleighLengthMicron = 1426.715
            };

            ThirdAxisConfig thirdAxis = new ThirdAxisConfig
            {
                Afactor = 0.0,
                Bfactor = 0.013944261,
                Cfactor = -7.5056114
            };

            double staticOffsetZ = -0.001;

            // CLI параметры для региона "edges"
            double cliDiameter = 80.0;  // edges_laser_beam_diameter

            // Геометрия региона (квадрат)
            float[][] geometryPoints = new float[][]
            {
                new float[] { 0, 0 },
                new float[] { 50, 0 },
                new float[] { 50, 50 },
                new float[] { 0, 50 },
                new float[] { 0, 0 }
            };

            Console.WriteLine("ШАГИ:\n");

            // ШАГ 1: Рассчитать z_diameter ОДИН РАЗ для всего региона
            float z_diameter = beamConfig.CalculateZOffset(cliDiameter);
            Console.WriteLine($"1. Рассчитать z_diameter для региона:");
            Console.WriteLine($"   z_diameter = beamConfig.CalculateZOffset({cliDiameter})");
            Console.WriteLine($"   z_diameter = {z_diameter:F3} mm\n");

            // ШАГ 2: Для КАЖДОЙ точки геометрии рассчитать свой z_total
            Console.WriteLine($"2. Для каждой точки рассчитать z_total:\n");

            structUdmPos[] hansPoints = new structUdmPos[geometryPoints.Length];

            for (int i = 0; i < geometryPoints.Length; i++)
            {
                float x = geometryPoints[i][0];
                float y = geometryPoints[i][1];

                // Рассчитать z_field для этой точки
                float z_field = thirdAxis.CalculateFieldCorrection(x, y);

                // Итоговый Z
                float z_total = z_diameter + z_field + (float)staticOffsetZ;

                // Создать точку для Hans
                hansPoints[i] = new structUdmPos
                {
                    x = x,
                    y = y,
                    z = z_total  // ← ИТОГОВЫЙ Z (разный для каждой точки!)
                };

                Console.WriteLine($"   Point[{i}]: ({x,5:F1}, {y,5:F1})");
                Console.WriteLine($"      z_field = {z_field:F3} mm");
                Console.WriteLine($"      z_total = {z_diameter:F3} + {z_field:F3} + {staticOffsetZ:F3} = {z_total:F3} mm");
                Console.WriteLine();
            }

            // ШАГ 3: Добавить геометрию в Hans
            Console.WriteLine($"3. Добавить геометрию в Hans:");
            Console.WriteLine($"   HM_UDM_DLL.UDM_AddPolyline3D(hansPoints, {hansPoints.Length}, layerIndex);\n");

            // Для наглядности выведем массив
            Console.WriteLine("   Результат - массив structUdmPos:");
            Console.WriteLine("   ┌──────────────┬──────────────┬──────────────┐");
            Console.WriteLine("   │ X (mm)       │ Y (mm)       │ Z (mm)       │");
            Console.WriteLine("   ├──────────────┼──────────────┼──────────────┤");
            foreach (var p in hansPoints)
            {
                Console.WriteLine($"   │ {p.x,8:F3}     │ {p.y,8:F3}     │ {p.z,8:F3}     │");
            }
            Console.WriteLine("   └──────────────┴──────────────┴──────────────┘\n");

            Console.WriteLine("✅ РЕЗУЛЬТАТ:");
            Console.WriteLine("   - Все точки имеют правильный диаметр луча (80 μm)");
            Console.WriteLine("   - Каждая точка компенсирует кривизну поля своим Z\n");
        }

        /// <summary>
        /// ПРИМЕР 3: Почему нужно складывать Z
        /// </summary>
        public static void Example3_WhyAddZComponents()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример 3: Почему нужно СКЛАДЫВАТЬ Z-компоненты?            ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("🎯 ФИЗИЧЕСКИЙ СМЫСЛ:\n");

            Console.WriteLine("1️⃣  z_diameter (дефокусировка):");
            Console.WriteLine("   - Перемещает фокус ВВЕРХ/ВНИЗ по оси Z");
            Console.WriteLine("   - Управляет размером пятна луча");
            Console.WriteLine("   - Пример: z_diameter = +1.894 mm → фокус на 1.894 мм НИЖЕ");
            Console.WriteLine("            → диаметр пятна = 80 μm\n");

            Console.WriteLine("2️⃣  z_field (коррекция кривизны):");
            Console.WriteLine("   - Компенсирует искажения F-theta линзы");
            Console.WriteLine("   - У центра поля одна коррекция, у края другая");
            Console.WriteLine("   - Пример: z_field(центр) = -7.506 mm");
            Console.WriteLine("            z_field(край)  = -4.720 mm");
            Console.WriteLine("   - Разница ~2.8 mm компенсирует кривизну!\n");

            Console.WriteLine("3️⃣  z_static (калибровка):");
            Console.WriteLine("   - Общее смещение для всей системы");
            Console.WriteLine("   - Устанавливается при калибровке машины");
            Console.WriteLine("   - Пример: z_static = -0.001 mm\n");

            Console.WriteLine("─────────────────────────────────────────────────────────────────\n");

            Console.WriteLine("❓ ЧТО БУДЕТ, ЕСЛИ НЕ СКЛАДЫВАТЬ?\n");

            BeamConfig beamConfig = new BeamConfig
            {
                MinBeamDiameterMicron = 48.141,
                RayleighLengthMicron = 1426.715
            };

            ThirdAxisConfig thirdAxis = new ThirdAxisConfig
            {
                Afactor = 0.0,
                Bfactor = 0.013944261,
                Cfactor = -7.5056114
            };

            float z_diameter = beamConfig.CalculateZOffset(80.0);
            float z_field_center = thirdAxis.CalculateFieldCorrection(0, 0);
            float z_field_edge = thirdAxis.CalculateFieldCorrection(200, 0);

            Console.WriteLine("ВАРИАНТ А: Использовать ТОЛЬКО z_diameter (НЕПРАВИЛЬНО ❌)");
            Console.WriteLine($"   structUdmPos.z = {z_diameter:F3} mm (везде одинаковый)\n");
            Console.WriteLine("   🔴 ПРОБЛЕМА:");
            Console.WriteLine("      - Диаметр правильный (80 μm) ✓");
            Console.WriteLine("      - НО кривизна поля НЕ скомпенсирована ✗");
            Console.WriteLine("      - У края поля фокус будет на 2.8 мм выше, чем у центра!");
            Console.WriteLine("      - Качество печати неравномерное\n");

            Console.WriteLine("ВАРИАНТ Б: Использовать ТОЛЬКО z_field (НЕПРАВИЛЬНО ❌)");
            Console.WriteLine($"   structUdmPos.z = z_field (разный для каждой точки)\n");
            Console.WriteLine("   🔴 ПРОБЛЕМА:");
            Console.WriteLine("      - Кривизна скомпенсирована ✓");
            Console.WriteLine("      - НО диаметр неправильный ✗");
            Console.WriteLine($"      - Получится d₀ = {beamConfig.MinBeamDiameterMicron:F1} μm вместо 80 μm!");
            Console.WriteLine("      - Слишком малый диаметр → слишком высокая плотность энергии\n");

            Console.WriteLine("ВАРИАНТ В: СКЛАДЫВАТЬ z_diameter + z_field (ПРАВИЛЬНО ✅)");
            float z_total_center = z_diameter + z_field_center;
            float z_total_edge = z_diameter + z_field_edge;
            Console.WriteLine($"   Центр: z = {z_diameter:F3} + {z_field_center:F3} = {z_total_center:F3} mm");
            Console.WriteLine($"   Край:  z = {z_diameter:F3} + {z_field_edge:F3} = {z_total_edge:F3} mm\n");
            Console.WriteLine("   ✅ РЕЗУЛЬТАТ:");
            Console.WriteLine("      - Диаметр правильный (80 μm) везде ✓");
            Console.WriteLine("      - Кривизна скомпенсирована ✓");
            Console.WriteLine("      - Качество печати равномерное по всему полю ✓\n");

            Console.WriteLine("─────────────────────────────────────────────────────────────────\n");

            Console.WriteLine("💡 АНАЛОГИЯ:\n");
            Console.WriteLine("   Представьте, что печатаете на криво лежащем листе бумаги:");
            Console.WriteLine("   - z_diameter - это высота, на которую поднимаете ручку");
            Console.WriteLine("                  (чтобы линия была нужной толщины)");
            Console.WriteLine("   - z_field    - это коррекция для кривизны листа");
            Console.WriteLine("                  (чтобы везде касаться с одинаковой силой)");
            Console.WriteLine("   - z_total    - итоговая высота ручки над РОВНЫМ столом\n");

            Console.WriteLine("   Нужно учитывать ОБА фактора одновременно!\n");
        }

        /// <summary>
        /// ПРИМЕР 4: Упрощенный helper метод
        /// </summary>
        public static void Example4_HelperMethod()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример 4: Готовый helper метод                             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("/// <summary>");
            Console.WriteLine("/// Рассчитать итоговый Z для точки с учетом диаметра и коррекции");
            Console.WriteLine("/// </summary>");
            Console.WriteLine("public static float CalculateTotalZ(");
            Console.WriteLine("    float x, float y,                    // Координаты точки");
            Console.WriteLine("    double cliDiameter,                  // Целевой диаметр из CLI (μm)");
            Console.WriteLine("    BeamConfig beamConfig,               // Оптика лазера");
            Console.WriteLine("    ThirdAxisConfig thirdAxis,           // Коррекция кривизны");
            Console.WriteLine("    double staticOffsetZ)                // Статический offset");
            Console.WriteLine("{");
            Console.WriteLine("    // 1. Z для диаметра (одинаковый для всего региона)");
            Console.WriteLine("    float z_diameter = beamConfig.CalculateZOffset(cliDiameter);");
            Console.WriteLine();
            Console.WriteLine("    // 2. Z коррекция поля (индивидуальный для точки)");
            Console.WriteLine("    float z_field = thirdAxis.CalculateFieldCorrection(x, y);");
            Console.WriteLine();
            Console.WriteLine("    // 3. Сложить все компоненты");
            Console.WriteLine("    return z_diameter + z_field + (float)staticOffsetZ;");
            Console.WriteLine("}\n");

            Console.WriteLine("ИСПОЛЬЗОВАНИЕ:\n");
            Console.WriteLine("foreach (var point in geometryPoints)");
            Console.WriteLine("{");
            Console.WriteLine("    structUdmPos hansPoint = new structUdmPos");
            Console.WriteLine("    {");
            Console.WriteLine("        x = point.X,");
            Console.WriteLine("        y = point.Y,");
            Console.WriteLine("        z = CalculateTotalZ(");
            Console.WriteLine("                point.X, point.Y,");
            Console.WriteLine("                cliDiameter: 80.0,");
            Console.WriteLine("                beamConfig,");
            Console.WriteLine("                thirdAxis,");
            Console.WriteLine("                staticOffsetZ)");
            Console.WriteLine("    };");
            Console.WriteLine();
            Console.WriteLine("    hansPoints.Add(hansPoint);");
            Console.WriteLine("}\n");
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Hans Z-Correction: Как использовать диаметр + коррекцию    ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("Выберите пример:");
            Console.WriteLine("1. Визуализация - как складываются Z");
            Console.WriteLine("2. Реальный код для Hans API");
            Console.WriteLine("3. Почему нужно складывать Z");
            Console.WriteLine("4. Готовый helper метод");
            Console.WriteLine("5. Все примеры");
            Console.WriteLine("\nВыбор: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Example1_VisualizeCombinedZ();
                    break;
                case "2":
                    Example2_RealCodeUsage();
                    break;
                case "3":
                    Example3_WhyAddZComponents();
                    break;
                case "4":
                    Example4_HelperMethod();
                    break;
                case "5":
                default:
                    Example1_VisualizeCombinedZ();
                    Console.WriteLine("\n" + new string('═', 65) + "\n");
                    Example2_RealCodeUsage();
                    Console.WriteLine("\n" + new string('═', 65) + "\n");
                    Example3_WhyAddZComponents();
                    Console.WriteLine("\n" + new string('═', 65) + "\n");
                    Example4_HelperMethod();
                    break;
            }

            Console.WriteLine("\n\nНажмите любую клавишу...");
            Console.ReadKey();
        }
    }
}
