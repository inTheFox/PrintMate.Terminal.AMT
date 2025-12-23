using System;
using System.Collections.Generic;
using System.Linq;

namespace PrintMateMC.ScannerConfig
{
    /// <summary>
    /// УТИЛИТЫ ДЛЯ РАБОТЫ С КОНФИГУРАЦИЕЙ СКАНЕРА
    ///
    /// Дополнительные функции для анализа, тестирования и калибровки
    /// </summary>
    public class ScannerConfigUtilities
    {
        // ═══════════════════════════════════════════════════════════════════════
        // УТИЛИТА 0: Вычисление zCoefficient из beamConfig
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Вычисляет теоретический zCoefficient из Rayleigh length
        /// Использует формулу Гауссова луча
        /// </summary>
        public static double CalculateZCoefficientFromRayleigh(
            double minBeamDiameterMicron,
            double rayleighLengthMicron)
        {
            // На расстоянии z_R диаметр увеличивается в √2 раза
            double zRayleighMm = rayleighLengthMicron / 1000.0;
            double diameterAtRayleigh = minBeamDiameterMicron * Math.Sqrt(2);
            double deltaDiameter = diameterAtRayleigh - minBeamDiameterMicron;

            // Z = (Δd / 10) × k  =>  k = Z / (Δd / 10)
            double zCoefficient = zRayleighMm / (deltaDiameter / 10.0);

            return zCoefficient;
        }

        /// <summary>
        /// Выводит детальный отчет о вычислении zCoefficient
        /// </summary>
        public static void PrintZCoefficientCalculation(
            double minBeamDiameterMicron,
            double rayleighLengthMicron,
            double m2 = 1.0,
            double focalLengthMm = 538.46)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ВЫЧИСЛЕНИЕ zCoefficient ИЗ ПАРАМЕТРОВ ОПТИКИ            ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("ВХОДНЫЕ ПАРАМЕТРЫ (из beamConfig):");
            Console.WriteLine($"  Минимальный диаметр (d₀):  {minBeamDiameterMicron:F3} μm");
            Console.WriteLine($"  Rayleigh length (z_R):     {rayleighLengthMicron:F3} μm");
            Console.WriteLine($"  Фактор качества луча (M²): {m2:F3}");
            Console.WriteLine($"  Фокусное расстояние:       {focalLengthMm:F2} мм");
            Console.WriteLine();

            double zRayleighMm = rayleighLengthMicron / 1000.0;
            double diameterAtRayleigh = minBeamDiameterMicron * Math.Sqrt(2);
            double deltaDiameter = diameterAtRayleigh - minBeamDiameterMicron;
            double zCoefficient = CalculateZCoefficientFromRayleigh(minBeamDiameterMicron, rayleighLengthMicron);

