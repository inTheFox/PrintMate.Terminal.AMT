# Документация по библиотеке HM_HashuScan.dll

## Обзор

**HM_HashuScan.dll** - это библиотека для управления сканерными системами Hans (гальваносканерами) для лазерной маркировки и 3D-печати металлами. Библиотека предоставляет два основных API:

1. **API управления сканером** - прямое управление оборудованием в режиме реального времени
2. **UDM API** - программное создание файлов маркировки (Universal Data Model)

## Зависимые библиотеки

```
HM_HashuScan.dll         - Основная библиотека
HM_Comm.dll             - Коммуникационный модуль
Hashu4Java_64.dll       - JNA обертка для Java
Udm4Java_64.dll         - UDM обертка для Java
libwinpthread-1.dll     - Поддержка многопоточности
```

---

## Часть 1: API Управления Сканером

### 1.1. Инициализация и Подключение

#### `HM_InitBoard(IntPtr hWnd)`
Инициализирует систему управления сканером.

**Параметры:**
- `hWnd` - дескриптор окна для получения сообщений Windows (можно `IntPtr.Zero`)

**Возврат:** `0` - успешно, иначе код ошибки

**Сообщения:**
- `HM_MSG_DeviceStatusUpdate (5991)` - изменение статуса подключения

**Пример:**
```csharp
int result = HM_HashuScanDLL.HM_InitBoard(IntPtr.Zero);
if (result == 0) {
    Console.WriteLine("Система инициализирована");
}
```

---

#### `HM_ConnectByIpStr(string pIp)`
Подключение к карте сканера по IP адресу.

**Параметры:**
- `pIp` - IP адрес в формате "172.18.34.227"

**Возврат:** `0` - успешно

**Пример:**
```csharp
string scannerIP = "172.18.34.227";
int result = HM_HashuScanDLL.HM_ConnectByIpStr(scannerIP);
if (result == 0) {
    int cardIndex = HM_HashuScanDLL.HM_GetIndexByIpAddr(scannerIP);
    Console.WriteLine($"Подключено, индекс карты: {cardIndex}");
}
```

---

#### `HM_ConnectTo(int nIndex)`
Подключение к карте по индексу.

**Параметры:**
- `nIndex` - индекс карты (0, 1, 2...)

**Применение:** Если IP адреса известны заранее и добавлены в system.ini

---

#### `HM_DisconnectTo(int ipIndex)`
Отключение от карты сканера.

**Параметры:**
- `ipIndex` - индекс карты

**Применение:** Вызывать перед завершением приложения

---

#### `HM_GetIndexByIpAddr(string strIP)`
Получить индекс карты по IP адресу.

**Возврат:** Индекс карты или `-1` если не найдена

---

#### `HM_GetConnectStatus(int ipIndex)`
Проверка статуса подключения.

**Возврат:**
- `0` - `HM_DEV_Connect` - подключена
- `1` - `HM_DEV_Ready` - готова к работе
- `2` - `HM_DEV_NotAvailable` - недоступна

---

### 1.2. Загрузка и Выполнение Маркировки

#### `HM_DownloadMarkFile(int ipIndex, string filePath, IntPtr hWnd)`
Загружает файл маркировки (*.bin) в оперативную память (DDR) карты.

**Параметры:**
- `ipIndex` - индекс карты
- `filePath` - полный путь к `.bin` файлу
- `hWnd` - дескриптор окна для сообщений о прогрессе

**Сообщения:**
- `HM_MSG_StreamProgress (6011)` - прогресс загрузки
- `HM_MSG_StreamEnd (6012)` - загрузка завершена

**Пример:**
```csharp
string markFile = @"C:\MarkFiles\test.bin";
int result = HM_HashuScanDLL.HM_DownloadMarkFile(cardIndex, markFile, IntPtr.Zero);
```

---

#### `HM_DownloadMarkFileBuff(int ipIndex, IntPtr pUDMBuff, int nBytesCount, IntPtr hWnd)`
Загрузка из буфера памяти (без файла на диске).

