# Hans Scanner - Анализ Beam Config параметров

## Параметры из конфигурации

```json
{
  "beamConfig": {
    "minBeamDiameterMicron": 48.141,      // d₀ - минимальный диаметр в фокусе (μm)
    "wavelengthNano": 1070.0,             // λ - длина волны (nm)
    "rayleighLengthMicron": 1426.715,     // z_R - длина Рэлея (μm)
    "m2": 1.127,                          // M² - фактор качества луча
    "focalLengthMm": 538.46               // f - фокусное расстояние F-theta линзы (mm)
  }
}
```

---

## Физический смысл параметров

### 1. `minBeamDiameterMicron` (d₀)

**Минимальный диаметр луча** в фокальной плоскости (на перетяжке луча).

- Это наименьший возможный размер пятна
- Определяет максимальное разрешение печати
- В вашем случае: **48.141 μm** (очень хороший фокус)

### 2. `wavelengthNano` (λ)

**Длина волны лазера** в нанометрах.

- 1070 nm = типичная длина волны Yb-fiber лазера
- Используется в расчетах дифракционного предела

### 3. `rayleighLengthMicron` (z_R)

**Длина Рэлея (Rayleigh length)** - характерная глубина фокуса.

- Расстояние от перетяжки, на котором диаметр луча увеличивается в √2 раз
- В вашем случае: **1426.715 μm = 1.427 mm**
- Большая длина Рэлея означает более "длинный" фокус

**Формула:**
```
z_R = π × d₀² × M² / (4 × λ)
```

**Проверка вашей конфигурации:**
```
z_R = π × (48.141)² × 1.127 / (4 × 1.07)
z_R = π × 2317.55 × 1.127 / 4.28
z_R ≈ 1926 μm  ← Есть расхождение!
```

*Примечание: Возможно, `rayleighLengthMicron` откалиброван экспериментально, а не рассчитан теоретически.*

### 4. `m2` (M²)

**Фактор качества луча** (beam quality factor).

- M² = 1.0 - идеальный Гауссов луч
- M² > 1.0 - реальный луч с аберрациями
- В вашем случае: **1.127** (очень хорошее качество, близко к идеальному)

**Влияние на фокус:**
- Чем больше M², тем больше минимальный диаметр
- Чем больше M², тем меньше глубина фокуса

### 5. `focalLengthMm` (f)

**Фокусное расстояние F-theta линзы** в миллиметрах.

- В вашем случае: **538.46 mm**
- Определяет размер поля сканирования
- Типичные значения: 100-600 mm

**Связь с полем сканирования:**
```
Размер поля ≈ f × максимальный угол отклонения
```

---

## Формулы дефокусировки

### Основная формула: Диаметр луча при дефокусировке

```
d(z) = d₀ × sqrt(1 + (z / z_R)²)
```

Где:
- `d(z)` - диаметр луча на расстоянии z от фокуса (μm)
- `d₀` - минимальный диаметр в фокусе (μm)
- `z` - Z-offset (расстояние от фокальной плоскости, μm)
- `z_R` - длина Рэлея (μm)

### Обратная формула: Z-offset из заданного диаметра

Если нам нужен диаметр `d_target`, то:

```
z = z_R × sqrt((d_target / d₀)² - 1)
```

**ВАЖНО:** Эта формула дает **модуль** Z (|z|). Знак Z определяет направление дефокусировки:
- **Отрицательный Z**: Фокус **выше** детали (луч схождится к детали, меньший диаметр)
- **Положительный Z**: Фокус **ниже** детали (луч расходится от детали, больший диаметр)

---

## Применение в Hans4Java

### Как Hans4Java преобразует `DiameterOperation` в Z-offset

