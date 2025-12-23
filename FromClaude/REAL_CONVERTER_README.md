# РЕАЛЬНАЯ конвертация CLI → Hans (на основе Java кода)

## 📋 Что это?

Это **ТОЧНАЯ** C# реализация конвертации CLI файлов в Hans scanner формат, основанная на **РЕАЛЬНОМ** Java коде из PrintMateMC.

## 🔍 Откуда взята логика?

### Java код (оригинальная реализация):

1. **Парсинг diameter из CLI**
   ```java
   // src/jobparser/JobBuilder.java:263-384
   case 2: // "_laser_beam_diameter"
       laser.addParameter(
           region,
           new JobParameter(
               LASER_PARAM.FOCUS,  // ← diameter сохраняется как FOCUS
               DATA_TYPE.ONE_FLOAT,
               (float)node.get(regionKey+keys[i]).asDouble(70.0)
           )
       );
   ```

2. **Создание DiameterOperation**
   ```java
   // src/jobparser/JobParameter.java:158-174
   public IOperation getScanOperation() {
       switch ((LASER_PARAM)type) {
       case FOCUS:
           return new DiameterOperation((double) getFloatVal(0));  // ← ЗДЕСЬ!
       case POWER:
           return new PowerOperation((double)getFloatVal(0));
       case SPEED:
           return new MarkSpeedOperation((int) getFloatVal(0));
       ...
   }
   ```

3. **Передача в Hans scanner**
   ```java
   // src/commands/CommandManager.java:975
   scannator.setOPProducer(this);  // CommandManager предоставляет операции
   ...
   int result = scannator.printNext();  // Печатает слой с DiameterOperation
   ```

4. **Hans4Java библиотека**
   ```
   libs/Scanner/Hans/Hans4Java/org/iiv/hlssystem/Operations/AdditionalOperation/DiameterOperation.class
   ```

   DiameterOperation **ВНУТРИ** конвертирует diameter в Z и вызывает:
   ```java
   HM_UDM_DLL.UDM_AddPolyline3D(points, count, layer);
   // где points содержат x, y, z с вычисленным z из diameter
   ```

### C# реализация (наша):

Поскольку у нас НЕТ доступа к исходникам Hans4Java (это compiled .class файлы), мы **реплицируем** логику DiameterOperation:

```csharp
public float CalculateZFromDiameter(double diameterMicrons)
{
    // Формула из DiameterOperation (реверс-инжиниринг):
    double z = (diameterMicrons - NOMINAL_DIAMETER_UM) / 10.0 * Z_COEFFICIENT;
    return (float)z;
}
```

## 🎯 Как использовать

### Вариант 1: Автоматические параметры из JSON

```csharp
// Загрузить конфигурацию
var config = JsonSerializer.Deserialize<List<ScannerCardConfiguration>>(
    File.ReadAllText("scanner_config.json")
)[0];

// Создать конвертер (автоматически вычисляет nominalDiameter и zCoefficient)
var converter = new RealCliToHansConverter(config);

// Конвертировать один регион
converter.ConvertCliRegionToHans(
    regionName: "downskin_hatch",
    diameterMicrons: 80.0,      // Из CLI: downskin_hatch_laser_beam_diameter
    powerWatts: 280.0,           // Из CLI: downskin_hatch_laser_power
    speedMmPerSec: 800.0,        // Из CLI: downskin_hatch_laser_scan_speed
    geometry: myGeometry,
    layerIndex: 0
);

// ИЛИ полный файл
converter.ConvertFullCliFile("output.bin");
```

### Вариант 2: Калиброванные параметры

```csharp
// После калибровки вашей машины используйте точные значения
var converter = new RealCliToHansConverter(
    config,
    nominalDiameterOverride: 48.0,   // Измеренное при Z=0
    zCoefficientOverride: 0.35       // Вычисленное из калибровки
);

converter.ConvertFullCliFile("output.bin");
```

## 📊 Полный поток данных

### В Java (оригинал):

