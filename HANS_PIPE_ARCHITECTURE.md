# Hans Scanner Named Pipe Architecture

## Проблема

Hans SDK не поддерживает одновременную запись UDM файлов из разных потоков, что делает невозможным параллельную работу с двумя сканаторами в одном процессе.

## Решение

Использование **Named Pipes** для изоляции каждого сканатора в отдельном процессе `HansScannerHost.exe`, который взаимодействует с основным приложением через межпроцессное взаимодействие (IPC).

## Архитектура

```
┌─────────────────────────────────────────────────────────────────────┐
│                    PrintMate.Terminal (Main App)                    │
│                                                                       │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │            MultiScanatorSystemProxy                           │  │
│  │                                                                │  │
│  │  ┌──────────────────┐         ┌──────────────────┐           │  │
│  │  │ UdmBuilder #1    │         │ UdmBuilder #2    │           │  │
│  │  │ (227 scanner)    │         │ (228 scanner)    │           │  │
│  │  └────────┬─────────┘         └────────┬─────────┘           │  │
│  │           │                             │                      │  │
│  │           │ Generate                    │ Generate             │  │
│  │           │ UDM File                    │ UDM File             │  │
│  │           ▼                             ▼                      │  │
│  │  ┌──────────────────┐         ┌──────────────────┐           │  │
│  │  │ProxyClient #1    │         │ProxyClient #2    │           │  │
│  │  │                  │         │                  │           │  │
│  │  └────────┬─────────┘         └────────┬─────────┘           │  │
│  └───────────┼──────────────────────────────┼───────────────────┘  │
└──────────────┼──────────────────────────────┼──────────────────────┘
               │                               │
               │ Named Pipe                    │ Named Pipe
               │ hans_scanner_0                │ hans_scanner_1
               │                               │
       ┌───────▼────────────┐        ┌─────────▼────────────┐
       │ HansScannerHost.exe│        │ HansScannerHost.exe  │
       │   (Process #1)     │        │    (Process #2)      │
       │                    │        │                      │
       │  ┌──────────────┐  │        │   ┌──────────────┐  │
       │  │ PipeServer   │  │        │   │ PipeServer   │  │
       │  └──────┬───────┘  │        │   └──────┬───────┘  │
       │         │           │        │          │          │
       │  ┌──────▼───────┐  │        │   ┌──────▼───────┐  │
       │  │ HansWrapper  │  │        │   │ HansWrapper  │  │
       │  │              │  │        │   │              │  │
       │  │ ┌──────────┐ │  │        │   │ ┌──────────┐ │  │
       │  │ │ Hans SDK │ │  │        │   │ │ Hans SDK │ │  │
       │  │ │          │ │  │        │   │ │          │ │  │
       │  │ └─────┬────┘ │  │        │   │ └─────┬────┘ │  │
       │  └───────┼──────┘  │        │   └───────┼──────┘  │
       └──────────┼─────────┘        └───────────┼─────────┘
                  │                               │
                  │ USB/Ethernet                  │ USB/Ethernet
                  ▼                               ▼
       ┌──────────────────┐        ┌──────────────────┐
       │ Hans Controller  │        │ Hans Controller  │
       │  172.18.34.227   │        │  172.18.34.228   │
       └──────────────────┘        └──────────────────┘
```

## Компоненты

### 1. HansScannerHost.exe (Новый консольный процесс)

**Расположение:** `HansScannerHost/`

**Функции:**
- Named Pipe сервер для приёма команд
- Обёртка над Hans SDK
- Изолированная работа с одним сканатором

**Файлы:**
- `Program.cs` - точка входа, парсинг аргументов
- `HansPipeServer.cs` - Named Pipe сервер
- `HansScannerWrapper.cs` - обёртка над Hans SDK
- `PipeMessages.cs` - модели для сериализации

**Запуск:**
```bash
HansScannerHost.exe [pipeName] [ipAddress] [boardIndex]

# Пример:
HansScannerHost.exe hans_scanner_0 172.18.34.227 0
```

### 2. ScanatorProxyClient (Клиент в основном приложении)

**Расположение:** `PrintMate.Terminal/Hans/ScanatorProxyClient.cs`

**Функции:**
- Запуск HansScannerHost процесса
- Подключение к Named Pipe
- Отправка команд и получение ответов
- Управление жизненным циклом хост-процесса

**API:**
```csharp
var client = new ScanatorProxyClient("hans_scanner_0", "172.18.34.227", 0);
await client.StartHostAndConnectAsync();
await client.ConnectAsync();
await client.ConfigureAsync();
await client.DownloadMarkFileAsync("path/to/udm.bin");
await client.StartMarkAsync();
client.Dispose(); // Завершает хост-процесс
```

### 3. MultiScanatorSystemProxy (Менеджер сканаторов)

**Расположение:** `PrintMate.Terminal/Hans/MultiScanatorSystemProxy.cs`

**Функции:**
- Управление несколькими `ScanatorProxyClient`
- Генерация UDM файлов через `UdmBuilder`
- Координация параллельной маркировки

**Использование:**
```csharp
var multiSystem = new MultiScanatorSystemProxy(eventAggregator);
await multiSystem.InitializeAsync();        // Запускает все хост-процессы
await multiSystem.ConnectAllAsync();        // Подключается к контроллерам
await multiSystem.ConfigureAllAsync();      // Настраивает сканаторы
await multiSystem.StartLayerMarkingAsync(layer); // Маркировка слоя
```

### 4. PipeMessages (Общие модели)

