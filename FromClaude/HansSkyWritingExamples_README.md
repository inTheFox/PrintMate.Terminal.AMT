# Hans Scanner SkyWriting Examples - README

Эта папка содержит примеры использования функции **SkyWriting** из Hans Scanner API при конвертации CLI файлов в формат Hans `.bin`.

## Что такое SkyWriting?

**SkyWriting** - это технология, при которой лазер остается включенным во время перехода между соседними сегментами векторов. Это уменьшает время обработки и может улучшить качество печати за счет более плавных переходов.

### Когда использовать SkyWriting:

✅ **ВКЛЮЧЕНО** (skywriting = 1):
- Edges (контуры деталей)
- Downskin borders и hatch
- Infill borders и hatch
- Upskin borders и hatch

❌ **ВЫКЛЮЧЕНО** (skywriting = 0):
- Support structures (поддержки)
- Особые случаи, требующие точного контроля

## Структура примеров

### 1. HansSkyWritingExample1_Basic.cs
**Базовые примеры использования SkyWriting**

- `Example_BasicSkyWriting()` - Простое включение/выключение SkyWriting
- `Example_SingleRegionWithSkyWriting()` - Конвертация одного региона с учетом SkyWriting

```csharp
// Пример использования
HansSkyWritingExample1_Basic.Example_BasicSkyWriting();
```

### 2. HansSkyWritingExample2_Advanced.cs
**Продвинутые возможности SkyWriting**

- `Example_AdvancedSkyWritingMode()` - Расширенный режим с параметрами uniformLen, accLen, angleLimit
- `Example_SkyWritingWithJumpExtend()` - Использование JumpExtendLen вместе с SkyWriting
- `Example_NoSkyWritingForSupports()` - Правильное отключение SkyWriting для поддержек

```csharp
// Пример использования
HansSkyWritingExample2_Advanced.Example_AdvancedSkyWritingMode();
```

### 3. HansSkyWritingExample3_FullCliConversion.cs
**Полная конвертация CLI с множественными регионами**

- `Example_CompleteCliConversion()` - Конвертация всех регионов из CLI файла
- Поддержка edges, downskin, infill, upskin, supports
- Правильная обработка параметров SkyWriting для каждого региона

```csharp
// Пример использования
HansSkyWritingExample3_FullCliConversion.Example_CompleteCliConversion();
```

### 4. HansSkyWritingExample4_PerRegionSwitch.cs
**Переключение SkyWriting между регионами**

⚠️ **ВАЖНО**: Hans API не поддерживает изменение SkyWriting внутри одного UDM файла!

- `Example_WrongApproach_DoNotUse()` - ❌ Неправильный подход (для обучения)
- `Example_CorrectApproach1_SeparateFiles()` - ✅ Отдельные файлы для каждого региона
- `Example_CorrectApproach2_GroupBySkyWriting()` - ✅ Группировка регионов по SkyWriting
- `Example_CorrectApproach3_MultipleLayersSameSkyWriting()` - ✅ Множество слоев с одинаковым SkyWriting

```csharp
// Правильный подход
HansSkyWritingExample4_PerRegionSwitch.Example_CorrectApproach2_GroupBySkyWriting();
```

### 5. HansSkyWritingExample5_RealWorldUsage.cs
**Реалистичный пример работы с CLI**

- `Example_RealWorldCliToHansConversion()` - Полный рабочий пример
- Парсинг JSON параметров из CLI файла
- Автоматическая группировка регионов по SkyWriting
- Создание оптимального количества выходных файлов

```csharp
// Реальный пример конвертации
HansSkyWritingExample5_RealWorldUsage.Example_RealWorldCliToHansConversion();
```

## Ключевые моменты работы с SkyWriting

### 1. Установка SkyWriting

```csharp
// Включить SkyWriting
HM_UDM_DLL.UDM_SkyWriting(1);

// Выключить SkyWriting
HM_UDM_DLL.UDM_SkyWriting(0);
```

### 2. Расширенный режим

```csharp
// Более точный контроль
HM_UDM_DLL.UDM_SetSkyWritingMode(
    enable: 1,              // Включить
    mode: 0,                // Режим (обычно 0)
    uniformLen: 0.1f,       // Длина равномерного участка (мм)
    accLen: 0.05f,          // Длина ускорения (мм)
    angleLimit: 120.0f      // Предел угла (градусы)
);
```

### 3. Ограничения Hans API

⚠️ **КРИТИЧЕСКИ ВАЖНО**:

1. **SkyWriting устанавливается один раз** для всего UDM файла после `UDM_NewFile()`
2. **Нельзя изменить** SkyWriting в середине файла
3. **Все слои в файле** используют одинаковое значение SkyWriting

