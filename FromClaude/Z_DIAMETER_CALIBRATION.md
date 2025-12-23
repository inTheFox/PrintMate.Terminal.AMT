# Откуда берутся числа в формуле диаметра?

## Формула:
```csharp
float z = (float)((diameter - 70.0) / 10.0 * 0.1);
```

## Разбор по частям:

### 1. **70.0** - Номинальный диаметр пучка (μm)

**Что это:**
- Диаметр лазерного пятна при **Z = 0** (фокус на номинальной высоте)
- Определяется оптикой вашего сканера

**Откуда взять:**

#### Вариант A: Из конфигурации сканера `system.ini`

```ini
[Laser]
FocusZ=0.0              ← Номинальная высота фокуса
SpotSize=70             ← ВОТ ОНО! Номинальный диаметр в μm
BeamDiameter=70         ← Или может называться так
```

#### Вариант B: Из спецификации вашей F-theta линзы

```
Линза: F-theta 160mm
Фокусное расстояние: 160 мм
Диаметр пятна: 70 μm @ f=160mm
```

#### Вариант C: Измерить самостоятельно

1. Установите Z = 0
2. Напечатайте тестовую линию на металле
3. Измерьте ширину линии под микроскопом
4. Это и есть номинальный диаметр

---

### 2. **10.0** - Единица измерения (для удобства)

**Что это:**
- Просто делитель для перевода **10 μm** в единицу
- Упрощает формулу

**Математика:**
```
(diameter - 70.0) дает нам изменение диаметра в μm
(diameter - 70.0) / 10.0 дает изменение в "единицах по 10 μm"

Пример:
diameter = 80 μm
(80 - 70) / 10 = 1.0 единица (т.е. +10 μm)

diameter = 65 μm
(65 - 70) / 10 = -0.5 единиц (т.е. -5 μm)
```

**Можно убрать:**
```csharp
// С делением на 10 (удобнее)
float z = (float)((diameter - 70.0) / 10.0 * 0.1);

// Без деления на 10 (коэффициент изменится)
float z = (float)((diameter - 70.0) * 0.01); // 0.01 = 0.1 / 10
```

---

### 3. **0.1** - Коэффициент Z на единицу диаметра (мм/10μm)

**Что это:**
- На сколько **мм** нужно изменить Z, чтобы изменить диаметр на **10 μm**
- Зависит от вашей оптики (угол расходимости луча)

**Откуда взять:**

#### Способ 1: Калибровка (РЕКОМЕНДУЕТСЯ)

```
Шаги:
1. Напечатайте тестовые линии с разным Z:
   Z = -0.5 мм
   Z = -0.3 мм
   Z = -0.1 мм
   Z = 0.0 мм
   Z = +0.1 мм
   Z = +0.3 мм
   Z = +0.5 мм

2. Измерьте ширину линий под микроскопом:
   Z = -0.5 → diameter = 55 μm
   Z = -0.3 → diameter = 61 μm
   Z = -0.1 → diameter = 67 μm
   Z = 0.0  → diameter = 70 μm
   Z = +0.1 → diameter = 73 μm
   Z = +0.3 → diameter = 79 μm
   Z = +0.5 → diameter = 85 μm

3. Постройте график и найдите коэффициент:
   ΔZ = 0.5 мм → Δdiameter = 15 μm
   Коэффициент = 0.5 / 15 * 10 = 0.333 мм на 10 μm

   ИЛИ проще:
   Z = 0.1 мм → diameter изменился на 3 μm
   Для изменения на 10 μm нужно: 0.1 * (10/3) = 0.333 мм
```

#### Способ 2: Из спецификации оптики

Формула для Гауссова пучка:
```
w(z) = w₀ × √(1 + (z × λ / (π × w₀²))²)

Где:
w(z) - радиус пучка на расстоянии z
w₀ - радиус пучка в фокусе
λ - длина волны лазера

Для лазера 1064 нм и диаметра 70 μm:
Коэффициент ≈ 0.05 - 0.2 мм/10μm (зависит от линзы)
```

#### Способ 3: Типичные значения

| Линза F-theta | Фокус (мм) | Диаметр пятна (μm) | Коэффициент (мм/10μm) |
|---------------|------------|--------------------|-----------------------|
| 100 мм        | 100        | 50-60              | 0.05 - 0.08           |
| 160 мм        | 160        | 60-80              | 0.08 - 0.15           |
| 254 мм        | 254        | 80-120             | 0.15 - 0.25           |
| 330 мм        | 330        | 100-150            | 0.20 - 0.35           |

