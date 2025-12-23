# Hans Dual Scanner System - Сводка реализации

## Что было реализовано

Полная система для управления двумя лазерными сканаторами Hans на основе GMC Control Card SDK с автоматическим разделением заданий из CLI файлов.

### 1. **UdmBuilder.cs** - Генератор UDM файлов
Конвертирует распарсенные CLI проекты в бинарные UDM файлы для Hans SDK.

**Ключевые возможности:**
- ✅ Автоматическая группировка регионов по параметрам (диаметр, мощность, скорость)
- ✅ Конвертация диаметра пучка в Z-offset через BeamConfig (формула Гауссова луча)
- ✅ Конвертация мощности из Watts → Percentage
- ✅ Поддержка Process Variables (скорость-зависимые параметры)
- ✅ Поддержка SkyWriting режима
- ✅ Генерация UDM для одного слоя или всего проекта

**API:**
```csharp
string BuildLayerUdm(Project project, int layerIndex, string outputPath)
List<string> BuildAllLayers(Project project, string outputDirectory)
```

### 2. **DualScannerLayerSplitter.cs** - Разделение заданий
Разделяет проект между двумя сканаторами используя различные стратегии.

**Реализованные стратегии:**
- ✅ **ByLaserId** (ОСНОВНАЯ) - использует `Region.LaserNum` из CLI файла
  - LaserNum 0, 1 → Scanner 1
  - LaserNum 2, 3 → Scanner 2
- ✅ Interleaved - чередование слоев
- ✅ SpatialLeftRight - разделение по координате X
- ✅ ByParts - разделение по деталям
- ✅ ByRegionType - контуры/заполнение

**API:**
```csharp
(Project Scanner1Project, Project Scanner2Project) SplitProject(Project originalProject)
```

### 3. **MultiScanatorJobBuilder.cs** - Координатор сканаторов
Управляет двумя сканаторами синхронно.

**Функции:**
- ✅ Подготовка задания (разделение + генерация UDM)
- ✅ Загрузка слоев в оба сканатора
- ✅ Параллельный запуск маркировки
- ✅ Ожидание завершения с таймаутом
- ✅ Мониторинг прогресса
- ✅ Проверка готовности сканаторов

**API:**
```csharp
DualScanatorJob PrepareJob(Project cliProject, string outputDirectory)
void LoadLayer(DualScanatorJob job, int layerIndex)
void StartMarkingBoth()
Task WaitForCompletionAsync(int timeoutMs = 300000)
```

### 4. **ScanatorSystem.cs** - Управление одним сканатором
Обертка над Hans SDK для одного сканатора.

**Функции:**
- ✅ Подключение/отключение
- ✅ Конфигурация параметров
- ✅ Загрузка UDM файлов
- ✅ Управление маркировкой (старт/стоп/пауза)
- ✅ Мониторинг прогресса
- ✅ Интеграция с UdmBuilder

### 5. **MultiScanatorSystem.cs** - Обработчик событий
Windows Forms для обработки сообщений от Hans SDK.

**Обрабатываемые события:**
- ✅ DeviceStatusUpdate - обновление статуса
- ✅ MarkOver - завершение маркировки
- ✅ StreamProgress - прогресс выполнения
- ✅ Публикация событий через Prism EventAggregator

### 6. **ScanatorConfigurationLoader.cs** - Конфигурация
Загрузка/сохранение конфигураций сканаторов из JSON.

**Возможности:**
- ✅ Загрузка из JSON файлов
- ✅ Создание конфигураций по умолчанию
- ✅ Сохранение в JSON
- ✅ Валидация конфигурации
- ✅ Создание dual-scanner setup

**Структура конфигурации:**
```csharp
ScanatorConfiguration
├── CardInfo (IP, индекс)
├── ScannerConfig (поле, протокол, смещения)
├── BeamConfig (диаметр, Rayleigh length, M²)
├── LaserPowerConfig (макс. мощность, коррекция)
└── ProcessVariablesMap (процесс-переменные)
```

### 7. **BeamConfig.cs** - Расчет диаметра пучка
Автоматический расчет Z-offset для заданного диаметра.

**Формула:**
```
Гауссов луч: d(z) = d₀ × √(1 + (z/z_R)²)
Обратная:    z = z_R × √((d/d₀)² - 1)

где:
  d₀ - минимальный диаметр (48.141 μm)
  z_R - длина Рэлея (1426.715 μm)
  d(z) - целевой диаметр (например, 80 μm)
```