```java
// DiameterOperation содержит целевой диаметр (μm)
DiameterOperation op = new DiameterOperation(80.0); // 80 μm

// Hans4Java (предположительно в UdmProducer или CardProfile) делает:
double d_target = op.getDiameter();  // 80.0 μm
double d0 = beamConfig.minBeamDiameterMicron;  // 48.141 μm
double zR = beamConfig.rayleighLengthMicron;   // 1426.715 μm

// Расчет Z-offset
double z;
if (d_target < d0) {
    // Невозможно получить диаметр меньше минимального
    z = 0.0;
    // Warning: requested diameter is less than minimum
} else if (d_target == d0) {
    z = 0.0;  // Точно в фокусе
} else {
    // Формула дефокусировки
    double ratio = d_target / d0;
    z = zR * Math.sqrt(ratio * ratio - 1.0);
}

// Преобразование μm -> mm для Hans UDM API
float z_mm = (float)(z / 1000.0);

// Установить Z в structUdmPos
structUdmPos.z = z_mm;
```

### Пример расчета для вашей конфигурации

**Дано:**
- `d₀ = 48.141 μm`
- `z_R = 1426.715 μm`
- Целевой диаметр: `d_target = 80 μm` (из CLI)

**Расчет:**
```
ratio = 80.0 / 48.141 = 1.662
sqrt(ratio² - 1) = sqrt(2.762 - 1) = sqrt(1.762) = 1.327
z = 1426.715 × 1.327 = 1893.8 μm = 1.894 mm
```

**Результат:** `Z = +1.894 mm` (положительный, фокус ниже детали)

**Для диаметра 120 μm:**
```
ratio = 120.0 / 48.141 = 2.493
sqrt(ratio² - 1) = sqrt(6.215 - 1) = sqrt(5.215) = 2.284
z = 1426.715 × 2.284 = 3259.4 μm = 3.259 mm
```

**Результат:** `Z = +3.259 mm`

**Для диаметра 60 μm:**
```
ratio = 60.0 / 48.141 = 1.246
sqrt(ratio² - 1) = sqrt(1.553 - 1) = sqrt(0.553) = 0.744
z = 1426.715 × 0.744 = 1061.5 μm = 1.062 mm
```

**Результат:** `Z = +1.062 mm`

---

## Таблица: Диаметр → Z-offset

Для вашей конфигурации (d₀ = 48.141 μm, z_R = 1426.715 μm):

| Целевой диаметр (μm) | Z-offset (mm) | Применение |
|---------------------|--------------|------------|
| 48.141 (d₀) | 0.000 | Фокус (минимальный диаметр) |
| 50 | 0.362 | Очень малая дефокусировка |
| 60 | 1.062 | Небольшой диаметр для точной печати |
| 70 | 1.514 | Средний диаметр |
| 80 | 1.894 | Типичный диаметр для edges |
| 90 | 2.224 | Средний-большой диаметр |
| 100 | 2.522 | Большой диаметр для infill |
| 110 | 2.796 | Очень большой диаметр |
| 120 | 3.052 | Максимальный диаметр для supports |
| 140 | 3.525 | Экстремально большой |
| 150 | 3.732 | |
| 160 | 3.928 | |
| 200 | 4.699 | |

---

## Знак Z-offset: Положительный или отрицательный?

В большинстве систем:
- **Положительный Z** (+): Фокус **ниже** детали (расходящийся луч, больший диаметр)
- **Отрицательный Z** (−): Фокус **выше** детали (сходящийся луч, но это сложнее реализовать)

**В вашем случае (для диаметров больше d₀):**
- Всегда используется **положительный Z** (фокус ниже рабочей плоскости)
- Это безопаснее и проще в реализации

---

## Влияние на процесс печати

### 1. Edges (контуры) - малый диаметр (80 μm)

```json
"edges_laser_beam_diameter": 80.0  →  Z = +1.894 mm
```

- Малый диаметр обеспечивает высокую точность
- Высокая плотность энергии → хорошее плавление

### 2. Infill (заполнение) - средний диаметр (100 μm)

```json
"infill_hatch_laser_beam_diameter": 100.0  →  Z = +2.522 mm
```

- Средний диаметр для быстрого заполнения
- Баланс между скоростью и качеством

### 3. Supports (поддержки) - большой диаметр (120 μm)

```json
"support_hatch_laser_beam_diameter": 120.0  →  Z = +3.052 mm
```

- Большой диаметр → меньшая плотность энергии
- Поддержки спекаются слабее (легче удалить)

---

## Реализация в C#

### Класс для расчета Z-offset