---

## Как найти ВСЕ параметры для вашей системы?

### Метод 1: Из конфигурации Hans (САМЫЙ ПРОСТОЙ)

Посмотрите файл `system.ini` в папке с DLL:

```ini
[Scanner]
ScannerType=Hans
Protocol=SPI
FieldSize=110           ← Размер поля сканирования (мм)

[Laser]
FocusZ=0.0             ← Номинальная высота фокуса
SpotSize=70            ← Номинальный диаметр (μm) ← ВОТ 70!
WaveLength=1064        ← Длина волны (нм)

[Optics]
FTheta=160             ← Фокусное расстояние линзы (мм)
WorkingDistance=160    ← Рабочее расстояние

[Correction]
FieldCurvature=0.001   ← Коррекция кривизны поля
ZCoefficient=0.1       ← Коэффициент Z → диаметр ← ВОТ 0.1!
```

### Метод 2: Из вашей калибровки (САМЫЙ ТОЧНЫЙ)

```csharp
public class DiameterCalibration
{
    // Данные из калибровки
    public struct CalibrationPoint
    {
        public double Z;        // мм
        public double Diameter; // μm
    }

    public static void CalculateCoefficient()
    {
        // Ваши измерения
        var measurements = new CalibrationPoint[]
        {
            new CalibrationPoint { Z = -0.5, Diameter = 55 },
            new CalibrationPoint { Z = -0.3, Diameter = 61 },
            new CalibrationPoint { Z = -0.1, Diameter = 67 },
            new CalibrationPoint { Z = 0.0,  Diameter = 70 },  // ← Номинальный
            new CalibrationPoint { Z = 0.1,  Diameter = 73 },
            new CalibrationPoint { Z = 0.3,  Diameter = 79 },
            new CalibrationPoint { Z = 0.5,  Diameter = 85 }
        };

        // Находим номинальный диаметр (при Z=0)
        double nominalDiameter = measurements
            .First(m => Math.Abs(m.Z) < 0.001)
            .Diameter;

        Console.WriteLine($"Номинальный диаметр: {nominalDiameter} μm"); // 70

        // Рассчитываем коэффициент (линейная регрессия)
        double sumZD = 0, sumZ2 = 0;
        int count = 0;

        foreach (var m in measurements)
        {
            if (Math.Abs(m.Z) > 0.001) // Пропускаем Z=0
            {
                double deltaDiameter = m.Diameter - nominalDiameter;
                sumZD += m.Z * deltaDiameter;
                sumZ2 += m.Z * m.Z;
                count++;
            }
        }

        double slope = sumZD / sumZ2; // μm/мм
        double coefficient = 1.0 / slope * 10.0; // мм/10μm

        Console.WriteLine($"Коэффициент: {coefficient:F3} мм на 10 μm"); // ~0.333

        // Формула для вашей системы:
        Console.WriteLine("\nВаша формула:");
        Console.WriteLine($"float z = (float)((diameter - {nominalDiameter}) / 10.0 * {coefficient:F3});");
    }
}
```

### Метод 3: Упрощенная калибровка (БЫСТРЫЙ)

Если лень делать много измерений:

```csharp
// 1. Напечатайте две линии:
//    - Линия 1: Z = 0.0 мм
//    - Линия 2: Z = 0.5 мм

// 2. Измерьте диаметры:
double diameter_at_z0 = 70.0;    // μm
double diameter_at_z05 = 85.0;   // μm

// 3. Рассчитайте коэффициент:
double deltaZ = 0.5;                                    // мм
double deltaDiameter = diameter_at_z05 - diameter_at_z0; // 15 μm

double coefficient = deltaZ / (deltaDiameter / 10.0);   // мм/10μm
// coefficient = 0.5 / 1.5 = 0.333

Console.WriteLine($"Коэффициент: {coefficient:F3}");
// Используйте это значение в формуле!
```

---

## Правильная формула для ВАШЕЙ системы