**API:**
```csharp
float CalculateZOffset(double targetDiameterMicron)
double CalculateDiameter(float zOffsetMm)
```

### 8. **UsageExample.cs** - Примеры использования
Полные рабочие примеры:
- ✅ CompleteWorkflowExample() - полный workflow от CLI до печати
- ✅ SimpleExample() - простой пример одного слоя
- ✅ TestUdmBuilderExample() - тестирование генерации UDM
- ✅ TestLayerSplitterExample() - тестирование всех стратегий
- ✅ ConfigurationExample() - работа с конфигурацией

### 9. **README.md** - Документация
Полная документация с:
- ✅ Описание архитектуры
- ✅ API reference для всех компонентов
- ✅ Примеры кода
- ✅ Объяснение формул и алгоритмов
- ✅ Troubleshooting

## Полный workflow

```
1. Парсинг CLI
   ProjectManager.ParseFileAsync("file.cli") → Project

2. Разделение между сканаторами
   DualScannerLayerSplitter.SplitProject(project)
   → (Project Scanner1, Project Scanner2)

   Использует Region.LaserNum из CLI:
   - LaserNum 0, 1 → Scanner 1
   - LaserNum 2, 3 → Scanner 2

3. Генерация UDM файлов
   UdmBuilder.BuildAllLayers(project1) → UDM files для Scanner 1
   UdmBuilder.BuildAllLayers(project2) → UDM files для Scanner 2

   Каждый регион конвертируется:
   - Диаметр пучка → Z-offset (через BeamConfig)
   - Мощность Watts → Percentage
   - Параметры группируются для оптимизации

4. Подключение к сканаторам
   Scanner1.Connect() → GMC Card 172.18.34.227
   Scanner2.Connect() → GMC Card 172.18.34.228

5. Печать послойно
   Для каждого слоя:
   - LoadLayer(job, layerIndex) → загрузка UDM в оба сканатора
   - StartMarkingBoth() → старт параллельной маркировки
   - WaitForCompletionAsync() → ожидание завершения
   - Пауза для recoater (нанесение порошка)
```

## Интеграция с существующей системой

### Используемые парсеры
```
PrintMate.Terminal/Parsers/
├── CliParser/
│   └── CliProvider.cs          → парсит CLI файлы
├── CncParser/
│   └── CncProvider.cs          → парсит CNC файлы
├── Shared/Models/
│   ├── Project.cs              → структура проекта
│   ├── Layer.cs                → слой
│   ├── Region.cs               → регион с LaserNum ✓
│   └── RegionParameters.cs     → параметры (мощность, скорость, диаметр)
└── ProjectManager.cs           → фасад для парсинга
```

### Используемые модели
- ✅ **Region.LaserNum** - используется для разделения между сканаторами
- ✅ **RegionParameters** - конвертируется в MarkParameter
  - LaserBeamDiameter → Z-offset
  - LaserPower → LaserPower%
  - LaserSpeed → MarkSpeed
  - Skywriting → SkyWriting delays

### Интеграция с Hans.NET
```
Hans.NET/
├── HM_HashuScanDLL.cs     → управление сканаторами
│   ├── HM_InitBoard()
│   ├── HM_ConnectByIpStr()
│   ├── HM_StartMark()
│   └── ... (используется в ScanatorSystem)
│
└── HM_UDM_DLL.cs          → генерация UDM файлов
    ├── UDM_NewFile()
    ├── UDM_SetProtocol()
    ├── UDM_SetLayersPara()
    ├── UDM_AddPolyline3D()
    ├── UDM_Main()
    └── UDM_SaveToFile()  (используется в UdmBuilder)
```

## Основные технические решения

### 1. Диаметр пучка через Z-offset
Вместо несуществующей функции `UDM_SetDiameter()`:
- Используем 3D режим (`UDM_SetProtocol(0, 1)`)
- Конвертируем диаметр в Z через формулу Гауссова луча
- Каждая точка получает Z = f(диаметр)

### 2. Группировка регионов по параметрам
Регионы с одинаковыми параметрами объединяются в один слой MarkParameter:
- Уменьшает количество переключений параметров
- Оптимизирует производительность
- Реализовано через RegionParametersComparer

### 3. Разделение по LaserNum из CLI
CLI файл уже содержит информацию о назначении лазеров:
```csharp
Region { LaserNum = 0 } → Scanner 1  // Laser 1
Region { LaserNum = 1 } → Scanner 1  // Laser 2 (резерв)
Region { LaserNum = 2 } → Scanner 2  // Laser 3
Region { LaserNum = 3 } → Scanner 2  // Laser 4 (резерв)
```

