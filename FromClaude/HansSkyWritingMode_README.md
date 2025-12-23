# UDM_SetSkyWritingMode - Подробное руководство

## Описание функции

```csharp
public extern static int UDM_SetSkyWritingMode(
    int enable,        // 0 = выключить, 1 = включить SkyWriting
    int mode,          // Режим (обычно 0)
    float uniformLen,  // Длина равномерного участка (мм)
    float accLen,      // Длина участка ускорения (мм)
    float angleLimit   // Предельный угол (градусы)
);
```

## Параметры функции

### 1. `enable` (int)
- **0**: Отключить SkyWriting
- **1**: Включить SkyWriting
- Соответствует параметру `swenable` из конфигурации сканера
- Берется из CLI JSON: `"edge_skywriting": "1"`

### 2. `mode` (int)
- **0**: Стандартный режим (используется по умолчанию)
- Другие значения зарезервированы для специальных режимов
- В большинстве случаев всегда используйте **0**

### 3. `uniformLen` (float, мм)
- Длина равномерного участка траектории
- **Диапазон**: 0.05 - 0.3 мм
- **Типичные значения**:
  - 0.08 - 0.10 мм: для низких скоростей (500-800 mm/s)
  - 0.10 - 0.15 мм: для средних скоростей (800-1500 mm/s)
  - 0.15 - 0.25 мм: для высоких скоростей (1500-2500 mm/s)
- **Соответствует**: параметру `umax` из конфигурации сканера
- **Влияние**: чем больше → тем более плавная траектория, но может округлять углы

### 4. `accLen` (float, мм)
- Длина участка ускорения/торможения
- **Диапазон**: 0.02 - 0.15 мм
- **Типичное значение**: 0.5 × uniformLen
- **Влияние**: чем больше → тем плавнее ускорение/торможение

### 5. `angleLimit` (float, градусы)
- Предельный угол, при котором применяется SkyWriting
- **Диапазон**: 60° - 150°
- **Типичные значения**:
  - 130° - 150°: применяется почти ко всем углам
  - 110° - 130°: стандартный режим
  - 90° - 110°: только для плавных изгибов
  - 60° - 90°: только для очень плавных кривых
- **Влияние**: чем меньше → тем реже применяется SkyWriting

## Связь с CLI параметрами

### Вариант 1: Простой режим (только enable/disable)

В CLI JSON:
```json
{
  "edge_skywriting": "1",
  "infill_hatch_skywriting": "1",
  "support_hatch_skywriting": "0"
}
```

Использование:
```csharp
int enable = int.Parse(cliParams.EdgeSkywriting);

// Использовать значения по умолчанию
HM_UDM_DLL.UDM_SetSkyWritingMode(
    enable,
    mode: 0,
    uniformLen: 0.1f,
    accLen: 0.05f,
    angleLimit: 120.0f
);
```

### Вариант 2: Расширенный режим (с параметрами)

В CLI JSON:
```json
{
  "edge_skywriting": "1",
  "edge_skywriting_uniformLen": "0.12",
  "edge_skywriting_accLen": "0.06",
  "edge_skywriting_angleLimit": "110.0"
}
```

Использование:
```csharp
HM_UDM_DLL.UDM_SetSkyWritingMode(
    enable: int.Parse(cliParams.EdgeSkywriting),
    mode: 0,
    uniformLen: float.Parse(cliParams.EdgeSkywritingUniformLen),
    accLen: float.Parse(cliParams.EdgeSkywritingAccLen),
    angleLimit: float.Parse(cliParams.EdgeSkywritingAngleLimit)
);
```

### Вариант 3: Из конфигурации сканера

В scanner config JSON:
```json
{
  "processVariablesMap": {
    "markSpeed": [
      {
        "markSpeed": 1000,
        "swenable": true,
        "umax": 0.15,
        "accLen": 0.075,
        "angleLimit": 110.0
      }
    ]
  }
}
```

Маппинг параметров:
```csharp
int enable = config.SWEnable ? 1 : 0;
float uniformLen = (float)config.Umax;       // umax → uniformLen
float accLen = (float)config.AccLen;
float angleLimit = (float)config.AngleLimit;

HM_UDM_DLL.UDM_SetSkyWritingMode(
    enable,
    0,
    uniformLen,
    accLen,
    angleLimit
);
```

## Рекомендации по выбору параметров

### Для контуров (Edges, Borders)
```csharp
HM_UDM_DLL.UDM_SetSkyWritingMode(
    enable: 1,
    mode: 0,
    uniformLen: 0.10f,   // Умеренное сглаживание
    accLen: 0.05f,
    angleLimit: 120.0f   // Стандартный угол
);
```