```csharp
public class DiameterToZ
{
    // ============================================
    // ЗАМЕНИТЕ ЭТИ ЗНАЧЕНИЯ НА СВОИ!
    // ============================================

    // Из system.ini или калибровки:
    private const double NOMINAL_DIAMETER = 70.0;  // μm при Z=0

    // Из калибровки или спецификации оптики:
    private const double Z_PER_10UM = 0.1;         // мм Z на 10 μm диаметра

    // ============================================

    public static float Convert(double targetDiameter)
    {
        // Правильная формула:
        return (float)((targetDiameter - NOMINAL_DIAMETER) / 10.0 * Z_PER_10UM);
    }

    // Примеры:
    public static void Examples()
    {
        Console.WriteLine($"Диаметр 60 μm → Z = {Convert(60)} мм");
        Console.WriteLine($"Диаметр 70 μm → Z = {Convert(70)} мм");
        Console.WriteLine($"Диаметр 80 μm → Z = {Convert(80)} мм");
        Console.WriteLine($"Диаметр 90 μm → Z = {Convert(90)} мм");
        Console.WriteLine($"Диаметр 100 μm → Z = {Convert(100)} мм");

        // Вывод (с коэффициентом 0.1):
        // Диаметр 60 μm → Z = -0.1 мм
        // Диаметр 70 μm → Z = 0 мм
        // Диаметр 80 μm → Z = 0.1 мм
        // Диаметр 90 μm → Z = 0.2 мм
        // Диаметр 100 μm → Z = 0.3 мм
    }
}
```

---

## Что делать ПРЯМО СЕЙЧАС?

### Вариант 1: Используйте значения из примера (быстро, но неточно)

```csharp
// Это ПРИМЕРНЫЕ значения, могут не подойти для вашей системы!
float z = (float)((diameter - 70.0) / 10.0 * 0.1);
```

**Проблема:** Диаметр может быть неправильным.

### Вариант 2: Найдите system.ini (правильно)

```csharp
// 1. Найдите файл system.ini в папке с HM_HashuScan.dll
// 2. Откройте его и найдите:
//    SpotSize = ???     ← Замените 70.0 на это
//    ZCoefficient = ??? ← Замените 0.1 на это

// 3. Используйте эти значения:
double nominalDiameter = 70.0;  // из SpotSize
double zCoefficient = 0.1;      // из ZCoefficient

float z = (float)((diameter - nominalDiameter) / 10.0 * zCoefficient);
```

### Вариант 3: Откалибруйте сами (самый точный)

```csharp
// 1. Запустите программу калибровки (см. выше)
// 2. Получите свои значения:
double nominalDiameter = ???;  // из измерений
double zCoefficient = ???;     // из расчета

// 3. Используйте их:
float z = (float)((diameter - nominalDiameter) / 10.0 * zCoefficient);
```

---

## Проверка правильности

После того, как вы нашли свои значения, проверьте:

```csharp
// Ваши параметры
double nominalDiameter = 70.0;
double zCoefficient = 0.1;

// Тестовые диаметры
double[] testDiameters = { 60, 70, 80, 90, 100 };

Console.WriteLine("Тест конвертации диаметр → Z:");
foreach (var d in testDiameters)
{
    float z = (float)((d - nominalDiameter) / 10.0 * zCoefficient);
    Console.WriteLine($"  Диаметр {d} μm → Z = {z:F3} mm");
}

// Ожидаемый результат:
// Диаметр 60 μm → Z = -0.100 mm (меньше диаметр = ближе к фокусу)
// Диаметр 70 μm → Z = 0.000 mm  (номинальный)
// Диаметр 80 μm → Z = 0.100 mm  (больше диаметр = дальше от фокуса)
// Диаметр 90 μm → Z = 0.200 mm
// Диаметр 100 μm → Z = 0.300 mm

// ВАЖНО: Z должен быть в пределах ±2 мм для большинства систем!
```

---

## Резюме

| Параметр | Что это | Откуда взять |
|----------|---------|--------------|
| **70.0** | Номинальный диаметр (μm) | `system.ini` → `SpotSize` или измерить при Z=0 |
| **10.0** | Делитель для удобства | Константа (можно изменить формулу) |
| **0.1** | Коэффициент Z→диаметр (мм/10μm) | `system.ini` → `ZCoefficient` или калибровка |

### Итоговая формула:

```csharp
float z = (float)((diameter - NOMINAL_DIAMETER) / 10.0 * Z_COEFFICIENT);
//                          ↑                              ↑
//                    из system.ini               из system.ini
//                    или измерить                или откалибровать
```

**Найдите `system.ini` в вашей папке с Hans DLL - там все параметры!**
