using System;
using System.Collections.Generic;

/// <summary>
/// Примеры применения диаметра пучка (laser_beam_diameter) в HashuScan
///
/// Диаметр пучка (FOCUS в терминологии PrintMateMC) - это размер лазерного пятна на поверхности материала.
/// Единицы измерения: микрометры (μm)
///
/// Влияние диаметра на процесс печати:
/// - Больший диаметр = больше энергии на площадь = более глубокое проплавление
/// - Меньший диаметр = выше точность = более тонкие детали
/// </summary>
public class DiameterUsageExamples
{
    #region Example 1: Базовое применение диаметра пучка для разных регионов

    /// <summary>
    /// Пример 1: Установка диаметра для различных регионов геометрии
    ///
    /// В CLI файле разные регионы имеют разные параметры диаметра:
    /// - Контур (border): обычно меньший диаметр для точности
    /// - Заполнение (hatch): больший диаметр для производительности
    /// </summary>
    public static void Example1_RegionSpecificDiameter()
    {
        Console.WriteLine("=== Пример 1: Диаметр пучка для разных регионов ===\n");

        // Параметры из JSON в CLI файле
        var borderDiameter = 70.0;   // upskin_border_laser_beam_diameter
        var hatchDiameter = 80.0;    // upskin_hatch_laser_beam_diameter

        var operations = new List<IOperation>();

        // === КОНТУР (Border) ===
        Console.WriteLine($"[КОНТУР] Диаметр пучка: {borderDiameter} μm");
        operations.Add(new DiameterOperation(borderDiameter));
        operations.Add(new PowerOperation(280.0));
        operations.Add(new MarkSpeedOperation(600));

        // Рисуем контур квадрата
        operations.Add(new JumpOperation(-50, -50));
        operations.Add(new MarkOperation(50, -50));   // Нижняя сторона
        operations.Add(new MarkOperation(50, 50));    // Правая сторона
        operations.Add(new MarkOperation(-50, 50));   // Верхняя сторона
        operations.Add(new MarkOperation(-50, -50));  // Левая сторона

        // === ЗАПОЛНЕНИЕ (Hatch) ===
        Console.WriteLine($"[ЗАПОЛНЕНИЕ] Диаметр пучка: {hatchDiameter} μm");
        operations.Add(new DiameterOperation(hatchDiameter));  // Изменяем диаметр!
        operations.Add(new PowerOperation(320.0));
        operations.Add(new MarkSpeedOperation(1200));

        // Рисуем штриховку внутри
        for (double y = -45; y <= 45; y += 0.1)  // Расстояние между линиями = 100 μm
        {
            operations.Add(new JumpOperation(-45, y));
            operations.Add(new MarkOperation(45, y));
        }

        Console.WriteLine($"\nСоздано {operations.Count} операций");
        Console.WriteLine("Контур: тонкий пучок 70 μm для точности");
        Console.WriteLine("Заполнение: широкий пучок 80 μm для скорости\n");
    }

    #endregion

    #region Example 2: Диаметр для разных типов слоев (upskin, downskin, infill)

    /// <summary>
    /// Пример 2: Различный диаметр для верхних, нижних и внутренних слоев
    ///
    /// Типичные значения из CLI файлов:
    /// - Downskin (нижние слои): 80 μm - требуют большего проплавления
    /// - Upskin (верхние слои): 70 μm - требуют гладкой поверхности
    /// - Infill (заполнение): 90 μm - максимальная скорость
    /// </summary>
    public static void Example2_LayerTypeDiameter()
    {
        Console.WriteLine("=== Пример 2: Диаметр для разных типов слоев ===\n");

        // Параметры из CLI JSON
        var downskinDiameter = 80.0;  // downskin_hatch_laser_beam_diameter
        var upskinDiameter = 70.0;    // upskin_hatch_laser_beam_diameter
        var infillDiameter = 90.0;    // infill_hatch_laser_beam_diameter

        // Слой 1: Downskin (первый слой на подложке)
        Console.WriteLine($"Слой 1 (DOWNSKIN): диаметр {downskinDiameter} μm");
        var layer1Ops = new List<IOperation>
        {
            new DiameterOperation(downskinDiameter),
            new PowerOperation(280.0),
            new MarkSpeedOperation(800)
        };
        Console.WriteLine("  → Больший диаметр для хорошего проплавления подложки\n");

        // Слой 50: Infill (середина детали)
        Console.WriteLine($"Слой 50 (INFILL): диаметр {infillDiameter} μm");
        var layer50Ops = new List<IOperation>
        {
            new DiameterOperation(infillDiameter),
            new PowerOperation(350.0),
            new MarkSpeedOperation(1400)
        };
        Console.WriteLine("  → Максимальный диаметр для скорости печати\n");

        // Слой 100: Upskin (последний слой)
        Console.WriteLine($"Слой 100 (UPSKIN): диаметр {upskinDiameter} μm");
        var layer100Ops = new List<IOperation>
        {
            new DiameterOperation(upskinDiameter),
            new PowerOperation(250.0),
            new MarkSpeedOperation(700)
        };
        Console.WriteLine("  → Минимальный диаметр для гладкой верхней поверхности\n");
    }

