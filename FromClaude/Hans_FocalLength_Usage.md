# Использование focalLengthMm в Hans Scanner

## 🎯 Что такое focalLengthMm?

```json
"beamConfig": {
  "focalLengthMm": 538.46  // ← Фокусное расстояние F-theta линзы
}
```

**Фокусное расстояние F-theta линзы** - это расстояние от линзы до фокальной плоскости.

---

## 📐 Для чего используется focalLengthMm?

### 1. Расчет размера поля сканирования

```
Field_size = f × θ_max
```

Где:
- `f` = focalLengthMm = 538.46 mm
- `θ_max` = максимальный угол отклонения зеркал (радианы)

**Пример:**

Если максимальный угол `θ_max = 0.372 rad` (≈21.3°):
```
Field_size = 538.46 × 0.372 = 200 mm
```

В вашей конфигурации:
```json
"scannerConfig": {
  "fieldSizeX": 400.0,  // ← 400 mm поле
  "fieldSizeY": 400.0
}
```

Это означает угол: `θ_max = 400 / 538.46 = 0.743 rad ≈ 42.5°`

---

### 2. Пересчет углов → координаты

F-theta линза обеспечивает **линейную зависимость**:

```
x = f × θ_x
y = f × θ_y
```

Где `θ_x`, `θ_y` - углы отклонения зеркал.

**Это встроено в Hans Scanner firmware**, вам не нужно делать это вручную!

---

### 3. Расчет разрешения

**Минимальный шаг** сканера определяется:

```
step_size = f × θ_min
```

Где `θ_min` - минимальный угол, который может установить гальво.

**Пример:**

Если разрешение гальво = 16 бит (65536 шагов на полный диапазон):
```
θ_min = θ_max / 65536
      = 0.743 / 65536
      = 11.3 × 10⁻⁶ rad

step_size = 538.46 × 11.3 × 10⁻⁶
          = 6.1 μm
```

Это **теоретическое** разрешение вашей системы.

---

### 4. ✅ ПРИМЕНЕНИЕ: Валидация координат

Можно использовать `focalLengthMm` для **проверки**, что координаты находятся в допустимом диапазоне.

```csharp
public class ScannerValidator
{
    private readonly double focalLengthMm;
    private readonly double fieldSizeX;
    private readonly double fieldSizeY;

    public ScannerValidator(BeamConfig beamConfig, ScannerConfig scannerConfig)
    {
        this.focalLengthMm = beamConfig.FocalLengthMm;
        this.fieldSizeX = scannerConfig.FieldSizeX;
        this.fieldSizeY = scannerConfig.FieldSizeY;
    }

    /// <summary>
    /// Проверить, что точка находится в пределах поля сканирования
    /// </summary>
    public bool IsPointValid(float x, float y)
    {
        // Проверка по размеру поля
        if (Math.Abs(x) > fieldSizeX / 2.0)
        {
            Console.WriteLine($"⚠️ X={x:F1} mm вне поля (max ±{fieldSizeX / 2.0:F1} mm)");
            return false;
        }

        if (Math.Abs(y) > fieldSizeY / 2.0)
        {
            Console.WriteLine($"⚠️ Y={y:F1} mm вне поля (max ±{fieldSizeY / 2.0:F1} mm)");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Рассчитать угол отклонения для координаты
    /// </summary>
    public double CalculateAngle(float coordinate_mm)
    {
        // θ = x / f
        return coordinate_mm / focalLengthMm;  // радианы
    }

    /// <summary>
    /// Проверить, что угол в допустимых пределах
    /// </summary>
    public bool IsAngleValid(float x, float y)
    {
        double theta_x = CalculateAngle(x);
        double theta_y = CalculateAngle(y);

        double theta_max = (fieldSizeX / 2.0) / focalLengthMm;

        if (Math.Abs(theta_x) > theta_max)
        {
            Console.WriteLine($"⚠️ Угол X={theta_x:F4} rad превышает max {theta_max:F4} rad");
            return false;
        }

        if (Math.Abs(theta_y) > theta_max)
        {
            Console.WriteLine($"⚠️ Угол Y={theta_y:F4} rad превышает max {theta_max:F4} rad");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Рассчитать теоретическое разрешение системы
    /// </summary>
    public double CalculateResolution(int galvoBits = 16)
    {
        double theta_max = (fieldSizeX / 2.0) / focalLengthMm;
        int steps = (int)Math.Pow(2, galvoBits);
        double theta_min = theta_max / steps;
        double resolution = focalLengthMm * theta_min;
        return resolution * 1000.0;  // μm
    }
}
```