**Применение:** Когда файл генерируется динамически через UDM API

---

#### `HM_BurnMarkFile(int ipIndex, bool enable)`
Записать файл маркировки во Flash память для автономной работы.

**Параметры:**
- `enable` - `true` = записать, `false` = стереть

**⚠ Внимание:** Операция необратима, износ Flash ограничен

---

#### `HM_StartMark(int ipIndex)`
Запустить выполнение маркировки.

**Предусловия:** Файл должен быть загружен, статус = Ready

**Пример:**
```csharp
HM_HashuScanDLL.HM_StartMark(cardIndex);
while (HM_HashuScanDLL.HM_GetWorkStatus(cardIndex) == 2) { // 2 = Running
    int progress = HM_HashuScanDLL.HM_ExecuteProgress(cardIndex);
    Console.Write($"\rПрогресс: {progress}%");
    Thread.Sleep(100);
}
```

---

#### `HM_StopMark(int ipIndex)`
Остановить маркировку (жесткая остановка).

---

#### `HM_PauseMark(int ipIndex)`
Приостановить маркировку с сохранением позиции.

---

#### `HM_ContinueMark(int ipIndex)`
Продолжить маркировку после паузы.

---

#### `HM_ExecuteProgress(int ipIndex)`
Получить прогресс выполнения.

**Возврат:** 0-100 (процент выполнения)

**Сообщения:** `HM_MSG_QueryExecProcess (6037)` - обновление прогресса

---

#### `HM_GetWorkStatus(int ipIndex)`
Получить рабочий статус сканера.

**Возврат:**
- `1` - Ready (готов)
- `2` - Running (выполняется маркировка)
- `3` - Alarm (авария)

---

### 1.3. Управление Координатами и Смещениями

#### `HM_SetOffset(int ipIndex, float offsetX, float offsetY, float offsetZ)`
Установить смещение поля сканирования.

**Параметры:**
- `offsetX, offsetY` - смещение по осям X, Y в миллиметрах
- `offsetZ` - смещение по оси Z (фокус) в миллиметрах
  - Положительное значение = фокус ниже
  - Отрицательное значение = фокус выше

**Применение:**
- Выравнивание нескольких лазеров
- Компенсация механического смещения
- Динамическая корректировка позиции детали

**Пример из конфигурации:**
```csharp
// Карта 0: offsetY = 105.03 мм
HM_HashuScanDLL.HM_SetOffset(cardIndex0, 0.0f, 105.03f, -0.001f);

// Карта 1: offsetY = -105.03 мм (лазеры на расстоянии 210 мм)
HM_HashuScanDLL.HM_SetOffset(cardIndex1, -2.636f, -105.03f, 0.102f);
```

---

#### `HM_SetRotates(int ipIndex, float angle, float centryX, float centryY)`
Повернуть поле сканирования.

**Параметры:**
- `angle` - угол поворота в градусах
- `centryX, centryY` - центр поворота в миллиметрах

**Применение:**
- Компенсация механического наклона сканера
- Поворот детали без физического перемещения

**Пример:**
```csharp
// Повернуть на 15° вокруг центра поля
HM_HashuScanDLL.HM_SetRotates(cardIndex, 15.0f, 0.0f, 0.0f);
```

---

#### `HM_ScannerJump(int ipIndex, float X, float Y, float Z)`
Переместить луч в указанную позицию без включения лазера.

**Применение:**
- Позиционирование для визуальной проверки
- Тестирование поля сканирования
- Калибровка

**Пример - проверка углов поля:**
```csharp
// Центр
HM_HashuScanDLL.HM_ScannerJump(cardIndex, 0, 0, 0);
Thread.Sleep(500);

// Правый верхний угол
HM_HashuScanDLL.HM_ScannerJump(cardIndex, 200, 200, 0);
Thread.Sleep(500);
```

---

#### `HM_SetCoordinate(int ipIndex, int coordinate)`
Установить тип системы координат.

**Параметры:**
- `coordinate` - код системы координат (0-7)

