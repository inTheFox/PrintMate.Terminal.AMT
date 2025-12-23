using Hans.NET.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Калибровка параметров луча на основе реальных измерений
    /// </summary>
    public static class CalibrationFromMeasurements
    {
        public class Measurement
        {
            public double RequestedDiameterMicron { get; set; }
            public double MeasuredDiameterMicron { get; set; }
            public double CalculatedZ { get; set; }  // Z координата которая была отправлена в сканер
        }

        /// <summary>
        /// Анализирует измерения и предлагает калибровку
        /// </summary>
        public static void AnalyzeAndCalibrate(ScanatorConfiguration config, List<Measurement> measurements)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              КАЛИБРОВКА ПО РЕАЛЬНЫМ ИЗМЕРЕНИЯМ                        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ТЕКУЩИЕ ПАРАМЕТРЫ КОНФИГУРАЦИИ                                      │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"│ minBeamDiameterMicron: {config.BeamConfig.MinBeamDiameterMicron:F2} мкм");
            Console.WriteLine($"│ rayleighLengthMicron: {config.BeamConfig.RayleighLengthMicron:F2} мкм");
            Console.WriteLine($"│ M²: {config.BeamConfig.M2:F3}");
            Console.WriteLine($"│ wavelengthNano: {config.BeamConfig.WavelengthNano:F1} нм");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ АНАЛИЗ ИЗМЕРЕНИЙ                                                    │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("│ Запрошено │ Измерено │ Z (мм)   │ Ошибка (мкм) │ Ошибка (%) │");
            Console.WriteLine("├───────────┼──────────┼──────────┼──────────────┼────────────┤");

            foreach (var m in measurements)
            {
                double error = m.MeasuredDiameterMicron - m.RequestedDiameterMicron;
                double errorPercent = error / m.RequestedDiameterMicron * 100.0;

                Console.WriteLine($"│ {m.RequestedDiameterMicron,9:F1} │ {m.MeasuredDiameterMicron,8:F1} │ {m.CalculatedZ,8:F6} │ {error,12:F1} │ {errorPercent,10:F1} │");
            }
            Console.WriteLine("└───────────┴──────────┴──────────┴──────────────┴────────────┘");
            Console.WriteLine();

            // ШАГИ КАЛИБРОВКИ

            // 1. Найти точку с минимальным измеренным диаметром (это реальный d₀)
            var minMeasurement = measurements.OrderBy(m => m.MeasuredDiameterMicron).First();
            double realMinDiameter = minMeasurement.MeasuredDiameterMicron;

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ШАГ 1: ОПРЕДЕЛЕНИЕ РЕАЛЬНОГО МИНИМАЛЬНОГО ДИАМЕТРА                 │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"│ Текущий minBeamDiameter: {config.BeamConfig.MinBeamDiameterMicron:F2} мкм");
            Console.WriteLine($"│ Минимальный измеренный: {realMinDiameter:F2} мкм");
            Console.WriteLine($"│ Разница: {realMinDiameter - config.BeamConfig.MinBeamDiameterMicron:F2} мкм");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();

            // 2. Пересчитать Rayleigh Length на основе измерений
            // Используем формулу: d(z) = d₀ * sqrt(1 + (z/zR)²)
            // Решаем для zR: zR = z / sqrt((d/d₀)² - 1)

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ШАГ 2: РАСЧЕТ РЕАЛЬНОЙ ДЛИНЫ РЭЛЕЯ                                 │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");

            List<double> calculatedZR = new List<double>();

            foreach (var m in measurements)
            {
                if (m.MeasuredDiameterMicron <= realMinDiameter * 1.01) continue; // Пропускаем фокус

                // Из измерений вычисляем какой должен быть zR
                // z (в микронах) = lensTravelMicron из расчета
                // Но у нас есть измеренный диаметр, из которого можем найти реальный z

                double ratio = m.MeasuredDiameterMicron / realMinDiameter;
                double zFromMeasurement = Math.Sqrt(ratio * ratio - 1);  // z/zR

                // Теперь нужно найти реальный z который был отправлен
                // У нас есть CalculatedZ - это Z координата в UDM

                // Обратный полином: f = (Z - c) / b
                double focalFromZ = (m.CalculatedZ - config.ThirdAxisConfig.Cfactor) / config.ThirdAxisConfig.Bfactor;
                double focalMicronFromZ = focalFromZ * 1000.0;
                double lensTravelMicron = focalMicronFromZ - config.BeamConfig.FocalLengthMm * 1000.0;

                // Теперь zR = lensTravelMicron / sqrt((d_measured/d₀)² - 1)
                if (zFromMeasurement > 0.01)
                {
                    double zR = lensTravelMicron / zFromMeasurement;
                    calculatedZR.Add(zR);

                    Console.WriteLine($"│ Запрошено: {m.RequestedDiameterMicron:F1} мкм → Измерено: {m.MeasuredDiameterMicron:F1} мкм");
                    Console.WriteLine($"│   lensTravelMicron: {lensTravelMicron:F2} мкм");
                    Console.WriteLine($"│   Вычисленный zR: {zR:F2} мкм");
                }
            }

            if (calculatedZR.Count > 0)
            {
                double avgZR = calculatedZR.Average();
                Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
                Console.WriteLine($"│ Текущий rayleighLength: {config.BeamConfig.RayleighLengthMicron:F2} мкм");
                Console.WriteLine($"│ Средний из измерений: {avgZR:F2} мкм");
                Console.WriteLine($"│ Разница: {avgZR - config.BeamConfig.RayleighLengthMicron:F2} мкм");
                Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
                Console.WriteLine();

                // 3. Вычислить новый M²
                // zR = π * (d₀/2)² / (λ * M²)
                // M² = π * (d₀/2)² / (λ * zR)

                double wavelengthMicron = config.BeamConfig.WavelengthNano / 1000.0;
                double newM2 = (Math.PI * Math.Pow(realMinDiameter / 2, 2)) / (wavelengthMicron * avgZR);

                Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine("│ ШАГ 3: РАСЧЕТ M² (КАЧЕСТВО ЛУЧА)                                    │");
                Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
                Console.WriteLine($"│ Текущий M²: {config.BeamConfig.M2:F3}");
                Console.WriteLine($"│ Вычисленный M²: {newM2:F3}");
                Console.WriteLine($"│ Разница: {newM2 - config.BeamConfig.M2:F3}");
                Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
                Console.WriteLine();

                // 4. РЕКОМЕНДУЕМЫЕ ПАРАМЕТРЫ
                Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                  РЕКОМЕНДУЕМЫЕ ПАРАМЕТРЫ                              ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine("Обновите scanator_config_test.json:");
                Console.WriteLine();
                Console.WriteLine("\"beamConfig\": {");
                Console.WriteLine($"  \"minBeamDiameterMicron\": {realMinDiameter:F2},");
                Console.WriteLine($"  \"wavelengthNano\": {config.BeamConfig.WavelengthNano:F1},");
                Console.WriteLine($"  \"rayleighLengthMicron\": {avgZR:F2},");
                Console.WriteLine($"  \"m2\": {newM2:F3},");
                Console.WriteLine($"  \"focalLengthMm\": {config.BeamConfig.FocalLengthMm:F2}");
                Console.WriteLine("}");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("⚠️ Недостаточно данных для калибровки zR");
                Console.WriteLine("Нужны измерения с расфокусировкой (диаметр > минимального)");
            }
        }

        /// <summary>
        /// Быстрый анализ с вашими текущими данными
        /// </summary>
        public static void AnalyzeCurrentData(ScanatorConfiguration config)
        {
            // ВАЖНО: Укажите какие диаметры вы запрашивали для каждого измерения!
            Console.WriteLine("⚠️ ВВЕДИТЕ ДАННЫЕ ОБ ИЗМЕРЕНИЯХ:");
            Console.WriteLine();
            Console.WriteLine("У вас есть 3 измерения: 49.8, 71, 294 мкм");
            Console.WriteLine("Какие диаметры вы ЗАПРАШИВАЛИ для каждого из них?");
            Console.WriteLine();
            Console.WriteLine("Пожалуйста, создайте список измерений:");
            Console.WriteLine();
            Console.WriteLine("var measurements = new List<Measurement>");
            Console.WriteLine("{");
            Console.WriteLine("    new Measurement { RequestedDiameterMicron = ???, MeasuredDiameterMicron = 49.8, CalculatedZ = ??? },");
            Console.WriteLine("    new Measurement { RequestedDiameterMicron = ???, MeasuredDiameterMicron = 71, CalculatedZ = ??? },");
            Console.WriteLine("    new Measurement { RequestedDiameterMicron = ???, MeasuredDiameterMicron = 294, CalculatedZ = ??? }");
            Console.WriteLine("};");
            Console.WriteLine();
            Console.WriteLine("И вызовите: CalibrationFromMeasurements.AnalyzeAndCalibrate(config, measurements);");
        }
    }
}
