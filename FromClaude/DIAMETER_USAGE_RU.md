# Применение диаметра пучка (laser_beam_diameter) в HashuScan

## Что такое диаметр пучка?

**Диаметр пучка** (laser beam diameter, FOCUS в коде PrintMateMC) - это размер лазерного пятна на поверхности материала, измеряемый в **микрометрах (μm)**.

### ⚠️ Важно понимать:

- **Это НЕ координата Z** (высота/фокусное расстояние)
- Это **параметр фокусировки оптики**, который определяет размер пятна
- Устанавливается через `DiameterOperation` в HashuScan API
- Влияет на качество, скорость и энергетику процесса

---

## Как задается в коде

### Java (PrintMateMC):
```java
// Из CLI параметра создается операция
case FOCUS:
    return new DiameterOperation((double) getFloatVal(0));  // 80 μm
```

### C#:
```csharp
// Установка диаметра пучка 80 микрометров
var diameterOp = new DiameterOperation(80.0);

// Отправка в сканер
scannerOperations.Add(diameterOp);
```

---

## Типичные значения диаметра для разных регионов

| Регион геометрии | Диаметр (μm) | Назначение | Причина |
|-----------------|--------------|------------|---------|
| **EDGES** | 60-65 | Края отверстий | Максимальная точность |
| **CONTOUR** | 70-75 | Внешний контур | Баланс точность/скорость |
| **CONTOUR_UPSKIN** | 70 | Контур верх. слоя | Гладкая поверхность |
| **CONTOUR_DOWNSKIN** | 75 | Контур ниж. слоя | Проплавление подложки |
| **UPSKIN** | 70-75 | Верхние слои | Качество поверхности |
| **DOWNSKIN** | 80 | Нижние слои | Хорошее проплавление |
| **INFILL** | 85-95 | Заполнение | Максимальная скорость |
| **SUPPORT** | 90-100 | Поддержки | Быстрая печать |

---

## Примеры применения

### Пример 1: Базовое использование

```csharp
var operations = new List<IOperation>();

// Установка диаметра для контура
operations.Add(new DiameterOperation(70.0));  // 70 μm
operations.Add(new PowerOperation(260.0));     // 260 W
operations.Add(new MarkSpeedOperation(600));   // 600 mm/s

// Рисование квадрата
operations.Add(new JumpOperation(-10, -10));
operations.Add(new MarkOperation(10, -10));
operations.Add(new MarkOperation(10, 10));
operations.Add(new MarkOperation(-10, 10));
operations.Add(new MarkOperation(-10, -10));

// Изменение диаметра для заполнения
operations.Add(new DiameterOperation(90.0));  // 90 μm - больше!
operations.Add(new PowerOperation(350.0));
operations.Add(new MarkSpeedOperation(1400));

// Штриховка внутри
for (double y = -9; y <= 9; y += 0.072)  // шаг 72 μm
{
    operations.Add(new JumpOperation(-9, y));
    operations.Add(new MarkOperation(9, y));
}
```

### Пример 2: Применение параметров из CLI файла

```csharp
// JSON из CLI файла $PARAMETER_SET:
// "downskin_hatch_laser_beam_diameter": 80.0

var downskinDiameter = 80.0;  // из JSON
var downskinPower = 280.0;
var downskinSpeed = 800;

// Применение к операциям
operations.Add(new DiameterOperation(downskinDiameter));
operations.Add(new PowerOperation(downskinPower));
operations.Add(new MarkSpeedOperation(downskinSpeed));
```

### Пример 3: Динамическое изменение диаметра

```csharp
double currentDiameter = 0.0;

foreach (var geometry in cliGeometries)
{
    var requiredDiameter = GetDiameterForRegion(geometry.Region);

    // Меняем диаметр только если нужно
    if (Math.Abs(currentDiameter - requiredDiameter) > 0.001)
    {
        operations.Add(new DiameterOperation(requiredDiameter));
        currentDiameter = requiredDiameter;
    }

    // Рисуем геометрию с текущим диаметром
    DrawGeometry(geometry, operations);
}

double GetDiameterForRegion(GeometryRegion region)
{
    return region switch
    {
        GeometryRegion.Edges => 65.0,
        GeometryRegion.Contour => 70.0,
        GeometryRegion.Upskin => 75.0,
        GeometryRegion.Downskin => 80.0,
        GeometryRegion.Infill => 90.0,
        GeometryRegion.Support => 95.0,
        _ => 80.0
    };
}
```