            Console.WriteLine("РАСЧЕТ:");
            Console.WriteLine($"  1. На расстоянии z_R = {zRayleighMm:F3} мм:");
            Console.WriteLine($"     d(z_R) = d₀ × √2 = {minBeamDiameterMicron:F3} × 1.414 = {diameterAtRayleigh:F3} μm");
            Console.WriteLine();
            Console.WriteLine($"  2. Изменение диаметра:");
            Console.WriteLine($"     Δd = {diameterAtRayleigh:F3} - {minBeamDiameterMicron:F3} = {deltaDiameter:F3} μm");
            Console.WriteLine();
            Console.WriteLine($"  3. Применяем формулу Z = (Δd / 10) × k:");
            Console.WriteLine($"     {zRayleighMm:F3} = ({deltaDiameter:F3} / 10) × k");
            Console.WriteLine($"     k = {zRayleighMm:F3} / ({deltaDiameter:F3} / 10)");
            Console.WriteLine($"     k = {zCoefficient:F3} мм/10μm");
            Console.WriteLine();

            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  РЕЗУЛЬТАТ: zCoefficient = {zCoefficient:F3} мм/10μm              ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            Console.WriteLine("⚠ ВАЖНО:");
            Console.WriteLine("  Это ТЕОРЕТИЧЕСКОЕ значение из идеальной Гауссовой оптики!");
            Console.WriteLine("  Реальное значение может отличаться из-за:");
            Console.WriteLine("    • Аберраций F-theta линзы");
            Console.WriteLine("    • Термических эффектов в галванометрах");
            Console.WriteLine("    • Асферической коррекции линзы");
            Console.WriteLine();
            Console.WriteLine("  Для ТОЧНЫХ результатов проведите калибровку:");
            Console.WriteLine("    1. Напечатайте тестовые линии с разными Z");
            Console.WriteLine("    2. Измерьте ширину под микроскопом");
            Console.WriteLine("    3. Вычислите реальный zCoefficient");
            Console.WriteLine();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // УТИЛИТА 1: Таблица конвертации диаметр ↔ Z
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Выводит таблицу конвертации диаметр пучка ↔ Z-offset
        ///
        /// ВАЖНО: nominalDiameter берите из beamConfig.minBeamDiameterMicron
        ///        zCoefficient вычисляйте через CalculateZCoefficientFromRayleigh()
        ///        или используйте значение из калибровки!
        /// </summary>
        public static void PrintDiameterToZTable(
            double nominalDiameter = 48.141,
            double zCoefficient = 0.343)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ТАБЛИЦА КОНВЕРТАЦИИ: Диаметр ↔ Z                        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine($"Параметры системы:");
            Console.WriteLine($"  Номинальный диаметр: {nominalDiameter} μm (при Z=0)");
            Console.WriteLine($"  Коэффициент: {zCoefficient} мм/10μm");
            Console.WriteLine();
            Console.WriteLine("┌──────────────┬────────────┬─────────────────────────────┐");
            Console.WriteLine("│ Диаметр (μm) │ Z (мм)     │ Назначение                  │");
            Console.WriteLine("├──────────────┼────────────┼─────────────────────────────┤");

            var testCases = new[]
            {
                (60.0, "Edges, мелкие детали"),
                (65.0, "Edges (типичный)"),
                (70.0, "Contour, UpskinContour"),
                (80.0, "Downskin, UpskinHatch"),
                (90.0, "Infill (заполнение)"),
                (100.0, "Support (поддержки)"),
                (120.0, "Номинальный диаметр (Z=0)"),
                (140.0, "Грубая печать"),
                (160.0, "Максимум для оптики")
            };

            foreach (var (diameter, purpose) in testCases)
            {
                double z = (diameter - nominalDiameter) / 10.0 * zCoefficient;
                string zStr = $"{z:+0.000;-0.000;0.000}";
                Console.WriteLine($"│ {diameter,12:F1} │ {zStr,10} │ {purpose,-27} │");
            }

            Console.WriteLine("└──────────────┴────────────┴─────────────────────────────┘");
            Console.WriteLine();
            Console.WriteLine("ПРИМЕЧАНИЯ:");
            Console.WriteLine("  • Z > 0: Расфокусировка вверх (больший диаметр)");
            Console.WriteLine("  • Z < 0: Фокусировка вниз (меньший диаметр)");
            Console.WriteLine("  • Z = 0: Номинальный диаметр");
            Console.WriteLine($"  • Рекомендуемый диапазон Z: ±{zCoefficient * 4:F1} мм");
            Console.WriteLine();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // УТИЛИТА 2: Таблица коррекции кривизны поля
        // ═══════════════════════════════════════════════════════════════════════

        public static void PrintFieldCurvatureTable(
            double bfactor = 0.013944261,
            double cfactor = -7.5056114,
            double afactor = 0.0)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ТАБЛИЦА КОРРЕКЦИИ КРИВИЗНЫ ПОЛЯ                         ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine($"Формула: Z_corr = A×r² + B×r + C");
            Console.WriteLine($"  A (afactor): {afactor:F6}");
            Console.WriteLine($"  B (bfactor): {bfactor:F6}");
            Console.WriteLine($"  C (cfactor): {cfactor:F6}");
            Console.WriteLine();
            Console.WriteLine("┌──────┬─────────────┬──────────────┬──────────────┬─────────────────┐");
            Console.WriteLine("│ r(мм)│ Точка       │ Z_corr (мм)  │ Δ от центра  │ Физ. смысл      │");
            Console.WriteLine("├──────┼─────────────┼──────────────┼──────────────┼─────────────────┤");