    #endregion

    #region Example 3: Динамическое изменение диаметра в зависимости от геометрии

    /// <summary>
    /// Пример 3: Изменение диаметра на лету в зависимости от типа геометрии
    ///
    /// Это демонстрирует, как PrintMateMC обрабатывает CLI файл:
    /// - Парсит GeometryID
    /// - Определяет регион (CONTOUR, INFILL, etc.)
    /// - Применяет соответствующий диаметр
    /// </summary>
    public static void Example3_DynamicDiameterChange()
    {
        Console.WriteLine("=== Пример 3: Динамическое изменение диаметра ===\n");

        var operations = new List<IOperation>();

        // Симуляция обработки CLI геометрии
        var geometries = new[]
        {
            new { ID = 10001, Type = "POLYLINE", Region = "CONTOUR_UPSKIN", Diameter = 70.0 },
            new { ID = 10002, Type = "HATCHES", Region = "UPSKIN", Diameter = 75.0 },
            new { ID = 10003, Type = "POLYLINE", Region = "EDGES", Diameter = 65.0 },
            new { ID = 20001, Type = "HATCHES", Region = "INFILL", Diameter = 90.0 }
        };

        double currentDiameter = 0.0;

        foreach (var geom in geometries)
        {
            // Проверяем, нужно ли менять диаметр
            if (Math.Abs(currentDiameter - geom.Diameter) > 0.001)
            {
                operations.Add(new DiameterOperation(geom.Diameter));
                currentDiameter = geom.Diameter;

                Console.WriteLine($"GeometryID {geom.ID} ({geom.Region}):");
                Console.WriteLine($"  → Установлен диаметр {geom.Diameter} μm");
            }
            else
            {
                Console.WriteLine($"GeometryID {geom.ID} ({geom.Region}):");
                Console.WriteLine($"  → Используется текущий диаметр {currentDiameter} μm");
            }
        }

        Console.WriteLine($"\nВсего смен диаметра: {operations.Count}");
        Console.WriteLine("Оптимизация: диаметр меняется только при необходимости\n");
    }

    #endregion

    #region Example 4: Расчет расстояния между линиями штриховки на основе диаметра

    /// <summary>
    /// Пример 4: Расстояние между линиями штриховки зависит от диаметра пучка
    ///
    /// Правило: расстояние = диаметр × коэффициент перекрытия
    /// Типичный коэффициент: 0.7-0.9 для хорошего проплавления
    /// </summary>
    public static void Example4_HatchSpacingFromDiameter()
    {
        Console.WriteLine("=== Пример 4: Расчет расстояния штриховки ===\n");

        var diameters = new[] { 60.0, 80.0, 100.0 };
        var overlapFactor = 0.8;  // 80% перекрытие

        foreach (var diameter in diameters)
        {
            var hatchSpacing = diameter * overlapFactor / 1000.0;  // Конвертируем μm в mm

            Console.WriteLine($"Диаметр пучка: {diameter} μm");
            Console.WriteLine($"  Перекрытие: {overlapFactor * 100}%");
            Console.WriteLine($"  Расстояние между линиями: {hatchSpacing:F3} mm ({diameter * overlapFactor:F1} μm)");

            // Пример генерации штриховки
            var operations = new List<IOperation>
            {
                new DiameterOperation(diameter),
                new PowerOperation(300.0),
                new MarkSpeedOperation(1000)
            };

            int lineCount = 0;
            for (double y = -10.0; y <= 10.0; y += hatchSpacing)
            {
                operations.Add(new JumpOperation(-10.0, y));
                operations.Add(new MarkOperation(10.0, y));
                lineCount++;
            }

            Console.WriteLine($"  Создано линий штриховки: {lineCount}");
            Console.WriteLine($"  Общая длина пути: {lineCount * 20:F1} mm\n");
        }
    }

    #endregion

    #region Example 5: Применение параметров из CLI файла

