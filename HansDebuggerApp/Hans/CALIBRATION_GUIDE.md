# Руководство по калибровке полинома управления диаметром пучка

## Обзор

Этот документ описывает процесс калибровки полинома 3D коррекции для точного управления диаметром лазерного пучка в системах Hans GMC с вариофокальной линзой.

## Проблема

При использовании вариофокальной линзы для изменения диаметра пучка, SDK Hans требует полиномиальную коррекцию Z-координаты:

```
Z_corrected = a*f² + b*f + c
```

Где:
- `f` — focal length с учётом смещения для диаметра (mm)
- `a, b, c` — коэффициенты полинома
- `Z_corrected` — корректированная Z-координата для SDK

**Неправильные коэффициенты** приводят к тому, что:
- Запрашиваемый диаметр 80 μm → Реальный диаметр 250 μm ❌
- SDK отклоняет отрицательные Z значения
- Управление диаметром не работает

## Решение: Калибровка полинома

### Метод 1: Простая калибровка (Simple Calibration)

**Когда использовать:**
- У вас уже есть откалиброванный `bfactor`
- Нужно быстро исправить отрицательные Z

**Как работает:**
1. Использует существующий `bfactor` из конфигурации
2. Вычисляет `cfactor` так, чтобы для минимального диаметра (65 μm) Z ≈ 0

**Код:**
```csharp
var calibrator = new PolynomialCalibrator(beamConfig, baseFocal);
var result = calibrator.CalibrateSimple(existingBfactor: 0.013944261, minDiameterMicron: 65.0);

Console.WriteLine($"cfactor = {result.Cfactor:F6}");
// Output: cfactor = -7.507
```

**Результат:**
```json
{
  "correctionPolynomial": [-7.507, 0.013944261, 0.0]
}
```

---

### Метод 2: Полная калибровка (Full Calibration)

**Когда использовать:**
- Первая калибровка новой системы
- После замены вариофокальной линзы
- Когда нужна максимальная точность

**Процесс:**

#### Шаг 1: Подготовка точек тестирования

```csharp
var calibrator = new PolynomialCalibrator(beamConfig, baseFocal);

// Выбираем целевые диаметры для тестирования
double[] targetDiameters = { 65.0, 80.0, 100.0, 120.0, 150.0 };

// Получаем список точек для тестирования
var testPoints = calibrator.PrepareCalibrationPoints(targetDiameters);

// Выводим таблицу
foreach (var point in testPoints)
{
    Console.WriteLine($"Target: {point.TargetDiameterMicron} μm, Z offset: {point.ZOffsetMm:F3} mm");
}
```

**Output:**
```
Target: 65.0 μm, Z offset: 0.000 mm
Target: 80.0 μm, Z offset: 1.378 mm
Target: 100.0 μm, Z offset: 2.246 mm
Target: 120.0 μm, Z offset: 3.042 mm
Target: 150.0 μm, Z offset: 4.188 mm
```

#### Шаг 2: Генерация UDM файлов и тестирование

Для каждого целевого диаметра:

1. **Генерируем UDM файл:**
   ```csharp
   var builder = new TestUdmBuilder(config);
   string udmPath = builder.BuildSinglePoint(x: 0, y: 0,
       beamDiameterMicron: 65.0,
       powerWatts: 75.0,
       dwellTimeMs: 10000);
   ```

2. **Загружаем на сканатор и выполняем маркировку**

3. **Измеряем реальный диаметр пятна:**
   - Камера с калибровкой
   - Тест на прожиг (burn test)
   - Профилометр луча

4. **Записываем результат:**
   ```
   Целевой: 65.0 μm → Измеренный: 66.5 μm
   Целевой: 80.0 μm → Измеренный: 82.1 μm
   Целевой: 100.0 μm → Измеренный: 98.5 μm
   ...
   ```

#### Шаг 3: Калибровка по измерениям

```csharp
// Заполняем измеренные значения
var measurements = new List<PolynomialCalibrator.CalibrationPoint>
{
    new() { TargetDiameterMicron = 65.0, MeasuredDiameterMicron = 66.5, ZOffsetMm = 0.0, FocalLengthMm = 538.46 },
    new() { TargetDiameterMicron = 80.0, MeasuredDiameterMicron = 82.1, ZOffsetMm = 1.378, FocalLengthMm = 539.838 },
    new() { TargetDiameterMicron = 100.0, MeasuredDiameterMicron = 98.5, ZOffsetMm = 2.246, FocalLengthMm = 540.706 },
    new() { TargetDiameterMicron = 120.0, MeasuredDiameterMicron = 119.2, ZOffsetMm = 3.042, FocalLengthMm = 541.502 },
    new() { TargetDiameterMicron = 150.0, MeasuredDiameterMicron = 153.8, ZOffsetMm = 4.188, FocalLengthMm = 542.648 }
};

// Выполняем калибровку методом наименьших квадратов
var result = calibrator.CalibrateLinearPolynomial(measurements);

// Выводим отчёт
Console.WriteLine(calibrator.GenerateCalibrationReport(result));
```

