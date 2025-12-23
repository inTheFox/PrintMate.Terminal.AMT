using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Observer.Shared.Models;

namespace PrintMate.Terminal.Services
{
    /// <summary>
    /// HTTP-клиент для взаимодействия с Observer API
    /// </summary>
    public class ObserverApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public ObserverApiClient(string baseUrl = "http://localhost:21720")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Получить статусы всех сервисов
        /// </summary>
        public async Task<List<ServiceStatusDto>> GetServicesStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/services/getStatus");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ServiceStatusDto>>(json, _jsonOptions) ?? new List<ServiceStatusDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ObserverApiClient] Ошибка получения статусов: {ex.Message}");
                return new List<ServiceStatusDto>();
            }
        }

        /// <summary>
        /// Получить список сервисов
        /// </summary>
        public async Task<List<ServiceInfo>> GetServicesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/services/getServices");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ServiceInfo>>(json, _jsonOptions) ?? new List<ServiceInfo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ObserverApiClient] Ошибка получения сервисов: {ex.Message}");
                return new List<ServiceInfo>();
            }
        }

        /// <summary>
        /// Запустить сервис
        /// </summary>
        public async Task<bool> StartServiceAsync(string serviceId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/services/start/{serviceId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ObserverApiClient] Ошибка запуска сервиса {serviceId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Остановить сервис
        /// </summary>
        public async Task<bool> StopServiceAsync(string serviceId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/services/stop/{serviceId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ObserverApiClient] Ошибка остановки сервиса {serviceId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Перезапустить сервис
        /// </summary>
        public async Task<bool> RestartServiceAsync(string serviceId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/services/restart/{serviceId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ObserverApiClient] Ошибка перезапуска сервиса {serviceId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Проверить доступность Observer
        /// </summary>
        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/services/getStatus");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