    /// <summary>
    /// Пример 5: Полный цикл обработки параметров из CLI JSON
    ///
    /// Демонстрирует, как параметры из $PARAMETER_SET применяются к геометрии
    /// </summary>
    public static void Example5_CliParameterSetUsage()
    {
        Console.WriteLine("=== Пример 5: Применение параметров из CLI ===\n");

        // Имитация JSON из CLI файла $PARAMETER_SET
        var parameterSet = new Dictionary<string, object>
        {
            // Downskin параметры
            ["downskin_hatch_laser_beam_diameter"] = 80.0,
            ["downskin_hatch_laser_power"] = 280.0,
            ["downskin_hatch_laser_speed"] = 800,
            ["downskin_hatch_skywriting"] = 0,

            // Upskin параметры
            ["upskin_contour_laser_beam_diameter"] = 70.0,
            ["upskin_contour_laser_power"] = 250.0,
            ["upskin_contour_laser_speed"] = 600,
            ["upskin_contour_skywriting"] = 1,

            // Infill параметры
            ["infill_hatch_laser_beam_diameter"] = 90.0,
            ["infill_hatch_laser_power"] = 350.0,
            ["infill_hatch_laser_speed"] = 1400,
            ["infill_hatch_skywriting"] = 0
        };

        // Функция парсинга параметров (как в JobBuilder.java)
        void ApplyParameters(string region, List<IOperation> ops)
        {
            var prefix = region.ToLower();

            var diameter = (double)parameterSet[$"{prefix}_laser_beam_diameter"];
            var power = (double)parameterSet[$"{prefix}_laser_power"];
            var speed = (int)parameterSet[$"{prefix}_laser_speed"];
            var skywriting = (int)parameterSet[$"{prefix}_skywriting"];

            ops.Add(new DiameterOperation(diameter));
            ops.Add(new PowerOperation(power));
            ops.Add(new MarkSpeedOperation(speed));
            ops.Add(new SWEnableOperation(skywriting == 1));

            Console.WriteLine($"Регион: {region.ToUpper()}");
            Console.WriteLine($"  Диаметр: {diameter} μm");
            Console.WriteLine($"  Мощность: {power} W");
            Console.WriteLine($"  Скорость: {speed} mm/s");
            Console.WriteLine($"  SkyWriting: {(skywriting == 1 ? "ВКЛ" : "ВЫКЛ")}\n");
        }

        var downskinOps = new List<IOperation>();
        var upskinOps = new List<IOperation>();
        var infillOps = new List<IOperation>();

        ApplyParameters("downskin_hatch", downskinOps);
        ApplyParameters("upskin_contour", upskinOps);
        ApplyParameters("infill_hatch", infillOps);

        Console.WriteLine("✓ Параметры успешно применены к 3 регионам");
    }

    #endregion

    #region Example 6: Влияние диаметра на энергетический режим

    /// <summary>
    /// Пример 6: Расчет объемной плотности энергии (VED) с учетом диаметра
    ///
    /// VED = P / (v × h × t)
    /// где:
    /// P - мощность лазера (W)
    /// v - скорость сканирования (mm/s)
    /// h - расстояние между штрихами (mm)
    /// t - толщина слоя (mm)
    ///
    /// Диаметр пучка влияет на расстояние между штрихами (h)
    /// </summary>
    public static void Example6_VolumetricEnergyDensity()
    {
        Console.WriteLine("=== Пример 6: Влияние диаметра на энергетический режим ===\n");

        var power = 300.0;           // W
        var speed = 1000;            // mm/s
        var layerThickness = 0.04;   // mm (40 μm)
        var overlapFactor = 0.8;

        var diameters = new[] { 60.0, 80.0, 100.0 };

        Console.WriteLine($"Параметры:");
        Console.WriteLine($"  Мощность: {power} W");
        Console.WriteLine($"  Скорость: {speed} mm/s");
        Console.WriteLine($"  Толщина слоя: {layerThickness} mm\n");

        foreach (var diameter in diameters)
        {
            var hatchSpacing = (diameter / 1000.0) * overlapFactor;  // μm → mm
            var ved = power / (speed * hatchSpacing * layerThickness);

            Console.WriteLine($"Диаметр пучка: {diameter} μm");
            Console.WriteLine($"  Расстояние штриховки: {hatchSpacing:F4} mm");
            Console.WriteLine($"  VED: {ved:F1} J/mm³");

            if (ved < 50)
                Console.WriteLine($"  ⚠ Низкая энергия - возможна неполная плавка");
            else if (ved > 150)
                Console.WriteLine($"  ⚠ Высокая энергия - возможно испарение");
            else
                Console.WriteLine($"  ✓ Оптимальный энергетический режим");

            Console.WriteLine();
        }
    }