            var testPoints = new[]
            {
                (0.0, "(0, 0)", "Центр"),
                (50.0, "(50, 0)", "Близко"),
                (100.0, "(100, 0)", "Средняя зона"),
                (141.42, "(100, 100)", "Диагональ"),
                (150.0, "(150, 0)", "Дальняя зона"),
                (200.0, "(200, 0)", "Край поля"),
                (212.13, "(150, 150)", "Диагональ"),
                (282.84, "(200, 200)", "Угол поля")
            };

            double zCenter = afactor * 0 * 0 + bfactor * 0 + cfactor;

            foreach (var (r, point, description) in testPoints)
            {
                double zCorr = afactor * r * r + bfactor * r + cfactor;
                double delta = zCorr - zCenter;

                Console.WriteLine(
                    $"│ {r,5:F0} │ {point,-11} │ {zCorr,12:F3} │ {delta,12:+0.000;-0.000;0.000} │ {description,-15} │"
                );
            }

            Console.WriteLine("└──────┴─────────────┴──────────────┴──────────────┴─────────────────┘");
            Console.WriteLine();
            Console.WriteLine("ФИЗИЧЕСКИЙ СМЫСЛ:");
            Console.WriteLine("  • Положительная Δ = фокус выше (ближе к линзе)");
            Console.WriteLine("  • Отрицательная Δ = фокус ниже (дальше от линзы)");
            Console.WriteLine($"  • Максимальная коррекция: ±{Math.Abs(zCenter - (bfactor * 283 + cfactor)):F2} мм");
            Console.WriteLine("  • Без коррекции качество на краях поля будет ЗНАЧИТЕЛЬНО хуже!");
            Console.WriteLine();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // УТИЛИТА 3: Анализ коррекции мощности
        // ═══════════════════════════════════════════════════════════════════════

        public static void AnalyzePowerCorrection(
            double[] actualPowerTable,
            double maxPower,
            double kFactor,
            double cFactor)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  АНАЛИЗ КОРРЕКЦИИ МОЩНОСТИ ЛАЗЕРА                        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine($"Максимальная мощность: {maxPower} Вт");
            Console.WriteLine($"Таблица коррекции: [{string.Join(", ", actualPowerTable.Select(p => p.ToString("F0")))}]");
            Console.WriteLine($"Коэффициенты смещения: K={kFactor:F6}, C={cFactor:F6}");
            Console.WriteLine();
            Console.WriteLine("┌───────────┬───────────┬───────────┬───────────┬───────────┬───────────┐");
            Console.WriteLine("│ Запрош(Вт)│ Запр.(%)  │ Интерп(Вт)│ Offset(Вт)│ Итого(Вт) │ Итого(%)  │");
            Console.WriteLine("├───────────┼───────────┼───────────┼───────────┼───────────┼───────────┤");

            double[] testPowers = { 50, 100, 150, 200, 250, 280, 300, 350, 400, 450, 500 };