**Коды систем координат:**
```
0-7: Различные варианты ориентации осей и зеркалирования
```

---

### 1.4. Управление Направляющим Лазером

#### `HM_SetGuidLaser(int ipIndex, bool enable)`
Включить/выключить красный направляющий лазер.

**Применение:**
- Визуальное позиционирование
- Проверка поля сканирования
- Калибровка

**Пример - визуальная проверка:**
```csharp
// Включить красный лазер
HM_HashuScanDLL.HM_SetGuidLaser(cardIndex, true);

// Обвести контур детали
HM_HashuScanDLL.HM_ScannerJump(cardIndex, x1, y1, 0);
Thread.Sleep(500);
HM_HashuScanDLL.HM_ScannerJump(cardIndex, x2, y2, 0);
Thread.Sleep(500);

// Выключить
HM_HashuScanDLL.HM_SetGuidLaser(cardIndex, false);
```

---

### 1.5. Калибровочные Таблицы (Коррекция)

#### `HM_DownloadCorrection(int ipIndex, string filePath, IntPtr hWnd)`
Загрузить файл коррекции (*.crt) в DDR память.

**Применение:** Временная загрузка для тестирования

---

#### `HM_BurnCorrection(int ipIndex, string filePath, IntPtr hWnd)`
Записать файл коррекции во Flash память.

**⚠ Внимание:** Постоянная запись, операция необратима

---

#### `HM_SelectCorrection(int ipIndex, int crtIndex)`
Выбрать активную таблицу коррекции.

**Параметры:**
- `crtIndex` - индекс таблицы: `0` или `1`

**Применение:** Системы с двумя сканирующими головками

**Пример:**
```csharp
// Переключиться на вторую головку
HM_HashuScanDLL.HM_SelectCorrection(cardIndex, 1);
```

---

### 1.6. Цифровые Входы/Выходы

#### Входы

##### `HM_GetInput_GMC2(int ipIndex)`
##### `HM_GetInput_GMC4(int ipIndex)`
Получить состояние входов карты.

**Возврат:** Битовая маска (младший бит = IN0)

**Пример:**
```csharp
int inputs = HM_HashuScanDLL.HM_GetInput_GMC2(cardIndex);
for (int i = 0; i < 8; i++) {
    bool isHigh = (inputs & (1 << i)) != 0;
    Console.WriteLine($"IN{i}: {(isHigh ? "HIGH" : "LOW")}");
}
```

---

##### `HM_GetLaserInput(int ipIndex)`
Получить состояние аварийных входов от лазера.

**Возврат:** Битовая маска (Alarm1, Alarm2, Alarm3, Alarm4)

**Пример:**
```csharp
int alarms = HM_HashuScanDLL.HM_GetLaserInput(cardIndex);
if ((alarms & 0x0F) != 0) {
    Console.WriteLine("⚠ Обнаружена авария лазера!");
}
```

---

#### Выходы

##### `HM_SetOutputOn_GMC2(int ipIndex, int nOutIndex)`
##### `HM_SetOutputOff_GMC2(int ipIndex, int nOutIndex)`
Управление выходами карты GMC2.

**Параметры:**
- `nOutIndex` - номер выхода (0-7)

**Применение:**
- OUT0: Затвор лазера
- OUT1: Сигнальная лампа
- OUT2: Пневматический зажим
- OUT3: Продувка защитного стекла

**Пример - управление затвором:**
```csharp
// Открыть затвор
HM_HashuScanDLL.HM_SetOutputOn_GMC2(cardIndex, 0);
Thread.Sleep(100); // Время открытия

// Выполнить маркировку
HM_HashuScanDLL.HM_StartMark(cardIndex);

// Закрыть затвор
HM_HashuScanDLL.HM_SetOutputOff_GMC2(cardIndex, 0);
```

---

### 1.7. Аналоговые Выходы

#### `HM_SetAnalog(int ipIndex, float VoutA, float VoutB)`
Установить значения аналоговых выходов VOUTA и VOUTB.

