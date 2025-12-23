using System;
using Hans.NET;

/// <summary>
/// ВАША КОНФИГУРАЦИЯ: F-theta линза 538.46 мм
///
/// Параметры рассчитаны для вашей оптической системы
/// </summary>
public class YourSystem_538mm
{
    // ============================================================================
    // ПАРАМЕТРЫ ВАШЕЙ СИСТЕМЫ (F-theta 538.46 мм)
    // ============================================================================

    /// <summary>
    /// Номинальный диаметр пучка при Z=0
    ///
    /// Для линзы 538.46 мм типичный диаметр: 120-150 μm
    ///
    /// Формула: d ≈ 4 × λ × f / (π × D)
    /// где:
    ///   λ = 1.064 μm (длина волны для 1064 нм лазера)
    ///   f = 538.46 мм (фокусное расстояние)
    ///   D = диаметр входящего луча (обычно 10-14 мм)
    ///
    /// Пример расчета:
    /// d = 4 × 1.064 × 538.46 / (3.14159 × 12) ≈ 60.8 μm (теоретический)
    ///
    /// НО! Реальный диаметр обычно больше из-за аберраций: ~120-150 μm
    /// </summary>
    public const double NOMINAL_DIAMETER_UM = 120.0; // μm при Z=0

    /// <summary>
    /// Коэффициент Z → диаметр (мм на 10 μm изменения диаметра)
    ///
    /// Для больших линз коэффициент выше (луч расходится медленнее)
    ///
    /// Расчет для вашей линзы:
    /// - Rayleigh range: z_R = π × w₀² / λ
    /// - Для w₀ ≈ 60 μm: z_R ≈ 10.7 мм
    /// - Коэффициент ≈ 0.2 - 0.4 мм/10μm
    ///
    /// Рекомендуемое значение для 538 мм линзы: 0.3
    /// </summary>
    public const double Z_COEFFICIENT = 0.3; // мм на 10 μm

    // ============================================================================

    /// <summary>
    /// Конвертация диаметра в Z-смещение для ВАШЕЙ системы
    /// </summary>
    /// <param name="targetDiameter">Желаемый диаметр в μm</param>
    /// <returns>Z-смещение в мм</returns>
    public static float DiameterToZ(double targetDiameter)
    {
        return (float)((targetDiameter - NOMINAL_DIAMETER_UM) / 10.0 * Z_COEFFICIENT);
    }

    /// <summary>
    /// Обратная конвертация: Z → диаметр
    /// </summary>
    public static double ZToDiameter(float z)
    {
        return NOMINAL_DIAMETER_UM + (z / Z_COEFFICIENT * 10.0);
    }

    /// <summary>
    /// ПРАКТИЧЕСКИЙ ПРИМЕР: Применение диаметра 80 μm из CLI
    /// </summary>
    public static void Example_ApplyDiameter80()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  ВАША СИСТЕМА: F-theta 538.46 мм                         ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Параметры из CLI
        double diameter = 80.0;  // μm из downskin_hatch_laser_beam_diameter
        double power = 280.0;    // W
        int speed = 800;         // mm/s

        Console.WriteLine("Параметры из CLI файла:");
        Console.WriteLine($"  Диаметр: {diameter} μm");
        Console.WriteLine($"  Мощность: {power} W");
        Console.WriteLine($"  Скорость: {speed} mm/s");
        Console.WriteLine();

        // Конвертация диаметра в Z
        float z = DiameterToZ(diameter);

        Console.WriteLine("Расчет Z для вашей системы:");
        Console.WriteLine($"  Номинальный диаметр: {NOMINAL_DIAMETER_UM} μm (при Z=0)");
        Console.WriteLine($"  Коэффициент: {Z_COEFFICIENT} мм/10μm");
        Console.WriteLine($"  Формула: z = ({diameter} - {NOMINAL_DIAMETER_UM}) / 10.0 * {Z_COEFFICIENT}");
        Console.WriteLine($"  Результат: z = {z:F3} мм");
        Console.WriteLine();