**Output:**
```
╔═══════════════════════════════════════════════════════════════╗
║           CALIBRATION REPORT - Z CORRECTION POLYNOMIAL         ║
╚═══════════════════════════════════════════════════════════════╝

CALIBRATED COEFFICIENTS:
  bfactor = 0.013956782
  cfactor = -7.515234
  afactor = 0.000000

POLYNOMIAL FORMULA:
  Z(f) = 0.000000×f² + 0.013956782×f + (-7.515234)

JSON CONFIGURATION:
  "correctionPolynomial": [
    -7.515234,  // Cfactor (constant)
    0.013956782,  // Bfactor (linear)
    0.000000   // Afactor (quadratic)
  ]

ERROR STATISTICS:
  RMS Error:     2.15 μm
  Max Error:     3.80 μm

CALIBRATION POINTS:
┌─────────────┬─────────────┬──────────┬──────────────┐
│ Target (μm) │ Measured    │ Z (mm)   │ Error (μm)   │
├─────────────┼─────────────┼──────────┼──────────────┤
│        65.0 │        66.5 │    0.000 │         1.50 │
│        80.0 │        82.1 │    1.378 │         2.10 │
│       100.0 │        98.5 │    2.246 │         1.50 │
│       120.0 │       119.2 │    3.042 │         0.80 │
│       150.0 │       153.8 │    4.188 │         3.80 │
└─────────────┴─────────────┴──────────┴──────────────┘
```

#### Шаг 4: Обновление конфигурации

Скопируйте новые значения в ваш JSON файл:

```json
{
  "thirdAxisConfig": {
    "bfactor": 0.013956782,
    "cfactor": -7.515234,
    "afactor": 0.0,
    "baseFocal": 538.46,
    "correctionPolynomial": [-7.515234, 0.013956782, 0.0]
  }
}
```

---

## Критерии качества калибровки

| RMS Error | Качество | Рекомендация |
|-----------|----------|--------------|
| < 5 μm    | ⭐⭐⭐ Отлично | Использовать результат |
| 5-10 μm   | ⭐⭐ Хорошо   | Использовать результат |
| 10-20 μm  | ⭐ Приемлемо | Рассмотреть повторную калибровку |
| > 20 μm   | ❌ Плохо     | Проверить оборудование, повторить измерения |

---

## Частые проблемы

### Проблема 1: Отрицательные Z значения

**Симптом:**
```
Z final: -0.362 mm
⚠️ WARNING: Z = -0.362 mm is OUT OF RANGE [0, 1] mm!
```

**Причина:** Неправильный `cfactor`

**Решение:** Запустите простую калибровку:
```csharp
var result = calibrator.CalibrateSimple(existingBfactor, minDiameter);
```

### Проблема 2: Большая ошибка калибровки (RMS > 20 μm)

**Возможные причины:**
1. Неточные измерения диаметра
2. Неправильный `RayleighLengthMicron`
3. M² фактор не откалиброван
4. Вариофокальная линза работает некорректно

**Решение:**
1. Проверьте точность измерительного оборудования
2. Откалибруйте `RayleighLengthMicron` и `M2`:
   ```csharp
   beamConfig.RayleighLengthMicron = π × (d₀/2)² × M² / λ
   ```
3. Используйте больше точек калибровки (7-10 вместо 5)

### Проблема 3: SDK возвращает странный Z

**Симптом:**
```
Z final: 0.020 mm
Z final 2222 (SDK): 0.0003 mm  ← Большая разница!
```

**Причина:** SDK применяет обратный полином неправильно

**Решение:** Проверьте:
1. Порядок коэффициентов в `correctionPolynomial`: `[cfactor, bfactor, afactor]`
2. Что `baseFocal` совпадает с параметром `UDM_Set3dCorrectionPara`

---

## Пример запуска

```csharp
// В Main или кнопке UI
CalibrationExample.RunCalibrationExample();

// Или интерактивный мастер (TODO)
// CalibrationExample.InteractiveCalibrationWizard();
```

---

## Формулы

### Расчёт Z offset для диаметра (Gaussian Beam):

```
d(z) = d₀ × sqrt[1 + (z/z_R)²]

Где:
  d(z) - диаметр на расстоянии z
  d₀ - минимальный диаметр (в фокусе)
  z_R - Rayleigh length

Обратное:
  z = z_R × sqrt[(d/d₀)² - 1]
```

### Rayleigh Length:

```
z_R = π × (d₀/2)² × M² / λ

Где:
  d₀ - минимальный диаметр
  M² - beam quality factor
  λ - длина волны
```

### Полином коррекции:

```
Z_corrected = a × f² + b × f + c

Где:
  f - focal length с учётом Z offset (mm)
  a, b, c - калиброванные коэффициенты
```

---

## Дополнительные ресурсы

- `PolynomialCalibrator.cs` - Класс калибратора
- `CalibrationExample.cs` - Примеры использования
- `TestUdmBuilder.cs` - Генератор UDM файлов для тестирования
- `BeamConfig.cs` - Модель конфигурации луча

---

## Changelog

- **2025-01-19**: Первая версия руководства