**Параметры:**
- `VoutA, VoutB` - значение от 0.0 до 1.0
  - `0.0` = 0 В
  - `0.5` = 5 В
  - `1.0` = 10 В

**Применение:**
- Управление мощностью лазера (аналоговое)
- Управление скоростью вентилятора
- Регулировка пневматического давления

**Пример - плавное изменение мощности:**
```csharp
for (float v = 0.0f; v <= 1.0f; v += 0.1f) {
    HM_HashuScanDLL.HM_SetAnalog(cardIndex, v, 0.0f);
    Console.WriteLine($"VOUTA: {v * 10:F1} В");
    Thread.Sleep(200);
}
```

---

### 1.8. Мониторинг Гальванометров

#### `HM_GetFeedbackPosXY(int ipIndex, ref short fbX, ref short fbY)`
Получить фактическую позицию зеркал.

**Применение:** Проверка точности позиционирования

---

#### `HM_GetCmdPosXY(int ipIndex, ref short cmdX, ref short cmdY)`
Получить командную позицию.

---

#### `HM_GetXYGalvoStatus(int ipIndex, ref short xStatus, ref short yStatus)`
Получить статус моторов X и Y.

**Возврат:**
- `0` - норма
- `0x01` - ошибка позиции
- `0x02` - ошибка скорости
- `0x04` - перегрузка по току
- `0x08` - ошибка датчика

---

#### `HM_GetZGalvoStatus(int ipIndex, ref short zStatus)`
Получить статус мотора Z (фокус).

---

#### `HM_ClearCloseLoopAlarm(int ipIndex)`
Сбросить ошибку замкнутого контура.

**Пример - мониторинг и сброс ошибок:**
```csharp
short xStatus = 0, yStatus = 0;
HM_HashuScanDLL.HM_GetXYGalvoStatus(cardIndex, ref xStatus, ref yStatus);

if (xStatus != 0 || yStatus != 0) {
    Console.WriteLine("⚠ Обнаружена ошибка гальванометра!");
    HM_HashuScanDLL.HM_ClearCloseLoopAlarm(cardIndex);
    Console.WriteLine("✓ Ошибка сброшена");
}
```

---

### 1.9. Дополнительные Функции

#### `HM_SetMarkRegion(int ipIndex, int region)`
Установить активную область маркировки.

**Применение:** Разделение рабочего поля на зоны

---

#### `HM_GetMarkRegion(int ipIndex)`
Получить текущую область маркировки.

---

## Часть 2: UDM API (Создание Файлов Маркировки)

UDM (Universal Data Model) - API для программного создания файлов маркировки без использования графических редакторов.

### 2.1. Жизненный Цикл UDM Файла

```csharp
// 1. Создать новый файл
UDM_NewFile();

// 2. Установить протокол
UDM_SetProtocol(protocol: 0, dimensional: 0); // SPI, 2D

// 3. Настроить параметры слоев
MarkParameter[] layers = new MarkParameter[1];
layers[0] = MarkParameter.CreateDefault();
UDM_SetLayersPara(layers, 1);

// 4. Начать основной блок
UDM_Main();

// 5. Добавить геометрию
UDM_AddPolyline2D(points, count, layerIndex: 0);

// 6. Завершить основной блок
UDM_EndMain();

// 7. Сохранить файл
UDM_SaveToFile(@"C:\output.bin");
```

---

### 2.2. Основные Функции UDM

#### `UDM_NewFile()`
Создать новый пустой файл, очистить предыдущие данные.

---

#### `UDM_SetProtocol(int nProtocol, int nDimensional)`
Установить протокол связи и режим.

**Параметры:**
- `nProtocol`:
  - `0` = SPI протокол
  - `1` = XY2-100 протокол
  - `2` = SL2 протокол
- `nDimensional`:
  - `0` = 2D маркировка (Z постоянный)
  - `1` = 3D маркировка (Z переменный)

---

#### `UDM_SetLayersPara(MarkParameter[] layersParameter, int count)`
Установить параметры для слоев маркировки.