**Причины**:
- Контуры требуют точности
- Умеренное сглаживание сохраняет геометрию
- angleLimit 120° применяется к большинству углов

### Для заполнения (Infill, Hatch)
```csharp
HM_UDM_DLL.UDM_SetSkyWritingMode(
    enable: 1,
    mode: 0,
    uniformLen: 0.15f,   // Агрессивное сглаживание
    accLen: 0.08f,
    angleLimit: 100.0f   // Только для плавных углов
);
```

**Причины**:
- Заполнение менее критично к точности
- Больше uniformLen → быстрее печать
- Меньше angleLimit → SkyWriting только на плавных участках

### Для поддержек (Supports)
```csharp
HM_UDM_DLL.UDM_SetSkyWritingMode(
    enable: 0,           // ВЫКЛЮЧЕНО
    mode: 0,
    uniformLen: 0.0f,
    accLen: 0.0f,
    angleLimit: 0.0f
);
```

**Причины**:
- Поддержки часто требуют точного контроля
- Обычно не критичны по времени

### Адаптивный выбор по скорости

```csharp
float speed = 1500; // mm/s

float uniformLen = speed switch
{
    <= 800 => 0.08f,
    <= 1250 => 0.12f,
    <= 2000 => 0.18f,
    _ => 0.25f
};

float accLen = uniformLen * 0.5f;

float angleLimit = speed switch
{
    <= 800 => 130.0f,
    <= 1250 => 110.0f,
    <= 2000 => 90.0f,
    _ => 80.0f
};

HM_UDM_DLL.UDM_SetSkyWritingMode(1, 0, uniformLen, accLen, angleLimit);
```

## Визуальное влияние параметров

### uniformLen (Длина равномерного участка)

```
uniformLen = 0.05 мм (маленькое):
    ╱╲    Более точное следование траектории
   ╱  ╲   Острые углы сохраняются
  ╱    ╲

uniformLen = 0.2 мм (большое):
    ╱─╲   Плавные переходы
   ╱   ╲  Углы скругляются
  ╱     ╲
```

### angleLimit (Предельный угол)

```
angleLimit = 150° (большой):
  Почти все углы сглаживаются
  │╲
  │ ╲ ← SkyWriting применяется
  │  ╲

angleLimit = 80° (маленький):
  Только плавные кривые сглаживаются
  │╲
  │ ╲ ← SkyWriting НЕ применяется (угол слишком острый)
  │  ╲
```

## Примеры использования

### Пример 1: Базовое использование

```csharp
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1);

// Установить SkyWriting с параметрами по умолчанию
HM_UDM_DLL.UDM_SetSkyWritingMode(
    enable: 1,
    mode: 0,
    uniformLen: 0.1f,
    accLen: 0.05f,
    angleLimit: 120.0f
);

// Настроить параметры слоя
MarkParameter[] layers = new MarkParameter[1];
layers[0] = new MarkParameter
{
    MarkSpeed = 800,
    LaserPower = 50.0f,
    JumpSpeed = 5000
};
HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

// Добавить геометрию
structUdmPos[] points = new structUdmPos[]
{
    new structUdmPos { x = 0, y = 0, z = 0 },
    new structUdmPos { x = 5, y = 0, z = 0 },
    new structUdmPos { x = 5, y = 5, z = 0 }
};
HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, 0);

// Генерация и сохранение
HM_UDM_DLL.UDM_Main();
HM_UDM_DLL.UDM_SaveToFile("output.bin");
HM_UDM_DLL.UDM_EndMain();
```

### Пример 2: Из CLI JSON

```csharp
string json = @"{
    ""edge_skywriting"": ""1"",
    ""edge_skywriting_uniformLen"": ""0.12"",
    ""edge_skywriting_accLen"": ""0.06"",
    ""edge_skywriting_angleLimit"": ""110.0""
}";

var parameters = JsonSerializer.Deserialize<CliParameters>(json);

HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1);

HM_UDM_DLL.UDM_SetSkyWritingMode(
    enable: int.Parse(parameters.EdgeSkywriting),
    mode: 0,
    uniformLen: float.Parse(parameters.EdgeSkywritingUniformLen),
    accLen: float.Parse(parameters.EdgeSkywritingAccLen),
    angleLimit: float.Parse(parameters.EdgeSkywritingAngleLimit)
);

// Остальная логика...
```

### Пример 3: Разные параметры для разных регионов

