using LoggingService.Shared.Models;
using LoggingService.Data;
using LoggingService.Hubs;
using MongoDB.Driver;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.Json;
using LogLevel = LoggingService.Shared.Models.LogLevel;

namespace LoggingService.Services
{
    public class FileLogStorage : IHostedService
    {
        private readonly string _logDirectory;
        private readonly ConcurrentQueue<LogEntry> _logQueue = new();
        private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
        private Timer? _flushTimer;
        private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(5);
        private readonly MongoDbContext _dbContext;
        private readonly IHubContext<LogsHub> _hubContext;

        public FileLogStorage(IConfiguration configuration, MongoDbContext dbContext, IHubContext<LogsHub> hubContext)
        {
            _logDirectory = configuration.GetValue<string>("LogDirectory") ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(_logDirectory);
            _dbContext = dbContext;
            _hubContext = hubContext;
        }

        public async Task WriteLogAsync(LogEntry logEntry)
        {
            _logQueue.Enqueue(logEntry);

            // Если очередь большая, сбрасываем сразу
            if (_logQueue.Count >= 100)
            {
                await FlushLogsAsync();
            }
        }

        private async Task FlushLogsAsync()
        {
            if (_logQueue.IsEmpty) return;

            await _writeSemaphore.WaitAsync();
            try
            {
                var logsToWrite = new List<LogEntry>();
                while (_logQueue.TryDequeue(out var log))
                {
                    logsToWrite.Add(log);
                }

                if (logsToWrite.Count == 0) return;

                // Группируем по дате и приложению
                var logGroups = logsToWrite.GroupBy(l => new
                {
                    Date = l.Timestamp.Date,
                    Application = l.Application
                });

                foreach (var group in logGroups)
                {
                    var fileName = $"{group.Key.Application}_{group.Key.Date:yyyy-MM-dd-hh-mm-ss}.txt";
                    var filePath = Path.Combine(_logDirectory, fileName);

                    // Формируем текстовые строки логов
                    var logLines = new List<string>();
                    foreach (var log in group)
                    {
                        var levelStr = log.Level switch
                        {
                            LogLevel.Trace => "TRACE",
                            LogLevel.Debug => "DEBUG",
                            LogLevel.Information => "INFO ",
                            LogLevel.Warning => "WARN ",
                            LogLevel.Error => "ERROR",
                            LogLevel.Critical => "FATAL",
                            _ => "UNKNW"
                        };

                        var logLine = $"{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{levelStr}] [{log.Category}] {log.Message}"; 
                        var consoleLine = $"{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{levelStr}] [{log.Category}] [{log.Application}] {log.Message}";
                        Console.WriteLine(consoleLine);

                        if (log.Exception != null)
                        {
                            logLine += $"\n    Exception: {log.Exception}";
                        }

                        if (log.Properties != null && log.Properties.Count > 0)
                        {
                            var props = string.Join(", ", log.Properties.Select(p => $"{p.Key}={p.Value}"));
                            logLine += $"\n    Properties: {props}";
                        }

                        logLines.Add(logLine);
                    }

                    // Добавляем логи в файл построчно
                    await File.AppendAllLinesAsync(filePath, logLines);
                }

                // Записываем в MongoDB
                var documents = logsToWrite.Select(log => new LogEntryDocument
                {
                    SessionId = log.SessionId,
                    Timestamp = log.Timestamp.ToUniversalTime(), // MongoDB хранит время в UTC
                    Level = log.Level,
                    Application = log.Application,
                    Category = log.Category,
                    Message = log.Message,
                    Exception = log.Exception,
                    Properties = log.Properties
                }).ToList();

                if (documents.Count > 0)
                {
                    await _dbContext.Logs.InsertManyAsync(documents);
                }

                // Отправляем логи через SignalR всем подключенным клиентам
                foreach (var log in logsToWrite)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveLogEntry", log);
                }
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        public async Task<LogQueryResponse> QueryLogsAsync(LogQueryRequest request)
        {
            var startDate = (request.StartDate ?? DateTime.Now.AddDays(-7)).ToUniversalTime();
            var endDate = (request.EndDate ?? DateTime.Now).ToUniversalTime();

            // Используем MongoDB для быстрого поиска
            var filterBuilder = Builders<LogEntryDocument>.Filter;
            var filters = new List<FilterDefinition<LogEntryDocument>>();

            // Фильтр по дате
            filters.Add(filterBuilder.Gte(l => l.Timestamp, startDate));
            filters.Add(filterBuilder.Lte(l => l.Timestamp, endDate));

            // Фильтр по приложению
            if (!string.IsNullOrEmpty(request.Application))
            {
                filters.Add(filterBuilder.Eq(l => l.Application, request.Application));
            }

            // Фильтр по уровню
            if (request.MinLevel != null)
            {
                filters.Add(filterBuilder.Gte(l => l.Level, request.MinLevel.Value));
            }

            // Поиск по тексту
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                var textFilter = filterBuilder.Or(
                    filterBuilder.Regex(l => l.Message, new MongoDB.Bson.BsonRegularExpression(request.SearchText, "i")),
                    filterBuilder.Regex(l => l.Category, new MongoDB.Bson.BsonRegularExpression(request.SearchText, "i"))
                );
                filters.Add(textFilter);
            }

            // Объединяем все фильтры
            var combinedFilter = filterBuilder.And(filters);

            // Подсчет общего количества
            var totalCount = await _dbContext.Logs.CountDocumentsAsync(combinedFilter);

            // Получаем данные с пагинацией
            var documents = await _dbContext.Logs
                .Find(combinedFilter)
                .SortByDescending(l => l.Timestamp)
                .Skip(request.Skip)
                .Limit(request.Take)
                .ToListAsync();

            // Преобразуем в LogEntry
            var logs = documents.Select(doc => new LogEntry
            {
                SessionId = doc.SessionId,
                Timestamp = doc.Timestamp.ToLocalTime(), // Конвертируем обратно в локальное время
                Level = doc.Level,
                Application = doc.Application,
                Category = doc.Category,
                Message = doc.Message,
                Exception = doc.Exception,
                Properties = doc.Properties
            }).ToList();

            return new LogQueryResponse
            {
                TotalCount = (int)totalCount,
                Logs = logs
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _flushTimer = new Timer(async _ => await FlushLogsAsync(), null, _flushInterval, _flushInterval);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _flushTimer?.Dispose();
            await FlushLogsAsync();
        }
    }
}