**Структура MarkParameter** (см. раздел 2.3)

**Пример - два слоя:**
```csharp
MarkParameter[] layers = new MarkParameter[2];

// Слой 0: Быстрый контур
layers[0] = MarkParameter.CreateDefault();
layers[0].MarkSpeed = 2000;  // 2 м/с
layers[0].LaserPower = 20.0f; // 20%

// Слой 1: Медленная гравировка
layers[1] = MarkParameter.CreateDefault();
layers[1].MarkSpeed = 500;    // 0.5 м/с
layers[1].LaserPower = 70.0f; // 70%
layers[1].MarkCount = 3;      // 3 прохода

UDM_SetLayersPara(layers, 2);
```

---

### 2.3. Структура MarkParameter

Полное описание всех параметров слоя:

| Параметр | Тип | Единица | Описание |
|----------|-----|---------|----------|
| `MarkSpeed` | UInt32 | мм/с | Скорость маркировки |
| `JumpSpeed` | UInt32 | мм/с | Скорость прыжков (без лазера) |
| `MarkDelay` | UInt32 | мкс | Задержка перед маркировкой |
| `JumpDelay` | UInt32 | мкс | Задержка после прыжка |
| `PolygonDelay` | UInt32 | мкс | Задержка на углах |
| `MarkCount` | UInt32 | - | Количество проходов |
| `LaserOnDelay` | float | мкс | Задержка включения лазера |
| `LaserOffDelay` | float | мкс | Задержка выключения лазера |
| `FPKDelay` | float | мкс | First Pulse Killer - задержка |
| `FPKLength` | float | мкс | FPK - длительность |
| `QDelay` | float | мкс | Q-модуляция - задержка |
| `DutyCycle` | float | 0.0-1.0 | Скважность при маркировке |
| `Frequency` | float | кГц | Частота импульсов |
| `StandbyFrequency` | float | кГц | Частота в режиме ожидания |
| `StandbyDutyCycle` | float | 0.0-1.0 | Скважность в ожидании |
| `LaserPower` | float | 0-100 | Мощность лазера (%) |
| `AnalogMode` | UInt32 | 0/1 | Аналоговое управление мощностью |
| `Waveform` | UInt32 | 0-63 | Номер формы волны (SPI) |
| `PulseWidthMode` | UInt32 | 0/1 | Режим управления длительностью |
| `PulseWidth` | UInt32 | нс | Длительность импульса (MOPA) |

---

### 2.4. Добавление Геометрии

#### `UDM_AddPolyline2D(structUdmPos[] nPos, int nCount, int layerIndex)`
Добавить 2D полилинию (ломаную).

**Применение:** Контуры, линии, текст

**Пример - квадрат:**
```csharp
structUdmPos[] square = new structUdmPos[] {
    new structUdmPos(-10, -10, 0),
    new structUdmPos( 10, -10, 0),
    new structUdmPos( 10,  10, 0),
    new structUdmPos(-10,  10, 0),
    new structUdmPos(-10, -10, 0)  // Замкнуть
};
UDM_AddPolyline2D(square, 5, layerIndex: 0);
```

---

#### `UDM_AddPolyline3D(structUdmPos[] nPos, int nCount, int layerIndex)`
Добавить 3D полилинию с изменением фокуса.

**Применение:** Маркировка на криволинейных поверхностях

**Пример - спираль:**
```csharp
structUdmPos[] spiral = new structUdmPos[100];
for (int i = 0; i < 100; i++) {
    double angle = i * 0.1;
    spiral[i].x = 10 * (float)Math.Cos(angle);
    spiral[i].y = 10 * (float)Math.Sin(angle);
    spiral[i].z = i * 0.1f;  // Подъем по спирали
}
UDM_AddPolyline3D(spiral, 100, layerIndex: 0);
```

---

#### `UDM_AddPoint2D(structUdmPos pos, float time, int layerIndex)`
Добавить точечную маркировку.

**Параметры:**
- `pos` - координаты точки
- `time` - время маркировки в миллисекундах

