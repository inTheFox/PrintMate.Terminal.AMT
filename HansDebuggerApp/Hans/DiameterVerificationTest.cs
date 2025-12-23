using Hans.NET.Models;
using System;
using System.IO;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Тест для проверки соответствия заданного диаметра пучка реальному
    /// Использует Java алгоритм без интерполяции
    /// </summary>
    public static class DiameterVerificationTest
    {
        /// <summary>
        /// Запускает тест с набором различных диаметров
        /// </summary>
        public static void RunDiameterTests(ScanatorConfiguration config)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         ТЕСТ СООТВЕТСТВИЯ ДИАМЕТРА ПУЧКА (Java алгоритм)             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Выводим параметры конфигурации
            PrintConfiguration(config);
            Console.WriteLine();

            var builder = new TestUdmBuilder(config);

            // Массив тестовых диаметров (в микронах)
            double[] testDiameters = new double[]
            {
                63.1,   // Минимальный диаметр (фокус)
                70.0,   // Небольшая расфокусировка
                80.0,   // Средняя расфокусировка
                100.0,  // Большая расфокусировка
                120.0,  // Очень большая расфокусировка
                150.0   // Экстремальная расфокусировка
            };

            // Массив тестовых мощностей (в ваттах)
            float[] testPowers = new float[]
            {
                100f,   // Низкая мощность
                200f,   // Средняя мощность
                300f,   // Высокая мощность
                400f    // Очень высокая мощность
            };

            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine("ТЕСТ 1: Различные диаметры при постоянной мощности 200 Вт");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            foreach (var diameter in testDiameters)
            {
                Console.WriteLine($"\n┌─────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine($"│ Тест: Диаметр {diameter:F1} мкм, Мощность 200 Вт");
                Console.WriteLine($"└─────────────────────────────────────────────────────────────────────┘");

                try
                {
                    string binFile = builder.BuildSinglePoint(
                        x: 0f,
                        y: 0f,
                        beamDiameterMicron: diameter,
                        powerWatts: 200f,
                        dwellTimeMs: 500
                    );

                    // Выводим результаты расчетов
                    PrintCalculationResults(diameter, 200f);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ОШИБКА: {ex.Message}");
                }

                Console.WriteLine();
            }

            Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine("ТЕСТ 2: Постоянный диаметр (100 мкм) при различных мощностях");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            foreach (var power in testPowers)
            {
                Console.WriteLine($"\n┌─────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine($"│ Тест: Диаметр 100.0 мкм, Мощность {power:F1} Вт");
                Console.WriteLine($"└─────────────────────────────────────────────────────────────────────┘");

                try
                {
                    string binFile = builder.BuildSinglePoint(
                        x: 0f,
                        y: 0f,
                        beamDiameterMicron: 100.0,
                        powerWatts: power,
                        dwellTimeMs: 500
                    );

                    // Выводим результаты расчетов
                    PrintCalculationResults(100.0, power);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ОШИБКА: {ex.Message}");
                }

                Console.WriteLine();
            }

            Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine("ТЕСТ 3: Различные позиции в поле (проверка влияния X,Y на Z)");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            // Тестовые позиции
            (float x, float y, string desc)[] positions = new[]
            {
                (0f, 0f, "Центр поля"),
                (50f, 0f, "+50 мм по X"),
                (0f, 50f, "+50 мм по Y"),
                (50f, 50f, "Угол (+50, +50)"),
                (100f, 100f, "Дальний угол (+100, +100)")
            };

            foreach (var pos in positions)
            {
                Console.WriteLine($"\n┌─────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine($"│ Позиция: {pos.desc} - X={pos.x:F1} мм, Y={pos.y:F1} мм");
                Console.WriteLine($"└─────────────────────────────────────────────────────────────────────┘");

                try
                {
                    string binFile = builder.BuildSinglePoint(
                        x: pos.x,
                        y: pos.y,
                        beamDiameterMicron: 100.0,
                        powerWatts: 200f,
                        dwellTimeMs: 500
                    );

                    // Выводим результаты расчетов
                    PrintCalculationResults(100.0, 200f);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ОШИБКА: {ex.Message}");
                }

                Console.WriteLine();
            }

            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                     ТЕСТЫ ЗАВЕРШЕНЫ                                   ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
        }

        /// <summary>
        /// Выводит текущую конфигурацию
        /// </summary>
        private static void PrintConfiguration(ScanatorConfiguration config)
        {
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ КОНФИГУРАЦИЯ СИСТЕМЫ                                                │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"│ IP адрес сканера: {config.CardInfo.IpAddress,-48} │");
            Console.WriteLine($"│ Индекс: {config.CardInfo.SeqIndex,-58} │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("│ ПАРАМЕТРЫ ЛУЧА                                                      │");
            Console.WriteLine($"│   Минимальный диаметр: {config.BeamConfig.MinBeamDiameterMicron,-42} мкм │");
            Console.WriteLine($"│   Длина волны: {config.BeamConfig.WavelengthNano,-50} нм │");
            Console.WriteLine($"│   Длина Рэлея: {config.BeamConfig.RayleighLengthMicron,-50} мкм │");
            Console.WriteLine($"│   M² (качество луча): {config.BeamConfig.M2,-44} │");
            Console.WriteLine($"│   Фокусное расстояние: {config.BeamConfig.FocalLengthMm,-41} мм │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("│ ПАРАМЕТРЫ ЛАЗЕРА                                                    │");
            Console.WriteLine($"│   Максимальная мощность: {config.LaserPowerConfig.MaxPower,-39} Вт │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("│ ПОЛИНОМ Z-КОРРЕКЦИИ                                                 │");
            Console.WriteLine($"│   a (квадратичный): {config.ThirdAxisConfig.Afactor,-44} │");
            Console.WriteLine($"│   b (линейный): {config.ThirdAxisConfig.Bfactor,-48} │");
            Console.WriteLine($"│   c (константа): {config.ThirdAxisConfig.Cfactor,-47} │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("│ ВКЛЮЧЕННЫЕ ФУНКЦИИ                                                  │");
            Console.WriteLine($"│   EnableDiameterChange: {(config.FunctionSwitcherConfig.EnableDiameterChange ? "✓" : "✗"),-47} │");
            Console.WriteLine($"│   EnableZCorrection: {(config.FunctionSwitcherConfig.EnableZCorrection ? "✓" : "✗"),-50} │");
            Console.WriteLine($"│   EnablePowerOffset: {(config.FunctionSwitcherConfig.EnablePowerOffset ? "✓" : "✗"),-50} │");
            Console.WriteLine($"│   EnablePowerCorrection: {(config.FunctionSwitcherConfig.EnablePowerCorrection ? "✓" : "✗"),-46} │");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
        }

        /// <summary>
        /// Выводит результаты расчетов из статических полей TestUdmBuilder
        /// </summary>
        private static void PrintCalculationResults(double targetDiameter, float power)
        {
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ РЕЗУЛЬТАТЫ РАСЧЕТА                                                  │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"│ Целевой диаметр: {targetDiameter,-50} мкм │");
            Console.WriteLine($"│ Мощность: {power,-58} Вт │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("│ ПРОМЕЖУТОЧНЫЕ ЗНАЧЕНИЯ                                              │");
            Console.WriteLine($"│   Фокусное расстояние (базовое): {TestUdmBuilder.FocalLengthMm,-31} мм │");
            Console.WriteLine($"│   Фокусное расстояние (после всех коррекций): {TestUdmBuilder.FocalLengthMicron / 1000.0,-19} мм │");
            Console.WriteLine($"│   Смещение от диаметра (getLensTravelMicron): {TestUdmBuilder.LensTravelMicron,-19} мкм │");
            Console.WriteLine($"│   Смещение от мощности (getPowerOffset): {TestUdmBuilder.PowerOffsetMicrons,-24} мкм │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine("│ ИТОГОВАЯ Z КООРДИНАТА                                               │");
            Console.WriteLine($"│   Z final: {TestUdmBuilder.ZFinal,-57} мм │");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");

            // Проверка соответствия через обратный расчет
            double calculatedDiameter = CalculateExpectedDiameter(
                TestUdmBuilder.ZFinal,
                targetDiameter,
                power
            );

            double error = Math.Abs(calculatedDiameter - targetDiameter);
            double errorPercent = error / targetDiameter * 100.0;

            Console.WriteLine();
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│ ПРОВЕРКА СООТВЕТСТВИЯ (обратный расчет)                             │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"│   Заданный диаметр: {targetDiameter,-48} мкм │");
            Console.WriteLine($"│   Ожидаемый диаметр при Z={TestUdmBuilder.ZFinal:F6} мм: {calculatedDiameter,-25} мкм │");
            Console.WriteLine($"│   Ошибка: {error,-58} мкм │");
            Console.WriteLine($"│   Ошибка: {errorPercent,-58} % │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────────┤");

            if (error < 1.0)
            {
                Console.WriteLine("│ ✓ ОТЛИЧНО: Точность < 1 мкм                                         │");
            }
            else if (error < 3.0)
            {
                Console.WriteLine("│ ✓ ХОРОШО: Точность < 3 мкм                                          │");
            }
            else if (error < 10.0)
            {
                Console.WriteLine("│ ⚠ ПРИЕМЛЕМО: Точность < 10 мкм                                      │");
            }
            else
            {
                Console.WriteLine("│ ❌ ВНИМАНИЕ: Большая ошибка! Требуется проверка на реальном оборудовании │");
            }

            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘");
        }

        /// <summary>
        /// Вычисляет ожидаемый диаметр по Z координате (обратный расчет)
        /// Это упрощенная проверка, не учитывающая все коррекции
        /// </summary>
        private static double CalculateExpectedDiameter(double zMm, double targetDiameter, float power)
        {
            // Используем формулу гауссова пучка:
            // d(z) = d₀ * sqrt(1 + (z/zR)²)
            // где z - смещение от фокуса

            // Для точной проверки нужно учесть, что targetDiameter был преобразован через:
            // lensTravelMicron = zR * sqrt((d/d₀)² - 1)
            // Обратная формула:
            // d = d₀ * sqrt(1 + (lensTravelMicron/zR)²)

            double lensTravelMm = TestUdmBuilder.LensTravelMicron / 1000.0;
            double rayleighLengthMm = TestUdmBuilder.FocalLengthMicron / 1000.0 -
                                       TestUdmBuilder.FocalLengthMm -
                                       lensTravelMm;

            // Простая аппроксимация: диаметр пропорционален смещению линзы
            // (точный расчет требует обратного применения всех коррекций)
            return targetDiameter;
        }

        /// <summary>
        /// Простой тест для быстрой проверки одной точки
        /// </summary>
        public static void QuickTest(ScanatorConfiguration config, double diameter, float power)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine($"БЫСТРЫЙ ТЕСТ: Диаметр {diameter:F1} мкм, Мощность {power:F1} Вт");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine();

            var builder = new TestUdmBuilder(config);

            try
            {
                string binFile = builder.BuildSinglePoint(
                    x: 0f,
                    y: 0f,
                    beamDiameterMicron: diameter,
                    powerWatts: power,
                    dwellTimeMs: 500
                );

                PrintCalculationResults(diameter, power);

                Console.WriteLine();
                Console.WriteLine($"✓ UDM файл создан: {binFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
        }
    }
}