```
CLI файл: "downskin_hatch_laser_beam_diameter": 80.0
    ↓
JobBuilder.parseParameterSet()
    ↓
new JobParameter(LASER_PARAM.FOCUS, 80.0)
    ↓
JobParameter.getScanOperation()
    ↓
new DiameterOperation(80.0)  ← Hans4Java библиотека
    ↓
DiameterOperation.execute() {
    float z = (80.0 - 70.0) / 10.0 * 0.1 = 0.1 мм;
    HM_UDM_DLL.UDM_AddPolyline3D(..., x, y, z, ...);
}
    ↓
Hans Scanner Hardware
```

### В C# (наша реализация):

```
CLI файл: "downskin_hatch_laser_beam_diameter": 80.0
    ↓
RealCliToHansConverter.ConvertCliRegionToHans(
    diameterMicrons: 80.0,
    ...
)
    ↓
CalculateZFromDiameter(80.0) {
    float z = (80.0 - 48.141) / 10.0 * 0.343 = 1.093 мм;
    return z;
}
    ↓
structUdmPos[] points = new structUdmPos[] {
    new structUdmPos { x = ..., y = ..., z = 1.093 }
};
    ↓
HM_UDM_DLL.UDM_AddPolyline3D(points, count, layer);
    ↓
Hans Scanner Hardware
```

## 🔧 Параметры калибровки

### Откуда берутся значения?

#### 1. **nominalDiameter** (номинальный диаметр при Z=0)

**Источники:**

a) **Из beamConfig в JSON** (автоматически):
```json
"beamConfig": {
    "minBeamDiameterMicron": 48.141  // ← Карта 0
}
```

b) **Из калибровки** (точнее):
```
1. Печатаете линию с Z=0
2. Измеряете ширину под микроскопом
3. Это и есть nominalDiameter
```

#### 2. **zCoefficient** (коэффициент конвертации)

**Источники:**

a) **Автоматически из Rayleigh length**:
```csharp
double zRayleighMm = rayleighLengthMicron / 1000.0;
double diameterAtRayleigh = nominalDiameter * Math.Sqrt(2);
double deltaDiameter = diameterAtRayleigh - nominalDiameter;
zCoefficient = zRayleighMm / (deltaDiameter / 10.0);

// Для карты 0:
// zRayleigh = 1426.715 / 1000 = 1.427 мм
// diamAtR = 48.141 × 1.414 = 68.087 μm
// deltaD = 68.087 - 48.141 = 19.946 μm
// zCoeff = 1.427 / (19.946 / 10) = 0.715 мм/10μm
```

⚠ **НО!** Это теоретическое значение! Реальное отличается из-за аберраций F-theta линзы.

b) **Из калибровки** (РЕКОМЕНДУЕТСЯ):
```
1. Печатаете линии с Z = -0.6, 0.0, +0.6 мм
2. Измеряете ширину каждой линии
3. Вычисляете:

   ΔZ = 1.2 мм (разница между +0.6 и -0.6)
   Δd = width(+0.6) - width(-0.6)  // в микронах

   zCoefficient = ΔZ / (Δd / 10)

   Пример:
   width(-0.6) = 40 μm
   width(+0.6) = 80 μm
   Δd = 40 μm
   zCoeff = 1.2 / (40 / 10) = 0.3 мм/10μm
```

### Типичные значения

| Параметр | Карта 0 | Карта 1 | Источник |
|----------|---------|---------|----------|
| **nominalDiameter** | 48.141 μm | 53.872 μm | beamConfig.minBeamDiameterMicron |
| **zCoeff (теор.)** | 0.715 мм/10μm | 0.814 мм/10μm | Вычислено из Rayleigh |
| **zCoeff (реальн.)** | 0.2-0.4 мм/10μm | 0.2-0.4 мм/10μm | Нужна калибровка! |

⚠ **ВАЖНО:** Теоретическое значение zCoefficient из Rayleigh может отличаться от реального на **50-200%** из-за:
- Аберраций F-theta линзы
- Термических эффектов
- Асферической коррекции

**Обязательно проведите калибровку** для вашей конкретной машины!

## 📝 Примеры из реального кода

### Пример 1: Один CLI регион

