using Hans.NET.Models;
using System;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Простой тест для диагностики проблемы с диаметром
    /// </summary>
    public static class SimpleDebugTest
    {
        public static void TestCalculations()
        {
            var config = RunDiameterTests.LoadConfiguration();
            if (config == null)
            {
                Console.WriteLine("❌ Не удалось загрузить конфигурацию!");
                return;
            }

            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              ДИАГНОСТИКА РАСЧЕТОВ ДИАМЕТРА                            ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Тестовые диаметры
            double[] testDiameters = { 63.1, 80.0, 100.0, 150.0, 200.0 };

            foreach (var requestedDiameter in testDiameters)
            {
                Console.WriteLine($"═══════════════════════════════════════════════════════════════════════");
                Console.WriteLine($"ТЕСТ: Запрошенный диаметр = {requestedDiameter:F1} мкм");
                Console.WriteLine($"═══════════════════════════════════════════════════════════════════════");
                Console.WriteLine();

                // Вручную повторяем расчет из TestUdmBuilder
                double minDiameter = config.BeamConfig.MinBeamDiameterMicron;
                double rayleighLength = config.BeamConfig.RayleighLengthMicron;
                double focalLength = config.BeamConfig.FocalLengthMm;

                Console.WriteLine($"Параметры конфига:");
                Console.WriteLine($"  minBeamDiameterMicron (d₀): {minDiameter:F2} мкм");
                Console.WriteLine($"  rayleighLengthMicron (zR): {rayleighLength:F2} мкм");
                Console.WriteLine($"  focalLengthMm: {focalLength:F2} мм");
                Console.WriteLine();

                // Шаг 1: getLensTravelMicron
                double lensTravelMicron;
                if (requestedDiameter < minDiameter)
                {
                    lensTravelMicron = 0;
                    Console.WriteLine($"Запрошенный диаметр {requestedDiameter:F1} < минимум {minDiameter:F1}");
                    Console.WriteLine($"  → lensTravelMicron = 0 (фокус)");
                }
                else
                {
                    // Формула из Java BeamConfig.java line 201:
                    // return rayleighLengthMicron * Math.sqrt((((beamDiam / 2) * (beamDiam / 2)) / ((minDiameter / 2) * (minDiameter / 2))) - 1);
                    double ratio = (requestedDiameter / minDiameter);
                    lensTravelMicron = rayleighLength * Math.Sqrt(ratio * ratio - 1);

                    Console.WriteLine($"Расчет lensTravelMicron:");
                    Console.WriteLine($"  ratio = d/d₀ = {requestedDiameter:F1}/{minDiameter:F1} = {ratio:F4}");
                    Console.WriteLine($"  sqrt((d/d₀)² - 1) = sqrt({ratio * ratio:F4} - 1) = {Math.Sqrt(ratio * ratio - 1):F4}");
                    Console.WriteLine($"  lensTravelMicron = zR × sqrt(...) = {rayleighLength:F2} × {Math.Sqrt(ratio * ratio - 1):F4}");
                    Console.WriteLine($"  lensTravelMicron = {lensTravelMicron:F3} мкм");
                }
                Console.WriteLine();

                // Шаг 2: focalLengthMicron
                double focalLengthMicron = focalLength * 1000.0;  // мм → мкм
                Console.WriteLine($"Базовое фокусное расстояние:");
                Console.WriteLine($"  focalLengthMicron = {focalLength:F2} мм × 1000 = {focalLengthMicron:F1} мкм");
                Console.WriteLine();

                // Шаг 3: Добавляем lensTravelMicron
                focalLengthMicron += lensTravelMicron;
                Console.WriteLine($"После добавления смещения линзы:");
                Console.WriteLine($"  focalLengthMicron = {focalLengthMicron - lensTravelMicron:F1} + {lensTravelMicron:F3}");
                Console.WriteLine($"  focalLengthMicron = {focalLengthMicron:F3} мкм = {focalLengthMicron / 1000.0:F6} мм");
                Console.WriteLine();

                // Шаг 4: Полином Z-коррекции
                double f = focalLengthMicron / 1000.0;  // мкм → мм
                double a = config.ThirdAxisConfig.Afactor;
                double b = config.ThirdAxisConfig.Bfactor;
                double c = config.ThirdAxisConfig.Cfactor;

                double zCoord = a * f * f + b * f + c;

                Console.WriteLine($"Полином Z-коррекции:");
                Console.WriteLine($"  f = {f:F6} мм");
                Console.WriteLine($"  Z = a×f² + b×f + c");
                Console.WriteLine($"  Z = {a}×{f:F6}² + {b:F9}×{f:F6} + {c:F9}");
                Console.WriteLine($"  Z = {a * f * f:F9} + {b * f:F9} + {c:F9}");
                Console.WriteLine($"  Z = {zCoord:F9} мм");
                Console.WriteLine();

                // Шаг 5: Умножение на K_FACTOR_AXES_Z
                const double K_FACTOR = 4.0;
                double zFinal = zCoord * K_FACTOR;

                Console.WriteLine($"Применение K_FACTOR_AXES_Z:");
                Console.WriteLine($"  Z (до умножения) = {zCoord:F9} мм");
                Console.WriteLine($"  K_FACTOR_AXES_Z = {K_FACTOR}");
                Console.WriteLine($"  Z (в UDM) = {zCoord:F9} × {K_FACTOR} = {zFinal:F9} мм");
                Console.WriteLine();

                // ОБРАТНЫЙ РАСЧЕТ: Какой диаметр получится при этой Z?
                Console.WriteLine($"┌─────────────────────────────────────────────────────────────────────┐");
                Console.WriteLine($"│ ОБРАТНЫЙ РАСЧЕТ (проверка)                                          │");
                Console.WriteLine($"└─────────────────────────────────────────────────────────────────────┘");
                Console.WriteLine();

                // Из Z находим f (обратный полином)
                // Z = b*f + c (при a=0)  →  f = (Z - c) / b
                double fBack = (zCoord - c) / b;
                Console.WriteLine($"Из Z находим f:");
                Console.WriteLine($"  f = (Z - c) / b = ({zCoord:F9} - {c:F9}) / {b:F9}");
                Console.WriteLine($"  f = {fBack:F6} мм");
                Console.WriteLine();

                // Из f находим lensTravelMicron
                double focalMicronBack = fBack * 1000.0;
                double lensTravelBack = focalMicronBack - focalLength * 1000.0;

                Console.WriteLine($"Из f находим lensTravelMicron:");
                Console.WriteLine($"  focalMicronBack = {fBack:F6} мм × 1000 = {focalMicronBack:F3} мкм");
                Console.WriteLine($"  lensTravelBack = {focalMicronBack:F3} - {focalLength * 1000.0:F1} = {lensTravelBack:F3} мкм");
                Console.WriteLine();

                // Из lensTravelMicron находим диаметр
                // lensTravelMicron = zR * sqrt((d/d₀)² - 1)
                // → (d/d₀)² = (lensTravelMicron/zR)² + 1
                // → d = d₀ * sqrt((lensTravelMicron/zR)² + 1)
                double expectedDiameter;
                if (lensTravelBack <= 0)
                {
                    expectedDiameter = minDiameter;
                }
                else
                {
                    double ratioBack = lensTravelBack / rayleighLength;
                    expectedDiameter = minDiameter * Math.Sqrt(ratioBack * ratioBack + 1);
                }

                Console.WriteLine($"Из lensTravelMicron находим диаметр:");
                Console.WriteLine($"  ratio = lensTravelMicron/zR = {lensTravelBack:F3}/{rayleighLength:F2} = {lensTravelBack / rayleighLength:F6}");
                Console.WriteLine($"  d = d₀ × sqrt((ratio)² + 1)");
                Console.WriteLine($"  d = {minDiameter:F2} × sqrt({(lensTravelBack / rayleighLength) * (lensTravelBack / rayleighLength):F6} + 1)");
                Console.WriteLine($"  d = {minDiameter:F2} × {Math.Sqrt((lensTravelBack / rayleighLength) * (lensTravelBack / rayleighLength) + 1):F6}");
                Console.WriteLine($"  d = {expectedDiameter:F2} мкм");
                Console.WriteLine();

                Console.WriteLine($"╔═══════════════════════════════════════════════════════════════════════╗");
                Console.WriteLine($"║ ИТОГ                                                                  ║");
                Console.WriteLine($"╚═══════════════════════════════════════════════════════════════════════╝");
                Console.WriteLine($"  Запрошено:         {requestedDiameter:F1} мкм");
                Console.WriteLine($"  Обратный расчет:   {expectedDiameter:F2} мкм");
                Console.WriteLine($"  Расхождение:       {Math.Abs(expectedDiameter - requestedDiameter):F2} мкм ({Math.Abs(expectedDiameter - requestedDiameter) / requestedDiameter * 100:F2}%)");
                Console.WriteLine();

                if (Math.Abs(expectedDiameter - requestedDiameter) < 0.1)
                {
                    Console.WriteLine($"  ✅ Расчет корректен!");
                }
                else
                {
                    Console.WriteLine($"  ⚠️ Есть расхождение!");
                }

                Console.WriteLine();
                Console.WriteLine();
            }

            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║ ЗАКЛЮЧЕНИЕ                                                            ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Если обратный расчет показывает правильные диаметры, то:");
            Console.WriteLine();
            Console.WriteLine("1. Алгоритм расчета ПРАВИЛЬНЫЙ");
            Console.WriteLine("2. Проблема в ПАРАМЕТРАХ конфигурации:");
            Console.WriteLine($"   - minBeamDiameterMicron = {config.BeamConfig.MinBeamDiameterMicron:F2} мкм");
            Console.WriteLine($"   - rayleighLengthMicron = {config.BeamConfig.RayleighLengthMicron:F2} мкм");
            Console.WriteLine($"   - M² = {config.BeamConfig.M2:F3}");
            Console.WriteLine();
            Console.WriteLine("3. ИЛИ проблема в применении Z координаты сканером:");
            Console.WriteLine("   - Сканер может использовать другой масштаб Z");
            Console.WriteLine("   - Проверьте настройки Hans Laser Marker");
            Console.WriteLine("   - Возможно нужна калибровка полинома в сканере");
            Console.WriteLine();
        }
    }
}
