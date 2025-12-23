using Hans.NET.Models;
using System;
using System.Collections.Generic;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Помощник для калибровки параметров на основе реальных измерений
    /// </summary>
    public static class CalibrationHelper
    {
        public class Measurement
        {
            public double RequestedDiameterMicron { get; set; }
            public double MeasuredDiameterMicron { get; set; }
        }

        /// <summary>
        /// Быстрый анализ для калибровки параметров
        /// </summary>
        public static void AnalyzeAndSuggestCorrection(ScanatorConfiguration config, List<Measurement> measurements)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           АНАЛИЗ ИЗМЕРЕНИЙ И КОРРЕКЦИЯ ПАРАМЕТРОВ                    ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ТЕКУЩИЕ ПАРАМЕТРЫ                                                   │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"│ minBeamDiameterMicron: {config.BeamConfig.MinBeamDiameterMicron:F2} мкм");
            Console.WriteLine($"│ rayleighLengthMicron: {config.BeamConfig.RayleighLengthMicron:F2} мкм");
            Console.WriteLine($"│ M²: {config.BeamConfig.M2:F3}");
            Console.WriteLine($"│ focalLengthMm: {config.BeamConfig.FocalLengthMm:F2} мм");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            // Анализируем измерения
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ИЗМЕРЕНИЯ                                                           │");
            Console.WriteLine("├───────────┬──────────┬────────────┬──────────────────────────────────┤");
            Console.WriteLine("│ Запрошено │ Измерено │ Ошибка     │ Коэффициент (измерено/запрос)   │");
            Console.WriteLine("│    (мкм)  │   (мкм)  │   (мкм)    │                                  │");
            Console.WriteLine("├───────────┼──────────┼────────────┼──────────────────────────────────┤");

            double sumRatio = 0;
            int count = 0;

            foreach (var m in measurements)
            {
                double error = m.MeasuredDiameterMicron - m.RequestedDiameterMicron;
                double ratio = m.MeasuredDiameterMicron / m.RequestedDiameterMicron;

                Console.WriteLine($"│ {m.RequestedDiameterMicron,9:F1} │ {m.MeasuredDiameterMicron,8:F1} │ {error,10:F1} │ {ratio,32:F4} │");

                sumRatio += ratio;
                count++;
            }

            Console.WriteLine("└───────────┴──────────┴────────────┴──────────────────────────────────┘");
            Console.WriteLine();

            if (count == 0)
            {
                Console.WriteLine("⚠️ Нет данных для анализа!");
                return;
            }

            double avgRatio = sumRatio / count;

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ АНАЛИЗ                                                              │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"│ Средний коэффициент: {avgRatio:F4}");
            Console.WriteLine($"│ Средняя ошибка: {(avgRatio - 1) * 100:F2}%");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            // Предлагаем коррекцию
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ РЕКОМЕНДУЕМАЯ КОРРЕКЦИЯ                                             │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");

            if (Math.Abs(avgRatio - 1.0) < 0.05)
            {
                Console.WriteLine("│ ✅ Калибровка ХОРОШАЯ! Ошибка < 5%");
                Console.WriteLine("│    Дополнительная коррекция не требуется.");
            }
            else if (avgRatio < 1.0)
            {
                // Измеренный диаметр меньше запрошенного
                Console.WriteLine($"│ ⚠️ Диаметры МЕНЬШЕ запрошенных на {(1 - avgRatio) * 100:F1}%");
                Console.WriteLine("│");
                Console.WriteLine("│ ВАРИАНТ 1: Увеличить Rayleigh Length");
                double correctionFactor = 1.0 / avgRatio;
                double newRayleighLength = config.BeamConfig.RayleighLengthMicron * correctionFactor;
                Console.WriteLine($"│   Текущий rayleighLengthMicron: {config.BeamConfig.RayleighLengthMicron:F2}");
                Console.WriteLine($"│   Новый rayleighLengthMicron: {newRayleighLength:F2}");
                Console.WriteLine($"│   Коэффициент коррекции: {correctionFactor:F4}");
                Console.WriteLine("│");
                Console.WriteLine("│ ВАРИАНТ 2: Уменьшить minBeamDiameter");
                double newMinDiameter = config.BeamConfig.MinBeamDiameterMicron * avgRatio;
                Console.WriteLine($"│   Текущий minBeamDiameterMicron: {config.BeamConfig.MinBeamDiameterMicron:F2}");
                Console.WriteLine($"│   Новый minBeamDiameterMicron: {newMinDiameter:F2}");
            }
            else
            {
                // Измеренный диаметр больше запрошенного
                Console.WriteLine($"│ ⚠️ Диаметры БОЛЬШЕ запрошенных на {(avgRatio - 1) * 100:F1}%");
                Console.WriteLine("│");
                Console.WriteLine("│ ВАРИАНТ 1: Уменьшить Rayleigh Length");
                double correctionFactor = 1.0 / avgRatio;
                double newRayleighLength = config.BeamConfig.RayleighLengthMicron * correctionFactor;
                Console.WriteLine($"│   Текущий rayleighLengthMicron: {config.BeamConfig.RayleighLengthMicron:F2}");
                Console.WriteLine($"│   Новый rayleighLengthMicron: {newRayleighLength:F2}");
                Console.WriteLine($"│   Коэффициент коррекции: {correctionFactor:F4}");
                Console.WriteLine("│");
                Console.WriteLine("│ ВАРИАНТ 2: Увеличить minBeamDiameter");
                double newMinDiameter = config.BeamConfig.MinBeamDiameterMicron * avgRatio;
                Console.WriteLine($"│   Текущий minBeamDiameterMicron: {config.BeamConfig.MinBeamDiameterMicron:F2}");
                Console.WriteLine($"│   Новый minBeamDiameterMicron: {newMinDiameter:F2}");
            }

            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            // Формат для копирования в JSON
            double correctionFactor2 = 1.0 / avgRatio;
            double newRayleighLength2 = config.BeamConfig.RayleighLengthMicron * correctionFactor2;

            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         ОБНОВЛЕННАЯ КОНФИГУРАЦИЯ (скопируйте в JSON)                 ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("\"beamConfig\": {");
            Console.WriteLine($"  \"minBeamDiameterMicron\": {config.BeamConfig.MinBeamDiameterMicron:F2},");
            Console.WriteLine($"  \"wavelengthNano\": {config.BeamConfig.WavelengthNano:F1},");
            Console.WriteLine($"  \"rayleighLengthMicron\": {newRayleighLength2:F2},");
            Console.WriteLine($"  \"m2\": {config.BeamConfig.M2:F3},");
            Console.WriteLine($"  \"focalLengthMm\": {config.BeamConfig.FocalLengthMm:F2}");
            Console.WriteLine("}");
            Console.WriteLine();
        }

        /// <summary>
        /// Тест с текущими данными (80 → 61)
        /// </summary>
        public static void TestCurrentMeasurement()
        {
            try
            {
                var config = RunDiameterTests.LoadConfiguration();
                if (config == null)
                {
                    Console.WriteLine("❌ Не удалось загрузить конфигурацию!");
                    return;
                }

                var measurements = new List<Measurement>
                {
                    new Measurement { RequestedDiameterMicron = 80.0, MeasuredDiameterMicron = 61.0 }
                };

                AnalyzeAndSuggestCorrection(config, measurements);

                Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
                Console.WriteLine("СЛЕДУЮЩИЕ ШАГИ:");
                Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
                Console.WriteLine();
                Console.WriteLine("1. Прожгите еще 2-3 точки с РАЗНЫМИ диаметрами:");
                Console.WriteLine("   - 100 мкм");
                Console.WriteLine("   - 150 мкм");
                Console.WriteLine("   - 200 мкм");
                Console.WriteLine();
                Console.WriteLine("2. Измерьте диаметры");
                Console.WriteLine();
                Console.WriteLine("3. Добавьте измерения в список:");
                Console.WriteLine("   var measurements = new List<Measurement>");
                Console.WriteLine("   {");
                Console.WriteLine("       new Measurement { RequestedDiameterMicron = 80.0, MeasuredDiameterMicron = 61.0 },");
                Console.WriteLine("       new Measurement { RequestedDiameterMicron = 100.0, MeasuredDiameterMicron = ??? },");
                Console.WriteLine("       new Measurement { RequestedDiameterMicron = 150.0, MeasuredDiameterMicron = ??? },");
                Console.WriteLine("       new Measurement { RequestedDiameterMicron = 200.0, MeasuredDiameterMicron = ??? }");
                Console.WriteLine("   };");
                Console.WriteLine();
                Console.WriteLine("4. Запустите: CalibrationHelper.AnalyzeAndSuggestCorrection(config, measurements);");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
