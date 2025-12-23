# Hans beamConfig - Краткая Сводка

## 🎯 Главное

Параметры `beamConfig` используются для **расчета Z-offset** из целевого диаметра луча.

```json
{
  "beamConfig": {
    "minBeamDiameterMicron": 48.141,      // d₀
    "rayleighLengthMicron": 1426.715,     // z_R
    "wavelengthNano": 1070.0,             // λ
    "m2": 1.127,                          // M²
    "focalLengthMm": 538.46               // f
  }
}
```

---

## 📐 Формула

```
d(z) = d₀ × sqrt(1 + (z / z_R)²)
```

**Обратная формула (то, что нужно):**

```
z = z_R × sqrt((d_target / d₀)² - 1)
```

Где:
- `z` - Z-offset (mm)
- `d_target` - целевой диаметр из CLI (μm)
- `d₀` - минимальный диаметр (μm)
- `z_R` - длина Рэлея (μm)

---

## 🔄 Как это работает в Hans4Java

```java
// 1. CLI содержит параметр diameter
"edges_laser_beam_diameter": 80.0  // μm

// 2. Парсер создает DiameterOperation
DiameterOperation op = new DiameterOperation(80.0);

// 3. UdmProducer обрабатывает операцию
case OpType.DIAMETER:
    double targetDiameter = (Double)op.getData()[0];  // 80.0

    // 4. Рассчитывается Z-offset
    double z = this.cardProfile.beamConfig.calculateZOffset(targetDiameter);
    // z = 1426.715 × sqrt((80/48.141)² - 1) = 1.894 mm

    // 5. Z применяется к геометрии
    structUdmPos.z = z;
    break;
```

---

## 💻 Реализация в C#

```csharp
public class BeamConfig
{
    public double MinBeamDiameterMicron { get; set; } = 48.141;
    public double RayleighLengthMicron { get; set; } = 1426.715;

    public float CalculateZOffset(double targetDiameterMicron)
    {
        if (targetDiameterMicron <= MinBeamDiameterMicron)
            return 0.0f;

        // z = z_R × sqrt((d_target / d₀)² - 1)
        double ratio = targetDiameterMicron / MinBeamDiameterMicron;
        double z_micron = RayleighLengthMicron * Math.Sqrt(ratio * ratio - 1.0);

        return (float)(z_micron / 1000.0);  // μm → mm
    }
}
```

---

## 📊 Примеры для вашей конфигурации

| CLI Parameter | Diameter (μm) | Z-offset (mm) | Применение |
|--------------|--------------|--------------|------------|
| `edges_laser_beam_diameter` | 80 | **1.894** | Контуры детали |
| `downskin_border_laser_beam_diameter` | 90 | **2.224** | Нижняя граница |
| `infill_hatch_laser_beam_diameter` | 100 | **2.522** | Заполнение |
| `support_hatch_laser_beam_diameter` | 120 | **3.052** | Поддержки |

---

## ✅ Как использовать в CLI конвертере

```csharp
// 1. Создать beamConfig из JSON
BeamConfig beamConfig = new BeamConfig
{
    MinBeamDiameterMicron = 48.141,
    RayleighLengthMicron = 1426.715
};

// 2. При обработке CLI региона
CliRegion region = /* ... parse from CLI ... */;
// region.BeamDiameter = 80.0 (из edges_laser_beam_diameter)

// 3. Рассчитать Z-offset
float z = beamConfig.CalculateZOffset(region.BeamDiameter);
// z = 1.894 mm

// 4. Применить к геометрии
structUdmPos[] points = new structUdmPos[region.Points.Count];
for (int i = 0; i < region.Points.Count; i++)
{
    points[i] = new structUdmPos
    {
        x = region.Points[i].X,
        y = region.Points[i].Y,
        z = z  // ← Применить рассчитанный Z-offset
    };
}

HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, layerIndex);
```

---

## 🔍 Физический смысл

### Почему `z` положительный?

- Z=0: Фокус точно на детали (d = 48.141 μm)
- Z>0: Фокус **ниже** детали → луч расходится → больший диаметр
- Z<0: Фокус **выше** детали (не используется в вашей системе)

### Зачем разные диаметры?

1. **Малый диаметр (80 μm)** - для **edges**:
   - Высокая точность
   - Высокая плотность энергии
   - Хорошее плавление границ

2. **Средний диаметр (100 μm)** - для **infill**:
   - Баланс скорости и качества
   - Хорошее перекрытие треков

3. **Большой диаметр (120 μm)** - для **supports**:
   - Низкая плотность энергии
   - Слабое спекание
   - Легко удалить

---

## 📁 Полный пример

Смотрите:
- **[Hans_CLI_Complete_Example.cs](Hans_CLI_Complete_Example.cs)** - полный рабочий пример
- **[Hans_BeamConfig_Analysis.md](Hans_BeamConfig_Analysis.md)** - детальный анализ

---

## 🎓 Ключевые выводы

1. ✅ `beamConfig` используется для **расчета Z-offset** из diameter
2. ✅ Формула: `z = z_R × sqrt((d_target / d₀)² - 1)`
3. ✅ Z-offset устанавливается в `structUdmPos.z`
4. ✅ Разные регионы CLI → разные диаметры → разные Z-offset
5. ✅ Hans4Java делает это автоматически через `DiameterOperation`

---

**Версия:** 1.0
**Дата:** 2025
