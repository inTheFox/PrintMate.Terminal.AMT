# Готово к тестированию - Параллельный запуск хостов

## ✅ Что исправлено

### 1. **Параллельный запуск хостов** (было последовательно)

**Было:**
```csharp
// Запускались последовательно - второй ждал завершения первого
for (int i = 0; i < _cardsConfiguration.Count; i++)
{
    await proxyClient.StartHostAndConnectAsync();
}
```

**Стало:**
```csharp
// Запускаются одновременно
var initTasks = new[]
{
    scanner0.StartHostAndConnectAsync(),
    scanner1.StartHostAndConnectAsync()
};
var results = await Task.WhenAll(initTasks);
```

**Результат:**
- ⚡ Оба хост-процесса запускаются одновременно
- ⚡ Общее время инициализации сократилось в 2 раза
- ⚡ Второй сканатор больше не ждёт первого

### 2. **Консоли хостов теперь видны** (было CreateNoWindow = true)

**Было:**
```csharp
CreateNoWindow = true  // Консоль скрыта
```

**Стало:**
```csharp
CreateNoWindow = false  // Консоль видна
```

**Результат:**
- 👁️ При запуске появятся **2 консольных окна** (по одному на каждый хост)
- 👁️ Можно видеть логи инициализации, ошибки, команды в реальном времени

### 3. **Увеличены задержки для race condition**

**ScanatorProxyClient.cs:**
- Задержка после запуска хоста: 1.5 секунды
- Задержка после ConnectAsync: 1 секунда
- **Итого: 2.5 секунды** на установку соединения

**HansPipeServer.cs:**
- Задержка после подключения клиента: 500ms
- Время для клиента создать StreamReader/Writer

### 4. **Подробное логирование**

Добавлены логи на каждом шаге:
```
✓ ConnectAsync completed
✓ Pipe IsConnected = true
Waiting 1 second for server to initialize...
Creating StreamReader...
✓ StreamReader created
Creating StreamWriter (with minimal params)...
✓ StreamWriter created
```

### 5. **Обработка ошибок при создании Stream**

Try-catch блоки с детальным выводом:
```csharp
catch (Exception ex)
{
    Console.WriteLine($"✗ StreamWriter creation failed: {ex.Message}");
    Console.WriteLine($"✗ Stack: {ex.StackTrace}");
    throw;
}
```

## 🚀 Как запустить тест

### Вариант 1: Через bat-файл

```bash
.\test_with_visible_consoles.bat
```

### Вариант 2: Напрямую

```bash
dotnet run --project PrintMate.Terminal\PrintMate.Terminal.csproj --configuration Debug
```

## 👀 Что ожидать при запуске

### Основная консоль (PrintMate.Terminal):

```
Starting Hans Scanner Host processes in parallel...
Waiting for 2 scanners to initialize...

Starting HansScannerHost: C:\...\HansScannerHost.exe
Arguments: scanner0 172.18.34.227 0
Host process started (PID: 12345)
Waiting for host initialization (1.5 seconds)...

Starting HansScannerHost: C:\...\HansScannerHost.exe
Arguments: scanner1 172.18.34.228 1
Host process started (PID: 12346)
Waiting for host initialization (1.5 seconds)...

Подключение к : scanner0...
✓ ConnectAsync completed
✓ Pipe IsConnected = true
Waiting 1 second for server to initialize...
Creating StreamReader...
✓ StreamReader created
Creating StreamWriter (with minimal params)...
✓ StreamWriter created
✓ Connected to HansScannerHost
Sending Ping command...
Ping result: True
✓ Connection established and verified!

Подключение к : scanner1...
✓ ConnectAsync completed
✓ Pipe IsConnected = true
Waiting 1 second for server to initialize...
Creating StreamReader...
✓ StreamReader created
Creating StreamWriter (with minimal params)...
✓ StreamWriter created
✓ Connected to HansScannerHost
Sending Ping command...
Ping result: True
✓ Connection established and verified!

✓ All 2 scanners initialized successfully!
```

### Консоль Хоста 1 (scanner0):