**Применение:** Точечная гравировка, растры

**Пример - растр 10x10:**
```csharp
for (int y = 0; y < 10; y++) {
    for (int x = 0; x < 10; x++) {
        structUdmPos dot = new structUdmPos(x * 2.0f, y * 2.0f, 0);
        UDM_AddPoint2D(dot, time: 5.0f, layerIndex: 0);
    }
}
```

---

### 2.5. Управляющие Команды

#### `UDM_Jump(float x, float y, float z)`
Прыжок в позицию без лазера.

**Применение:** Переход между удаленными областями

---

#### `UDM_Wait(float msTime)`
Пауза в миллисекундах.

**Применение:**
- Ожидание стабилизации
- Синхронизация с внешним оборудованием

---

#### `UDM_SetGuidLaser(bool enable)`
Включить/выключить направляющий лазер в файле.

---

#### `UDM_SetInput(UInt32 uInIndex)`
Ожидать сигнал на входе.

**Параметры:**
- `uInIndex` - номер входа (0-7)

**Применение:** Синхронизация с датчиками, кнопками

**Пример - ожидание кнопки "Старт":**
```csharp
UDM_SetInput(0); // Ждать сигнал на IN0
UDM_AddPolyline2D(geometry, count, 0); // Выполнить после сигнала
```

---

#### `UDM_SetOutPutOn(UInt32 nOutIndex)`
#### `UDM_SetOutPutOff(UInt32 nOutIndex)`
Управление одним выходом.

---

#### `UDM_SetOutPutAll(UInt32 uData)`
Установить все выходы одновременно.

**Пример:**
```csharp
// Включить OUT0 и OUT1, выключить OUT2 и OUT3
UDM_SetOutPutAll(0b0011); // = 3
```

---

#### `UDM_SetAnalogValue(float VoutA, float VoutB)`
Установить аналоговые выходы в файле.

---

### 2.6. Трансформации

#### `UDM_SetOffset(float offsetX, float offsetY, float offsetZ)`
Применить смещение ко всем последующим координатам.

**Пример - создать два одинаковых объекта:**
```csharp
// Первый объект слева
UDM_SetOffset(-20, 0, 0);
AddSquare();

// Второй объект справа
UDM_SetOffset(20, 0, 0);
AddSquare();
```

---

#### `UDM_SetRotate(float angle, float centryX, float centryY)`
Повернуть последующие координаты.

**Пример - создать 6 объектов по кругу:**
```csharp
for (int i = 0; i < 6; i++) {
    float angle = i * 60.0f;
    UDM_SetRotate(angle, 0, 0);
    AddShape();
}
```

---

### 2.7. Расширенные Функции

#### `UDM_SetSkyWritingMode(int enable, int mode, float uniformLen, float accLen, float angleLimit)`
Включить режим SkyWriting (непрерывная маркировка).

**Параметры:**
- `enable` - 0/1
- `mode` - режим работы
- `uniformLen` - длина участка равномерной скорости (мм)
- `accLen` - длина участка ускорения (мм)
- `angleLimit` - предельный угол для активации (градусы)

**Применение:** Улучшение качества на высоких скоростях и острых углах

**Пример:**
```csharp
UDM_SetSkyWritingMode(
    enable: 1,
    mode: 0,
    uniformLen: 0.5f,
    accLen: 0.2f,
    angleLimit: 120.0f
);
```

---

#### `UDM_SetJumpExtendLen(float jumpExtendLen)`
Установить длину продления прыжка.

**Применение:** Плавность перемещений

---

#### `UDM_RepeatStart(int repeatCount)`
#### `UDM_RepeatEnd(int startAddress)`
Создать цикл повторения.

**Пример:**
```csharp
int startAddr = UDM_RepeatStart(5); // Повторить 5 раз
// ... добавить геометрию ...
UDM_RepeatEnd(startAddr);
```

---

#### `UDM_FootTrigger(uint nDelayTime, int nTriggerType)`
Включить триггер от педали.

