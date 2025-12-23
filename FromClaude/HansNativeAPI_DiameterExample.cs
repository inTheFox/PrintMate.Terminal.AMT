using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Hans.NET;

/// <summary>
/// Как применить диаметр пучка в HM_HashuScan.dll
///
/// ВАЖНО: В Hans API диаметр пучка НЕ устанавливается напрямую!
/// Вместо этого используется ПАРАМЕТР Z в 3D-режиме или настройка фокуса в конфигурации.
/// </summary>
public class HansNativeAPI_DiameterExample
{
    /// <summary>
    /// СПОСОБ 1: Через параметр Z в 3D режиме
    ///
    /// В Hans сканере диаметр пучка связан с положением Z (фокусным расстоянием).
    /// Когда вы меняете Z, меняется фокус, что влияет на диаметр пятна.
    /// </summary>
    public static void Method1_Using3D_Z_Parameter()
    {
        Console.WriteLine("=== СПОСОБ 1: Диаметр через параметр Z (3D) ===\n");

        // Инициализация
        HM_UDM_DLL.UDM_NewFile();
        HM_UDM_DLL.UDM_SetProtocol(0, 1); // 3D режим!

        // Ваш диаметр пучка из CLI
        double desiredDiameter = 80.0; // μm

        // Конвертация диаметра в Z-смещение
        // Формула зависит от вашей оптики, примерная:
        // Z = (diameter - nominalDiameter) * conversionFactor
        double nominalDiameter = 70.0; // μm, номинальный диаметр при Z=0
        double conversionFactor = 1.0;  // мм на 10 μm изменения диаметра

        float zOffset = (float)((desiredDiameter - nominalDiameter) / 10.0 * conversionFactor);

        Console.WriteLine($"Желаемый диаметр: {desiredDiameter} μm");
        Console.WriteLine($"Z-смещение: {zOffset:F3} mm");
        Console.WriteLine();

        // Настройка параметров слоя
        MarkParameter[] layers = new MarkParameter[1];
        layers[0] = new MarkParameter
        {
            MarkSpeed = 800,
            JumpSpeed = 5000,
            LaserPower = 50.0f,
            // ... остальные параметры
        };
        HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

        // КЛЮЧЕВОЙ МОМЕНТ: Используем 3D функции с Z параметром
        structUdmPos[] polyline = new structUdmPos[4];

        polyline[0] = new structUdmPos { x = -10, y = -10, z = zOffset }; // ← Z влияет на диаметр!
        polyline[1] = new structUdmPos { x = 10, y = -10, z = zOffset };
        polyline[2] = new structUdmPos { x = 10, y = 10, z = zOffset };
        polyline[3] = new structUdmPos { x = -10, y = 10, z = zOffset };

        HM_UDM_DLL.UDM_AddPolyline3D(polyline, 4, 0);

        Console.WriteLine("✓ Полилиния добавлена с Z-смещением для диаметра 80 μm");
        Console.WriteLine();

        // Завершение
        HM_UDM_DLL.UDM_Main();
        HM_UDM_DLL.UDM_SaveToFile("output.bin");
        HM_UDM_DLL.UDM_EndMain();
    }

