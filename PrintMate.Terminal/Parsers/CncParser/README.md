# CNC Parser (G-code Parser)

## Описание

`CncProvider` — это парсер для CNC файлов в формате G-code, используемых в лазерной 3D-печати металлом. Парсер поддерживает как отдельные `.cnc` файлы, так и папки с несколькими CNC файлами (каждый файл может содержать один или несколько слоёв).

## Основные возможности

- ✅ Парсинг G-code команд (G0, G1, G90, G91)
- ✅ Поддержка M-кодов для управления лазером (M3, M5, M702, M704)
- ✅ Чтение конфигурации из комментариев
- ✅ Поддержка как отдельных файлов, так и директорий с CNC файлами
- ✅ Автоматическое определение слоёв по Z-координате
- ✅ Расчёт длины экспозиции и времени печати
- ✅ Единый интерфейс `IParserProvider` с CLI парсером

## Поддерживаемые G-коды

| Код | Описание |
|-----|----------|
| G0  | Быстрое перемещение (лазер выключен) |
| G1  | Линейное перемещение (лазер включён) |
| G90 | Абсолютное позиционирование |
| G91 | Относительное позиционирование |

## Поддерживаемые M-коды

| Код  | Описание |
|------|----------|
| M3   | Включить лазер |
| M5   | Выключить лазер |
| M702 | Установить мощность лазера (параметр P) |
| M704 | Установить скорость лазера (параметр S) |

## Формат CNC файла

### Базовая структура

```gcode
; MATERIAL: Ti6Al4V
; LAYER_HEIGHT: 0.05
; PROJECT_NAME: Test Project

; Начало слоя 0
G90                     ; Абсолютное позиционирование
G0 Z0.05                ; Перемещение на высоту 0.05 мм
M702 P200               ; Установить мощность 200 Вт
M704 S1000              ; Установить скорость 1000 мм/с

; REGION_TYPE: CONTOUR
M3                      ; Включить лазер
G1 X10.5 Y20.3          ; Рисовать линию до точки (10.5, 20.3)
G1 X15.2 Y25.8          ; Продолжить линию
M5                      ; Выключить лазер

; REGION_TYPE: INFILL
; HATCH_DISTANCE: 80
; HATCH_ANGLE: 67
M3
G1 X5.0 Y5.0
G1 X15.0 Y5.0
M5
```

### Конфигурация в комментариях

Парсер распознаёт следующие параметры в комментариях:

| Параметр | Описание | Пример |
|----------|----------|--------|
| `MATERIAL` | Название материала | `; MATERIAL: Ti6Al4V` |
| `LAYER_HEIGHT` | Толщина слоя (мм) | `; LAYER_HEIGHT: 0.05` |
| `PROJECT_NAME` | Название проекта | `; PROJECT_NAME: My Project` |
| `LAYER` | Номер слоя | `; LAYER: 5` |
| `REGION_TYPE` | Тип региона | `; REGION_TYPE: CONTOUR` |
| `BEAM_DIAMETER` | Диаметр луча (мкм) | `; BEAM_DIAMETER: 100` |
| `HATCH_DISTANCE` | Расстояние между штрихами (мкм) | `; HATCH_DISTANCE: 80` |
| `HATCH_ANGLE` | Угол штриховки (градусы) | `; HATCH_ANGLE: 67` |

### Типы регионов (REGION_TYPE)

- `INFILL` — заполнение
- `CONTOUR` — контур детали
- `SUPPORT` — контур поддержки
- `SUPPORT_FILL` — заполнение поддержки
- `UPSKIN` — верхняя поверхность
- `DOWNSKIN` — нижняя поверхность
- `CONTOUR_UPSKIN` — контур верхней поверхности
- `CONTOUR_DOWNSKIN` — контур нижней поверхности
- `EDGES` — края детали

## Использование

### Через ProjectManager (рекомендуется)

```csharp
// ProjectManager автоматически выберет нужный парсер
projectManager.Load("C:\\Projects\\MyProject.cnc");           // Один CNC файл
projectManager.Load("C:\\Projects\\MyCncProject");            // Папка с CNC файлами
```

### Напрямую через CncProvider

