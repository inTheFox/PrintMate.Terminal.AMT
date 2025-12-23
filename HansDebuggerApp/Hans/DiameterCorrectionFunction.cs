using System;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Функция коррекции диаметра пучка на основе реальных измерений
    /// </summary>
    public static class DiameterCorrectionFunction
    {
        /// <summary>
        /// Анализ реальных измерений и построение функции коррекции
        /// </summary>
        public static void AnalyzeAndBuildFunction()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         АНАЛИЗ ИЗМЕРЕНИЙ И ПОСТРОЕНИЕ ФУНКЦИИ КОРРЕКЦИИ              ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Реальные измерения
            (double requested, double measured)[] data =
            {
                (60.0, 51.0),
                (70.0, 59.0),
                (80.0, 68.0),
                (90.0, 75.8),
                (100.0, 83.0)
            };

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ РЕАЛЬНЫЕ ИЗМЕРЕНИЯ                                                  │");
            Console.WriteLine("├───────────┬──────────┬────────────┬──────────────────────────────────┤");
            Console.WriteLine("│ Запрошено │ Измерено │ Ошибка     │ Коэффициент (измерено/запрос)   │");
            Console.WriteLine("│    (мкм)  │   (мкм)  │   (мкм)    │                                  │");
            Console.WriteLine("├───────────┼──────────┼────────────┼──────────────────────────────────┤");

            double sumRatio = 0;
            double sumDiff = 0;
            int count = data.Length;

            foreach (var (requested, measured) in data)
            {
                double error = measured - requested;
                double ratio = measured / requested;
                double diff = requested - measured;

                Console.WriteLine($"│ {requested,9:F1} │ {measured,8:F1} │ {error,10:F1} │ {ratio,32:F6} │");

                sumRatio += ratio;
                sumDiff += diff;
            }

            Console.WriteLine("└───────────┴──────────┴────────────┴──────────────────────────────────┘");
            Console.WriteLine();

            double avgRatio = sumRatio / count;
            double avgDiff = sumDiff / count;

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ СТАТИСТИКА                                                          │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"│ Средний коэффициент (measured/requested): {avgRatio:F6}");
            Console.WriteLine($"│ Средняя разница (requested - measured): {avgDiff:F2} мкм");
            Console.WriteLine($"│ Ошибка: {(1 - avgRatio) * 100:F2}% (диаметры меньше на {(1 - avgRatio) * 100:F2}%)");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            // Анализ: проверяем линейность
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ АНАЛИЗ ЛИНЕЙНОСТИ                                                   │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");

            // Линейная регрессия: measured = k * requested + b
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            foreach (var (requested, measured) in data)
            {
                sumX += requested;
                sumY += measured;
                sumXY += requested * measured;
                sumX2 += requested * requested;
            }

            double k = (count * sumXY - sumX * sumY) / (count * sumX2 - sumX * sumX);
            double b = (sumY - k * sumX) / count;

            Console.WriteLine($"│ Линейная регрессия: measured = k × requested + b");
            Console.WriteLine($"│   k = {k:F6}");
            Console.WriteLine($"│   b = {b:F6}");
            Console.WriteLine("│");
            Console.WriteLine($"│ Формула: measured = {k:F6} × requested + {b:F6}");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            // Проверка качества регрессии
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ПРОВЕРКА КАЧЕСТВА РЕГРЕССИИ                                         │");
            Console.WriteLine("├───────────┬──────────┬──────────────┬───────────────────────────────┤");
            Console.WriteLine("│ Запрошено │ Измерено │ Предсказано  │ Ошибка предсказания           │");
            Console.WriteLine("│    (мкм)  │   (мкм)  │   (мкм)      │    (мкм)                      │");
            Console.WriteLine("├───────────┼──────────┼──────────────┼───────────────────────────────┤");

            double sumSquaredError = 0;
            foreach (var (requested, measured) in data)
            {
                double predicted = k * requested + b;
                double error = measured - predicted;
                sumSquaredError += error * error;

                Console.WriteLine($"│ {requested,9:F1} │ {measured,8:F1} │ {predicted,12:F2} │ {error,29:F2} │");
            }

            Console.WriteLine("└───────────┴──────────┴──────────────┴───────────────────────────────┘");
            Console.WriteLine();

            double rmse = Math.Sqrt(sumSquaredError / count);
            Console.WriteLine($"Среднеквадратичная ошибка (RMSE): {rmse:F2} мкм");
            Console.WriteLine();

            // ФУНКЦИЯ КОРРЕКЦИИ
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    ФУНКЦИЯ КОРРЕКЦИИ                                  ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Обратная функция: requested = (measured - b) / k
            // Но нам нужно: requestedCorrected = f(requestedOriginal)
            // Чтобы получить желаемый диаметр, нужно скорректировать запрос

            double correctionK = 1.0 / k;
            double correctionB = -b / k;

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ВАРИАНТ 1: Простая коррекция через коэффициент                     │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"│ Коэффициент коррекции: {correctionK:F6}");
            Console.WriteLine("│");
            Console.WriteLine($"│ Применение:");
            Console.WriteLine($"│   requestedCorrected = requestedOriginal × {correctionK:F6}");
            Console.WriteLine("│");
            Console.WriteLine($"│ Пример:");
            Console.WriteLine($"│   Хочу диаметр 100 мкм");
            Console.WriteLine($"│   Запрашиваю: 100 × {correctionK:F6} = {100 * correctionK:F2} мкм");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ВАРИАНТ 2: Линейная коррекция с offset                             │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"│ Формула коррекции:");
            Console.WriteLine($"│   requestedCorrected = {correctionK:F6} × requestedOriginal + {correctionB:F6}");
            Console.WriteLine("│");
            Console.WriteLine($"│ Пример:");
            Console.WriteLine($"│   Хочу диаметр 100 мкм");
            Console.WriteLine($"│   Запрашиваю: {correctionK:F6} × 100 + {correctionB:F6} = {correctionK * 100 + correctionB:F2} мкм");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            // Проверка коррекции
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ПРОВЕРКА КОРРЕКЦИИ                                                  │");
            Console.WriteLine("├──────────┬───────────────┬──────────────┬─────────────────────────────┤");
            Console.WriteLine("│ Желаемый │ Скорректир.   │ Ожидаемый    │ Ошибка после коррекции      │");
            Console.WriteLine("│  (мкм)   │ запрос (мкм)  │ результат    │        (мкм)                │");
            Console.WriteLine("├──────────┼───────────────┼──────────────┼─────────────────────────────┤");

            foreach (var (desired, _) in data)
            {
                double correctedRequest = correctionK * desired + correctionB;
                double expectedResult = k * correctedRequest + b;
                double errorAfter = expectedResult - desired;

                Console.WriteLine($"│ {desired,8:F1} │ {correctedRequest,13:F2} │ {expectedResult,12:F2} │ {errorAfter,27:F2} │");
            }

            Console.WriteLine("└──────────┴───────────────┴──────────────┴─────────────────────────────┘");
            Console.WriteLine();

            // КОД ДЛЯ КОПИРОВАНИЯ
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              КОД ФУНКЦИИ КОРРЕКЦИИ (для копирования)                 ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("/// <summary>");
            Console.WriteLine("/// Корректирует запрошенный диаметр для получения желаемого результата");
            Console.WriteLine("/// На основе реальных измерений (60→51, 70→59, 80→68, 90→75.8, 100→83)");
            Console.WriteLine("/// </summary>");
            Console.WriteLine("/// <param name=\"desiredDiameterMicron\">Желаемый диаметр в микронах</param>");
            Console.WriteLine("/// <returns>Скорректированный диаметр для запроса</returns>");
            Console.WriteLine("public static double CorrectDiameter(double desiredDiameterMicron)");
            Console.WriteLine("{");
            Console.WriteLine($"    // Коэффициенты на основе линейной регрессии");
            Console.WriteLine($"    const double k = {correctionK:F8};");
            Console.WriteLine($"    const double b = {correctionB:F8};");
            Console.WriteLine();
            Console.WriteLine($"    return k * desiredDiameterMicron + b;");
            Console.WriteLine("}");
            Console.WriteLine();

            Console.WriteLine("// Пример использования:");
            Console.WriteLine("double desiredDiameter = 100.0;  // Хочу получить 100 мкм");
            Console.WriteLine("double correctedDiameter = CorrectDiameter(desiredDiameter);");
            Console.WriteLine($"// correctedDiameter = {CorrectDiameter(100.0):F2} мкм");
            Console.WriteLine();
        }

        /// <summary>
        /// Корректирует запрошенный диаметр для получения желаемого результата
        /// На основе реальных измерений
        /// </summary>
        public static double CorrectDiameter(double desiredDiameterMicron)
        {
            // Вычисляем коэффициенты (можно закешировать)
            (double requested, double measured)[] data =
            {
                (60.0, 51.0),
                (70.0, 59.0),
                (80.0, 68.0),
                (90.0, 75.8),
                (100.0, 83.0)
            };

            int count = data.Length;
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            foreach (var (requested, measured) in data)
            {
                sumX += requested;
                sumY += measured;
                sumXY += requested * measured;
                sumX2 += requested * requested;
            }

            double k = (count * sumXY - sumX * sumY) / (count * sumX2 - sumX * sumX);
            double b = (sumY - k * sumX) / count;

            // Обратная функция
            double correctionK = 1.0 / k;
            double correctionB = -b / k;

            return correctionK * desiredDiameterMicron + correctionB;
        }
    }
}