            foreach (double requestedPower in testPowers)
            {
                // Нормализация
                double normalized = requestedPower / maxPower;

                // Интерполяция
                double index = normalized * (actualPowerTable.Length - 1);
                int lowerIdx = (int)Math.Floor(index);
                int upperIdx = Math.Min((int)Math.Ceiling(index), actualPowerTable.Length - 1);
                double fraction = index - lowerIdx;

                double interpolated = actualPowerTable[lowerIdx] +
                    (actualPowerTable[upperIdx] - actualPowerTable[lowerIdx]) * fraction;

                // Смещение
                double offset = kFactor * interpolated + cFactor;

                // Итого
                double finalPower = Math.Max(0, Math.Min(interpolated + offset, maxPower));
                double finalPercent = finalPower / maxPower * 100.0;

                Console.WriteLine(
                    $"│ {requestedPower,9:F0} │ {normalized * 100,9:F1} │ " +
                    $"{interpolated,9:F1} │ {offset,9:+0.0;-0.0;0.0} │ " +
                    $"{finalPower,9:F1} │ {finalPercent,9:F1} │"
                );
            }

            Console.WriteLine("└───────────┴───────────┴───────────┴───────────┴───────────┴───────────┘");
            Console.WriteLine();
            Console.WriteLine("ВЫВОДЫ:");

            // Анализ нелинейности
            double power50Percent = actualPowerTable[actualPowerTable.Length / 2];
            double expectedLinear = maxPower * 0.5;
            double nonlinearity = (power50Percent - expectedLinear) / expectedLinear * 100.0;

            Console.WriteLine($"  • Нелинейность при 50%: {Math.Abs(nonlinearity):F1}%");

            if (Math.Abs(nonlinearity) > 5)
            {
                Console.WriteLine("    ⚠ ВЫСОКАЯ нелинейность - коррекция критична!");
            }
            else
            {
                Console.WriteLine("    ✓ Умеренная нелинейность");
            }

            // Анализ смещения
            double avgOffset = testPowers.Average(p =>
            {
                double norm = p / maxPower;
                double idx = norm * (actualPowerTable.Length - 1);
                int li = (int)Math.Floor(idx);
                int ui = Math.Min((int)Math.Ceiling(idx), actualPowerTable.Length - 1);
                double frac = idx - li;
                double interp = actualPowerTable[li] + (actualPowerTable[ui] - actualPowerTable[li]) * frac;
                return kFactor * interp + cFactor;
            });

            Console.WriteLine($"  • Среднее смещение: {avgOffset:F1} Вт");

            if (Math.Abs(avgOffset) > 10)
            {
                Console.WriteLine("    ⚠ БОЛЬШОЕ смещение - требуется калибровка датчика!");
            }
            else
            {
                Console.WriteLine("    ✓ Небольшое смещение");
            }

            Console.WriteLine();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // УТИЛИТА 4: Генерация калибровочного файла для Z-offset
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Генерирует .bin файл с тестовыми линиями для калибровки zCoefficient
        ///
        /// Используйте значения из beamConfig для первого приближения,
        /// затем уточните после печати и измерений!
        /// </summary>
        public static void GenerateZCalibrationFile(
            string outputPath = "z_calibration.bin",
            double nominalDiameter = 48.141,
            double zCoefficient = 0.343)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ГЕНЕРАЦИЯ КАЛИБРОВОЧНОГО ФАЙЛА ДЛЯ Z-OFFSET             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1); // 3D режим

            // Тестовые Z-значения
            float[] testZValues = { -0.9f, -0.6f, -0.3f, 0.0f, 0.3f, 0.6f, 0.9f };

            Console.WriteLine("Создание тестовых линий:");
            Console.WriteLine();