    /// <summary>
    /// СПОСОБ 2: Через коррекцию 3D (field curvature)
    ///
    /// Используется для компенсации кривизны поля, что влияет на фокус и диаметр
    /// </summary>
    public static void Method2_Using3D_Correction()
    {
        Console.WriteLine("=== СПОСОБ 2: Диаметр через 3D коррекцию ===\n");

        HM_UDM_DLL.UDM_NewFile();
        HM_UDM_DLL.UDM_SetProtocol(0, 1); // 3D режим

        // Параметры коррекции (пример из scanner_config.json)
        float baseFocal = 0.0f; // Базовое фокусное расстояние

        // Коэффициенты коррекции для вашего диаметра
        // Обычно считываются из конфигурации сканера
        double[] paraK = new double[]
        {
            0.0,      // A: квадратичный коэффициент
            0.0,      // B: линейный коэффициент
            0.001     // C: постоянное смещение (влияет на диаметр)
        };

        HM_UDM_DLL.UDM_Set3dCorrectionPara(baseFocal, paraK, paraK.Length);

        Console.WriteLine($"Базовый фокус: {baseFocal} mm");
        Console.WriteLine($"Коррекция установлена: A={paraK[0]}, B={paraK[1]}, C={paraK[2]}");
        Console.WriteLine();

        // Теперь добавляем геометрию
        MarkParameter[] layers = new MarkParameter[1];
        layers[0] = new MarkParameter
        {
            MarkSpeed = 800,
            JumpSpeed = 5000,
            LaserPower = 50.0f
        };
        HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

        // Получаем скорректированное Z значение
        float x = 0.0f, y = 0.0f, height = 0.0f;
        int zValue = HM_UDM_DLL.UDM_GetZvalue(x, y, height);

        Console.WriteLine($"Скорректированное Z для точки ({x}, {y}): {zValue}");
        Console.WriteLine("✓ 3D коррекция применена для контроля диаметра");
        Console.WriteLine();

        HM_UDM_DLL.UDM_EndMain();
    }

    /// <summary>
    /// СПОСОБ 3: Косвенно через мощность лазера (LaserPower)
    ///
    /// Хотя это не прямой контроль диаметра, изменение мощности влияет
    /// на эффективный диаметр расплава
    /// </summary>
    public static void Method3_Indirect_Via_LaserPower()
    {
        Console.WriteLine("=== СПОСОБ 3: Косвенное влияние через мощность ===\n");

        HM_UDM_DLL.UDM_NewFile();
        HM_UDM_DLL.UDM_SetProtocol(0, 0); // 2D режим

        // Ваш диаметр из CLI
        double diameter = 80.0; // μm

        // Расчет мощности на основе диаметра (примерная формула)
        // Больший диаметр требует большей мощности для того же эффекта
        double basePower = 50.0; // % при диаметре 70 μm
        double baseDiameter = 70.0; // μm

        // P = P_base × (D / D_base)²
        double calculatedPower = basePower * Math.Pow(diameter / baseDiameter, 2);

        Console.WriteLine($"Диаметр: {diameter} μm");
        Console.WriteLine($"Рассчитанная мощность: {calculatedPower:F1}%");
        Console.WriteLine();

        // Настройка слоя с рассчитанной мощностью
        MarkParameter[] layers = new MarkParameter[1];
        layers[0] = new MarkParameter
        {
            MarkSpeed = 800,
            JumpSpeed = 5000,
            LaserPower = (float)calculatedPower, // ← Компенсация диаметра
            MarkDelay = 100,
            JumpDelay = 100,
            PolygonDelay = 50,
            Frequency = 30.0f,
            DutyCycle = 0.5f
        };

        HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

        Console.WriteLine("✓ Мощность настроена для компенсации диаметра 80 μm");
        Console.WriteLine();

        // Добавляем геометрию
        structUdmPos[] points = new structUdmPos[5];
        points[0] = new structUdmPos { x = -10, y = -10 };
        points[1] = new structUdmPos { x = 10, y = -10 };
        points[2] = new structUdmPos { x = 10, y = 10 };
        points[3] = new structUdmPos { x = -10, y = 10 };
        points[4] = new structUdmPos { x = -10, y = -10 };

        HM_UDM_DLL.UDM_AddPolyline2D(points, 5, 0);

        HM_UDM_DLL.UDM_Main();
        HM_UDM_DLL.UDM_SaveToFile("output.bin");
        HM_UDM_DLL.UDM_EndMain();
    }

    /// <summary>
    /// ПРАКТИЧЕСКИЙ ПРИМЕР: Применение диаметра из CLI файла
    ///
    /// Это то, что вам нужно в реальном коде!
    /// </summary>
    public static void PracticalExample_ApplyDiameterFromCLI()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  ПРАКТИЧЕСКИЙ ПРИМЕР: Применение диаметра из CLI       ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // ШАГ 1: Получили параметры из CLI
        var cliParams = new
        {
            diameter = 80.0,  // μm - из downskin_hatch_laser_beam_diameter
            power = 280.0,    // W  - из downskin_hatch_laser_power
            speed = 800       // mm/s - из downskin_hatch_laser_speed
        };

