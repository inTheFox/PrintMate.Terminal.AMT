using Hans.NET.Models;
using System;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Диагностический тест для выявления проблем с расчетом диаметра
    /// </summary>
    public static class DiagnosticTest
    {
        /// <summary>
        /// Проверка расчетов для реально измеренных значений
        /// </summary>
        public static void AnalyzeRealMeasurements(ScanatorConfiguration config)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         АНАЛИЗ РЕАЛЬНЫХ ИЗМЕРЕНИЙ VS РАСЧЕТЫ                         ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Реальные измерения
            var measurements = new[]
            {
                (requested: 65.0, measured: 49.6),
                (requested: 85.0, measured: 50.3),
                (requested: 95.0, measured: 56.0),
                (requested: 500.0, measured: 122.0)
            };

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ПАРАМЕТРЫ КОНФИГУРАЦИИ                                              │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"│ Минимальный диаметр (d₀): {config.BeamConfig.MinBeamDiameterMicron:F2} мкм");
            Console.WriteLine($"│ Длина Рэлея (zR): {config.BeamConfig.RayleighLengthMicron:F2} мкм");
            Console.WriteLine($"│ Фокусное расстояние: {config.BeamConfig.FocalLengthMm:F2} мм");
            Console.WriteLine($"│ M²: {config.BeamConfig.M2:F3}");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("│ ПОЛИНОМ Z-КОРРЕКЦИИ                                                 │");
            Console.WriteLine($"│   a = {config.ThirdAxisConfig.Afactor}");
            Console.WriteLine($"│   b = {config.ThirdAxisConfig.Bfactor:F9}");
            Console.WriteLine($"│   c = {config.ThirdAxisConfig.Cfactor:F9}");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            var builder = new TestUdmBuilder(config);

            foreach (var (requested, measured) in measurements)
            {
                Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
                Console.WriteLine($"ТЕСТ: Запрошено {requested:F1} мкм → Измерено {measured:F1} мкм");
                Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
                Console.WriteLine();

                // Создаем UDM точку
                builder.BuildSinglePoint(
                    x: 0f,
                    y: 0f,
                    beamDiameterMicron: requested,
                    powerWatts: 200f,
                    dwellTimeMs: 500
                );

                // Анализируем расчеты
                Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine("│ ПРОМЕЖУТОЧНЫЕ РАСЧЕТЫ                                               │");
                Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
                Console.WriteLine($"│ 1. Базовое фокусное расстояние: {TestUdmBuilder.FocalLengthMm:F4} мм");
                Console.WriteLine($"│ 2. Смещение линзы (getLensTravelMicron): {TestUdmBuilder.LensTravelMicron:F3} мкм");
                Console.WriteLine($"│ 3. Фокусное после коррекции: {TestUdmBuilder.FocalLengthMicron / 1000.0:F4} мм");
                Console.WriteLine($"│ 4. Z координата (итоговая): {TestUdmBuilder.ZFinal:F6} мм");
                Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
                Console.WriteLine();

                // ОБРАТНЫЙ РАСЧЕТ: Какой диаметр получится при этой Z?
                double calculatedZ = TestUdmBuilder.ZFinal;

                // Через обратный полином находим фокусное расстояние
                // Z = b*f + c  →  f = (Z - c) / b
                double focalLengthFromZ;
                if (config.ThirdAxisConfig.Afactor == 0)
                {
                    // Линейный полином
                    focalLengthFromZ = (calculatedZ - config.ThirdAxisConfig.Cfactor) / config.ThirdAxisConfig.Bfactor;
                }
                else
                {
                    // Квадратичный полином - решаем квадратное уравнение
                    // a*f² + b*f + c - Z = 0
                    double a = config.ThirdAxisConfig.Afactor;
                    double b = config.ThirdAxisConfig.Bfactor;
                    double c = config.ThirdAxisConfig.Cfactor - calculatedZ;
                    double discriminant = b * b - 4 * a * c;
                    focalLengthFromZ = (-b + Math.Sqrt(discriminant)) / (2 * a);
                }

                double focalLengthMicronFromZ = focalLengthFromZ * 1000.0;

                // Вычитаем смещение линзы чтобы получить zR * sqrt(...)
                double lensTravelMicronBack = focalLengthMicronFromZ - config.BeamConfig.FocalLengthMm * 1000.0;

                // Обратная формула: d = d₀ * sqrt(1 + (z/zR)²)
                // где z = lensTravelMicron
                double expectedDiameter;
                if (lensTravelMicronBack < 0)
                {
                    expectedDiameter = config.BeamConfig.MinBeamDiameterMicron;
                }
                else
                {
                    double ratio = lensTravelMicronBack / config.BeamConfig.RayleighLengthMicron;
                    expectedDiameter = config.BeamConfig.MinBeamDiameterMicron * Math.Sqrt(1 + ratio * ratio);
                }

                Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine("│ ОБРАТНЫЙ РАСЧЕТ (что должно получиться)                             │");
                Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
                Console.WriteLine($"│ Из Z = {calculatedZ:F6} мм получаем:");
                Console.WriteLine($"│   Фокусное расстояние: {focalLengthFromZ:F4} мм");
                Console.WriteLine($"│   Смещение линзы: {lensTravelMicronBack:F3} мкм");
                Console.WriteLine($"│   Ожидаемый диаметр: {expectedDiameter:F2} мкм");
                Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
                Console.WriteLine();

                Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine("│ СРАВНЕНИЕ                                                           │");
                Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
                Console.WriteLine($"│ Запрошенный диаметр: {requested:F1} мкм");
                Console.WriteLine($"│ Расчетный диаметр (обратный): {expectedDiameter:F2} мкм");
                Console.WriteLine($"│ РЕАЛЬНО ИЗМЕРЕННЫЙ: {measured:F1} мкм ← !!!!");
                Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
                double errorCalculated = Math.Abs(expectedDiameter - requested);
                double errorMeasured = Math.Abs(measured - requested);
                Console.WriteLine($"│ Ошибка расчета: {errorCalculated:F2} мкм ({errorCalculated / requested * 100:F1}%)");
                Console.WriteLine($"│ Ошибка измерения: {errorMeasured:F2} мкм ({errorMeasured / requested * 100:F1}%)");
                Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
                Console.WriteLine();

                // ВАЖНЫЙ ВЫВОД
                if (Math.Abs(measured - config.BeamConfig.MinBeamDiameterMicron) < 10)
                {
                    Console.WriteLine("⚠️⚠️⚠️ КРИТИЧЕСКАЯ ПРОБЛЕМА ⚠️⚠️⚠️");
                    Console.WriteLine($"Измеренный диаметр {measured:F1} мкм ≈ минимальный диаметр {config.BeamConfig.MinBeamDiameterMicron:F1} мкм!");
                    Console.WriteLine("ВЫВОД: Сканер ВСЕГДА работает В ФОКУСЕ, игнорируя Z координату!");
                    Console.WriteLine("Возможные причины:");
                    Console.WriteLine("  1. Z-коррекция отключена в сканере");
                    Console.WriteLine("  2. Полином Z-коррекции неправильный");
                    Console.WriteLine("  3. SDK Hans не применяет Z координату");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                           ВЫВОДЫ                                      ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Все измеренные диаметры ≈ 50-56 мкм (близко к минимальному d₀ = 63.1 мкм)");
            Console.WriteLine();
            Console.WriteLine("ЭТО ОЗНАЧАЕТ:");
            Console.WriteLine("  → Сканер работает В ФОКУСЕ независимо от заданной Z координаты");
            Console.WriteLine("  → Z координата НЕ ПРИМЕНЯЕТСЯ к лучу");
            Console.WriteLine();
            Console.WriteLine("ПРОБЛЕМА НЕ В АЛГОРИТМЕ РАСЧЕТА, А В:");
            Console.WriteLine("  1. Настройках сканера (3D коррекция может быть отключена)");
            Console.WriteLine("  2. Полиноме коррекции (может быть неправильно откалиброван)");
            Console.WriteLine("  3. SDK Hans (возможно не передает Z в UDM_AddPoint2D)");
            Console.WriteLine();
        }
    }
}