            for (int i = 0; i < testZValues.Length; i++)
            {
                float z = testZValues[i];
                float yPos = -60 + i * 20;

                // Параметры слоя
                MarkParameter param = new MarkParameter
                {
                    MarkSpeed = 800,
                    JumpSpeed = 5000,
                    LaserPower = 50.0f,
                    PolygonDelay = 100,
                    JumpDelay = 100,
                    MarkDelay = 100,
                    LaserOnDelay = 100,
                    LaserOffDelay = 100,
                    Frequency = 30.0f,
                    DutyCycle = 0.5f
                };

                HM_UDM_DLL.UDM_SetLayersPara(new[] { param }, 1);

                // Горизонтальная линия
                structUdmPos[] line = new structUdmPos[]
                {
                    new structUdmPos { x = -80, y = yPos, z = z },
                    new structUdmPos { x = 80, y = yPos, z = z }
                };

                HM_UDM_DLL.UDM_AddPolyline3D(line, 2, i);

                // Расчет ожидаемого диаметра
                double expectedDiameter = nominalDiameter + (z / zCoefficient * 10.0);

                Console.WriteLine($"  Линия {i + 1}: Z = {z,5:+0.0;-0.0;0.0} мм, " +
                    $"Y = {yPos,4:F0} мм, Ожид. диам.: {expectedDiameter:F1} μm");
            }

            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine();
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
            Console.WriteLine();
            Console.WriteLine("ИНСТРУКЦИЯ ПО КАЛИБРОВКЕ:");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            Console.WriteLine("1. Напечатайте калибровочный файл на вашей системе");
            Console.WriteLine("2. Измерьте ширину каждой линии под микроскопом:");
            Console.WriteLine("   Линия 1 (Z=-0.9): _____ μm");
            Console.WriteLine("   Линия 2 (Z=-0.6): _____ μm");
            Console.WriteLine("   Линия 3 (Z=-0.3): _____ μm");
            Console.WriteLine("   Линия 4 (Z=0.0):  _____ μm ← Номинальный диаметр!");
            Console.WriteLine("   Линия 5 (Z=+0.3): _____ μm");
            Console.WriteLine("   Линия 6 (Z=+0.6): _____ μm");
            Console.WriteLine("   Линия 7 (Z=+0.9): _____ μm");
            Console.WriteLine();
            Console.WriteLine("3. Рассчитайте параметры:");
            Console.WriteLine("   NOMINAL_DIAMETER = ширина линии 4 (при Z=0)");
            Console.WriteLine();
            Console.WriteLine("   Пример расчета коэффициента:");
            Console.WriteLine("   Если линия 4 = 120 μm, линия 7 = 140 μm:");
            Console.WriteLine("   ΔZ = 0.9 - 0.0 = 0.9 мм");
            Console.WriteLine("   Δdiameter = 140 - 120 = 20 μm");
            Console.WriteLine("   Z_COEFFICIENT = 0.9 / (20/10) = 0.45 мм/10μm");
            Console.WriteLine();
            Console.WriteLine("4. Обновите константы в коде!");
            Console.WriteLine();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // УТИЛИТА 5: Проверка корректности конфигурации
        // ═══════════════════════════════════════════════════════════════════════

        public static bool ValidateConfiguration(
            FullScannerConfigExample.ScannerCardConfiguration config)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ВАЛИДАЦИЯ КОНФИГУРАЦИИ СКАНЕРА                          ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            bool isValid = true;
            var errors = new List<string>();
            var warnings = new List<string>();

            // Проверка IP адреса
            if (!System.Net.IPAddress.TryParse(config.CardInfo.IpAddress, out _))
            {
                errors.Add($"Некорректный IP адрес: {config.CardInfo.IpAddress}");
                isValid = false;
            }

            // Проверка фокусного расстояния
            if (config.BeamConfig.FocalLengthMm <= 0)
            {
                errors.Add($"Фокусное расстояние должно быть > 0: {config.BeamConfig.FocalLengthMm}");
                isValid = false;
            }

            // Проверка M²
            if (config.BeamConfig.M2 < 1.0)
            {
                errors.Add($"Фактор M² не может быть < 1.0: {config.BeamConfig.M2}");
                isValid = false;
            }

            if (config.BeamConfig.M2 > 2.0)
            {
                warnings.Add($"Высокий M² ({config.BeamConfig.M2}) - качество луча низкое");
            }

            // Проверка размера поля
            if (config.ScannerConfig.FieldSizeX <= 0 || config.ScannerConfig.FieldSizeY <= 0)
            {
                errors.Add($"Размер поля должен быть > 0: {config.ScannerConfig.FieldSizeX} × {config.ScannerConfig.FieldSizeY}");
                isValid = false;
            }

