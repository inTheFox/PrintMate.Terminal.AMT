using Observer.Shared.Models;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Reflection;

namespace Observer.Services
{
    public class ServiceWorker : IHostedService
    {
        private List<ServiceInfo> _servicesInfo;
        private List<ServiceInstance> _servicesInstances;
        private HashSet<string> _disabledServices; // Сервисы отключенные вручную
        private CancellationTokenSource _observerCts;
        private const int ObserverIntervalMs = 5000; // Проверка каждые 5 секунд

        public ServiceWorker()
        {
            _servicesInstances = new List<ServiceInstance>();
            _disabledServices = new HashSet<string>();
            _observerCts = new CancellationTokenSource();

            // Загружаем все сервисы через рефлексию
            _servicesInfo = LoadServicesFromReflection();

            // Найти уже запущенные процессы
            DiscoverRunningServices();
        }

        private List<ServiceInfo> LoadServicesFromReflection()
        {
            var services = new List<ServiceInfo>();

            // Получаем все статические поля типа ServiceInfo из класса Services
            var fields = typeof(Shared.Models.Services)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(ServiceInfo));

            foreach (var field in fields)
            {
                var serviceInfo = field.GetValue(null) as ServiceInfo;
                if (serviceInfo != null)
                {
                    services.Add(serviceInfo);
                    Console.WriteLine($"Загружен сервис: {serviceInfo.Id} ({field.Name})");
                }
            }

            return services;
        }

        private void DiscoverRunningServices()
        {
            foreach (var serviceInfo in _servicesInfo)
            {
                var process = FindProcessByArguments(serviceInfo);
                if (process != null)
                {
                    _servicesInstances.Add(new ServiceInstance
                    {
                        ServiceInfo = serviceInfo,
                        Process = process
                    });
                    Console.WriteLine($"Обнаружен уже запущенный сервис {serviceInfo.Id} (PID: {process.Id})");
                }
            }
        }

        private Process? FindProcessByArguments(ServiceInfo serviceInfo)
        {
            try
            {
                var processName = Path.GetFileNameWithoutExtension(serviceInfo.Path);
                var processes = Process.GetProcessesByName(processName);

                foreach (var process in processes)
                {
                    var commandLine = GetCommandLine(process.Id);
                    if (commandLine != null &&
                        commandLine.Contains(serviceInfo.Url) &&
                        commandLine.Contains(serviceInfo.StartupArguments))
                    {
                        return process;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка поиска процесса: {e.Message}");
            }
            return null;
        }

        private string? GetCommandLine(int processId)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
                foreach (var obj in searcher.Get())
                {
                    return obj["CommandLine"]?.ToString();
                }
            }
            catch { }
            return null;
        }

        public bool IsServiceRunning(string serviceId)
        {
            var instance = _servicesInstances.FirstOrDefault(p => p.ServiceInfo.Id == serviceId);
            if (instance == null)
                return false;

            try
            {
                // Проверяем, жив ли процесс
                return !instance.Process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        public void StartService(ServiceInfo serviceInfo, bool enableAutoRestart = true)
        {
            // При ручном запуске снимаем флаг отключённого сервиса
            if (enableAutoRestart)
            {
                _disabledServices.Remove(serviceInfo.Id);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = serviceInfo.Path,
                    Arguments = $"{serviceInfo.Url} {serviceInfo.StartupArguments}",
                    UseShellExecute = true,
                    CreateNoWindow = false, // Показываем консоль для отладки
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    WorkingDirectory = Path.GetDirectoryName(serviceInfo.Path)
                }
            };
            process.Start();
            _servicesInstances.Add(new ServiceInstance
            {
                ServiceInfo = serviceInfo,
                Process = process
            });
            Console.WriteLine($"Сервис {serviceInfo.Id} запущен (PID: {process.Id}) URL: {serviceInfo.Url}");
        }

        /// <summary>
        /// Фоновый наблюдатель за сервисами - перезапускает упавшие сервисы
        /// </summary>
        private async Task ServicesObserver(CancellationToken cancellationToken)
        {
            Console.WriteLine("ServicesObserver запущен");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(ObserverIntervalMs, cancellationToken);

                    foreach (var serviceInfo in _servicesInfo)
                    {
                        // Пропускаем сервисы отключённые вручную
                        if (_disabledServices.Contains(serviceInfo.Id))
                            continue;

                        // Проверяем запущен ли сервис
                        if (!IsServiceRunning(serviceInfo.Id))
                        {
                            Console.WriteLine($"[Observer] Сервис {serviceInfo.Id} не запущен, перезапуск...");

                            // Удаляем старый instance если есть
                            var oldInstance = _servicesInstances.FirstOrDefault(p => p.ServiceInfo.Id == serviceInfo.Id);
                            if (oldInstance != null)
                            {
                                _servicesInstances.Remove(oldInstance);
                            }

                            // Перезапускаем (без изменения флага autoRestart)
                            StartService(serviceInfo, enableAutoRestart: false);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Observer] Ошибка: {ex.Message}");
                }
            }

            Console.WriteLine("ServicesObserver остановлен");
        }