        Console.WriteLine("Параметры из CLI файла:");
        Console.WriteLine($"  Диаметр: {cliParams.diameter} μm");
        Console.WriteLine($"  Мощность: {cliParams.power} W");
        Console.WriteLine($"  Скорость: {cliParams.speed} mm/s");
        Console.WriteLine();

        // ШАГ 2: Инициализация Hans
        HM_UDM_DLL.UDM_NewFile();

        // Выбираем режим в зависимости от того, нужен ли Z-контроль
        bool use3DMode = true; // Если нужен точный контроль диаметра через Z

        if (use3DMode)
        {
            HM_UDM_DLL.UDM_SetProtocol(0, 1); // 3D режим
            Console.WriteLine("✓ Установлен 3D режим для контроля диаметра через Z");
        }
        else
        {
            HM_UDM_DLL.UDM_SetProtocol(0, 0); // 2D режим
            Console.WriteLine("✓ Установлен 2D режим");
        }
        Console.WriteLine();

        // ШАГ 3: Конвертация диаметра в Z-смещение (для 3D режима)
        float zForDiameter = 0.0f;

        if (use3DMode)
        {
            // Параметры вашей оптики (из конфигурации сканера)
            double nominalDiameter = 70.0; // μm при Z=0
            double zPerDiameterChange = 0.1; // мм Z на 10 μm изменения диаметра

            double diameterDelta = cliParams.diameter - nominalDiameter; // 80 - 70 = 10 μm
            zForDiameter = (float)(diameterDelta / 10.0 * zPerDiameterChange);

            Console.WriteLine($"Расчет Z для диаметра {cliParams.diameter} μm:");
            Console.WriteLine($"  Номинальный диаметр: {nominalDiameter} μm (при Z=0)");
            Console.WriteLine($"  Дельта диаметра: {diameterDelta} μm");
            Console.WriteLine($"  Требуемое Z-смещение: {zForDiameter:F3} mm");
            Console.WriteLine();
        }

        // ШАГ 4: Настройка параметров слоя
        MarkParameter[] layers = new MarkParameter[1];
        layers[0] = new MarkParameter
        {
            MarkSpeed = (uint)cliParams.speed,
            JumpSpeed = 5000,
            MarkDelay = 100,
            JumpDelay = 100,
            PolygonDelay = 50,
            MarkCount = 1,
            LaserOnDelay = 50.0f,
            LaserOffDelay = 50.0f,
            FPKDelay = 0.0f,
            FPKLength = 0.0f,
            QDelay = 0.0f,
            DutyCycle = 0.5f,
            Frequency = 30.0f,
            StandbyFrequency = 30.0f,
            StandbyDutyCycle = 0.5f,
            LaserPower = (float)(cliParams.power / 500.0 * 100.0), // Конвертация W в %
            AnalogMode = 0,
            Waveform = 0,
            PulseWidthMode = 0,
            PulseWidth = 0
        };

        HM_UDM_DLL.UDM_SetLayersPara(layers, 1);
        Console.WriteLine("✓ Параметры слоя установлены");
        Console.WriteLine();

        // ШАГ 5: Добавление геометрии с применением диаметра
        Console.WriteLine("Добавление геометрии:");

        if (use3DMode)
        {
            // В 3D режиме используем Z для контроля диаметра
            structUdmPos[] polyline3D = new structUdmPos[]
            {
                new structUdmPos { x = -10, y = -10, z = zForDiameter }, // ← Z применяет диаметр!
                new structUdmPos { x = 10, y = -10, z = zForDiameter },
                new structUdmPos { x = 10, y = 10, z = zForDiameter },
                new structUdmPos { x = -10, y = 10, z = zForDiameter },
                new structUdmPos { x = -10, y = -10, z = zForDiameter }
            };

            HM_UDM_DLL.UDM_AddPolyline3D(polyline3D, 5, 0);
            Console.WriteLine($"  ✓ Добавлена 3D полилиния с Z={zForDiameter:F3} mm (диаметр {cliParams.diameter} μm)");
        }
        else
        {
            // В 2D режиме диаметр контролируется только через конфигурацию
            structUdmPos[] polyline2D = new structUdmPos[]
            {
                new structUdmPos { x = -10, y = -10 },
                new structUdmPos { x = 10, y = -10 },
                new structUdmPos { x = 10, y = 10 },
                new structUdmPos { x = -10, y = 10 },
                new structUdmPos { x = -10, y = -10 }
            };

            HM_UDM_DLL.UDM_AddPolyline2D(polyline2D, 5, 0);
            Console.WriteLine($"  ✓ Добавлена 2D полилиния (диаметр из конфигурации сканера)");
        }
        Console.WriteLine();