```csharp
var cncProvider = new CncProvider();

// Подписка на события
cncProvider.ParseStarted += (path) => Console.WriteLine($"Parsing: {path}");
cncProvider.ParseProgressChanged += (progress) => Console.WriteLine($"Progress: {progress}%");
cncProvider.ParseCompleted += (project) => Console.WriteLine($"Done: {project.ProjectInfo.Name}");
cncProvider.ParseError += (error) => Console.WriteLine($"Error: {error}");

// Парсинг
var project = await cncProvider.ParseAsync("C:\\path\\to\\file.cnc");

// Доступ к данным
Console.WriteLine($"Layers: {project.Layers.Count}");
Console.WriteLine($"Height: {project.ProjectInfo.ProjectHeight} mm");
Console.WriteLine($"Print time: {project.ProjectInfo.PrintTime}");
```

## Структура проекта

После парсинга создаётся объект `Project` со следующей структурой:

```
Project
├── ProjectInfo
│   ├── Name               // Название проекта
│   ├── ProjectHeight      // Общая высота (мм)
│   ├── LayerSliceHeight   // Толщина слоя (мм)
│   ├── MaterialName       // Название материала
│   ├── PrintTime          // Время печати (ЧЧ:ММ:СС)
│   └── ManifestPath       // Путь к файлу/папке
├── Configuration          // Параметры из комментариев
├── HeaderInfo             // Метаданные (детали, версия и т.д.)
└── Layers[]               // Массив слоёв
    └── Layer
        ├── Id             // Номер слоя (в микронах Z * 1000)
        └── Regions[]      // Массив регионов
            └── Region
                ├── GeometryRegion    // Тип (CONTOUR, INFILL и т.д.)
                ├── LaserNum          // Номер лазера (0)
                ├── Part              // Деталь
                ├── ExposeLength      // Длина экспозиции (мм)
                ├── Parameters        // Параметры лазера
                │   ├── LaserPower    // Мощность (Вт)
                │   ├── LaserSpeed    // Скорость (мм/с)
                │   ├── LaserBeamDiameter // Диаметр луча (мкм)
                │   ├── HatchDistance // Расстояние штриховки (мкм)
                │   └── Angle         // Угол штриховки (градусы)
                └── PolyLines[]       // Геометрия (полилинии)
                    └── PolyLine
                        └── Points[]  // Массив точек (X, Y)
```

## Особенности реализации

### Автоматическое определение слоёв

Слои создаются автоматически при изменении Z-координаты:

```gcode
G0 Z0.05    ; Создаётся слой с Id = 50 (0.05 мм * 1000)
G0 Z0.10    ; Создаётся слой с Id = 100 (0.10 мм * 1000)
```

### Группировка по регионам

Регионы автоматически группируются по типу и параметрам лазера. Новый регион создаётся при:
- Выключении лазера (M5)
- Изменении типа региона (REGION_TYPE в комментарии)
- Изменении параметров лазера (M702/M704)

### Поддержка папок с CNC файлами

Если передана папка, парсер:
1. Находит все `.cnc` файлы
2. Сортирует их по имени
3. Парсит каждый файл
4. Объединяет все слои в один проект

Это позволяет работать с проектами, где каждый слой в отдельном файле:
```
MyProject/
├── layer_0000.cnc
├── layer_0001.cnc
├── layer_0002.cnc
└── ...
```

## События

| Событие | Параметр | Описание |
|---------|----------|----------|
| `ParseStarted` | `string path` | Начало парсинга |
| `ParseProgressChanged` | `double progress` | Прогресс (0-100%) |
| `ParseCompleted` | `Project project` | Парсинг завершён |
| `ParseError` | `string error` | Ошибка парсинга |

## Совместимость с CLI парсером

Оба парсера (CNC и CLI) реализуют интерфейс `IParserProvider`, что позволяет:

- Использовать единый код для работы с проектами
- Легко переключаться между форматами
- Добавлять новые форматы без изменения существующего кода

```csharp
IParserProvider parser;

if (path.EndsWith(".cli"))
    parser = new CliProvider();
else
    parser = new CncProvider();

var project = await parser.ParseAsync(path);
```

## Ограничения

- Поддерживаются только линейные перемещения (G0, G1)
- Не поддерживаются дуги (G2, G3)
- Координаты должны быть в абсолютном режиме (G90) или относительном (G91)
- Все размеры должны быть в миллиметрах

## Примеры файлов

См. примеры CNC файлов в папке `Examples/CNC/` (если есть).
