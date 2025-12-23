using LoggingService.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Observer.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace LoggingService.Client
{
    public class LoggingClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _applicationName;
        private readonly Queue<LogEntry> _logQueue = new();
        private readonly SemaphoreSlim _queueLock = new(1, 1);
        private readonly Timer? _flushTimer;
        private readonly int _batchSize;
        private HubConnection? _hubConnection;
        private readonly Guid _sessionId;

        /// <summary>
        /// Событие, возникающее при получении нового лога в real-time
        /// </summary>
        public event EventHandler<LogEntry>? LogReceived;

        /// <summary>
        /// ID текущей сессии клиента
        /// </summary>
        public Guid SessionId => _sessionId;

        public LoggingClient(string applicationName, int batchSize = 50)
        {
            string baseUrl = Services.LoggingService.Url;
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _applicationName = applicationName;
            _batchSize = batchSize;
            _sessionId = Guid.NewGuid();

            _flushTimer = new Timer(async _ => await FlushAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            Console.WriteLine($"LoggingClient initialized with SessionId: {_sessionId}");
        }

        public async Task LogAsync(LogLevel level, string category, string message, Exception? exception = null, Dictionary<string, object>? properties = null)
        {
            var logEntry = new LogEntry
            {
                SessionId = _sessionId,
                Timestamp = DateTime.Now,
                Level = level,
                Application = _applicationName,
                Category = category,
                Message = message,
                Exception = exception?.ToString(),
                Properties = properties
            };

            await _queueLock.WaitAsync();
            try
            {
                _logQueue.Enqueue(logEntry);

                if (_logQueue.Count >= _batchSize)
                {
                    await FlushInternalAsync();
                }
            }
            finally
            {
                _queueLock.Release();
            }
        }

        public Task LogTraceAsync(string category, string message, Dictionary<string, object>? properties = null)
            => LogAsync(LogLevel.Trace, category, message, null, properties);

        public Task LogDebugAsync(string category, string message, Dictionary<string, object>? properties = null)
            => LogAsync(LogLevel.Debug, category, message, null, properties);

        public Task LogInformationAsync(string category, string message, Dictionary<string, object>? properties = null)
            => LogAsync(LogLevel.Information, category, message, null, properties);

        public Task LogWarningAsync(string category, string message, Dictionary<string, object>? properties = null)
            => LogAsync(LogLevel.Warning, category, message, null, properties);

        public Task LogErrorAsync(string category, string message, Exception? exception = null, Dictionary<string, object>? properties = null)
            => LogAsync(LogLevel.Error, category, message, exception, properties);

        public Task LogCriticalAsync(string category, string message, Exception? exception = null, Dictionary<string, object>? properties = null)
            => LogAsync(LogLevel.Critical, category, message, exception, properties);

        public async Task FlushAsync()
        {
            await _queueLock.WaitAsync();
            try
            {
                await FlushInternalAsync();
            }
            finally
            {
                _queueLock.Release();
            }
        }

        private async Task FlushInternalAsync()
        {
            if (_logQueue.Count == 0) return;

            var logsToSend = new List<LogEntry>();
            while (_logQueue.Count > 0 && logsToSend.Count < _batchSize)
            {
                logsToSend.Add(_logQueue.Dequeue());
            }

            try
            {
                if (logsToSend.Count == 1)
                {
                    await _httpClient.PostAsJsonAsync("/api/logs", logsToSend[0]);
                }
                else
                {
                    await _httpClient.PostAsJsonAsync("/api/logs/batch", logsToSend);
                }
            }
            catch (Exception ex)
            {
                // Если не удалось отправить, возвращаем логи в очередь
                Console.WriteLine($"Failed to send logs to logging service: {ex.Message}");
                foreach (var log in logsToSend)
                {
                    _logQueue.Enqueue(log);
                }
            }
        }

        public async Task<LogQueryResponse> QueryLogsAsync(LogQueryRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/logs/query", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<LogQueryResponse>() ?? new LogQueryResponse();
            }
            catch
            {
                return new LogQueryResponse();
            }
        }

        /// <summary>
        /// Запускает SignalR подключение для получения логов в real-time
        /// </summary>
        public async Task StartRealTimeConnectionAsync()
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                // Уже подключено
                return;
            }

            try
            {
                string hubUrl = $"{Services.LoggingService.Url}/logsHub";

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<LogEntry>("ReceiveLogEntry", (logEntry) =>
                {
                    LogReceived?.Invoke(this, logEntry);
                });

                await _hubConnection.StartAsync();
                Console.WriteLine($"SignalR connected to LoggingService at {hubUrl}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR connection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Останавливает SignalR подключение
        /// </summary>
        public async Task StopRealTimeConnectionAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        public void Dispose()
        {
            _flushTimer?.Dispose();
            FlushAsync().Wait();
            StopRealTimeConnectionAsync().Wait();
            _httpClient.Dispose();
        }
    }
}