        public void StopService(ServiceInfo serviceInfo, bool disableAutoRestart = true)
        {
            var instance = _servicesInstances.FirstOrDefault(p => p.ServiceInfo.Id == serviceInfo.Id);
            if (instance == null) return;

            // Помечаем сервис как отключённый вручную
            if (disableAutoRestart)
            {
                _disabledServices.Add(serviceInfo.Id);
                Console.WriteLine($"Сервис {serviceInfo.Id} помечен как отключённый (авто-перезапуск выключен)");
            }

            try
            {
                instance.Process.Kill(true);
                _servicesInstances.Remove(instance);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Включить авто-перезапуск для сервиса
        /// </summary>
        public void EnableAutoRestart(string serviceId)
        {
            _disabledServices.Remove(serviceId);
            Console.WriteLine($"Авто-перезапуск для сервиса {serviceId} включён");
        }

        /// <summary>
        /// Отключить авто-перезапуск для сервиса
        /// </summary>
        public void DisableAutoRestart(string serviceId)
        {
            _disabledServices.Add(serviceId);
            Console.WriteLine($"Авто-перезапуск для сервиса {serviceId} отключён");
        }

        /// <summary>
        /// Проверить включён ли авто-перезапуск
        /// </summary>
        public bool IsAutoRestartEnabled(string serviceId)
        {
            return !_disabledServices.Contains(serviceId);
        }

        public List<ServiceInfo> GetServicesInfo() => _servicesInfo;

        public List<ServiceStatusDto> GetServicesStatus()
        {
            var statusList = new List<ServiceStatusDto>();

            foreach (var serviceInfo in _servicesInfo)
            {
                var instance = _servicesInstances.FirstOrDefault(p => p.ServiceInfo.Id == serviceInfo.Id);
                bool isRunning = false;
                int? processId = null;

                if (instance != null)
                {
                    try
                    {
                        isRunning = !instance.Process.HasExited;
                        processId = instance.Process.Id;
                    }
                    catch
                    {
                        isRunning = false;
                    }
                }

                statusList.Add(new ServiceStatusDto
                {
                    Id = serviceInfo.Id,
                    Path = serviceInfo.Path,
                    Url = serviceInfo.Url,
                    StartupArguments = serviceInfo.StartupArguments,
                    IsRunning = isRunning,
                    ProcessId = processId,
                    AutoRestartEnabled = !_disabledServices.Contains(serviceInfo.Id)
                });
            }

            return statusList;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Запускаем все сервисы
            foreach (var serviceInfo in _servicesInfo)
            {
                if (IsServiceRunning(serviceInfo.Id))
                {
                    Console.WriteLine($"Сервис {serviceInfo.Id} уже запущен");
                    continue;
                }

                StartService(serviceInfo);
            }

            // Запускаем наблюдателя в фоне
            Task.Run(() => ServicesObserver(_observerCts.Token));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Останавливаем наблюдателя
            _observerCts.Cancel();

            // Останавливаем все сервисы (без отключения авто-перезапуска)
            foreach (var serviceInfo in _servicesInfo)
            {
                if (IsServiceRunning(serviceInfo.Id))
                {
                    StopService(serviceInfo, disableAutoRestart: false);
                    Console.WriteLine($"Сервис {serviceInfo.Id} остановлен");
                }
            }
            return Task.CompletedTask;
        }
    }
}
