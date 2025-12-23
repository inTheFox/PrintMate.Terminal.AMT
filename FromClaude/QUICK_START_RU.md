# Быстрый старт: Как применить диаметр пучка

## Краткий ответ

```csharp
// 1. Получили диаметр из CLI
double diameter = 80.0;  // из downskin_hatch_laser_beam_diameter

// 2. Создали операцию
var diameterOp = new DiameterOperation(diameter);

// 3. Добавили в список ПЕРЕД геометрией
operations.Add(diameterOp);
operations.Add(new JumpOperation(0, 0));
operations.Add(new MarkOperation(10, 10));

// 4. Отправили в Hans
scanner.LoadOperations(this);  // this = IOperationsProducer
```

**Вот и всё!** Диаметр применится ко всей последующей геометрии.

---

## Полный пример (5 минут)

### Шаг 1: Получить диаметр из CLI файла

```csharp
// У вас есть CLI файл с JSON параметрами
var cliParameters = new Dictionary<string, object>
{
    ["downskin_hatch_laser_beam_diameter"] = 80.0,
    ["downskin_hatch_laser_power"] = 280.0,
    ["downskin_hatch_laser_speed"] = 800
};

// Извлекаем диаметр
double diameter = (double)cliParameters["downskin_hatch_laser_beam_diameter"];
// diameter = 80.0 μm
```

### Шаг 2: Создать список операций

```csharp
var operations = new List<IOperation>();

// СНАЧАЛА устанавливаем параметры
operations.Add(new DiameterOperation(80.0));   // ← ВОТ ОН!
operations.Add(new PowerOperation(280.0));
operations.Add(new MarkSpeedOperation(800));

// ПОТОМ добавляем геометрию
operations.Add(new JumpOperation(-10, -10));
operations.Add(new MarkOperation(10, -10));
operations.Add(new MarkOperation(10, 10));
operations.Add(new MarkOperation(-10, 10));
operations.Add(new MarkOperation(-10, -10));
```

### Шаг 3: Отправить в Hans сканер

```csharp
// Вариант A: Через IOperationsProducer (как в PrintMateMC)
public class MyProducer : IOperationsProducer
{
    private List<IOperation> ops;

    public MyProducer(List<IOperation> operations)
    {
        this.ops = operations;
    }

    public object GetOperations()
    {
        return ops.ToArray();  // Hans получит ваши операции здесь
    }
}

// Использование
var producer = new MyProducer(operations);
scanner.LoadOperations(producer);
scanner.StartProcessing();

// Вариант B: Напрямую (если API позволяет)
scanner.SetOperations(operations.ToArray());
scanner.Execute();
```

---

## Важные моменты

### 1. Порядок имеет значение!

```csharp
// ✓ ПРАВИЛЬНО
operations.Add(new DiameterOperation(80.0));  // Сначала диаметр
operations.Add(new MarkOperation(10, 10));    // Потом геометрия

// ✗ НЕПРАВИЛЬНО
operations.Add(new MarkOperation(10, 10));    // Геометрия без диаметра!
operations.Add(new DiameterOperation(80.0));  // Диаметр применится к следующей геометрии
```

### 2. Диаметр действует до следующего изменения

```csharp
operations.Add(new DiameterOperation(80.0));   // Диаметр 80
operations.Add(new MarkOperation(0, 0));       // Использует 80
operations.Add(new MarkOperation(1, 0));       // Использует 80
operations.Add(new MarkOperation(2, 0));       // Использует 80

operations.Add(new DiameterOperation(70.0));   // Диаметр 70
operations.Add(new MarkOperation(3, 0));       // Использует 70
operations.Add(new MarkOperation(4, 0));       // Использует 70
```

### 3. Меняйте диаметр по необходимости

```csharp
double currentDiameter = 0.0;

foreach (var geometry in geometries)
{
    double requiredDiameter = GetDiameterForRegion(geometry.Region);

    // Меняем только если нужно (оптимизация)
    if (Math.Abs(currentDiameter - requiredDiameter) > 0.001)
    {
        operations.Add(new DiameterOperation(requiredDiameter));
        currentDiameter = requiredDiameter;
    }

    // Добавляем геометрию
    operations.AddRange(geometry.Operations);
}
```

---

## Как это работает внутри Hans

```
┌─────────────────────────────────────────────────────────────┐
│ Ваш код                                                      │
│                                                              │
│ operations.Add(new DiameterOperation(80.0));                │
│ operations.Add(new MarkOperation(10, 10));                  │
│                                                              │
│ scanner.LoadOperations(producer);                           │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ Hans Scanner (C++ внутри)                                   │
│                                                              │
│ 1. Вызывает producer.GetOperations()                        │
│ 2. Получает массив операций: [DiameterOp(80), MarkOp(...)] │
│ 3. Обрабатывает по порядку:                                 │
│    a) DiameterOperation(80.0)                               │
│       → SetParameter(DIAMETER, 80.0)                        │
│       → Настраивает оптику на диаметр 80 μm                 │
│    b) MarkOperation(10, 10)                                 │
│       → MoveTo(10, 10) с текущим диаметром 80 μm            │
│       → Включает лазер                                      │
└─────────────────────────────────────────────────────────────┘
```

---

## Полный рабочий код (копируй-вставляй)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