### 4. Рекомендуемая стратегия

**Вариант 1: Два файла (рекомендуется)**
```
output_with_skywriting.bin    - edges, infill, upskin, downskin (SkyWriting=1)
output_without_skywriting.bin - supports (SkyWriting=0)
```

**Вариант 2: Отдельный файл для каждого региона**
```
layer_edges.bin          (SkyWriting=1)
layer_infill.bin         (SkyWriting=1)
layer_supports.bin       (SkyWriting=0)
...
```

## Пример работы с вашим CLI JSON

Из вашего CLI файла:

```json
{
  "base": {
    "edge_skywriting": "1",
    "downskin_border_skywriting": "1",
    "downskin_hatch_skywriting": "1",
    "infill_border_skywriting": "1",
    "infill_hatch_skywriting": "1",
    "upskin_border_skywriting": "1",
    "upskin_hatch_skywriting": "1",
    "support_border_skywriting": "0",
    "support_hatch_skywriting": "0"
  }
}
```

### Оптимальная конвертация:

**Файл 1**: `output_with_skywriting.bin` (SkyWriting=1)
- edges (80μm, 140W, 550mm/s)
- downskin_border (80μm, 100W, 800mm/s)
- downskin_hatch (80μm, 180W, 1600mm/s)
- infill_border (80μm, 140W, 550mm/s)
- infill_hatch (80μm, 260W, 900mm/s)
- upskin_border (80μm, 170W, 500mm/s)
- upskin_hatch (80μm, 210W, 800mm/s)

**Файл 2**: `output_without_skywriting.bin` (SkyWriting=0)
- support_border (80μm, 100W, 425mm/s)
- support_hatch (80μm, 260W, 900mm/s)

## Быстрый старт

```csharp
using Hans.NET;
using PrintMateMC.Examples;

// 1. Базовый пример
HansSkyWritingExample1_Basic.Example_SingleRegionWithSkyWriting();

// 2. Правильная группировка регионов
HansSkyWritingExample4_PerRegionSwitch.Example_CorrectApproach2_GroupBySkyWriting();

// 3. Реальная конвертация CLI
HansSkyWritingExample5_RealWorldUsage.Example_RealWorldCliToHansConversion();
```

## Диаграмма потока данных

```
CLI JSON Parameters
        ↓
Parse skywriting values
        ↓
Group regions by skywriting value
        ↓
┌──────────────────┬──────────────────┐
│   SkyWriting=1   │   SkyWriting=0   │
│   ────────────   │   ────────────   │
│   - edges        │   - supports     │
│   - infill       │                  │
│   - upskin       │                  │
│   - downskin     │                  │
└──────────────────┴──────────────────┘
        ↓                    ↓
file_with_sw.bin    file_without_sw.bin
```

## Связанные функции Hans API

- `UDM_SkyWriting(int enable)` - Включить/выключить SkyWriting
- `UDM_SetSkyWritingMode(...)` - Расширенный режим с параметрами
- `UDM_SetJumpExtendLen(float)` - Продление прыжков (полезно с SkyWriting)

## Дополнительная информация

См. также:
- [CLAUDE.md](./CLAUDE.md) - Полная документация проекта
- [SCANNER_CONFIG_GUIDE.md](./SCANNER_CONFIG_GUIDE.md) - Конфигурация сканера
- [Hans Scanner Documentation](./HM_HashuScan_Documentation_RU.md) - Русская документация Hans

## Troubleshooting

### Проблема: SkyWriting не работает

**Решение**: Проверьте, что:
1. Вызов `UDM_SkyWriting()` происходит ПОСЛЕ `UDM_NewFile()`
2. Вызов `UDM_SkyWriting()` происходит ДО `UDM_Main()`
3. Используется протокол с поддержкой 3D: `UDM_SetProtocol(0, 1)`

### Проблема: Разные регионы требуют разный SkyWriting

**Решение**: Создайте отдельные файлы:
```csharp
// Файл 1: с SkyWriting
CreateFile(regions_with_sw, skywriting: 1, "file1.bin");

// Файл 2: без SkyWriting
CreateFile(regions_without_sw, skywriting: 0, "file2.bin");
```

### Проблема: Низкое качество печати с SkyWriting

**Решение**: Настройте расширенный режим:
```csharp
UDM_SetSkyWritingMode(
    enable: 1,
    mode: 0,
    uniformLen: 0.15f,    // Увеличить для большей стабильности
    accLen: 0.08f,        // Увеличить для плавности
    angleLimit: 90.0f     // Уменьшить для строгих углов
);
```

## Лицензия и авторство

Примеры созданы для проекта PrintMateMC.
© 2025 AMTech