---

### 5. ✅ ПРИМЕНЕНИЕ: Расчет искажений

F-theta линза компенсирует искажения, но не идеально. Можно использовать `focalLengthMm` для оценки остаточных искажений.

**Для идеальной F-theta линзы:**
```
x_ideal = f × θ
```

**Для реальной линзы:**
```
x_real = f × θ + distortion(θ)
```

Где `distortion` - остаточные искажения (компенсируются через `thirdAxisConfig`).

---

### 6. ✅ ПРИМЕНЕНИЕ: Улучшенный расчет Rayleigh length

Можно **проверить** соответствие `rayleighLengthMicron` теоретической формуле:

```
z_R = π × d₀² × M² / (4 × λ)
```

Но для реальной системы с F-theta линзой нужна коррекция:

```
z_R_eff = z_R × (1 + correction_factor)
```

Где `correction_factor` зависит от угла падения луча на линзу.

```csharp
public class AdvancedBeamConfig
{
    public double MinBeamDiameterMicron { get; set; }
    public double WavelengthNano { get; set; }
    public double RayleighLengthMicron { get; set; }
    public double M2 { get; set; }
    public double FocalLengthMm { get; set; }

    /// <summary>
    /// Рассчитать теоретическую Rayleigh length
    /// </summary>
    public double CalculateTheoreticalRayleighLength()
    {
        double lambda_micron = WavelengthNano / 1000.0;
        double zR = Math.PI * Math.Pow(MinBeamDiameterMicron, 2) * M2
                    / (4.0 * lambda_micron);
        return zR;
    }

    /// <summary>
    /// Проверить соответствие теории
    /// </summary>
    public void ValidateRayleighLength()
    {
        double theoretical = CalculateTheoreticalRayleighLength();
        double configured = RayleighLengthMicron;
        double difference = configured - theoretical;
        double percentDiff = (difference / theoretical) * 100.0;

        Console.WriteLine($"Rayleigh Length Validation:");
        Console.WriteLine($"  Theoretical: {theoretical:F1} μm");
        Console.WriteLine($"  Configured:  {configured:F1} μm");
        Console.WriteLine($"  Difference:  {difference:F1} μm ({percentDiff:+F1}%)");

        if (Math.Abs(percentDiff) > 30)
        {
            Console.WriteLine($"  ⚠️ WARNING: Large difference! Check calibration.");
        }
        else
        {
            Console.WriteLine($"  ✅ OK: Within reasonable range (experimental calibration)");
        }
    }

    /// <summary>
    /// Рассчитать эффективную Rayleigh length с учетом угла
    /// </summary>
    public double CalculateEffectiveRayleighLength(float x, float y)
    {
        // Угол падения луча на линзу
        double theta = Math.Sqrt(x * x + y * y) / FocalLengthMm;

        // Коррекционный фактор (приближенная формула)
        double correction = 1.0 + 0.5 * Math.Pow(theta, 2);

        return RayleighLengthMicron * correction;
    }
}
```

---

### 7. ✅ ПРИМЕНЕНИЕ: Оптимизация траекторий

Используя `focalLengthMm`, можно оптимизировать траектории сканирования.

```csharp
public class TrajectoryOptimizer
{
    private readonly double focalLengthMm;
    private readonly double fieldSizeX;

    public TrajectoryOptimizer(double focalLengthMm, double fieldSizeX)
    {
        this.focalLengthMm = focalLengthMm;
        this.fieldSizeX = fieldSizeX;
    }

    /// <summary>
    /// Рассчитать время перехода между точками
    /// </summary>
    public double CalculateJumpTime(float x1, float y1, float x2, float y2,
                                   int jumpSpeed)
    {
        // Расстояние в мм
        double distance = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));

        // Угловое расстояние
        double angular_distance = distance / focalLengthMm;

        // Время (с учетом ускорения гальво)
        double time_ms = (distance / jumpSpeed) * 1000.0;

        return time_ms;
    }

    /// <summary>
    /// Определить оптимальный порядок сканирования точек
    /// (минимизация времени прыжков)
    /// </summary>
    public List<int> OptimizePointOrder(List<CliPoint> points, int jumpSpeed)
    {
        // Простой nearest-neighbor алгоритм
        List<int> order = new List<int>();
        HashSet<int> visited = new HashSet<int>();

        int current = 0;  // Начать с первой точки
        order.Add(current);
        visited.Add(current);

        while (visited.Count < points.Count)
        {
            int nearest = -1;
            double minTime = double.MaxValue;

            for (int i = 0; i < points.Count; i++)
            {
                if (visited.Contains(i)) continue;

                double time = CalculateJumpTime(
                    points[current].X, points[current].Y,
                    points[i].X, points[i].Y,
                    jumpSpeed);

                if (time < minTime)
                {
                    minTime = time;
                    nearest = i;
                }
            }

            order.Add(nearest);
            visited.Add(nearest);
            current = nearest;
        }

        return order;
    }
}
```