public class QuickStart
{
    public static void Main()
    {
        // ШАГ 1: Получили диаметр из CLI (80 μm)
        double diameter = 80.0;

        // ШАГ 2: Создали список операций
        var operations = new List<IOperation>();

        // ШАГ 3: Добавили диаметр и геометрию
        operations.Add(new DiameterOperation(diameter));  // ← ПРИМЕНЕНИЕ!
        operations.Add(new PowerOperation(280.0));
        operations.Add(new MarkSpeedOperation(800));

        operations.Add(new JumpOperation(-10, -10));
        operations.Add(new MarkOperation(10, -10));
        operations.Add(new MarkOperation(10, 10));
        operations.Add(new MarkOperation(-10, 10));
        operations.Add(new MarkOperation(-10, -10));

        // ШАГ 4: Отправили в Hans
        SendToHans(operations);

        Console.WriteLine("✓ Диаметр 80 μm применен к геометрии!");
    }

    static void SendToHans(List<IOperation> ops)
    {
        // В реальном коде:
        // IHLSSystem scanner = MultiLaserSS.getInstance();
        // scanner.LoadOperations(new MyProducer(ops));
        // scanner.StartProcessing();

        // Для демо:
        Console.WriteLine($"Отправлено {ops.Count} операций в Hans:");
        foreach (var op in ops.Take(5))
        {
            if (op is DiameterOperation d)
                Console.WriteLine($"  → DiameterOperation({d.Value} μm)");
            else if (op is MarkOperation m)
                Console.WriteLine($"  → MarkOperation({m.X}, {m.Y})");
            else
                Console.WriteLine($"  → {op.GetType().Name}");
        }
    }
}

// Определения классов
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
```

---

## Сопоставление с кодом PrintMateMC

### Java (PrintMateMC):

```java
// JobParameter.java:162-163
case FOCUS:
    return new DiameterOperation((double) getFloatVal(0));

// JobUtils.java:330
ret[laserID].addAll( pInfo.getScannerOperations(laserID+1, region));

// LaserInfo.java:101-104
public List<IOperation> getOperations(GEOMETRY_REGION region) {
    List<JobParameter> params = getParameters(region);
    return params.stream()
        .map(JobParameter::getScanOperation)  // ← Здесь создается DiameterOperation
        .collect(Collectors.toList());
}

// CommandManager.java:823-828
@Override
public Object getOperations() {
    Object ret = JobUtils.loadOPs(jManager.getJob(), loadedLayer+1, jManager.getJobInfo());
    return ret;  // ← Hans получает операции здесь
}
```

### C# (Ваш код):

```csharp
// Шаг 1: Парсинг (аналог JobBuilder.parsePartParameterSet)
double diameter = (double)cliParams["downskin_hatch_laser_beam_diameter"];

// Шаг 2: Создание операции (аналог JobParameter.getScanOperation)
var diameterOp = new DiameterOperation(diameter);

// Шаг 3: Добавление в список (аналог LaserInfo.getOperations)
operations.Add(diameterOp);

// Шаг 4: Передача в Hans (аналог CommandManager.getOperations)
public object GetOperations() {
    return operations.ToArray();
}
```

---

## Типичные ошибки

### ❌ Ошибка 1: Не добавили DiameterOperation

```csharp
// Плохо
operations.Add(new MarkOperation(10, 10));
// Hans будет использовать диаметр по умолчанию или предыдущий!
```

### ❌ Ошибка 2: Добавили диаметр ПОСЛЕ геометрии

```csharp
// Плохо
operations.Add(new MarkOperation(10, 10));  // Без диаметра!
operations.Add(new DiameterOperation(80.0)); // Применится только к следующей
```

### ❌ Ошибка 3: Дублирование диаметра

```csharp
// Неэффективно (но работает)
operations.Add(new DiameterOperation(80.0));
operations.Add(new MarkOperation(0, 0));
operations.Add(new DiameterOperation(80.0));  // Дубликат!
operations.Add(new MarkOperation(1, 0));

// Лучше
operations.Add(new DiameterOperation(80.0));
operations.Add(new MarkOperation(0, 0));
operations.Add(new MarkOperation(1, 0));  // Диаметр уже установлен
```

### ✓ Правильно

```csharp
// Хорошо
operations.Add(new DiameterOperation(80.0));  // Диаметр ПЕРЕД геометрией
operations.Add(new PowerOperation(280.0));
operations.Add(new MarkSpeedOperation(800));
operations.Add(new MarkOperation(10, 10));
operations.Add(new MarkOperation(20, 20));
```

---

## Резюме (TL;DR)

### Вопрос: Как применить диаметр пучка?

**Ответ:** 3 шага

1. **Создать:** `new DiameterOperation(80.0)`
2. **Добавить:** `operations.Add(diameterOp)` ПЕРЕД геометрией
3. **Отправить:** `scanner.LoadOperations(producer)`

### Минимальный код

```csharp
operations.Add(new DiameterOperation(80.0));  // ← ЭТО ВСЁ!
operations.Add(new MarkOperation(10, 10));
scanner.LoadOperations(producer);
```

**Готово!** Диаметр 80 μm применен.

---

## Дополнительные файлы

- **HowToSendDiameterToHans.cs** - полные примеры с IOperationsProducer
- **DiameterUsageExamples.cs** - 7 детальных примеров использования
- **DIAMETER_USAGE_RU.md** - полная документация с формулами

Запуск примеров:
```bash
dotnet run --project HowToSendDiameterToHans.cs
```