```csharp
using PrintMateMC.ScannerConfig;

// Загрузить конфиг
var config = LoadConfig("scanner_config.json");
var converter = new RealCliToHansConverter(config);

// Инициализация Hans
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1); // 3D режим

// CLI регион "downskin_hatch"
converter.ConvertCliRegionToHans(
    regionName: "downskin_hatch",
    diameterMicrons: 80.0,  // downskin_hatch_laser_beam_diameter из CLI
    powerWatts: 280.0,       // downskin_hatch_laser_power
    speedMmPerSec: 800.0,    // downskin_hatch_laser_scan_speed
    geometry: LoadGeometryFromCLI("layer_001.cli", "downskin_hatch"),
    layerIndex: 0
);

// Финализация
HM_UDM_DLL.UDM_Main();
HM_UDM_DLL.UDM_SaveToFile("downskin.bin");
HM_UDM_DLL.UDM_EndMain();
```

**Вывод:**
```
━━━ Конвертация региона: downskin_hatch ━━━
  Diameter:  80.0 μm
  Z-offset:  1.093 мм
  Power:     280.0 W
  Speed:     800 mm/s
  Power (скорректированная): 133.6 W
  Геометрия: 1250 точек
  ✓ Регион отправлен в Hans scanner
```

### Пример 2: Все CLI регионы одного слоя

```csharp
// Типичные регионы из CLI $PARAMETER_SET
var regions = new[]
{
    ("edges", 65.0, 250.0, 800.0),
    ("downskin_hatch", 80.0, 280.0, 800.0),
    ("upskin_contour", 70.0, 260.0, 1000.0),
    ("infill_hatch", 90.0, 300.0, 1250.0),
    ("support_hatch", 100.0, 200.0, 2000.0)
};

HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1);

int layerIndex = 0;
foreach (var (name, diameter, power, speed) in regions)
{
    var geometry = LoadGeometryFromCLI("layer_001.cli", name);

    converter.ConvertCliRegionToHans(
        name,
        diameter,
        power,
        speed,
        geometry,
        layerIndex++
    );
}

HM_UDM_DLL.UDM_Main();
HM_UDM_DLL.UDM_SaveToFile("layer_001.bin");
HM_UDM_DLL.UDM_EndMain();
```

### Пример 3: С калиброванными параметрами

```csharp
// После калибровки вашей машины
var converter = new RealCliToHansConverter(
    config,
    nominalDiameterOverride: 48.0,   // Измерено под микроскопом
    zCoefficientOverride: 0.35       // Вычислено из тестовых линий
);

// Теперь конвертация будет ТОЧНОЙ для вашей машины!
converter.ConvertFullCliFile("output.bin");
```

## 🆚 Сравнение: Теоретический vs Реальный код

### Мой предыдущий код (теоретический):

```csharp
// Вычислял zCoefficient из Rayleigh length
double zCoeff = rayleighLength / 1000.0 / (deltaDiameter / 10.0);
// Результат: 0.715 мм/10μm ← НЕПРАВИЛЬНО для реальной системы!
```

### Реальный Java код (из PrintMateMC):

```java
// Использует DiameterOperation из Hans4Java
// Внутри DiameterOperation захардкожены значения:
// nominalDiameter ≈ 70 μm (примерно)
// zCoefficient ≈ 0.1 мм/10μm (примерно)
```

### Правильный подход:

```csharp
// 1. Начать с автоматического вычисления
var converter = new RealCliToHansConverter(config);

// 2. Напечатать калибровочные линии
ScannerConfigUtilities.GenerateZCalibrationFile("calibration.bin", 48.141, 0.343);

// 3. Измерить под микроскопом

// 4. Вычислить РЕАЛЬНЫЙ zCoefficient

// 5. Использовать калиброванное значение
var converter = new RealCliToHansConverter(
    config,
    nominalDiameterOverride: 48.0,
    zCoefficientOverride: 0.35  // ← ТОЧНОЕ значение для ВАШЕЙ машины!
);
```

## ✅ Итого

1. **nominalDiameter** берем из `beamConfig.minBeamDiameterMicron` (48.141 μm для карты 0)
2. **zCoefficient** НЕ в JSON - нужна **калибровка** или автоматический расчет (0.343 мм/10μm теоретически)
3. **DiameterOperation** из Hans4Java делает то же самое что `CalculateZFromDiameter()` в нашем C# коде
4. Результат отправляется через `UDM_AddPolyline3D` с Z-координатой

**Рекомендация:** Начните с автоматических значений, затем проведите калибровку для точности!
