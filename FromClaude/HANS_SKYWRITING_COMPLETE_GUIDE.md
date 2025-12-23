# Hans SkyWriting - Полное Руководство

## 📚 Оглавление

1. [Введение](#введение)
2. [Ключевые открытия](#ключевые-открытия)
3. [Файлы в этом решении](#файлы-в-этом-решении)
4. [Быстрый старт](#быстрый-старт)
5. [Детальная документация](#детальная-документация)
6. [Миграция с предыдущих подходов](#миграция-с-предыдущих-подходов)

---

## Введение

Это руководство содержит **финальное решение** для работы с Hans Scanner SkyWriting на основе **декомпилированного Hans4Java** кода из PrintMateMC.

### Что такое SkyWriting?

SkyWriting - это технология, при которой **лазер остается включенным** во время переходов между сегментами, что уменьшает время обработки и улучшает качество печати на плавных кривых.

### Источник решения

Все примеры основаны на декомпилированном файле:
```
org/iiv/hlssystem/multi/UdmProducer.class
```

Из Hans4Java JAR, используемого в PrintMateMC.

---

## Ключевые открытия

### ❗ КРИТИЧЕСКОЕ ОТКРЫТИЕ #1: Простой API

Hans4Java использует **ПРОСТУЮ** версию API:

```java
// В Hans4Java
lib.UDM_SkyWriting(boolean enable)
```

**НЕ** расширенную версию с 5 параметрами:

```csharp
// Это НЕ используется в Hans4Java!
UDM_SetSkyWritingMode(enable, mode, uniformLen, accLen, angleLimit)
```

### ❗ КРИТИЧЕСКОЕ ОТКРЫТИЕ #2: Обнуление задержек

Когда SkyWriting **ВКЛЮЧЕН**, `JumpDelay` и `PolygonDelay` устанавливаются в **НОЛЬ**:

```java
// Из UdmProducer.class
if (this.cardProfile.processVariables.isSWEnable) {
    this.currentMarkParameter.JumpDelay = 0;        // ← ОБНУЛИТЬ!
    this.currentMarkParameter.PolygonDelay = 0;     // ← ОБНУЛИТЬ!
    this.currentMarkParameter.MarkDelay = this.cardProfile.delaysSkyWritingConfig.markDelay;
    this.currentMarkParameter.LaserOnDelay = this.cardProfile.delaysSkyWritingConfig.laserOnDelay;
    this.currentMarkParameter.LaserOffDelay = this.cardProfile.delaysSkyWritingConfig.laserOffDelay;
}
```

### ❗ КРИТИЧЕСКОЕ ОТКРЫТИЕ #3: Два набора задержек

В конфигурации есть **два набора** задержек:

1. **Для SkyWriting** (когда включен):
   - `laserOnDelayForSkyWriting`
   - `laserOffDelayForSkyWriting`
   - `markDelayForSkyWriting`

2. **Для обычного режима** (когда выключен):
   - `laserOnDelay`
   - `laserOffDelay`
   - `markDelay`

### ❓ Вопрос: Где используется `umax`?

Параметр `umax` (uniformLen) **НЕ передается** в `UDM_SkyWriting()` в декомпилированном коде.

Возможные варианты:
- Устанавливается через другой API вызов
- Конфигурируется в native DLL по умолчанию
- Читается из `system.ini` файла

---

## Файлы в этом решении

### 🎯 Главные файлы (используйте эти!)

1. **[Hans_CSharp_Final_Solution.cs](Hans_CSharp_Final_Solution.cs)**
   - Финальное решение на основе декомпилированного кода
   - Метод `ApplySWEnableOperation_Hans4JavaWay()`
   - Класс `Hans4JavaFindings` с выводами

2. **[Hans_CSharp_Complete_Integration.cs](Hans_CSharp_Complete_Integration.cs)** ⭐ **НАЧНИТЕ С ЭТОГО!**
   - Полная интеграция CLI → Hans
   - Класс `CliToHansConverter`
   - Примеры с реальной конфигурацией

3. **[HansSkyWriting_ConfigAnalysis.md](HansSkyWriting_ConfigAnalysis.md)**
   - Анализ вашей конфигурации
   - Откуда берутся параметры
   - Формулы для расчета недостающих параметров

### 📖 Справочные файлы

4. **[HansSkyWritingMode_README.md](HansSkyWritingMode_README.md)**
   - Документация по `UDM_SetSkyWritingMode`
   - Описание всех параметров

5. **[HansSkyWriting_JavaUsage_Analysis.md](HansSkyWriting_JavaUsage_Analysis.md)**
   - Анализ Java кода PrintMateMC
   - Как Java использует SkyWriting

6. **[Hans_CSharp_HighLevel_API.cs](Hans_CSharp_HighLevel_API.cs)**
   - Высокоуровневая C# обертка (аналог Hans4Java)
   - Operations pattern

### 🗂️ Примеры (старые подходы)

7-11. **HansSkyWritingExample1_Basic.cs** через **Example5_RealWorldUsage.cs**
   - Базовые примеры (использовали неправильный подход)
   - **Не рекомендуется использовать** - смотрите файлы #1 и #2

12. **HansSkyWritingMode_CliExamples.cs**
   - Примеры с `UDM_SetSkyWritingMode` (5 параметров)
   - **Не используется в Hans4Java** - только для справки

---

## Быстрый старт

### Шаг 1: Подготовка конфигурации

Из вашего `scanner_config.json` извлеките параметры для нужной скорости:

```csharp
var speedConfig = new SpeedConfig
{
    MarkSpeed = 800,
    SWEnable = true,
    Umax = 0.1,
    // Задержки для SkyWriting
    LaserOnDelayForSkyWriting = 600.0,
    LaserOffDelayForSkyWriting = 730.0,
    MarkDelayForSkyWriting = 470,
    // Обычные задержки
    LaserOnDelay = 420.0,
    LaserOffDelay = 490.0,
    MarkDelay = 470,
    JumpDelay = 40000,
    PolygonDelay = 385
};
```

### Шаг 2: Применить SkyWriting точно как Hans4Java

```csharp
using PrintMateMC.HansFinal;

HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1);

// ✅ ПРАВИЛЬНЫЙ СПОСОБ (как в Hans4Java)
HansSkyWritingFinalSolution.ApplySWEnableOperation_Hans4JavaWay(
    enable: true,
    laserOnDelayForSkyWriting: 600.0f,
    laserOffDelayForSkyWriting: 730.0f,
    markDelayForSkyWriting: 470,
    laserOnDelayNormal: 420.0f,
    laserOffDelayNormal: 490.0f,
    markDelayNormal: 470,
    jumpDelayNormal: 40000,
    polygonDelayNormal: 385
);

// Добавить геометрию...
HM_UDM_DLL.UDM_Main();
HM_UDM_DLL.UDM_SaveToFile("output.bin");
HM_UDM_DLL.UDM_EndMain();
```

### Шаг 3: Конвертация CLI файла

```csharp
using PrintMateMC.HansFinal;

// Создать конфигурацию
LaserConfig config = new LaserConfig { /* ... */ };

// Создать регионы из CLI
List<CliRegion> regions = new List<CliRegion>
{
    new CliRegion
    {
        Name = "edges",
        SkyWritingEnabled = true,  // edge_skywriting = "1"
        MarkSpeed = 800,
        LaserPower = 140.0,
        BeamDiameter = 80.0,
        Geometry = /* ... */
    },
    // ... другие регионы
};

// Конвертировать
CliToHansConverter converter = new CliToHansConverter(config);
converter.ConvertFullCliFile(regions, "output_directory");
```

**Результат:**
- `regions_with_skywriting.bin` - регионы С SkyWriting
- `regions_without_skywriting.bin` - регионы БЕЗ SkyWriting

---

## Детальная документация

### Метод `ApplySWEnableOperation_Hans4JavaWay`

```csharp
public static void ApplySWEnableOperation_Hans4JavaWay(
    bool enable,                        // Включить/выключить SkyWriting
    float laserOnDelayForSkyWriting,   // Задержка ВКЛ для SkyWriting
    float laserOffDelayForSkyWriting,  // Задержка ВЫКЛ для SkyWriting
    int markDelayForSkyWriting,        // Задержка маркировки для SkyWriting
    float laserOnDelayNormal,          // Задержка ВКЛ обычная
    float laserOffDelayNormal,         // Задержка ВЫКЛ обычная
    int markDelayNormal,               // Задержка маркировки обычная
    int jumpDelayNormal,               // Задержка прыжка обычная
    int polygonDelayNormal)            // Задержка полигона обычная
```

**Что делает этот метод:**

1. Вызывает `UDM_SkyWriting(enable ? 1 : 0)`
2. Создает `MarkParameter` с правильными задержками:
   - Если `enable = true`:
     - `JumpDelay = 0`
     - `PolygonDelay = 0`
     - Использует задержки `*ForSkyWriting`
   - Если `enable = false`:
     - Использует обычные задержки
3. Применяет через `UDM_SetLayersPara`

### Класс `CliToHansConverter`

```csharp
public class CliToHansConverter
{
    private readonly LaserConfig laserConfig;

    public CliToHansConverter(LaserConfig config)
    {
        this.laserConfig = config;
    }

    // Конвертировать один регион
    public void ConvertRegion(CliRegion region, int layerIndex)

    // Конвертировать весь файл (создает отдельные .bin)
    public void ConvertFullCliFile(List<CliRegion> regions, string outputDirectory)
}
```

**Ключевые особенности:**

- Автоматически группирует регионы по SkyWriting состоянию
- Создает отдельные файлы для разных SkyWriting
- Рассчитывает Z-offset для управления диаметром луча
- Выбирает правильные параметры из конфигурации по скорости

### Расчет Z-offset для диаметра

```csharp
private float CalculateZOffset(double diameterMicrons)
{
    // CLI diameter (μm) → Hans Z offset (mm)
    return (float)((diameterMicrons - nominalDiameter) / 10.0 * zCoefficient);
}
```

**Параметры калибровки:**
- `nominalDiameter = 120.0` μm (диаметр при Z=0)
- `zCoefficient = 0.3` mm/10μm (коэффициент оптической системы)

**Примеры:**
- Для 80 μm: `Z = (80 - 120) / 10 × 0.3 = -1.2 mm`
- Для 140 μm: `Z = (140 - 120) / 10 × 0.3 = +0.6 mm`

---

## Миграция с предыдущих подходов

### ❌ Старый подход (неправильный)

```csharp
// НЕ используйте это!
HM_UDM_DLL.UDM_SetSkyWritingMode(
    enable: 1,
    mode: 0,
    uniformLen: 0.1f,
    accLen: 0.05f,
    angleLimit: 120.0f
);

// Параметры устанавливались БЕЗ обнуления задержек
MarkParameter[] layers = new MarkParameter[1];
layers[0].JumpDelay = 40000;  // ❌ НЕ обнулено!
layers[0].PolygonDelay = 385; // ❌ НЕ обнулено!
```

### ✅ Новый подход (правильный)

```csharp
// ✅ Используйте это!
HansSkyWritingFinalSolution.ApplySWEnableOperation_Hans4JavaWay(
    enable: true,
    laserOnDelayForSkyWriting: 600.0f,
    laserOffDelayForSkyWriting: 730.0f,
    markDelayForSkyWriting: 470,
    laserOnDelayNormal: 420.0f,
    laserOffDelayNormal: 490.0f,
    markDelayNormal: 470,
    jumpDelayNormal: 40000,
    polygonDelayNormal: 385
);

// Задержки автоматически установлены правильно:
// JumpDelay = 0 ✅
// PolygonDelay = 0 ✅
```

### Таблица различий

| Аспект | Старый подход | Новый подход (Hans4Java) |
|--------|--------------|--------------------------|
| API вызов | `UDM_SetSkyWritingMode` (5 параметров) | `UDM_SkyWriting` (1 параметр) |
| `JumpDelay` при SW ON | Не обнулялся ❌ | **0** ✅ |
| `PolygonDelay` при SW ON | Не обнулялся ❌ | **0** ✅ |
| Задержки для SW | Использовал обычные ❌ | Специальные `*ForSkyWriting` ✅ |
| Источник | Догадки/эксперименты | Декомпилированный Hans4Java ✅ |

---

## Примеры использования

### Пример 1: Одиночный слой с SkyWriting

```csharp
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1);

HansSkyWritingFinalSolution.ApplySWEnableOperation_Hans4JavaWay(
    enable: true,
    laserOnDelayForSkyWriting: 600.0f,
    laserOffDelayForSkyWriting: 730.0f,
    markDelayForSkyWriting: 470,
    laserOnDelayNormal: 420.0f,
    laserOffDelayNormal: 490.0f,
    markDelayNormal: 470,
    jumpDelayNormal: 40000,
    polygonDelayNormal: 385
);

structUdmPos[] points = new structUdmPos[]
{
    new structUdmPos { x = 0, y = 0, z = -1.2f },
    new structUdmPos { x = 10, y = 0, z = -1.2f }
};
HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, 0);

HM_UDM_DLL.UDM_Main();
HM_UDM_DLL.UDM_SaveToFile("single_layer.bin");
HM_UDM_DLL.UDM_EndMain();
```

### Пример 2: CLI регионы с разным SkyWriting

```csharp
List<CliRegion> regions = new List<CliRegion>
{
    new CliRegion
    {
        Name = "edges",
        SkyWritingEnabled = true,   // ← ON
        MarkSpeed = 800,
        LaserPower = 140.0,
        BeamDiameter = 80.0,
        Geometry = /* ... */
    },
    new CliRegion
    {
        Name = "supports",
        SkyWritingEnabled = false,  // ← OFF
        MarkSpeed = 800,
        LaserPower = 260.0,
        BeamDiameter = 120.0,
        Geometry = /* ... */
    }
};

CliToHansConverter converter = new CliToHansConverter(config);
converter.ConvertFullCliFile(regions, ".");
```

**Результат:**
- `regions_with_skywriting.bin` - содержит edges
- `regions_without_skywriting.bin` - содержит supports

### Пример 3: Переключение SkyWriting между файлами

```csharp
// Файл 1: С SkyWriting
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1);
HansSkyWritingFinalSolution.ApplySWEnableOperation_Hans4JavaWay(
    enable: true, /* ... */);
// Добавить геометрию...
HM_UDM_DLL.UDM_Main();
HM_UDM_DLL.UDM_SaveToFile("with_skywriting.bin");
HM_UDM_DLL.UDM_EndMain();

// Файл 2: БЕЗ SkyWriting
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1);
HansSkyWritingFinalSolution.ApplySWEnableOperation_Hans4JavaWay(
    enable: false, /* ... */);
// Добавить геометрию...
HM_UDM_DLL.UDM_Main();
HM_UDM_DLL.UDM_SaveToFile("without_skywriting.bin");
HM_UDM_DLL.UDM_EndMain();
```

---

## FAQ

### Q: Почему JumpDelay и PolygonDelay обнуляются?

**A:** При SkyWriting лазер **остается включенным** во время прыжков между сегментами. Задержки прыжка и полигона предназначены для обычного режима, где лазер выключается. При SkyWriting они не нужны и должны быть 0.

### Q: Нужно ли использовать `UDM_SetSkyWritingMode`?

**A:** Нет. Декомпилированный Hans4Java использует только `UDM_SkyWriting(boolean)`. Параметры `uniformLen`, `accLen`, `angleLimit` не передаются в UDM API напрямую.

### Q: Откуда берется `umax` из конфигурации?

**A:** `umax` (uniformLen) хранится в конфигурации, но **НЕ передается** в `UDM_SkyWriting()`. Возможно, он устанавливается через другой API или конфигурационный файл (`system.ini`).

### Q: Можно ли менять SkyWriting внутри одного файла?

**A:** Нет. UDM API не поддерживает изменение SkyWriting в середине файла. Нужно создавать **отдельные файлы** для регионов с разным SkyWriting.

### Q: Какие задержки использовать - обычные или `*ForSkyWriting`?

**A:** Зависит от состояния SkyWriting:
- **ON**: Используйте `laserOnDelayForSkyWriting`, `laserOffDelayForSkyWriting`, `markDelayForSkyWriting`
- **OFF**: Используйте `laserOnDelay`, `laserOffDelay`, `markDelay`

### Q: Зачем два набора задержек?

**A:** При SkyWriting лазер остается включенным дольше, поэтому требуются **большие задержки** для стабилизации. В обычном режиме лазер быстро включается/выключается, поэтому используются меньшие задержки.

### Q: Что делает `z` параметр в `structUdmPos`?

**A:** Z-offset управляет **дефокусировкой** луча, что изменяет диаметр пятна:
- Отрицательный Z → меньший диаметр (фокус выше детали)
- Положительный Z → больший диаметр (фокус ниже детали)

### Q: Нужно ли калибровать Z-offset?

**A:** Да. Коэффициенты `nominalDiameter` и `zCoefficient` зависят от вашей оптической системы. Проведите калибровку:
1. Создайте тестовый файл с разными Z значениями
2. Измерьте ширину линий под микроскопом
3. Рассчитайте коэффициенты

---

## Связанные файлы

### Обязательные для использования:
- ✅ [Hans_CSharp_Final_Solution.cs](Hans_CSharp_Final_Solution.cs)
- ✅ [Hans_CSharp_Complete_Integration.cs](Hans_CSharp_Complete_Integration.cs)
- ✅ [HansSkyWriting_ConfigAnalysis.md](HansSkyWriting_ConfigAnalysis.md)

### Справочные:
- 📖 [HansSkyWritingMode_README.md](HansSkyWritingMode_README.md)
- 📖 [HansSkyWriting_JavaUsage_Analysis.md](HansSkyWriting_JavaUsage_Analysis.md)
- 📖 [Hans_CSharp_HighLevel_API.cs](Hans_CSharp_HighLevel_API.cs)

### Устаревшие (не используйте):
- ❌ HansSkyWritingExample1-5_*.cs (старый подход)
- ❌ HansSkyWritingMode_CliExamples.cs (не используется в Hans4Java)

---

## Итоги

### ✅ Что нужно делать:

1. Использовать `UDM_SkyWriting(int enable)` - простую версию API
2. **ОБНУЛЯТЬ** `JumpDelay` и `PolygonDelay` когда SkyWriting ON
3. Использовать **два набора задержек**:
   - `*ForSkyWriting` когда ON
   - Обычные когда OFF
4. Создавать **отдельные файлы** для регионов с разным SkyWriting
5. Использовать метод `ApplySWEnableOperation_Hans4JavaWay()` из финального решения

### ❌ Что НЕ нужно делать:

1. НЕ использовать `UDM_SetSkyWritingMode` с 5 параметрами
2. НЕ пытаться менять SkyWriting внутри одного файла
3. НЕ забывать обнулять `JumpDelay` и `PolygonDelay`
4. НЕ использовать обычные задержки когда SkyWriting включен
5. НЕ использовать старые примеры (Example1-5)

---

## Заключение

Это руководство основано на **декомпилированном коде** Hans4Java из PrintMateMC и представляет собой **финальное решение** для работы с SkyWriting.

Все предыдущие подходы и догадки были исправлены на основе реального кода.

**Главный вывод:** Hans4Java использует простой API `UDM_SkyWriting(boolean)` и **обнуляет** `JumpDelay` и `PolygonDelay` при включении SkyWriting.

---

**Вопросы?** Смотрите исходный код в `Hans_CSharp_Final_Solution.cs` и `Hans_CSharp_Complete_Integration.cs`.

**Автор:** На основе декомпилированного Hans4Java (org.iiv.hlssystem.multi.UdmProducer.class)

**Дата:** 2025

**Версия:** 1.0 (Final)
