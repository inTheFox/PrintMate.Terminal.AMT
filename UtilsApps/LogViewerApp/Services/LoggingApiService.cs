using LoggingService.Shared.Models;
using System.Net.Http;
using System.Net.Http.Json;

namespace LogViewerApp.Services
{
    public class LoggingApiService
    {
        private readonly HttpClient _httpClient;

        public LoggingApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(Observer.Shared.Models.Services.LoggingService.Url)
            };
        }

        /// <summary>
        /// Загружает все логи с сервера
        /// </summary>
        public async Task<LogQueryResponse> QueryLogsAsync(LogQueryRequest? request = null)
        {
            try
            {
                request ??= new LogQueryRequest
                {
                    Skip = 0,
                    Take = 10000 // Загружаем последние 10000 логов
                };

                var response = await _httpClient.PostAsJsonAsync("/api/logs/query", request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<LogQueryResponse>();
                Console.WriteLine($"[API] Loaded {result?.Logs.Count ?? 0} logs from server");
                return result ?? new LogQueryResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to load logs: {ex.Message}");
                return new LogQueryResponse();
            }
        }
    }
}