### 4. Process Variables по скорости
Параметры (задержки, SkyWriting) зависят от скорости:
```csharp
MarkSpeed = 400  → delays = high, SW = off
MarkSpeed = 800  → delays = medium, SW = off
MarkSpeed = 1200 → delays = low, SW = on
```

## Файловая структура

```
PrintMate.Terminal/Hans/
├── UdmBuilder.cs                    ✓ Генератор UDM файлов
├── DualScannerLayerSplitter.cs      ✓ Разделение заданий
├── MultiScanatorJobBuilder.cs       ✓ Координатор сканаторов
├── ScanatorSystem.cs                ✓ Управление одним сканатором
├── MultiScanatorSystem.cs           ✓ Windows Forms обработчик событий
├── ScanatorConfigurationLoader.cs   ✓ Загрузка конфигураций
├── UsageExample.cs                  ✓ Примеры использования
├── README.md                        ✓ Документация
├── IMPLEMENTATION_SUMMARY.md        ✓ Этот файл
├── Models/
│   ├── ScanatorConfiguration.cs     ✓ Главная конфигурация
│   ├── BeamConfig.cs                ✓ Расчет диаметра
│   ├── ScannerConfig.cs             ✓ Параметры поля
│   ├── LaserPowerConfig.cs          ✓ Конфигурация мощности
│   ├── ProcessVariables.cs          ✓ Процесс-переменные
│   ├── ProcessVariablesMap.cs       ✓ Карта переменных
│   ├── CardInfo.cs                  ✓ IP адрес, индекс
│   ├── FunctionSwitcherConfig.cs    ✓ Переключатели функций
│   └── ThirdAxisConfig.cs           ✓ Коррекция поля
└── Events/
    ├── OnDeviceStatusUpdateEvent.cs ✓ Событие обновления статуса
    ├── OnNewDeviceDetectedEvent.cs  ✓ Событие обнаружения устройства
    └── OnDeviceMarkingOverEvent.cs  ✓ Событие завершения маркировки
```

## Зависимости

### Внутренние
- **Hans.NET** - P/Invoke обертки для HM_*.dll
- **PrintMate.Terminal.Parsers** - парсинг CLI/CNC файлов
- **Prism** - EventAggregator для событий
- **.NET 9.0** - целевой фреймворк

### Внешние (DLL)
- **HM_HashuScan.dll** - управление сканаторами
- **HM_Comm.dll** - коммуникация с картами
- **HM_UDM_DLL** (встроена в HM_HashuScan.dll) - генерация UDM

## Что НЕ реализовано (опционально)

Следующие функции из SDK не реализованы, т.к. не требуются для базового функционала:

- ❌ `UDM_SetSkyWritingMode()` - SkyWriting через параметры MarkParameter
- ❌ `UDM_SetJumpExtendLen()` - не критично
- ❌ `UDM_AddBreakAndCorPolyline3D()` - используется обычная UDM_AddPolyline3D
- ❌ Field curvature correction - используется ThirdAxisConfig если нужно

## Тестирование

Для тестирования используйте примеры из [UsageExample.cs](UsageExample.cs):

```csharp
// Тестирование UdmBuilder
await UsageExample.TestUdmBuilderExample();

// Тестирование всех стратегий разделения
await UsageExample.TestLayerSplitterExample();

// Полный workflow
await UsageExample.CompleteWorkflowExample(eventAggregator);
```

## Возможные улучшения

1. **Добавить поддержку >2 сканаторов** - обобщить MultiScanatorJobBuilder
2. **Оптимизация генерации UDM** - кэширование MarkParameter
3. **Мониторинг температуры** - добавить события для термоконтроля
4. **Автоматическая калибровка BeamConfig** - экспериментальное определение zCoefficient
5. **GUI для конфигурации** - визуальный редактор ScanatorConfiguration

## Заключение

Реализована полноценная система для управления двумя лазерными сканаторами Hans с поддержкой:
- ✅ Автоматического разделения заданий по LaserNum из CLI
- ✅ Точного контроля диаметра пучка через Z-offset
- ✅ Оптимизации параметров
- ✅ Параллельной работы сканаторов
- ✅ Гибкой конфигурации
- ✅ Полной документации

Система готова к интеграции в PrintMate.Terminal HMI.
