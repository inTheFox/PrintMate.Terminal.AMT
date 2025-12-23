using System;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Упрощенный расчет Z координаты на основе референсных данных от друга
    /// </summary>
    public static class SimplifiedZCalculation
    {
        /// <summary>
        /// ПРОСТАЯ ЛИНЕЙНАЯ ФОРМУЛА на основе референсных данных
        /// Diameter: 50-249 мкм → Z: -1.19 до -5.17 мм
        /// </summary>
        /// <param name="diameterMicron">Желаемый диаметр пучка в микронах</param>
        /// <returns>Z координата в миллиметрах</returns>
        public static double GetSimpleZ(double diameterMicron)
        {
            // Линейная регрессия по референсным данным (50-249 мкм):
            // Z = -0.016699704 × Diameter - 0.52960205

            const double k = -0.016699704;
            const double b = -0.52960205;

            return k * diameterMicron + b;
        }

        /// <summary>
        /// Тест: сравнение упрощенного расчета с нашими измерениями
        /// </summary>
        public static void TestSimplifiedCalculation()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║      УПРОЩЕННЫЙ РАСЧЕТ Z (на основе референсных данных)              ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            Console.WriteLine("РЕФЕРЕНСНАЯ ФОРМУЛА:");
            Console.WriteLine("  Z = -0.016699704 × Diameter - 0.52960205");
            Console.WriteLine();

            (double requested, double measured)[] measurements =
            {
                (60, 51.0),
                (70, 59.0),
                (80, 68.0),
                (90, 75.8),
                (100, 83.0)
            };

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ТЕСТ: Что получится если использовать упрощенную формулу           │");
            Console.WriteLine("├───────────┬────────────┬──────────────┬───────────────────────────────┤");
            Console.WriteLine("│ Запрошено │ Z (прост.) │ Измерено     │ Комментарий                   │");
            Console.WriteLine("│   (мкм)   │   (мм)     │    (мкм)     │                               │");
            Console.WriteLine("├───────────┼────────────┼──────────────┼───────────────────────────────┤");

            foreach (var (requested, measured) in measurements)
            {
                double zSimple = GetSimpleZ(requested);
                string comment = measured < requested ? "Меньше" : "Больше";

                Console.WriteLine($"│ {requested,9:F1} │ {zSimple,10:F6} │ {measured,12:F1} │ {comment,29} │");
            }

            Console.WriteLine("└───────────┴────────────┴──────────────┴───────────────────────────────┘");
            Console.WriteLine();

            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                      КЛЮЧЕВОЙ ВЫВОД                                   ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Референсные Z координаты ОТРИЦАТЕЛЬНЫЕ!");
            Console.WriteLine();
            Console.WriteLine("Возможные причины несоответствия:");
            Console.WriteLine("  1. Ваши Z координаты положительные (знак перепутан)");
            Console.WriteLine("  2. Используется другая точка отсчета");
            Console.WriteLine("  3. UDM_GetZvalue работает по-другому");
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine("РЕКОМЕНДАЦИЯ:");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("Попробуйте использовать ПРЯМУЮ линейную формулу:");
            Console.WriteLine();
            Console.WriteLine("  float z = (float)SimplifiedZCalculation.GetSimpleZ(beamDiameterMicron);");
            Console.WriteLine();
            Console.WriteLine("Вместо сложных расчетов с Rayleigh Length и полиномами.");
            Console.WriteLine();
        }

        /// <summary>
        /// Создает таблицу соответствия диаметр → Z для быстрого поиска
        /// </summary>
        public static void PrintLookupTable()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║        ТАБЛИЦА СООТВЕТСТВИЯ: Диаметр → Z координата                  ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            Console.WriteLine("│ Диаметр │ Z (мм)      │   │ Диаметр │ Z (мм)      │");
            Console.WriteLine("│  (мкм)  │             │   │  (мкм)  │             │");
            Console.WriteLine("├─────────┼─────────────┼───┼─────────┼─────────────┤");

            for (int d = 50; d <= 120; d += 5)
            {
                double z1 = GetSimpleZ(d);
                double z2 = GetSimpleZ(d + 5);

                if (d + 5 <= 120)
                {
                    Console.WriteLine($"│ {d,7} │ {z1,11:F7} │   │ {d + 5,7} │ {z2,11:F7} │");
                }
                else
                {
                    Console.WriteLine($"│ {d,7} │ {z1,11:F7} │   │         │             │");
                }
            }

            Console.WriteLine("└─────────┴─────────────┴───┴─────────┴─────────────┘");
            Console.WriteLine();
        }
    }
}