    #endregion

    #region Example 7: Практический пример из реального CLI файла

    /// <summary>
    /// Пример 7: Обработка реального слоя из CLI файла
    ///
    /// Симулирует обработку Layer 42 с разными регионами геометрии
    /// </summary>
    public static void Example7_RealWorldCliLayer()
    {
        Console.WriteLine("=== Пример 7: Обработка реального слоя CLI ===\n");
        Console.WriteLine("Layer #42, Z = 1.68 mm\n");

        var operations = new List<IOperation>();

        // 1. EDGES (края отверстий, тонкие стенки)
        Console.WriteLine("1. EDGES - края отверстий");
        operations.Add(new DiameterOperation(65.0));
        operations.Add(new PowerOperation(240.0));
        operations.Add(new MarkSpeedOperation(500));
        Console.WriteLine("   Диаметр: 65 μm (тонкий для точности)\n");

        // 2. CONTOUR - внешний контур детали
        Console.WriteLine("2. CONTOUR - внешний контур");
        operations.Add(new DiameterOperation(70.0));
        operations.Add(new PowerOperation(260.0));
        operations.Add(new MarkSpeedOperation(600));
        Console.WriteLine("   Диаметр: 70 μm (баланс точность/скорость)\n");

        // 3. UPSKIN - верхние поверхности
        Console.WriteLine("3. UPSKIN - верхние поверхности");
        operations.Add(new DiameterOperation(75.0));
        operations.Add(new PowerOperation(280.0));
        operations.Add(new MarkSpeedOperation(900));
        Console.WriteLine("   Диаметр: 75 μm (гладкая поверхность)\n");

        // 4. INFILL - основное заполнение
        Console.WriteLine("4. INFILL - основное заполнение");
        operations.Add(new DiameterOperation(90.0));
        operations.Add(new PowerOperation(350.0));
        operations.Add(new MarkSpeedOperation(1400));
        Console.WriteLine("   Диаметр: 90 μm (максимальная производительность)\n");

        // 5. SUPPORT - поддержки
        Console.WriteLine("5. SUPPORT - поддержки");
        operations.Add(new DiameterOperation(95.0));
        operations.Add(new PowerOperation(320.0));
        operations.Add(new MarkSpeedOperation(1600));
        Console.WriteLine("   Диаметр: 95 μm (быстрая печать, легко удаляются)\n");

        Console.WriteLine($"Итого: {operations.Count} операций настройки");
        Console.WriteLine("Диаметр меняется 5 раз в зависимости от типа геометрии");
    }

    #endregion

    // Основная программа
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  Примеры применения диаметра пучка в HashuScan           ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        Example1_RegionSpecificDiameter();
        Console.WriteLine(new string('─', 60) + "\n");

        Example2_LayerTypeDiameter();
        Console.WriteLine(new string('─', 60) + "\n");

        Example3_DynamicDiameterChange();
        Console.WriteLine(new string('─', 60) + "\n");

        Example4_HatchSpacingFromDiameter();
        Console.WriteLine(new string('─', 60) + "\n");

        Example5_CliParameterSetUsage();
        Console.WriteLine(new string('─', 60) + "\n");

        Example6_VolumetricEnergyDensity();
        Console.WriteLine(new string('─', 60) + "\n");

        Example7_RealWorldCliLayer();

        Console.WriteLine("\n\n✓ Все примеры выполнены успешно!");
    }
}

#region Определения классов операций (заглушки для примеров)

public interface IOperation { }

public class DiameterOperation : IOperation
{
    public double Value { get; }
    public DiameterOperation(double value) => Value = value;
}

public class PowerOperation : IOperation
{
    public double Value { get; }
    public PowerOperation(double value) => Value = value;
}

public class MarkSpeedOperation : IOperation
{
    public int Value { get; }
    public MarkSpeedOperation(int value) => Value = value;
}

public class SWEnableOperation : IOperation
{
    public bool Value { get; }
    public SWEnableOperation(bool value) => Value = value;
}

public class MarkOperation : IOperation
{
    public double X { get; }
    public double Y { get; }
    public MarkOperation(double x, double y) { X = x; Y = y; }
}

public class JumpOperation : IOperation
{
    public double X { get; }
    public double Y { get; }
    public JumpOperation(double x, double y) { X = x; Y = y; }
}

#endregion
