# LoggingService

Централизованный сервис логирования для приложений PrintMate.

## Описание

LoggingService - это ASP.NET Core Web API сервис, который принимает логи от различных приложений и сохраняет их в файловую систему.

## Архитектура

### Проекты

1. **LoggingService** - основной Web API сервис
2. **LoggingService.Shared** - общие модели данных
3. **LoggingService.Client** - клиентская библиотека для отправки логов

### Хранение логов

Логи сохраняются в файлы JSON с именами в формате: `{ApplicationName}_{yyyy-MM-dd}.json`

Путь по умолчанию: `{BaseDirectory}/Logs/`

## API Endpoints

### POST /api/logs
Отправка одного лога

**Request Body:**
```json
{
  "timestamp": "2025-11-26T20:00:00",
  "level": 2,
  "application": "MyApp",
  "category": "Startup",
  "message": "Application started",
  "exception": null,
  "properties": {
    "version": "1.0.0"
  }
}
```

### POST /api/logs/batch
Отправка пакета логов

**Request Body:**
```json
[
  { /* LogEntry 1 */ },
  { /* LogEntry 2 */ }
]
```

### POST /api/logs/query
Запрос логов с фильтрацией

**Request Body:**
```json
{
  "application": "MyApp",
  "minLevel": 3,
  "startDate": "2025-11-26T00:00:00",
  "endDate": "2025-11-26T23:59:59",
  "searchText": "error",
  "skip": 0,
  "take": 100
}
```

**Response:**
```json
{
  "logs": [ /* массив LogEntry */ ],
  "totalCount": 150
}
```

## LogLevel

- 0: Trace
- 1: Debug
- 2: Information
- 3: Warning
- 4: Error
- 5: Critical

## Использование клиентской библиотеки

### Установка

Добавьте ссылку на проект `LoggingService.Client` в ваш проект.

### Пример использования

```csharp
using LoggingService.Client;

// Создание клиента
var logger = new LoggingClient("http://localhost:5300", "MyApp");

// Отправка логов
await logger.LogInformationAsync("Startup", "Application started");
await logger.LogWarningAsync("Database", "Connection slow");
await logger.LogErrorAsync("API", "Request failed", exception);

// Принудительная отправка всех логов из очереди
await logger.FlushAsync();

// Запрос логов
var request = new LogQueryRequest
{
    Application = "MyApp",
    MinLevel = LogLevel.Warning,
    StartDate = DateTime.Now.AddDays(-7),
    Take = 50
};
var response = await logger.QueryLogsAsync(request);
```

## Конфигурация

### appsettings.json

```json
{
  "LogDirectory": "C:\\Logs"
}
```

## Автозапуск через Observer

LoggingService автоматически запускается через Observer и доступен по адресу `http://localhost:5300`

## Swagger UI

В режиме разработки доступен Swagger UI по адресу:
`http://localhost:5300/swagger`