---

## Влияние диаметра на процесс

### 1. Расстояние между линиями штриховки

**Правило:** `hatch_spacing = diameter × overlap_factor`

```csharp
var diameter = 80.0;        // μm
var overlapFactor = 0.8;    // 80% перекрытие
var hatchSpacing = (diameter * overlapFactor) / 1000.0;  // = 0.064 mm

// Генерация штриховки
for (double y = yMin; y <= yMax; y += hatchSpacing)
{
    operations.Add(new JumpOperation(xMin, y));
    operations.Add(new MarkOperation(xMax, y));
}
```

**Примеры:**
- Диаметр 60 μm → расстояние ~48 μm (0.048 mm)
- Диаметр 80 μm → расстояние ~64 μm (0.064 mm)
- Диаметр 100 μm → расстояние ~80 μm (0.080 mm)

### 2. Объемная плотность энергии (VED)

**Формула:** `VED = P / (v × h × t)` [J/mm³]

где:
- P = мощность (W)
- v = скорость (mm/s)
- h = расстояние между штрихами (mm) ← зависит от диаметра!
- t = толщина слоя (mm)

```csharp
var power = 300.0;              // W
var speed = 1000;               // mm/s
var layerThickness = 0.04;      // mm
var diameter = 80.0;            // μm
var hatchSpacing = 0.064;       // mm (из диаметра)

var ved = power / (speed * hatchSpacing * layerThickness);
// ved ≈ 117 J/mm³

Console.WriteLine($"VED: {ved:F1} J/mm³");
if (ved >= 50 && ved <= 150)
    Console.WriteLine("✓ Оптимальный режим");
```

### 3. Производительность

**Больший диаметр = меньше линий = быстрее печать**

```csharp
// Площадь 20×20 mm
var area = 20.0 * 20.0;  // 400 mm²

var examples = new[]
{
    new { Diameter = 60.0, Spacing = 0.048 },
    new { Diameter = 80.0, Spacing = 0.064 },
    new { Diameter = 100.0, Spacing = 0.080 }
};

foreach (var ex in examples)
{
    var lineCount = (int)(20.0 / ex.Spacing);
    var totalLength = lineCount * 20.0;  // mm
    var timeAt1000mms = totalLength / 1000.0;  // секунды

    Console.WriteLine($"Диаметр {ex.Diameter} μm:");
    Console.WriteLine($"  Линий: {lineCount}");
    Console.WriteLine($"  Длина пути: {totalLength:F0} mm");
    Console.WriteLine($"  Время (v=1000 mm/s): {timeAt1000mms:F2} s");
}

// Результат:
// 60 μm: ~417 линий, 8333 mm, 8.33 s
// 80 μm: ~313 линий, 6250 mm, 6.25 s  ← на 25% быстрее!
// 100 μm: ~250 линий, 5000 mm, 5.00 s  ← на 40% быстрее!
```

---

## Порядок операций в реальном слое

```csharp
public void ProcessLayer42()
{
    var ops = new List<IOperation>();

    // 1. EDGES (самый тонкий пучок)
    ops.Add(new DiameterOperation(65.0));
    ops.Add(new PowerOperation(240.0));
    ops.Add(new MarkSpeedOperation(500));
    // ... рисование edges

    // 2. CONTOUR (тонкий пучок)
    ops.Add(new DiameterOperation(70.0));
    ops.Add(new PowerOperation(260.0));
    ops.Add(new MarkSpeedOperation(600));
    // ... рисование contour

    // 3. UPSKIN (средний пучок)
    ops.Add(new DiameterOperation(75.0));
    ops.Add(new PowerOperation(280.0));
    ops.Add(new MarkSpeedOperation(900));
    // ... рисование upskin

    // 4. INFILL (самый широкий пучок)
    ops.Add(new DiameterOperation(90.0));
    ops.Add(new PowerOperation(350.0));
    ops.Add(new MarkSpeedOperation(1400));
    // ... рисование infill

    // 5. SUPPORT (широкий пучок)
    ops.Add(new DiameterOperation(95.0));
    ops.Add(new PowerOperation(320.0));
    ops.Add(new MarkSpeedOperation(1600));
    // ... рисование support

    SendToScanner(ops);
}
```

---

## Маппинг из CLI JSON в операции

### В CLI файле (JSON):

