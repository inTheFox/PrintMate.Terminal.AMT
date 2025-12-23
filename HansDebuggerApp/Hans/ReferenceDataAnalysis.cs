using System;
using System.Collections.Generic;
using System.Linq;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Анализ референсных данных от друга и сравнение с нашими расчетами
    /// </summary>
    public static class ReferenceDataAnalysis
    {
        /// <summary>
        /// Референсные данные: диаметр → Z координата (от друга)
        /// </summary>
        private static readonly Dictionary<int, double> ReferenceData = new Dictionary<int, double>
        {
            {50, -1.1944805}, {51, -1.2111802}, {52, -1.2278804}, {53, -1.2445796}, {54, -1.2612796},
            {55, -1.2779793}, {56, -1.2946789}, {57, -1.3113786}, {58, -1.3280783}, {59, -1.344778},
            {60, -1.3614776}, {61, -1.3781778}, {62, -1.394877}, {63, -1.4115771}, {64, -1.4282768},
            {65, -1.4449763}, {66, -1.461676}, {67, -1.4783757}, {68, -1.4950753}, {69, -1.511775},
            {70, -1.5284752}, {71, -1.5451744}, {72, -1.5618745}, {73, -1.5785742}, {74, -1.5952739},
            {75, -1.6119734}, {76, -1.6286731}, {77, -1.6453727}, {78, -1.6620724}, {79, -1.6787726},
            {80, -1.6954722}, {81, -1.7121719}, {82, -1.7288716}, {83, -1.7455713}, {84, -1.7622709},
            {85, -1.7789706}, {86, -1.7956702}, {87, -1.8123698}, {88, -1.82907}, {89, -1.8457696},
            {90, -1.8624693}, {91, -1.879169}, {92, -1.8958687}, {93, -1.9125683}, {94, -1.929268},
            {95, -1.9459677}, {96, -1.9626673}, {97, -1.9793674}, {98, -1.996067}, {99, -2.0127666},
            {100, -2.0294664}, {101, -2.046166}, {102, -2.0628657}, {103, -2.0795653}, {104, -2.096265},
            {105, -2.1129646}, {106, -2.129665}, {107, -2.1463645}, {108, -2.1630642}, {109, -2.1797638},
            {110, -2.1964633}, {111, -2.2131631}, {112, -2.2298627}, {113, -2.246563}, {114, -2.263262},
            {115, -2.2799623}, {116, -2.2966614}, {117, -2.3133616}, {118, -2.3300612}, {119, -2.3467607},
            {120, -2.3634605}
        };

        public static void AnalyzeReferenceData()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║       АНАЛИЗ РЕФЕРЕНСНЫХ ДАННЫХ (от друга)                            ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // 1. Проверяем линейность референсных данных
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ШАГ 1: АНАЛИЗ РЕФЕРЕНСНЫХ ДАННЫХ                                    │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");

            var refList = ReferenceData.OrderBy(x => x.Key).Take(10).ToList();

            Console.WriteLine("│ Диаметр │ Z координата │ Разница Z │ Шаг на 1 мкм │");
            Console.WriteLine("│  (мкм)  │    (мм)      │   (мм)    │     (мм)     │");
            Console.WriteLine("├─────────┼──────────────┼───────────┼──────────────┤");

            double? prevZ = null;
            double sumStep = 0;
            int stepCount = 0;

            foreach (var item in refList)
            {
                double diffZ = prevZ.HasValue ? item.Value - prevZ.Value : 0;
                if (prevZ.HasValue)
                {
                    sumStep += Math.Abs(diffZ);
                    stepCount++;
                }

                Console.WriteLine($"│ {item.Key,7} │ {item.Value,12:F7} │ {diffZ,9:F7} │ {(prevZ.HasValue ? Math.Abs(diffZ).ToString("F7") : "-"),12} │");
                prevZ = item.Value;
            }

            Console.WriteLine("└─────────┴──────────────┴───────────┴──────────────┘");
            Console.WriteLine();

            double avgStep = sumStep / stepCount;
            Console.WriteLine($"Средний шаг Z на 1 мкм диаметра: {avgStep:F7} мм");
            Console.WriteLine();

            // 2. Линейная регрессия референсных данных
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ШАГ 2: ЛИНЕЙНАЯ РЕГРЕССИЯ РЕФЕРЕНСНЫХ ДАННЫХ                        │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");

            int n = ReferenceData.Count;
            double sumD = 0, sumZ = 0, sumDZ = 0, sumD2 = 0;

            foreach (var kvp in ReferenceData)
            {
                double d = kvp.Key;
                double z = kvp.Value;
                sumD += d;
                sumZ += z;
                sumDZ += d * z;
                sumD2 += d * d;
            }

            double k_ref = (n * sumDZ - sumD * sumZ) / (n * sumD2 - sumD * sumD);
            double b_ref = (sumZ - k_ref * sumD) / n;

            Console.WriteLine($"│ Формула: Z = k × Diameter + b");
            Console.WriteLine($"│   k = {k_ref:F10}");
            Console.WriteLine($"│   b = {b_ref:F10}");
            Console.WriteLine("│");
            Console.WriteLine($"│ Формула: Z = {k_ref:F10} × D + {b_ref:F10}");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            // 3. Сравнение с нашими реальными измерениями
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ШАГ 3: ЧТО ДОЛЖНЫ БЫЛИ ПОЛУЧИТЬ ПО РЕФЕРЕНСНЫМ ДАННЫМ              │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");

            (int requested, double measured)[] ourMeasurements =
            {
                (60, 51.0),
                (70, 59.0),
                (80, 68.0),
                (90, 75.8),
                (100, 83.0)
            };

            Console.WriteLine("│ Запросили │ Получили │ Z (референс) │ Должно быть │ Ошибка    │");
            Console.WriteLine("│   (мкм)   │  (мкм)   │    (мм)      │    (мкм)    │   (мкм)   │");
            Console.WriteLine("├───────────┼──────────┼──────────────┼─────────────┼───────────┤");

            foreach (var (requested, measured) in ourMeasurements)
            {
                double zRef = ReferenceData.ContainsKey(requested) ? ReferenceData[requested] : k_ref * requested + b_ref;

                // Обратный расчет: какой диаметр соответствует этой Z?
                // Z = k × D + b  →  D = (Z - b) / k
                double shouldBeDiameter = (zRef - b_ref) / k_ref;
                double error = measured - shouldBeDiameter;

                Console.WriteLine($"│ {requested,9} │ {measured,8:F1} │ {zRef,12:F7} │ {shouldBeDiameter,11:F1} │ {error,9:F1} │");
            }

            Console.WriteLine("└───────────┴──────────────┴──────────────┴─────────────┴───────────┘");
            Console.WriteLine();

            // 4. ГЛАВНЫЙ ВЫВОД: Сравнение наших Z с референсными
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║ ШАГ 4: СРАВНЕНИЕ НАШИХ Z С РЕФЕРЕНСНЫМИ                               ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Загружаем конфигурацию чтобы посчитать наши Z
            var config = RunDiameterTests.LoadConfiguration();
            if (config == null)
            {
                Console.WriteLine("❌ Не удалось загрузить конфигурацию");
                return;
            }

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ Диаметр │ Наша Z (расчет) │ Референсная Z │ Разница │ Коэфф.  │");
            Console.WriteLine("│  (мкм)  │      (мм)       │     (мм)      │  (мм)   │         │");
            Console.WriteLine("├─────────┼─────────────────┼───────────────┼─────────┼─────────┤");

            double sumRatio = 0;
            int countRatio = 0;

            foreach (var diameter in new[] { 60, 70, 80, 90, 100 })
            {
                // Расчет нашей Z
                double minDiameter = config.BeamConfig.MinBeamDiameterMicron;
                double rayleighLength = config.BeamConfig.RayleighLengthMicron;
                double focalLength = config.BeamConfig.FocalLengthMm;

                double lensTravelMicron = 0;
                if (diameter >= minDiameter)
                {
                    double ratio = diameter / minDiameter;
                    lensTravelMicron = rayleighLength * Math.Sqrt(ratio * ratio - 1);
                }

                double focalLengthMicron = focalLength * 1000.0 + lensTravelMicron;
                double f = focalLengthMicron / 1000.0;

                double a = config.ThirdAxisConfig.Afactor;
                double b = config.ThirdAxisConfig.Bfactor;
                double c = config.ThirdAxisConfig.Cfactor;

                double ourZ = a * f * f + b * f + c;

                // Референсная Z
                double refZ = ReferenceData.ContainsKey(diameter) ? ReferenceData[diameter] : k_ref * diameter + b_ref;

                double diff = ourZ - refZ;
                double ratioZ = refZ / ourZ;

                Console.WriteLine($"│ {diameter,7} │ {ourZ,15:F7} │ {refZ,13:F7} │ {diff,7:F4} │ {ratioZ,7:F4} │");

                sumRatio += ratioZ;
                countRatio++;
            }

            Console.WriteLine("└─────────┴─────────────────┴───────────────┴─────────┴─────────┘");
            Console.WriteLine();

            double avgRatio = sumRatio / countRatio;
            Console.WriteLine($"Средний коэффициент (refZ / ourZ): {avgRatio:F6}");
            Console.WriteLine();

            // 5. ИТОГОВАЯ РЕКОМЕНДАЦИЯ
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                       ИТОГОВАЯ РЕКОМЕНДАЦИЯ                           ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            if (Math.Abs(avgRatio - 1.0) < 0.1)
            {
                Console.WriteLine("✅ Наши Z координаты близки к референсным!");
                Console.WriteLine($"   Коэффициент: {avgRatio:F4} (близко к 1.0)");
                Console.WriteLine();
                Console.WriteLine("ПРОБЛЕМА В ДРУГОМ:");
                Console.WriteLine("  - Возможно, нужно использовать другой метод UDM");
                Console.WriteLine("  - Или сканер не применяет Z координату как ожидается");
            }
            else
            {
                Console.WriteLine("⚠️ Наши Z координаты ОТЛИЧАЮТСЯ от референсных!");
                Console.WriteLine($"   Коэффициент: {avgRatio:F4}");
                Console.WriteLine();
                Console.WriteLine("РЕШЕНИЕ:");
                Console.WriteLine($"   Умножайте наши Z координаты на коэффициент: {avgRatio:F6}");
                Console.WriteLine();
                Console.WriteLine("ПРИМЕР КОДА:");
                Console.WriteLine($"   double zFinal = GetCorrectZValue(...) * {avgRatio:F6};");
            }

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine("ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА:");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("Референсные данные показывают ОТРИЦАТЕЛЬНЫЕ Z координаты.");
            Console.WriteLine($"Наши Z координаты: {(ourMeasurements[0].Item1 == 60 ? "положительные" : "отрицательные")}");
            Console.WriteLine();
            Console.WriteLine("⚠️ ВАЖНО: Проверьте ЗНАК Z координаты!");
            Console.WriteLine("   Возможно, нужно ИНВЕРТИРОВАТЬ знак: Z_final = -Z_calculated");
            Console.WriteLine();
        }

        /// <summary>
        /// Получить референсную Z координату для заданного диаметра
        /// </summary>
        public static double GetReferenceZ(double diameterMicron)
        {
            int d = (int)Math.Round(diameterMicron);

            if (ReferenceData.ContainsKey(d))
            {
                return ReferenceData[d];
            }

            // Линейная интерполяция
            double k = -0.0166997;  // из регрессии
            double b = -0.5296;
            return k * diameterMicron + b;
        }
    }
}