        // Инициализация Hans
        HM_UDM_DLL.UDM_NewFile();
        HM_UDM_DLL.UDM_SetProtocol(0, 1); // 3D режим!

        Console.WriteLine("✓ Hans инициализирован в 3D режиме");
        Console.WriteLine();

        // Настройка параметров слоя
        MarkParameter[] layers = new MarkParameter[1];
        layers[0] = new MarkParameter
        {
            MarkSpeed = (uint)speed,
            JumpSpeed = 5000,
            LaserPower = (float)(power / 500.0 * 100.0), // Конвертация W → %
            MarkDelay = 100,
            JumpDelay = 100,
            PolygonDelay = 50,
            Frequency = 30.0f,
            DutyCycle = 0.5f
        };

        HM_UDM_DLL.UDM_SetLayersPara(layers, 1);
        Console.WriteLine("✓ Параметры слоя установлены");
        Console.WriteLine();

        // Добавление геометрии с Z для диаметра 80 μm
        structUdmPos[] polyline = new structUdmPos[]
        {
            new structUdmPos { x = -50, y = -50, z = z },
            new structUdmPos { x = 50, y = -50, z = z },
            new structUdmPos { x = 50, y = 50, z = z },
            new structUdmPos { x = -50, y = 50, z = z },
            new structUdmPos { x = -50, y = -50, z = z }
        };

        HM_UDM_DLL.UDM_AddPolyline3D(polyline, 5, 0);

        Console.WriteLine($"✓ Геометрия добавлена с Z = {z:F3} мм");
        Console.WriteLine($"  → Диаметр пучка: {diameter} μm");
        Console.WriteLine();

        // Генерация
        HM_UDM_DLL.UDM_Main();
        HM_UDM_DLL.UDM_SaveToFile("diameter_80um_f538.bin");
        HM_UDM_DLL.UDM_EndMain();

        Console.WriteLine("✓ Файл сохранен: diameter_80um_f538.bin");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// Таблица конвертации для разных диаметров
    /// </summary>
    public static void ShowConversionTable()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  ТАБЛИЦА КОНВЕРТАЦИИ: Диаметр → Z (F-theta 538.46 мм)   ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Параметры системы:");
        Console.WriteLine($"  Номинальный диаметр: {NOMINAL_DIAMETER_UM} μm");
        Console.WriteLine($"  Коэффициент: {Z_COEFFICIENT} мм/10μm");
        Console.WriteLine();
        Console.WriteLine("┌──────────────┬────────────┬─────────────────────────────┐");
        Console.WriteLine("│ Диаметр (μm) │ Z (мм)     │ Назначение                  │");
        Console.WriteLine("├──────────────┼────────────┼─────────────────────────────┤");

        var testCases = new[]
        {
            (60.0, "Edges, мелкие детали"),
            (70.0, "Контур (CONTOUR)"),
            (80.0, "Downskin, Upskin"),
            (90.0, "Infill (заполнение)"),
            (100.0, "Поддержки (SUPPORT)"),
            (120.0, "Номинальный диаметр"),
            (140.0, "Грубая печать"),
            (160.0, "Максимум для оптики")
        };

        foreach (var (diameter, purpose) in testCases)
        {
            float z = DiameterToZ(diameter);
            string zStr = $"{z:+0.000;-0.000;0.000}";
            Console.WriteLine($"│ {diameter,12:F1} │ {zStr,10} │ {purpose,-27} │");
        }

        Console.WriteLine("└──────────────┴────────────┴─────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("ПРИМЕЧАНИЯ:");
        Console.WriteLine("  • Z > 0: Расфокусировка (больший диаметр)");
        Console.WriteLine("  • Z < 0: Фокусировка (меньший диаметр)");
        Console.WriteLine("  • Z = 0: Номинальный диаметр");
        Console.WriteLine($"  • Рекомендуемый диапазон Z: ±{Z_COEFFICIENT * 4:F1} мм");
        Console.WriteLine();
    }