```csharp
// Файл 1: Edges с умеренным сглаживанием
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1);
HM_UDM_DLL.UDM_SetSkyWritingMode(1, 0, 0.1f, 0.05f, 120.0f);
// ... добавить геометрию edges
HM_UDM_DLL.UDM_SaveToFile("edges.bin");

// Файл 2: Infill с агрессивным сглаживанием
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1);
HM_UDM_DLL.UDM_SetSkyWritingMode(1, 0, 0.18f, 0.09f, 90.0f);
// ... добавить геометрию infill
HM_UDM_DLL.UDM_SaveToFile("infill.bin");

// Файл 3: Support без SkyWriting
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1);
HM_UDM_DLL.UDM_SetSkyWritingMode(0, 0, 0.0f, 0.0f, 0.0f);
// ... добавить геометрию support
HM_UDM_DLL.UDM_SaveToFile("support.bin");
```

## Различия между UDM_SkyWriting и UDM_SetSkyWritingMode

### `UDM_SkyWriting(int enable)`
- **Простая** версия: только включить/выключить
- Использует параметры по умолчанию из `system.ini`
- Подходит для базового использования

```csharp
HM_UDM_DLL.UDM_SkyWriting(1); // Использует defaults
```

### `UDM_SetSkyWritingMode(int enable, int mode, float uniformLen, float accLen, float angleLimit)`
- **Расширенная** версия: полный контроль параметров
- Позволяет точно настроить поведение SkyWriting
- Рекомендуется для продвинутого использования

```csharp
HM_UDM_DLL.UDM_SetSkyWritingMode(1, 0, 0.12f, 0.06f, 110.0f);
```

### Когда использовать какую?

**Используйте `UDM_SkyWriting`**:
- Для быстрого прототипирования
- Когда параметры по умолчанию подходят
- Когда CLI содержит только `"skywriting": "1"` без дополнительных параметров

**Используйте `UDM_SetSkyWritingMode`**:
- Когда CLI содержит расширенные параметры
- Когда нужен точный контроль
- Для адаптации параметров по скорости или типу региона
- При использовании параметров из конфигурации сканера

## Troubleshooting

### Проблема: SkyWriting не применяется

**Возможные причины**:
1. `enable = 0` → Проверьте, что `enable = 1`
2. `angleLimit` слишком маленький → Увеличьте до 120°
3. Вызов функции ПОСЛЕ `UDM_Main()` → Вызывайте ДО `UDM_Main()`

### Проблема: Геометрия слишком скруглена

**Решение**:
- Уменьшите `uniformLen` (например, с 0.2 до 0.1)
- Уменьшите `angleLimit` (например, с 150° до 110°)

### Проблема: Печать медленная, несмотря на SkyWriting

**Решение**:
- Увеличьте `uniformLen` (например, с 0.08 до 0.15)
- Увеличьте `angleLimit` (например, с 90° до 130°)

### Проблема: Разные регионы требуют разных параметров

**Решение**: Создавайте отдельные файлы для каждой группы параметров
```csharp
// НЕ РАБОТАЕТ - нельзя менять в одном файле!
HM_UDM_DLL.UDM_SetSkyWritingMode(1, 0, 0.1f, 0.05f, 120.0f);
// добавить edges
HM_UDM_DLL.UDM_SetSkyWritingMode(1, 0, 0.2f, 0.1f, 90.0f); // ❌

// РАБОТАЕТ - отдельные файлы
CreateFile("edges.bin", uniformLen: 0.1f);
CreateFile("infill.bin", uniformLen: 0.2f);
```

## Связанные файлы примеров

- [HansSkyWritingMode_CliExamples.cs](HansSkyWritingMode_CliExamples.cs) - 6 практических примеров
- [HansSkyWritingExample1_Basic.cs](HansSkyWritingExample1_Basic.cs) - Базовые примеры
- [HansSkyWritingExample2_Advanced.cs](HansSkyWritingExample2_Advanced.cs) - Продвинутые примеры
- [HansSkyWritingExample4_PerRegionSwitch.cs](HansSkyWritingExample4_PerRegionSwitch.cs) - Переключение между регионами
- [HansSkyWritingExample5_RealWorldUsage.cs](HansSkyWritingExample5_RealWorldUsage.cs) - Реальные сценарии

## Дополнительная информация

- Hans Scanner Documentation: [HM_HashuScan_Documentation_RU.md](HM_HashuScan_Documentation_RU.md)
- Scanner Configuration Guide: [SCANNER_CONFIG_GUIDE.md](SCANNER_CONFIG_GUIDE.md)
- Project Overview: [CLAUDE.md](CLAUDE.md)
