# Как использовать diameter из CLI с вашей конфигурацией

## 🎯 Краткий ответ

CLI параметр `laser_beam_diameter` (μm) → **рассчитывается Z-offset** → устанавливается в `structUdmPos.z`

---

## 📐 Формула

```
Z-offset (mm) = z_R × sqrt((CLI_diameter / d₀)² - 1) / 1000
```

Где:
- `CLI_diameter` - из CLI JSON (80, 100, 120 μm...)
- `d₀` - `minBeamDiameterMicron` из beamConfig
- `z_R` - `rayleighLengthMicron` из beamConfig

---

## 🔧 У вас два лазера с РАЗНЫМИ параметрами:

### Laser 1 (172.18.34.227)
```json
"beamConfig": {
  "minBeamDiameterMicron": 48.141,      // ← d₀
  "rayleighLengthMicron": 1426.715      // ← z_R
}
```

### Laser 2 (172.18.34.228)
```json
"beamConfig": {
  "minBeamDiameterMicron": 53.872,      // ← d₀ БОЛЬШЕ
  "rayleighLengthMicron": 1616.16       // ← z_R БОЛЬШЕ
}
```

---

## 📊 Примеры расчета для вашей конфигурации

### Laser 1:

| CLI Parameter | Diameter (μm) | **Z-offset (mm)** |
|--------------|--------------|-------------------|
| `edges_laser_beam_diameter` | 80 | **1.894** |
| `downskin_border_laser_beam_diameter` | 90 | **2.224** |
| `infill_hatch_laser_beam_diameter` | 100 | **2.522** |
| `support_hatch_laser_beam_diameter` | 120 | **3.052** |

### Laser 2:

| CLI Parameter | Diameter (μm) | **Z-offset (mm)** |
|--------------|--------------|-------------------|
| `edges_laser_beam_diameter` | 80 | **1.476** |
| `downskin_border_laser_beam_diameter` | 90 | **1.799** |
| `infill_hatch_laser_beam_diameter` | 100 | **2.085** |
| `support_hatch_laser_beam_diameter` | 120 | **2.595** |

**⚠️ ВАЖНО:** Laser 2 требует МЕНЬШИЙ Z-offset для того же диаметра!

---

## 💻 Реализация в C#

```csharp
// 1. Создать BeamConfig для каждого лазера
BeamConfig laser1 = new BeamConfig
{
    MinBeamDiameterMicron = 48.141,
    RayleighLengthMicron = 1426.715
};

BeamConfig laser2 = new BeamConfig
{
    MinBeamDiameterMicron = 53.872,
    RayleighLengthMicron = 1616.16
};

// 2. При обработке CLI региона
double cliDiameter = 80.0;  // из edges_laser_beam_diameter
int laserIndex = 0;         // 0 = laser 1, 1 = laser 2

BeamConfig selectedLaser = (laserIndex == 0) ? laser1 : laser2;

// 3. Рассчитать Z-offset
float z = selectedLaser.CalculateZOffset(cliDiameter);

// 4. Применить к геометрии
structUdmPos[] points = new structUdmPos[...];
for (int i = 0; i < points.Length; i++)
{
    points[i] = new structUdmPos
    {
        x = ...,
        y = ...,
        z = z  // ← Рассчитанный Z-offset
    };
}

HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, layerIndex);
```

---

## 🔄 Полный workflow CLI → Hans

```
1. CLI JSON:
   {
     "edges_laser_beam_diameter": 80.0,
     "edge_skywriting": "1",
     "laser_scan_speed": 800,
     "laser_power": 140.0
   }

2. Парсинг → CliRegion:
   region.BeamDiameter = 80.0
   region.SkyWritingEnabled = true
   region.MarkSpeed = 800
   region.LaserPower = 140.0
   region.LaserIndex = 0  (какой лазер)

3. Выбор конфигурации:
   LaserCardConfig laser = laserConfigs[region.LaserIndex]
   SpeedConfig speed = laser.FindSpeedConfig(region.MarkSpeed)

4. Расчет Z-offset:
   float z = laser.BeamConfig.CalculateZOffset(region.BeamDiameter)
   // Для laser 1, diameter 80 → z = 1.894 mm

5. Применение SkyWriting:
   ApplySWEnableOperation_Hans4JavaWay(
       enable: region.SkyWritingEnabled,
       delays from speed config...
   )

6. Установка параметров:
   MarkParameter.MarkSpeed = region.MarkSpeed
   MarkParameter.LaserPower = region.LaserPower / maxPower * 100

7. Добавление геометрии:
   structUdmPos { x, y, z = z }
   UDM_AddPolyline3D(...)

8. Генерация файла:
   UDM_Main()
   UDM_SaveToFile("output.bin")
```

---

## ⚙️ Дополнительные коррекции Z

### 1. Third Axis Config - коррекция кривизны поля

```
Z_correction = A×r² + B×r + C
где r = sqrt(x² + y²)
```

**Для Laser 1:**
```
A = 0.0
B = 0.013944261
C = -7.5056114
```

**Для Laser 2:**
```
A = 0.0
B = 0.0139135085
C = -7.477292
```

**Применение:**
```csharp
float z_field = (float)(B * r + C);
float z_total = z_diameter + z_field;
```

### 2. Scanner Config - статический offset

**Для Laser 1:**
```json
"offsetZ": -0.001
```

**Для Laser 2:**
```json
"offsetZ": 0.102
```

### 3. Итоговый Z-offset

```csharp
float z_diameter = beamConfig.CalculateZOffset(cliDiameter);
float z_field = thirdAxisConfig.CalculateZCorrection(x, y);
float z_static = (float)scannerConfig.OffsetZ;

float z_total = z_diameter + z_field + z_static;

structUdmPos.z = z_total;
```

---

## 📁 Полный пример

Смотрите: **[Hans_DualLaser_CLI_Example.cs](Hans_DualLaser_CLI_Example.cs)**

Ключевой класс: `DualLaserCliConverter.ConvertRegion()`

---

## 🎓 Ключевые выводы

1. ✅ CLI `laser_beam_diameter` → Z-offset через `beamConfig`
2. ✅ У вас **два лазера** → **разные** `beamConfig` → **разные** Z-offset
3. ✅ Формула: `z = z_R × sqrt((d/d₀)² - 1)`
4. ✅ Дополнительно: коррекция кривизны поля + статический offset
5. ✅ Итоговый Z: `z_total = z_diameter + z_field + z_static`

---

## 🚀 Быстрый старт

```csharp
// Загрузить ваш JSON конфиг
var laserConfigs = LoadFromJson("your_config.json");

// Парсить CLI
var cliRegions = ParseCliFile("file.cli");

// Конвертировать
var converter = new DualLaserCliConverter(laserConfigs);
converter.ConvertFullCliFile(cliRegions, "output");
```

**Результат:** `.bin` файлы с правильными Z-offset для каждого региона!

---

**Версия:** 1.0
**Дата:** 2025
**Файл:** [Hans_DualLaser_CLI_Example.cs](Hans_DualLaser_CLI_Example.cs)