```json
{
  "downskin_hatch_laser_beam_diameter": 80.0,
  "downskin_hatch_laser_power": 280.0,
  "downskin_hatch_laser_speed": 800,
  "downskin_hatch_skywriting": 0,

  "upskin_contour_laser_beam_diameter": 70.0,
  "upskin_contour_laser_power": 250.0,
  "upskin_contour_laser_speed": 600,
  "upskin_contour_skywriting": 1
}
```

### В коде C#:

```csharp
// Парсинг параметров (как в JobBuilder.java)
void ApplyCliParameters(string regionPrefix, Dictionary<string, object> json)
{
    var diameter = (double)json[$"{regionPrefix}_laser_beam_diameter"];
    var power = (double)json[$"{regionPrefix}_laser_power"];
    var speed = (int)json[$"{regionPrefix}_laser_speed"];
    var skywriting = (int)json[$"{regionPrefix}_skywriting"];

    operations.Add(new DiameterOperation(diameter));   // ← Диаметр!
    operations.Add(new PowerOperation(power));
    operations.Add(new MarkSpeedOperation(speed));
    operations.Add(new SWEnableOperation(skywriting == 1));
}

// Использование
ApplyCliParameters("downskin_hatch", cliParameterSet);
// → DiameterOperation(80.0)

ApplyCliParameters("upskin_contour", cliParameterSet);
// → DiameterOperation(70.0)
```

---

## State-based подход

**Важно:** DiameterOperation работает как **установка состояния**, а не как команда на каждую точку.

```csharp
// ❌ НЕПРАВИЛЬНО - избыточно
operations.Add(new DiameterOperation(80.0));
operations.Add(new MarkOperation(0, 0));
operations.Add(new DiameterOperation(80.0));  // Дубликат!
operations.Add(new MarkOperation(1, 0));

// ✓ ПРАВИЛЬНО - диаметр устанавливается один раз
operations.Add(new DiameterOperation(80.0));
operations.Add(new MarkOperation(0, 0));
operations.Add(new MarkOperation(1, 0));
operations.Add(new MarkOperation(2, 0));
// Все операции Mark используют диаметр 80 μm

// Изменение диаметра только при необходимости
operations.Add(new DiameterOperation(70.0));  // Новый диаметр!
operations.Add(new MarkOperation(3, 0));
operations.Add(new MarkOperation(4, 0));
// Теперь используется 70 μm
```

---

## Связь с другими параметрами

### Триада параметров для каждого региона:

```csharp
// Для региона DOWNSKIN_HATCH из CLI:
operations.Add(new DiameterOperation(80.0));      // Диаметр
operations.Add(new PowerOperation(280.0));        // Мощность
operations.Add(new MarkSpeedOperation(800));      // Скорость

// Эти 3 параметра работают вместе:
// - Диаметр определяет размер пятна
// - Мощность определяет энергию
// - Скорость определяет время воздействия
```

### Баланс параметров:

| Диаметр ↑ | Мощность ↑ | Скорость ↑ | Результат |
|-----------|------------|------------|-----------|
| Больше | Больше | Больше | Быстрая печать, меньше точность |
| Меньше | Меньше | Меньше | Медленная печать, больше точность |
| Больше | Меньше | Больше | Недоплав |
| Меньше | Больше | Меньше | Переплав |

---

## Резюме

### Ключевые моменты:

1. **Диаметр пучка ≠ Z координата**
   - Диаметр = размер пятна на поверхности
   - Z = высота/фокусное расстояние

2. **Устанавливается через DiameterOperation**
   ```csharp
   new DiameterOperation(80.0)  // 80 микрометров
   ```

3. **Влияет на:**
   - Расстояние между линиями штриховки
   - Объемную плотность энергии (VED)
   - Скорость печати
   - Качество поверхности

4. **Типичные значения: 60-100 μm**
   - Тонкие детали: 60-70 μm
   - Контуры: 70-75 μm
   - Заполнение: 80-95 μm
   - Поддержки: 90-100 μm

5. **State-based параметр**
   - Устанавливается один раз
   - Действует на все последующие операции
   - Меняется только при необходимости

---

## Файлы примеров

- **DiameterUsageExamples.cs** - 7 полных примеров использования
- Запуск: `dotnet run` или компиляция в Visual Studio

Все примеры демонстрируют реальные сценарии из PrintMateMC/HashuScan.