    /// <summary>
    /// Обработка всех регионов из CLI с правильными диаметрами
    /// </summary>
    public static void ProcessAllRegionsFromCLI()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  ОБРАБОТКА ВСЕХ РЕГИОНОВ CLI                             ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Параметры из CLI $PARAMETER_SET
        var cliParams = new[]
        {
            new { Region = "edges", Diameter = 65.0, Power = 240.0, Speed = 500 },
            new { Region = "upskin_contour", Diameter = 70.0, Power = 250.0, Speed = 600 },
            new { Region = "downskin_hatch", Diameter = 80.0, Power = 280.0, Speed = 800 },
            new { Region = "infill_hatch", Diameter = 90.0, Power = 350.0, Speed = 1400 },
            new { Region = "support_hatch", Diameter = 100.0, Power = 320.0, Speed = 1600 }
        };

        HM_UDM_DLL.UDM_NewFile();
        HM_UDM_DLL.UDM_SetProtocol(0, 1); // 3D режим

        Console.WriteLine("Регионы и их параметры:");
        Console.WriteLine();

        int layerIndex = 0;
        foreach (var param in cliParams)
        {
            float z = DiameterToZ(param.Diameter);

            Console.WriteLine($"Регион: {param.Region.ToUpper()}");
            Console.WriteLine($"  Диаметр: {param.Diameter} μm → Z = {z:F3} мм");
            Console.WriteLine($"  Мощность: {param.Power} W");
            Console.WriteLine($"  Скорость: {param.Speed} mm/s");

            // Настройка параметров
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = new MarkParameter
            {
                MarkSpeed = (uint)param.Speed,
                LaserPower = (float)(param.Power / 500.0 * 100.0),
                JumpSpeed = 5000,
                MarkDelay = 100,
                JumpDelay = 100,
                PolygonDelay = 50,
                Frequency = 30.0f,
                DutyCycle = 0.5f
            };
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            // Пример геометрии (в реальности из CLI парсера)
            structUdmPos[] points = new structUdmPos[]
            {
                new structUdmPos { x = -10, y = -10, z = z },
                new structUdmPos { x = 10, y = -10, z = z },
                new structUdmPos { x = 10, y = 10, z = z },
                new structUdmPos { x = -10, y = 10, z = z }
            };

            HM_UDM_DLL.UDM_AddPolyline3D(points, 4, layerIndex++);

            Console.WriteLine($"  ✓ Геометрия добавлена");
            Console.WriteLine();
        }

        HM_UDM_DLL.UDM_Main();
        HM_UDM_DLL.UDM_SaveToFile("all_regions_f538.bin");
        HM_UDM_DLL.UDM_EndMain();

        Console.WriteLine("✓ Все регионы обработаны и сохранены");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// КАЛИБРОВКА: Как найти точные параметры для вашей системы
    /// </summary>
    public static void RunCalibration()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  КАЛИБРОВКА СИСТЕМЫ F-theta 538.46 мм                    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        Console.WriteLine("ТЕКУЩИЕ ПАРАМЕТРЫ (предварительные):");
        Console.WriteLine($"  Номинальный диаметр: {NOMINAL_DIAMETER_UM} μm");
        Console.WriteLine($"  Коэффициент: {Z_COEFFICIENT} мм/10μm");
        Console.WriteLine();

        Console.WriteLine("ДЛЯ ТОЧНОЙ КАЛИБРОВКИ:");
        Console.WriteLine("───────────────────────────────────────────────────────────");
        Console.WriteLine();

        Console.WriteLine("ШАГ 1: Создайте тестовый файл");
        Console.WriteLine("─────────────────────────────");

        HM_UDM_DLL.UDM_NewFile();
        HM_UDM_DLL.UDM_SetProtocol(0, 1);

        // Тестовые параметры
        MarkParameter[] layers = new MarkParameter[1];
        layers[0] = new MarkParameter
        {
            MarkSpeed = 800,
            LaserPower = 50.0f,
            JumpSpeed = 5000,
            MarkDelay = 100,
            JumpDelay = 100,
            PolygonDelay = 50,
            Frequency = 30.0f,
            DutyCycle = 0.5f
        };
        HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