            // Проверка максимальной мощности
            if (config.LaserPowerConfig.MaxPower <= 0)
            {
                errors.Add($"Максимальная мощность должна быть > 0: {config.LaserPowerConfig.MaxPower}");
                isValid = false;
            }

            // Проверка таблицы коррекции мощности
            if (config.LaserPowerConfig.ActualPowerCorrectionValue.Count < 2)
            {
                errors.Add("Таблица коррекции мощности должна содержать минимум 2 точки");
                isValid = false;
            }

            if (config.LaserPowerConfig.ActualPowerCorrectionValue[0] != 0.0)
            {
                warnings.Add("Первое значение таблицы коррекции должно быть 0.0");
            }

            // Проверка параметров процесса
            if (config.ProcessVariablesMap.MarkSpeed.Count == 0)
            {
                errors.Add("Нет наборов параметров для markSpeed");
                isValid = false;
            }

            foreach (var vars in config.ProcessVariablesMap.MarkSpeed)
            {
                if (vars.MarkSpeed <= 0 || vars.MarkSpeed > 10000)
                {
                    warnings.Add($"Подозрительная скорость маркировки: {vars.MarkSpeed} мм/с");
                }

                if (vars.JumpSpeed <= 0 || vars.JumpSpeed > 50000)
                {
                    warnings.Add($"Подозрительная скорость прыжка: {vars.JumpSpeed} мм/с");
                }

                if (vars.CurPower < 0 || vars.CurPower > config.LaserPowerConfig.MaxPower)
                {
                    errors.Add($"Мощность {vars.CurPower} Вт выходит за пределы 0-{config.LaserPowerConfig.MaxPower} Вт");
                    isValid = false;
                }
            }

            // Проверка Rayleigh length
            double calculatedRayleigh = Math.PI * Math.Pow(config.BeamConfig.MinBeamDiameterMicron / 2.0, 2) *
                config.BeamConfig.M2 / (config.BeamConfig.WavelengthNano / 1000.0);

            double rayleighDiff = Math.Abs(calculatedRayleigh - config.BeamConfig.RayleighLengthMicron) /
                config.BeamConfig.RayleighLengthMicron * 100.0;

            if (rayleighDiff > 5)
            {
                warnings.Add($"Rayleigh length не соответствует расчетному ({calculatedRayleigh:F1} vs {config.BeamConfig.RayleighLengthMicron:F1})");
            }