**Параметры:**
- `nDelayTime` - задержка после триггера (мс)
- `nTriggerType` - 0 = передний фронт, 1 = уровень

---

#### `UDM_SetCloseLoop(bool enable, int galvoType, int followErrorMax, int followErrorCount)`
Настроить замкнутый контур управления.

**Применение:** Высокоточное позиционирование

---

#### `UDM_Set3dCorrectionPara(float baseFocal, double[] paraK, int nCount)`
Установить параметры 3D коррекции кривизны поля.

**Параметры:**
- `baseFocal` - базовое фокусное расстояние
- `paraK` - массив коэффициентов полинома
- `nCount` - количество коэффициентов

**Применение:** Компенсация оптических искажений

---

### 2.8. Сохранение и Экспорт

#### `UDM_SaveToFile(string strFilePath)`
Сохранить файл на диск.

**Пример:**
```csharp
UDM_SaveToFile(@"C:\MarkFiles\output.bin");
```

---

#### `UDM_GetUDMBuffer(ref IntPtr pUdmBuffer, ref int nBytesCount)`
Получить буфер в памяти для прямой загрузки.

**Применение:** Загрузка без промежуточного файла

**Пример:**
```csharp
IntPtr buffer = IntPtr.Zero;
int size = 0;
UDM_GetUDMBuffer(ref buffer, ref size);

// Загрузить напрямую
HM_DownloadMarkFileBuff(cardIndex, buffer, size, IntPtr.Zero);
```

---

## Часть 3: Практические Рекомендации

### 3.1. Оптимизация Параметров

#### Скорость vs Качество

**Высокая скорость (контуры):**
```csharp
MarkSpeed = 2000;      // 2 м/с
LaserPower = 20-30%;
PolygonDelay = 100;    // Малая задержка
```

**Среднее качество (заливка):**
```csharp
MarkSpeed = 1000;      // 1 м/с
LaserPower = 40-50%;
PolygonDelay = 200;
```

**Высокое качество (гравировка):**
```csharp
MarkSpeed = 500;       // 0.5 м/с
LaserPower = 60-80%;
PolygonDelay = 400;
MarkCount = 2-3;       // Несколько проходов
```

---

### 3.2. Расчет Времени Выполнения

```csharp
// Время маркировки одного сегмента
double markTime = (length / markSpeed) * 1000; // мс

// Время прыжка
double jumpTime = (distance / jumpSpeed) * 1000; // мс

// Задержки
double delays = laserOnDelay + laserOffDelay + markDelay + polygonDelay;

// Общее время
double totalTime = segments * (markTime + delays) + (segments - 1) * (jumpTime + jumpDelay);
```

---

### 3.3. Расчет Плотности Энергии

```csharp
// Мощность лазера
double power = 50.0; // Вт

// Скорость
double speed = 1000.0; // мм/с

// Диаметр луча
double diameter = 0.065; // мм (65 мкм)

// Линейная плотность энергии
double linearEnergy = power / speed; // Дж/мм

// Флюенс (площадная плотность)
double fluence = linearEnergy / diameter * 1000; // Дж/мм²

Console.WriteLine($"Флюенс: {fluence:F2} Дж/мм²");

// Типичные значения для металлов:
// - Плавление: 0.5-5 Дж/мм²
// - Испарение: 5-50 Дж/мм²
```

---

### 3.4. Обработка Ошибок

```csharp
// Проверка результата каждой операции
int result = HM_StartMark(cardIndex);
if (result != 0) {
    Console.WriteLine($"Ошибка запуска: код {result}");
    return;
}

// Мониторинг статуса
int workStatus = HM_GetWorkStatus(cardIndex);
if (workStatus == 3) { // Alarm
    Console.WriteLine("Авария! Проверка причин...");

    // Проверить входы лазера
    int laserAlarms = HM_GetLaserInput(cardIndex);
    if (laserAlarms != 0) {
        Console.WriteLine("Авария лазера!");
    }

    // Проверить гальванометры
    short xStatus = 0, yStatus = 0;
    HM_GetXYGalvoStatus(cardIndex, ref xStatus, ref yStatus);
    if (xStatus != 0 || yStatus != 0) {
        Console.WriteLine("Ошибка позиционирования!");
        HM_ClearCloseLoopAlarm(cardIndex);
    }
}
```