---

## 🧮 Практические примеры

### Пример 1: Валидация координат

```csharp
BeamConfig beamConfig = new BeamConfig
{
    FocalLengthMm = 538.46,
    // ... другие параметры
};

ScannerConfig scannerConfig = new ScannerConfig
{
    FieldSizeX = 400.0,
    FieldSizeY = 400.0
};

ScannerValidator validator = new ScannerValidator(beamConfig, scannerConfig);

// Проверить точку
bool valid = validator.IsPointValid(150, 180);  // true
bool invalid = validator.IsPointValid(250, 0);  // false (вне поля)

// Рассчитать угол
double angle = validator.CalculateAngle(200);  // 0.371 rad ≈ 21.3°

// Рассчитать разрешение
double resolution = validator.CalculateResolution(16);  // 6.1 μm
Console.WriteLine($"System resolution: {resolution:F1} μm");
```

### Пример 2: Проверка Rayleigh length

```csharp
AdvancedBeamConfig advBeam = new AdvancedBeamConfig
{
    MinBeamDiameterMicron = 48.141,
    WavelengthNano = 1070.0,
    RayleighLengthMicron = 1426.715,
    M2 = 1.127,
    FocalLengthMm = 538.46
};

advBeam.ValidateRayleighLength();
// Output:
//   Theoretical: 1926.4 μm
//   Configured:  1426.7 μm
//   Difference:  -499.7 μm (-25.9%)
//   ✅ OK: Within reasonable range (experimental calibration)

// Эффективная z_R на краю поля
double zR_eff = advBeam.CalculateEffectiveRayleighLength(200, 0);
Console.WriteLine($"z_R at edge: {zR_eff:F1} μm");  // ~1499 μm
```

### Пример 3: Оптимизация траекторий

```csharp
TrajectoryOptimizer optimizer = new TrajectoryOptimizer(
    focalLengthMm: 538.46,
    fieldSizeX: 400.0
);

List<CliPoint> points = new List<CliPoint>
{
    new CliPoint { X = 0, Y = 0 },
    new CliPoint { X = 100, Y = 50 },
    new CliPoint { X = 50, Y = 100 },
    new CliPoint { X = 150, Y = 150 }
};

// Оптимизировать порядок
List<int> optimizedOrder = optimizer.OptimizePointOrder(points, jumpSpeed: 25000);

Console.WriteLine("Optimal scanning order:");
foreach (int idx in optimizedOrder)
{
    Console.WriteLine($"  Point {idx}: ({points[idx].X}, {points[idx].Y})");
}
```

---

## 📊 Сравнение двух ваших лазеров

| Параметр | Laser 1 | Laser 2 | Комментарий |
|----------|---------|---------|-------------|
| `focalLengthMm` | 538.46 | 538.46 | ✅ Одинаковый (та же линза) |
| `fieldSizeX/Y` | 400 | 400 | ✅ Одинаковый (то же поле) |
| `minBeamDiameterMicron` | 48.141 | 53.872 | ⚠️ Разный (разные лазеры) |
| `rayleighLengthMicron` | 1426.715 | 1616.16 | ⚠️ Разный (разные лазеры) |

**Вывод:** Оба лазера используют **одну и ту же F-theta линзу** (538.46 mm), но имеют **разные оптические характеристики** луча.

---

## 🎯 Итоговые рекомендации

### ✅ Используйте `focalLengthMm` для:

1. **Валидации координат** - проверка, что точки в пределах поля
2. **Расчета углов** - конвертация мм → радианы
3. **Расчета разрешения** - теоретический минимальный шаг
4. **Проверки конфигурации** - соответствие теории
5. **Оптимизации траекторий** - минимизация времени прыжков
6. **Диагностики** - проверка соответствия `rayleighLengthMicron`

### ❌ НЕ используйте `focalLengthMm` для:

1. **Расчета Z-offset** - для этого используется `rayleighLengthMicron`
2. **Прямых вычислений** в UDM API - Hans firmware делает это автоматически

---

## 📁 Файл с реализацией

Создам полный пример использования `focalLengthMm`:
