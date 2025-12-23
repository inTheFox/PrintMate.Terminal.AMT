# Как PrintMateMC использует SkyWriting на уровне Hans API (Java)

## Архитектура взаимодействия

```
┌──────────────────────────────────────────────────────────────────┐
│                    PrintMateMC (Java)                            │
│                                                                  │
│  JobBuilder (CLI/GCode Parser)                                  │
│     ↓                                                            │
│  JobParameter (LASER_PARAM.SKYWRITING = 0 или 1)               │
│     ↓                                                            │
│  LaserInfo.getOperations()                                      │
│     ↓                                                            │
│  JobParameter.getScanOperation()                                │
│     ↓                                                            │
│  new SWEnableOperation(boolean)  ← ВОТ КЛЮЧЕВОЙ МОМЕНТ!        │
│     ↓                                                            │
│  JobUtils.loadOPs() → ArrayList<IOperation>[]                  │
│     ↓                                                            │
│  CommandManager.setOPProducer()                                 │
│     ↓                                                            │
│  IHLSSystem (org.iiv.hlssystem)                                │
└──────────────┬───────────────────────────────────────────────────┘
               │
               ↓
┌──────────────────────────────────────────────────────────────────┐
│                Hans4Java JNI Bridge                              │
│  org.iiv.hlssystem.Operations.*                                 │
│   - DiameterOperation (FOCUS)                                   │
│   - PowerOperation (POWER)                                      │
│   - MarkSpeedOperation (SPEED)                                  │
│   - SWEnableOperation (SKYWRITING) ← ВКЛЮЧЕНИЕ SKYWRITING       │
│   - MarkOperation (печать точки)                                │
│   - JumpOperation (перемещение)                                 │
└──────────────┬───────────────────────────────────────────────────┘
               │
               ↓
┌──────────────────────────────────────────────────────────────────┐
│              Hans Native DLLs (C++)                              │
│  Hashu4Java_64.dll, HM_HashuScan.dll, HM_Comm.dll              │
└──────────────┬───────────────────────────────────────────────────┘
               │
               ↓
┌──────────────────────────────────────────────────────────────────┐
│              Hans Scanner Hardware                               │
└──────────────────────────────────────────────────────────────────┘
```

## Ключевые файлы и код

### 1. Парсинг SkyWriting из CLI/GCode

#### **JobBuilder.java** (строки 386-395)

```java
case 3:  // "_skywriting" ключ
    if (!node.has(regionKey+keys[i]))
        break;
    laser.addParameter(
        region,
        new JobParameter(
            LASER_PARAM.SKYWRITING,
            DATA_TYPE.ONE_INT,
            (int)node.get(regionKey+keys[i]).asInt(0)));  // Парсим из JSON
    break;
```

**CLI JSON пример:**
```json
{
  "edge_skywriting": "1",
  "infill_hatch_skywriting": "1",
  "support_hatch_skywriting": "0"
}
```

#### **GCodeSyntax.java** (строки 51, 183-185)

```java
public static String Laser_skywriting_TAG = "M706"; // M706 1 //skywriting is on

settingsTokens.add(new TokenASCII(
    LASER_PARAM.SKYWRITING,
    Pattern.compile(Laser_skywriting_TAG + "\\s+" + INTEGER_EXPRESSION + ".?")));
```

**G-Code пример:**
```gcode
M706 1  ; SkyWriting ON
M706 0  ; SkyWriting OFF
```

### 2. Хранение параметра SkyWriting

#### **LaserInfo.java** (строки 32, 130)

```java
// Dummy параметры по умолчанию
JobParameter sw = new JobParameter(LASER_PARAM.SKYWRITING, DATA_TYPE.ONE_INT, 0);

// Заполнение параметров региона
regionInfo.addParameter(new JobParameter(LASER_PARAM.SKYWRITING, DATA_TYPE.ONE_INT, 0));
```

Каждый регион (edges, infill, supports, etc.) имеет свой параметр SkyWriting.

### 3. **КЛЮЧЕВОЙ МОМЕНТ**: Преобразование в операцию Hans API

#### **JobParameter.java** (строки 158-174)

```java
public IOperation getScanOperation() {
    if (type==null || !(type instanceof LASER_PARAM))
        return null;

    switch ((LASER_PARAM)type) {
    case FOCUS:
        return new DiameterOperation((double) getFloatVal(0));
    case POWER:
        return new PowerOperation((double)getFloatVal(0));
    case SPEED:
        return new MarkSpeedOperation((int) getFloatVal(0));
    case SKYWRITING:
        // ← ВОТ ЭТО ГЛАВНОЕ!!!
        return new SWEnableOperation(getIntVal(0)==1?true:false);
    case WAIT_TIME:
        return new SleepOperation((double)getFloatVal(0));
    }
    return null;
}
```

**`SWEnableOperation`** - это Hans API класс для включения/выключения SkyWriting!

### 4. Генерация операций для Hans

#### **LaserInfo.java** (строки 101-105)

```java
public List<IOperation> getOperations(GEOMETRY_REGION region) {
    List<JobParameter> params = getParameters(region);
    if (params==null) return null;

    // Преобразуем параметры в операции
    return params.stream()
        .map(JobParameter::getScanOperation)  // ← Вызов getScanOperation()
        .collect(Collectors.toList());
}
```

**Результат для региона edges:**
```java
List<IOperation> operations = [
    new DiameterOperation(80.0),           // Focus: 80 микрон
    new PowerOperation(140.0),             // Power: 140 W
    new MarkSpeedOperation(550),           // Speed: 550 mm/s
    new SWEnableOperation(true)            // SkyWriting: ON
]
```

### 5. Вставка операций в поток геометрии

#### **JobUtils.java** (строки 318-330)

```java
for (GEOMETRY_REGION region : entry.getValue()[laserID]){
    if (region==null) continue;
    if ((path=part.getShapeForRegion(laserID, region))==null)
        continue;

    // Добавить конфигурационные операции перед геометрией
    ret[laserID].addAll(pInfo.getScannerOperations(laserID+1, region));

    // Теперь добавить геометрию (MarkOperation, JumpOperation)
    while (!it.isDone()) {
        segType = it.currentSegment(coords);
        switch (segType) {
            case PathIterator.SEG_LINETO:
                ret[laserID].add(new MarkOperation(coords[0]/1000.0, coords[1]/1000.0));
                break;
            case PathIterator.SEG_MOVETO:
                ret[laserID].add(new JumpOperation(coords[0]/1000.0, coords[1]/1000.0));
                break;
        }
    }
}
```

**Порядок операций для одного региона:**
```java
// 1. Конфигурация (для edges)
DiameterOperation(80.0)
PowerOperation(140.0)
MarkSpeedOperation(550)
SWEnableOperation(true)        // ← ВКЛЮЧЕНИЕ SKYWRITING

// 2. Геометрия edges
JumpOperation(0.0, 0.0)        // Переместиться к началу
MarkOperation(10.0, 0.0)       // Печатать линию
MarkOperation(10.0, 10.0)
MarkOperation(0.0, 10.0)
MarkOperation(0.0, 0.0)        // Замкнуть контур

// 3. Конфигурация (для infill)
DiameterOperation(80.0)
PowerOperation(260.0)
MarkSpeedOperation(900)
SWEnableOperation(true)        // ← SKYWRITING ВСЁ ЕЩЁ ON

// 4. Геометрия infill
JumpOperation(1.0, 1.0)
MarkOperation(9.0, 1.0)
...

// 5. Конфигурация (для supports)
DiameterOperation(80.0)
PowerOperation(260.0)
MarkSpeedOperation(900)
SWEnableOperation(false)       // ← ВЫКЛЮЧЕНИЕ SKYWRITING ДЛЯ SUPPORTS

// 6. Геометрия supports
JumpOperation(5.0, 5.0)
MarkOperation(6.0, 5.0)
...
```

### 6. Передача операций в Hans API

#### **CommandManager.java** (строки 384-432)

```java
private IHLSSystem getScannator() {
    return ScanSystemConnector.getScanner();
}

// Установка источника операций
public void setOPProducer(IOperationsProducer producer) {
    IHLSSystem scannator = getScannator();
    scannator.setOPProducer(producer);
}

// Загрузка следующего слоя
private boolean loadNextLayerTask() {
    IHLSSystem scannator = getScannator();
    if (scannator.loadNext() != IHLSSystem.SSystem_NO_ERROR) {
        return false;
    }
    return true;
}

// Печать слоя
private boolean runExposeTask() {
    IHLSSystem scannator = getScannator();
    int result = scannator.printNext();
    return result == IHLSSystem.SSystem_NO_ERROR;
}
```

**CommandManager реализует IOperationsProducer:**
```java
@Override
public List<IOperation>[] getOperations(int layerNum, int laserNum) {
    // Возвращает массив операций для каждого лазера
    return JobUtils.loadOPs(jobInfo, layerNum);
}
```

## Типы операций Hans API

| Java класс | Назначение | Параметры |
|-----------|-----------|-----------|
| `SWEnableOperation` | **Включить/выключить SkyWriting** | `boolean` (true/false) |
| `DiameterOperation` | Установить диаметр луча (focus) | `double` (микроны) |
| `PowerOperation` | Установить мощность лазера | `double` (Ватты) |
| `MarkSpeedOperation` | Установить скорость печати | `int` (мм/с) |
| `MarkOperation` | Печатать точку с лазером | `double x, double y` (мм) |
| `JumpOperation` | Переместиться без печати | `double x, double y` (мм) |
| `SleepOperation` | Задержка | `double` (мс) |

## Важные детали

### 1. SkyWriting применяется ПО РЕГИОНАМ

Каждый регион может иметь свой параметр SkyWriting:

```java
// Для edges
new SWEnableOperation(true)   // ON

// Для infill
new SWEnableOperation(true)   // ON

// Для supports
new SWEnableOperation(false)  // OFF
```

### 2. Операция SWEnableOperation вставляется ПЕРЕД геометрией

```java
// Сначала конфигурация
DiameterOperation(...)
PowerOperation(...)
MarkSpeedOperation(...)
SWEnableOperation(true)    // ← Включить SkyWriting

// Потом геометрия
MarkOperation(...)
MarkOperation(...)
```

### 3. Hans API обрабатывает операции ПОСЛЕДОВАТЕЛЬНО

Hans Scanner API читает операции по порядку:
1. Встречает `SWEnableOperation(true)` → включает SkyWriting
2. Обрабатывает `MarkOperation` с включенным SkyWriting
3. Встречает `SWEnableOperation(false)` → выключает SkyWriting
4. Обрабатывает следующие `MarkOperation` без SkyWriting

### 4. НЕТ прямых вызовов UDM_SkyWriting или UDM_SetSkyWritingMode

PrintMateMC **НЕ использует** низкоуровневые вызовы:
- ❌ `UDM_SkyWriting(int)`
- ❌ `UDM_SetSkyWritingMode(int, int, float, float, float)`

Вместо этого использует **высокоуровневую обертку**:
- ✅ `new SWEnableOperation(boolean)`

Hans4Java JNI bridge **внутри себя** вызывает нужные UDM функции.

## Аналог для C# (ваш код)

### Что делает PrintMateMC (Java):

```java
// Создать операцию SkyWriting
IOperation op = new SWEnableOperation(true);

// Добавить в список операций
operations.add(op);

// Отправить операции в Hans API
scannator.setOPProducer(operationsProducer);
scannator.loadNext();
scannator.printNext();
```

### Что нужно делать в вашем C# коде:

```csharp
// У вас НЕТ высокоуровневой обертки SWEnableOperation
// Поэтому используйте напрямую UDM API

// Вариант 1: Простой
HM_UDM_DLL.UDM_SkyWriting(1);  // Аналог new SWEnableOperation(true)

// Вариант 2: Расширенный (с параметрами из конфигурации)
HM_UDM_DLL.UDM_SetSkyWritingMode(
    enable: 1,
    mode: 0,
    uniformLen: 0.1f,
    accLen: 0.05f,
    angleLimit: 120.0f
);
```

## Важные различия Java vs C#

| Аспект | Java (PrintMateMC) | C# (ваш код) |
|--------|-------------------|--------------|
| Уровень API | Высокоуровневый (`SWEnableOperation`) | Низкоуровневый (`UDM_SkyWriting`) |
| Порядок операций | Операции в списке | Вызовы API перед геометрией |
| Переключение SkyWriting | Новая операция в потоке | **НЕЛЬЗЯ** - нужен новый файл |
| Параметры SkyWriting | Скрыты внутри Hans4Java | Явные параметры в API |

## Почему PrintMateMC может переключать SkyWriting, а вы нет?

**PrintMateMC использует другой workflow:**

1. Создает **список операций** в памяти
2. Hans API **читает операции по одной** через `IOperationsProducer.getOperations()`
3. Когда Hans API встречает `SWEnableOperation` в потоке → переключает SkyWriting

**Ваш C# код использует UDM workflow:**

1. Вызывает `UDM_SetSkyWritingMode()` **один раз** при создании файла
2. Добавляет **всю геометрию** через `UDM_AddPolyline3D()`
3. Генерирует файл `UDM_SaveToFile()`
4. ❌ **SkyWriting нельзя изменить** в середине файла

## Рекомендация для вашего C# кода

### Стратегия 1: Следуйте примерам из HansSkyWritingExample4

Создавайте **отдельные файлы** для регионов с разными SkyWriting:

```csharp
// Файл 1: edges, infill (SkyWriting ON)
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetSkyWritingMode(1, 0, 0.1f, 0.05f, 120.0f);
// добавить геометрию
HM_UDM_DLL.UDM_SaveToFile("with_skywriting.bin");

// Файл 2: supports (SkyWriting OFF)
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetSkyWritingMode(0, 0, 0.0f, 0.0f, 0.0f);
// добавить геометрию
HM_UDM_DLL.UDM_SaveToFile("without_skywriting.bin");
```

### Стратегия 2: Если нужен один файл

Используйте **один параметр SkyWriting** для всех регионов:

```csharp
// Определить общий параметр SkyWriting
bool useSkyWriting = true;  // Для большинства регионов

HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetSkyWritingMode(useSkyWriting ? 1 : 0, 0, 0.1f, 0.05f, 120.0f);

// Добавить ВСЕ регионы с одним SkyWriting
foreach (var region in allRegions) {
    HM_UDM_DLL.UDM_AddPolyline3D(region.Points, region.Count, layerIndex);
}

HM_UDM_DLL.UDM_SaveToFile("output.bin");
```

## Связанные файлы

- **Java реализация:**
  - [JobParameter.java:158-174](src/jobparser/JobParameter.java#L158-L174) - Создание SWEnableOperation
  - [LaserInfo.java:101-105](src/jobparser/LaserInfo.java#L101-L105) - Генерация операций
  - [JobUtils.java:318-330](src/jobparser/JobUtils.java#L318-L330) - Вставка операций
  - [CommandManager.java](src/commands/CommandManager.java) - Управление печатью
  - [ScanSystemConnector.java](src/connectors/ScanSystemConnector.java) - Подключение к Hans

- **C# примеры:**
  - [HansSkyWritingMode_CliExamples.cs](HansSkyWritingMode_CliExamples.cs) - 6 примеров использования
  - [HansSkyWritingMode_README.md](HansSkyWritingMode_README.md) - Документация параметров
  - [HansSkyWritingExample4_PerRegionSwitch.cs](HansSkyWritingExample4_PerRegionSwitch.cs) - Правильные подходы

## Итоговая схема потока данных PrintMateMC

```
CLI File                   JobBuilder.parseParameterSet()
  ↓                              ↓
{ "edge_skywriting": "1" }  → LASER_PARAM.SKYWRITING = 1
                                 ↓
                            LaserInfo.getOperations()
                                 ↓
                            JobParameter.getScanOperation()
                                 ↓
                            new SWEnableOperation(true)
                                 ↓
                            JobUtils.loadOPs() → ArrayList<IOperation>
                                 ↓
                            [
                              DiameterOperation(80.0),
                              PowerOperation(140.0),
                              MarkSpeedOperation(550),
                              SWEnableOperation(true),    ← SKYWRITING ON
                              MarkOperation(0, 0),
                              MarkOperation(10, 0),
                              ...
                              SWEnableOperation(false),   ← SKYWRITING OFF (для supports)
                              MarkOperation(...),
                              ...
                            ]
                                 ↓
                            CommandManager.setOPProducer()
                                 ↓
                            IHLSSystem.loadNext()
                                 ↓
                            IHLSSystem.printNext()
                                 ↓
                            Hans Scanner Hardware
```

---

**Главный вывод:** PrintMateMC использует `SWEnableOperation` как часть потока операций, что позволяет динамически переключать SkyWriting. В вашем C# коде с прямым UDM API это невозможно - нужно создавать отдельные файлы.