```
===========================================
Hans Scanner Host Process (with HWND)
===========================================
Pipe Name: scanner0
IP Address: 172.18.34.227
Board Index: 0
===========================================
Hidden form created for HWND
[scanner0] Initializing scanner with HWND: 123456
Hans SDK initialized successfully
[scanner0] Hans Pipe Server started
READY - waiting for client connections...

[scanner0] Waiting for client connection...
[scanner0] Client connected
[scanner0] Waiting for client to setup streams (500ms)...
[scanner0] Ready to handle commands
[scanner0] Received: {"RequestId":"...","Command":"Ping","Payload":null}
[scanner0] Sent: {"RequestId":"...","Success":true,"Message":"Pong",...}
```

### Консоль Хоста 2 (scanner1):

```
===========================================
Hans Scanner Host Process (with HWND)
===========================================
Pipe Name: scanner1
IP Address: 172.18.34.228
Board Index: 1
===========================================
Hidden form created for HWND
[scanner1] Initializing scanner with HWND: 789012
Hans SDK initialized successfully
[scanner1] Hans Pipe Server started
READY - waiting for client connections...

[scanner1] Waiting for client connection...
[scanner1] Client connected
[scanner1] Waiting for client to setup streams (500ms)...
[scanner1] Ready to handle commands
[scanner1] Received: {"RequestId":"...","Command":"Ping","Payload":null}
[scanner1] Sent: {"RequestId":"...","Success":true,"Message":"Pong",...}
```

## ❌ Возможные проблемы

### Проблема 1: Не видно консолей хостов

**Причина:** CreateNoWindow не изменился на false

**Решение:**
1. Откройте [ScanatorProxyClient.cs](PrintMate.Terminal/Hans/ScanatorProxyClient.cs:72)
2. Проверьте: `CreateNoWindow = false`
3. Пересоберите: `dotnet build --configuration Debug`

### Проблема 2: StreamWriter всё ещё не создаётся

**Симптом:**
```
Creating StreamWriter (with minimal params)...
[зависание здесь без ошибки]
```

**Диагностика:**
1. Посмотрите консоль хоста - видите ли вы "Ready to handle commands" **ДО** зависания клиента?
2. Если НЕТ - задержка на сервере недостаточна, увеличьте с 500ms до 1000ms
3. Если ДА - проблема в Windows Named Pipes, рассмотрите переход на TCP

**Временное решение:**
Увеличьте задержку в [HansPipeServer.cs:102](HansScannerHost/HansPipeServer.cs:102):
```csharp
await Task.Delay(1000, ct);  // Было 500
```

### Проблема 3: HansScannerHost.exe не найден

**Ошибка:**
```
HansScannerHost.exe not found at: C:\...\HansScannerHost.exe
```

**Решение:**
```bash
dotnet build HansScannerHost\HansScannerHost.csproj --configuration Debug
```

Проверьте, что файл существует:
```bash
dir PrintMate.Terminal\bin\Debug\net9.0-windows\HansScannerHost.exe
```

### Проблема 4: "Failed scanners: 0, 1"

**Причина:** Оба сканатора не смогли инициализироваться

**Решение:**
1. Проверьте логи в консолях хостов (если они открылись)
2. Проверьте, что конфигурация загружается:
   ```bash
   dir PrintMate.Terminal\bin\Debug\net9.0-windows\ScanAPI\ScanAPIConfig__03_07_2025__32_0001.json
   ```
3. Если файла нет - создайте его или измените путь в [MultiScanatorSystemProxy.cs:50-52](PrintMate.Terminal/Hans/MultiScanatorSystemProxy.cs:50-52)

## 📊 Производительность

**Было (последовательно):**
- Сканатор 0: 2.5 сек
- Сканатор 1: 2.5 сек
- **Итого: 5+ секунд**

**Стало (параллельно):**
- Оба сканатора: 2.5 сек одновременно
- **Итого: ~2.5 секунд**

**Ускорение: 2x** 🚀

## 📝 Следующие шаги

Если всё работает:
1. ✅ Оба хоста запустились
2. ✅ Оба подключились
3. ✅ Ping прошёл успешно

Можете переходить к тестированию реальной маркировки:
```csharp
await multiSystem.ConnectAllAsync();
await multiSystem.ConfigureAllAsync();
await multiSystem.StartLayerMarkingAsync(layer);
```

## 🐛 Если нужна помощь

Сохраните и отправьте:
1. Вывод основной консоли (PrintMate.Terminal)
2. Вывод консоли Хоста 1 (scanner0)
3. Вывод консоли Хоста 2 (scanner1)

Это поможет быстро диагностировать проблему.