**Расположение:**
- `PrintMate.Terminal/Hans/Models/PipeMessages.cs`
- `HansScannerHost/PipeMessages.cs` (копия)

**Модели:**
```csharp
HansRequest    // Запрос от клиента к хосту
HansResponse   // Ответ от хоста клиенту
ConnectParams  // Параметры подключения
DownloadMarkFileParams // Параметры загрузки UDM
ScanatorStatus // Статус сканатора
```

## Протокол коммуникации

### Поддерживаемые команды

| Команда | Описание | Payload |
|---------|----------|---------|
| `Ping` | Проверка связи | - |
| `Connect` | Подключение к контроллеру | `ConnectParams` |
| `Disconnect` | Отключение | - |
| `Configure` | Настройка сканатора (field size, offsets) | - |
| `DownloadMarkFile` | Загрузка UDM файла | `DownloadMarkFileParams` |
| `StartMark` | Начать маркировку | - |
| `StopMark` | Остановить маркировку | - |
| `GetStatus` | Получить статус | - |
| `Shutdown` | Завершить хост-процесс | - |

### Формат сообщений

**Request:**
```json
{
  "RequestId": "guid",
  "Command": "DownloadMarkFile",
  "Payload": "{\"UdmFilePath\":\"C:\\\\temp\\\\layer.bin\"}"
}
```

**Response:**
```json
{
  "RequestId": "guid",
  "Success": true,
  "Message": "Mark file downloaded",
  "Data": null,
  "ErrorDetails": null
}
```

Все сообщения передаются как **одна строка JSON** через `StreamWriter`/`StreamReader`.

## Workflow маркировки слоя

```
1. [Main App] MultiScanatorSystemProxy.StartLayerMarkingAsync(layer)
2. [Main App] UdmBuilder #1 генерирует UDM для LaserId 0,1
3. [Main App] UdmBuilder #2 генерирует UDM для LaserId 2+
4. [Main App] ProxyClient #1 отправляет DownloadMarkFile(udm1.bin)
5. [Host #1]  Получает команду, вызывает Hans SDK для загрузки
6. [Main App] ProxyClient #2 отправляет DownloadMarkFile(udm2.bin)
7. [Host #2]  Получает команду, вызывает Hans SDK для загрузки
8. [Main App] Ждёт завершения загрузки на обоих
9. [Main App] Параллельно отправляет StartMark на оба сканатора
10. [Host #1 & #2] Запускают маркировку
11. [Main App] Мониторит прогресс через GetStatus
```

## Преимущества подхода

✅ **Изоляция процессов** - каждый сканатор в своём процессе
✅ **Обход ограничений SDK** - нет конфликтов при записи UDM
✅ **Независимые сбои** - падение одного процесса не влияет на другой
✅ **Простая отладка** - консоль каждого хоста показывает логи
✅ **Параллельная работа** - реальная многозадачность через OS
✅ **Чистое разделение ответственности:**
  - Main App: генерация UDM, оркестрация
  - Host: только работа с Hans SDK

## Недостатки

❌ **Сложность** - дополнительный IPC слой
❌ **Накладные расходы** - сериализация, межпроцессное взаимодействие
❌ **Больше процессов** - 3 процесса вместо 1 (main + 2 hosts)
❌ **Отладка** - сложнее дебажить через процессы

## Альтернативные подходы (отклонённые)

### Семафоры/Mutex для синхронизации
```csharp
// Проблема: не решает конфликты внутри нативного SDK
private static readonly SemaphoreSlim _udmWriteLock = new(1, 1);
await _udmWriteLock.WaitAsync();
// SDK.DownloadMarkFile(...);
_udmWriteLock.Release();
```
❌ Не работает, т.к. Hans SDK имеет глобальное состояние

### AppDomain изоляция
❌ Не поддерживается в .NET Core/9

## Требования для запуска

- Windows (Named Pipes, P/Invoke)
- .NET 9.0 Runtime
- Hans.NET с нативными DLL:
  - `HM_HashuScan.dll`
  - `HM_UDM_DLL.dll`
  - `HM_Comm.dll`

## Build & Deploy

```bash
# Сборка HansScannerHost
dotnet build HansScannerHost/HansScannerHost.csproj --configuration Release

# HansScannerHost.exe должен быть в той же папке, что и PrintMate.Terminal.exe
# ScanatorProxyClient ищет его через AppDomain.CurrentDomain.BaseDirectory
```

## Примеры использования

См. [PrintMate.Terminal/Hans/Examples/PipeExample.cs](PrintMate.Terminal/Hans/Examples/PipeExample.cs)

## Отладка

### Показать консоли хост-процессов

В `ScanatorProxyClient.cs:47`:
```csharp
CreateNoWindow = false, // false для показа консоли
```

### Логи хост-процесса

Все команды логируются в консоль:
```
[hans_scanner_0] Received: {"Command":"Connect",...}
Connecting to 172.18.34.227, board 0...
Connect result: True
[hans_scanner_0] Sent: {"Success":true,...}
```

## Дальнейшие улучшения

- [ ] Добавить heartbeat (периодический ping)
- [ ] Реализовать переподключение при обрыве связи
- [ ] Добавить асинхронные события (прогресс маркировки)
- [ ] Логирование в файлы вместо консоли
- [ ] Graceful shutdown при закрытии основного приложения
- [ ] Unit-тесты для протокола

## Заключение

Named Pipe архитектура успешно решает проблему ограничений Hans SDK, обеспечивая надёжную параллельную работу с двумя сканаторами через процессную изоляцию.