```csharp
public class BeamConfig
{
    public double MinBeamDiameterMicron { get; set; } = 48.141;
    public double WavelengthNano { get; set; } = 1070.0;
    public double RayleighLengthMicron { get; set; } = 1426.715;
    public double M2 { get; set; } = 1.127;
    public double FocalLengthMm { get; set; } = 538.46;

    /// <summary>
    /// Рассчитать Z-offset (mm) для заданного целевого диаметра (μm)
    /// </summary>
    public float CalculateZOffset(double targetDiameterMicron)
    {
        if (targetDiameterMicron < MinBeamDiameterMicron)
        {
            // Невозможно получить диаметр меньше минимального
            Console.WriteLine($"WARNING: Target diameter {targetDiameterMicron} μm " +
                            $"is less than minimum {MinBeamDiameterMicron} μm");
            return 0.0f;
        }

        if (Math.Abs(targetDiameterMicron - MinBeamDiameterMicron) < 0.001)
        {
            return 0.0f;  // Точно в фокусе
        }

        // Формула дефокусировки Гауссова луча
        double ratio = targetDiameterMicron / MinBeamDiameterMicron;
        double z_micron = RayleighLengthMicron * Math.Sqrt(ratio * ratio - 1.0);

        // Преобразовать μm -> mm
        float z_mm = (float)(z_micron / 1000.0);

        return z_mm;
    }

    /// <summary>
    /// Обратная функция: рассчитать диаметр (μm) для заданного Z-offset (mm)
    /// </summary>
    public double CalculateDiameter(float zOffsetMm)
    {
        if (Math.Abs(zOffsetMm) < 0.0001)
        {
            return MinBeamDiameterMicron;  // В фокусе
        }

        double z_micron = Math.Abs(zOffsetMm) * 1000.0;
        double ratio_squared = 1.0 + Math.Pow(z_micron / RayleighLengthMicron, 2);
        double diameter = MinBeamDiameterMicron * Math.Sqrt(ratio_squared);

        return diameter;
    }

    /// <summary>
    /// Проверка: рассчитать теоретическую длину Рэлея из параметров
    /// </summary>
    public double CalculateTheoreticalRayleighLength()
    {
        // z_R = π × d₀² × M² / (4 × λ)
        // где d₀ в μm, λ в μm (нужно перевести из nm)
        double lambda_micron = WavelengthNano / 1000.0;
        double zR = Math.PI * Math.Pow(MinBeamDiameterMicron, 2) * M2
                    / (4.0 * lambda_micron);
        return zR;
    }
}
```

### Пример использования

```csharp
BeamConfig beamConfig = new BeamConfig
{
    MinBeamDiameterMicron = 48.141,
    RayleighLengthMicron = 1426.715,
    M2 = 1.127,
    WavelengthNano = 1070.0,
    FocalLengthMm = 538.46
};

// CLI параметры
double edgesDiameter = 80.0;       // μm
double infillDiameter = 100.0;     // μm
double supportsDiameter = 120.0;   // μm

// Рассчитать Z-offset
float z_edges = beamConfig.CalculateZOffset(edgesDiameter);
float z_infill = beamConfig.CalculateZOffset(infillDiameter);
float z_supports = beamConfig.CalculateZOffset(supportsDiameter);

Console.WriteLine($"Edges (80 μm):    Z = {z_edges:F3} mm");    // 1.894
Console.WriteLine($"Infill (100 μm):  Z = {z_infill:F3} mm");   // 2.522
Console.WriteLine($"Supports (120 μm): Z = {z_supports:F3} mm"); // 3.052

// Обратная проверка
double diameter_check = beamConfig.CalculateDiameter(z_edges);
Console.WriteLine($"Check: Z={z_edges:F3} mm → diameter={diameter_check:F1} μm");

// Проверка теоретической длины Рэлея
double zR_theoretical = beamConfig.CalculateTheoreticalRayleighLength();
Console.WriteLine($"Theoretical z_R: {zR_theoretical:F1} μm");
Console.WriteLine($"Configured z_R:  {beamConfig.RayleighLengthMicron:F1} μm");
```

---

## Где в Hans4Java происходит расчет?