---

### 3.5. Типичные Сценарии Использования

#### Сценарий 1: Автоматическая Линия

```csharp
// 1. Ожидание детали на позиции (датчик на IN0)
UDM_SetInput(0);

// 2. Зажать деталь (OUT0 = пневматика)
UDM_SetOutPutOn(0);
UDM_Wait(200); // Время зажатия

// 3. Выполнить маркировку
UDM_AddPolyline2D(geometry, count, 0);

// 4. Разжать деталь
UDM_SetOutPutOff(0);

// 5. Сигнал готовности (OUT1 = зеленая лампа)
UDM_SetOutPutOn(1);
UDM_Wait(1000);
UDM_SetOutPutOff(1);
```

---

#### Сценарий 2: Несколько Лазеров

```csharp
// Подключиться к обеим картам
string[] ips = { "172.18.34.227", "172.18.34.228" };
int[] cards = new int[2];

for (int i = 0; i < 2; i++) {
    HM_ConnectByIpStr(ips[i]);
    cards[i] = HM_GetIndexByIpAddr(ips[i]);

    // Применить смещения из конфигурации
    if (i == 0) {
        HM_SetOffset(cards[i], 0.0f, 105.03f, -0.001f);
    } else {
        HM_SetOffset(cards[i], -2.636f, -105.03f, 0.102f);
    }
}

// Загрузить разные файлы
HM_DownloadMarkFile(cards[0], "part1.bin", IntPtr.Zero);
HM_DownloadMarkFile(cards[1], "part2.bin", IntPtr.Zero);

// Запустить синхронно
HM_StartMark(cards[0]);
HM_StartMark(cards[1]);

// Ждать завершения обоих
while (HM_GetWorkStatus(cards[0]) == 2 || HM_GetWorkStatus(cards[1]) == 2) {
    Thread.Sleep(100);
}
```

---

## Часть 4: Диагностика и Отладка

### 4.1. Коды Ошибок

| Код | Описание | Решение |
|-----|----------|---------|
| 0 | Успешно | - |
| 1 | Общая ошибка | Проверить подключение |
| -1 | Устройство не найдено | Проверить IP адрес |
| -2 | Тайм-аут | Проверить сеть |
| -3 | Файл не найден | Проверить путь к файлу |

### 4.2. Проверка Сети

```csharp
// Пинг сканера
System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
var reply = ping.Send("172.18.34.227", 1000);
if (reply.Status == System.Net.NetworkInformation.IPStatus.Success) {
    Console.WriteLine($"Пинг: {reply.RoundtripTime} мс");
} else {
    Console.WriteLine("Сканер недоступен!");
}
```

### 4.3. Логирование

```csharp
// Логировать все операции
void LogOperation(string operation, int result) {
    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
    string status = result == 0 ? "OK" : $"ERROR {result}";
    Console.WriteLine($"[{timestamp}] {operation}: {status}");
}

// Использование
int result = HM_StartMark(cardIndex);
LogOperation("StartMark", result);
```

---

## Часть 5: Примеры Кода

Все примеры кода находятся в файлах:
- `HM_HashuScan_Examples.cs` - примеры управления сканером
- `HM_UDM_Examples.cs` - примеры создания файлов маркировки

---

## Заключение

Библиотека HM_HashuScan.dll предоставляет полный контроль над сканерными системами Hans:

✅ **API управления** - для работы с реальным оборудованием
✅ **UDM API** - для программного создания траекторий
✅ **Множественные лазеры** - поддержка нескольких карт
✅ **3D маркировка** - с динамической фокусировкой
✅ **I/O управление** - интеграция с автоматикой
✅ **Расширенные функции** - SkyWriting, циклы, коррекция

**Документация актуальна для версии библиотеки из сентября 2024 года.**