            // Вывод результатов
            if (errors.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ ОШИБКИ:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  • {error}");
                }
                Console.ResetColor();
                Console.WriteLine();
            }

            if (warnings.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ ПРЕДУПРЕЖДЕНИЯ:");
                foreach (var warning in warnings)
                {
                    Console.WriteLine($"  • {warning}");
                }
                Console.ResetColor();
                Console.WriteLine();
            }

            if (isValid && warnings.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Конфигурация корректна, проблем не обнаружено");
                Console.ResetColor();
            }
            else if (isValid)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Конфигурация валидна, но есть {warnings.Count} предупреждений");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Конфигурация НЕ валидна: {errors.Count} ошибок");
                Console.ResetColor();
            }

            Console.WriteLine();
            return isValid;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // УТИЛИТА 6: Генерация тестового файла для визуализации коррекций
        // ═══════════════════════════════════════════════════════════════════════

        public static void GenerateVisualizationFile(
            FullScannerConfigExample.ScannerCardConfiguration config,
            string outputPath = "corrections_visualization.bin")
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ГЕНЕРАЦИЯ ФАЙЛА ВИЗУАЛИЗАЦИИ КОРРЕКЦИЙ                  ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Параметры
            MarkParameter param = new MarkParameter
            {
                MarkSpeed = 800,
                JumpSpeed = 5000,
                LaserPower = 40.0f,
                PolygonDelay = 100,
                JumpDelay = 100,
                MarkDelay = 100,
                LaserOnDelay = 100,
                LaserOffDelay = 100,
                Frequency = 30.0f,
                DutyCycle = 0.5f
            };

            HM_UDM_DLL.UDM_SetLayersPara(new[] { param }, 1);

            // Сетка для визуализации коррекции кривизны
            int gridSize = 50; // мм
            int layerIdx = 0;

            Console.WriteLine("Создание сетки для визуализации коррекции кривизны поля...");

            for (int x = -150; x <= 150; x += gridSize)
            {
                for (int y = -150; y <= 150; y += gridSize)
                {
                    double r = Math.Sqrt(x * x + y * y);
                    float zCorr = (float)(
                        config.ThirdAxisConfig.BFactor * r +
                        config.ThirdAxisConfig.CFactor
                    );

                    // Маленький крестик в каждой точке сетки
                    structUdmPos[] cross = new structUdmPos[]
                    {
                        new structUdmPos { x = x - 5, y = y, z = zCorr },
                        new structUdmPos { x = x + 5, y = y, z = zCorr },
                        new structUdmPos { x = x, y = y - 5, z = zCorr },
                        new structUdmPos { x = x, y = y + 5, z = zCorr }
                    };

                    HM_UDM_DLL.UDM_AddPolyline3D(cross, 4, layerIdx++);
                }
            }

            // Концентрические круги для демонстрации
            Console.WriteLine("Создание концентрических кругов...");

            int[] radii = { 50, 100, 150, 200 };
            foreach (int radius in radii)
            {
                int points = 36;
                structUdmPos[] circle = new structUdmPos[points + 1];

                for (int i = 0; i <= points; i++)
                {
                    double angle = i * 2 * Math.PI / points;
                    double x = radius * Math.Cos(angle);
                    double y = radius * Math.Sin(angle);

                    double r = Math.Sqrt(x * x + y * y);
                    float zCorr = (float)(
                        config.ThirdAxisConfig.BFactor * r +
                        config.ThirdAxisConfig.CFactor
                    );

                    circle[i] = new structUdmPos
                    {
                        x = (float)x,
                        y = (float)y,
                        z = zCorr
                    };
                }

                HM_UDM_DLL.UDM_AddPolyline3D(circle, circle.Length, layerIdx++);
            }

            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine();
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
            Console.WriteLine();
            Console.WriteLine("Этот файл визуализирует:");
            Console.WriteLine("  • Сетку с Z-коррекцией кривизны поля");
            Console.WriteLine("  • Концентрические круги на разных расстояниях");
            Console.WriteLine("  • Каждая точка имеет свою Z-координату согласно коррекции");
            Console.WriteLine();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ГЛАВНАЯ ФУНКЦИЯ ДЕМОНСТРАЦИИ
        // ═══════════════════════════════════════════════════════════════════════

        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  УТИЛИТЫ ДЛЯ РАБОТЫ С КОНФИГУРАЦИЕЙ СКАНЕРА              ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // УТИЛИТА 1: Таблица диаметр ↔ Z
            PrintDiameterToZTable();

            Console.WriteLine("\nНажмите Enter для продолжения...");
            Console.ReadLine();
            Console.Clear();

            // УТИЛИТА 2: Таблица коррекции кривизны
            PrintFieldCurvatureTable();

            Console.WriteLine("\nНажмите Enter для продолжения...");
            Console.ReadLine();
            Console.Clear();

            // УТИЛИТА 3: Анализ коррекции мощности
            double[] powerTable = { 0.0, 67.0, 176.0, 281.0, 382.0, 475.0 };
            AnalyzePowerCorrection(powerTable, 500.0, -0.6839859, 51.298943);

            Console.WriteLine("\nНажмите Enter для продолжения...");
            Console.ReadLine();
            Console.Clear();

            // УТИЛИТА 4: Генерация калибровочного файла
            Console.WriteLine("Генерация калибровочного файла для Z-offset...\n");
            GenerateZCalibrationFile();

            Console.WriteLine("\nНажмите Enter для завершения...");
            Console.ReadLine();
        }
    }
}