        // ШАГ 6: Генерация и сохранение
        HM_UDM_DLL.UDM_Main();
        HM_UDM_DLL.UDM_SaveToFile("diameter_80um_output.bin");
        HM_UDM_DLL.UDM_EndMain();

        Console.WriteLine("✓ Файл сохранен: diameter_80um_output.bin");
        Console.WriteLine();

        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("РЕЗЮМЕ:");
        Console.WriteLine($"  Диаметр {cliParams.diameter} μm применен через:");
        if (use3DMode)
            Console.WriteLine($"  - Z-смещение: {zForDiameter:F3} mm");
        Console.WriteLine($"  - Скорость маркировки: {cliParams.speed} mm/s");
        Console.WriteLine($"  - Мощность лазера: {layers[0].LaserPower:F1}%");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// ВАЖНАЯ ИНФОРМАЦИЯ: Почему нет прямой функции для диаметра
    /// </summary>
    public static void ExplainWhy()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  ПОЧЕМУ НЕТ ПРЯМОЙ ФУНКЦИИ ДЛЯ ДИАМЕТРА?               ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        Console.WriteLine("В Hans API (HM_HashuScan.dll) НЕТ функции типа:");
        Console.WriteLine("  ❌ UDM_SetDiameter(float diameter)");
        Console.WriteLine("  ❌ UDM_SetBeamSize(float size)");
        Console.WriteLine();

        Console.WriteLine("ПРИЧИНЫ:");
        Console.WriteLine("─────────────────────────────────────────────────────────");
        Console.WriteLine("1. Диаметр пучка - это ОПТИЧЕСКИЙ параметр");
        Console.WriteLine("   Зависит от:");
        Console.WriteLine("   - Фокусного расстояния линзы");
        Console.WriteLine("   - Положения по оси Z");
        Console.WriteLine("   - Настройки оптической системы");
        Console.WriteLine();

        Console.WriteLine("2. Диаметр контролируется КОСВЕННО через:");
        Console.WriteLine("   ✓ Z-координату (в 3D режиме)");
        Console.WriteLine("   ✓ 3D коррекцию (UDM_Set3dCorrectionPara)");
        Console.WriteLine("   ✓ Конфигурацию сканера (system.ini)");
        Console.WriteLine();

        Console.WriteLine("3. В конфигурации сканера (system.ini) есть:");
        Console.WriteLine("   - FocusZ = базовое положение фокуса");
        Console.WriteLine("   - FieldCurvature = коррекция кривизны");
        Console.WriteLine("   - Эти параметры определяют диаметр по умолчанию");
        Console.WriteLine();

        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    // ГЛАВНАЯ ФУНКЦИЯ
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Объяснение
        ExplainWhy();

        Console.WriteLine("Нажмите Enter для запуска практического примера...");
        Console.ReadLine();
        Console.Clear();

        // Практический пример
        PracticalExample_ApplyDiameterFromCLI();

        Console.WriteLine("\n\n📖 Хотите увидеть другие способы? (y/n)");
        if (Console.ReadLine()?.ToLower() == "y")
        {
            Console.Clear();
            Method1_Using3D_Z_Parameter();

            Console.WriteLine("Нажмите Enter для продолжения...");
            Console.ReadLine();
            Console.Clear();

            Method2_Using3D_Correction();

            Console.WriteLine("Нажмите Enter для продолжения...");
            Console.ReadLine();
            Console.Clear();

            Method3_Indirect_Via_LaserPower();
        }

        Console.WriteLine("\n\n✓ Готово! Теперь вы знаете, как применить диаметр пучка в Hans API.");
    }
}