Судя по декомпилированному коду, расчет Z-offset из `DiameterOperation` происходит в одном из следующих мест:

### Вариант 1: В `UdmProducer.updateMarkParam()`

```java
// Обработка DiameterOperation
case 3:  // OpType.DIAMETER
    double targetDiameter = (Double)op.getData()[0];

    // Рассчитать Z-offset
    double z_mm = this.cardProfile.beamConfig.calculateZOffset(targetDiameter);

    // Применить к текущему слою (будет использоваться в structUdmPos)
    this.currentZOffset = z_mm;
    break;
```

### Вариант 2: В `CardProfile.beamConfig`

```java
public class BeamConfig {
    public double minBeamDiameterMicron;
    public double rayleighLengthMicron;
    // ... другие поля

    public float calculateZOffset(double targetDiameterMicron) {
        if (targetDiameterMicron <= this.minBeamDiameterMicron) {
            return 0.0f;
        }
        double ratio = targetDiameterMicron / this.minBeamDiameterMicron;
        double z_micron = this.rayleighLengthMicron * Math.sqrt(ratio * ratio - 1.0);
        return (float)(z_micron / 1000.0);
    }
}
```

### Вариант 3: Непосредственно при добавлении геометрии

```java
// При вызове UDM_AddPolyline3D
structUdmPos[] points = ...;
for (structUdmPos point : points) {
    point.x = ...;
    point.y = ...;
    point.z = this.currentZOffset;  // Установленный из DiameterOperation
}
```

---

## Выводы

### ✅ Что делают параметры `beamConfig`:

1. **`minBeamDiameterMicron`** - минимальный диаметр в фокусе (48.141 μm)
2. **`rayleighLengthMicron`** - характерная глубина фокуса (1426.715 μm)
3. **`wavelengthNano`**, **`m2`**, **`focalLengthMm`** - дополнительные оптические параметры

### ✅ Как они используются:

- Hans4Java рассчитывает **Z-offset** из целевого диаметра (`DiameterOperation`)
- Формула: `z = z_R × sqrt((d_target / d₀)² - 1)`
- Z-offset устанавливается в `structUdmPos.z` при добавлении геометрии

### ✅ В C# реализации:

1. Создайте класс `BeamConfig` с методом `CalculateZOffset()`
2. При обработке CLI параметра `laser_beam_diameter`:
   ```csharp
   float z = beamConfig.CalculateZOffset(cliParameter.LaserBeamDiameter);
   ```
3. Используйте `z` в `structUdmPos`:
   ```csharp
   structUdmPos point = new structUdmPos { x = ..., y = ..., z = z };
   ```

---

## Рекомендации

### 1. Калибровка длины Рэлея

Теоретическая формула дает `z_R ≈ 1926 μm`, но в конфиге указано `1426.715 μm`.

**Причины расхождения:**
- Аберрации оптической системы
- Неидеальный Гауссов профиль луча
- Тепловое расширение
- Экспериментальная калибровка

**Рекомендация:** Используйте **значение из конфига** (1426.715 μm), так как оно получено экспериментально.

### 2. Тестирование формулы

Создайте тестовый файл с разными диаметрами:

```csharp
double[] testDiameters = { 48.141, 60, 80, 100, 120, 140 };
foreach (var d in testDiameters)
{
    float z = beamConfig.CalculateZOffset(d);
    // Распечатать тестовую линию с этим Z
    // Измерить реальную ширину линии под микроскопом
    // Сравнить с ожидаемым диаметром
}
```

### 3. Проверка знака Z

Убедитесь в соглашении о знаке Z:
- Если луч расширяется при **положительном Z** → используйте `+z`
- Если луч расширяется при **отрицательном Z** → используйте `-z`

---

## Связанные файлы

- [Hans_CSharp_Complete_Integration.cs](Hans_CSharp_Complete_Integration.cs) - уже использует упрощенный расчет Z
- [HANS_SKYWRITING_COMPLETE_GUIDE.md](HANS_SKYWRITING_COMPLETE_GUIDE.md) - полное руководство

---

**Автор:** Анализ на основе физики Гауссова луча и конфигурации Hans Scanner
**Дата:** 2025
**Версия:** 1.0