        // Создаем тестовые линии с разным Z
        float[] testZValues = { -0.6f, -0.3f, 0.0f, 0.3f, 0.6f };

        for (int i = 0; i < testZValues.Length; i++)
        {
            float z = testZValues[i];
            float yPos = -40 + i * 20;

            structUdmPos[] line = new structUdmPos[]
            {
                new structUdmPos { x = -40, y = yPos, z = z },
                new structUdmPos { x = 40, y = yPos, z = z }
            };

            HM_UDM_DLL.UDM_AddPolyline3D(line, 2, i);

            Console.WriteLine($"  Линия {i + 1}: Z = {z:+0.0;-0.0;0.0} мм, Y = {yPos} мм");
        }

        HM_UDM_DLL.UDM_Main();
        HM_UDM_DLL.UDM_SaveToFile("calibration_test_f538.bin");
        HM_UDM_DLL.UDM_EndMain();

        Console.WriteLine();
        Console.WriteLine("✓ Файл сохранен: calibration_test_f538.bin");
        Console.WriteLine();

        Console.WriteLine("ШАГ 2: Напечатайте тестовый файл");
        Console.WriteLine("──────────────────────────────────");
        Console.WriteLine("  Запустите calibration_test_f538.bin на вашем сканере");
        Console.WriteLine();

        Console.WriteLine("ШАГ 3: Измерьте ширину линий");
        Console.WriteLine("──────────────────────────────");
        Console.WriteLine("  Используйте микроскоп для измерения ширины каждой линии:");
        Console.WriteLine("  Линия 1 (Z=-0.6): _____ μm");
        Console.WriteLine("  Линия 2 (Z=-0.3): _____ μm");
        Console.WriteLine("  Линия 3 (Z=0.0):  _____ μm ← Это ваш номинальный диаметр!");
        Console.WriteLine("  Линия 4 (Z=+0.3): _____ μm");
        Console.WriteLine("  Линия 5 (Z=+0.6): _____ μm");
        Console.WriteLine();

        Console.WriteLine("ШАГ 4: Рассчитайте параметры");
        Console.WriteLine("─────────────────────────────");
        Console.WriteLine("  Пример расчета:");
        Console.WriteLine("  Если линия 3 (Z=0) имеет ширину 120 μm,");
        Console.WriteLine("  а линия 5 (Z=+0.6) имеет ширину 140 μm:");
        Console.WriteLine();
        Console.WriteLine("  Номинальный диаметр = 120 μm");
        Console.WriteLine("  ΔZ = 0.6 мм");
        Console.WriteLine("  Δdiameter = 140 - 120 = 20 μm");
        Console.WriteLine("  Коэффициент = 0.6 / (20/10) = 0.3 мм/10μm");
        Console.WriteLine();
        Console.WriteLine("  Обновите константы в коде:");
        Console.WriteLine("  NOMINAL_DIAMETER_UM = 120.0;");
        Console.WriteLine("  Z_COEFFICIENT = 0.3;");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
    }

    // ГЛАВНАЯ ФУНКЦИЯ
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Показываем таблицу конвертации
        ShowConversionTable();

        Console.WriteLine("Нажмите Enter для продолжения...");
        Console.ReadLine();
        Console.Clear();

        // Пример применения диаметра 80 μm
        Example_ApplyDiameter80();

        Console.WriteLine("Нажмите Enter для продолжения...");
        Console.ReadLine();
        Console.Clear();

        // Обработка всех регионов
        ProcessAllRegionsFromCLI();

        Console.WriteLine("\n\nХотите запустить калибровку? (y/n)");
        if (Console.ReadLine()?.ToLower() == "y")
        {
            Console.Clear();
            RunCalibration();
        }

        Console.WriteLine("\n\n✓ Готово!");
    }
}
