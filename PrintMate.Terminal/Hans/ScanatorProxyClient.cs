using HandyControl.Controls;
using Hans.NET.libs;
using Hans.NET.Models;
using HansHostProvider.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Observer.Shared.Models;
using PrintMate.Terminal;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Hans;
using PrintMate.Terminal.Hans.Events;
using PrintMate.Terminal.Services;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ScanatorStatus = HansHostProvider.Shared.ScanatorStatus;

namespace HansScannerHost.Models
{
    /// <summary>
    /// Клиент для взаимодействия с HansHostProvider через SignalR
    /// </summary>
    public class ScanatorProxyClient : IDisposable
    {
        private readonly string _ipAddress;
        private readonly string _serviceUrl;
        private int _boardIndex = -1;
        private UdmBuilderJavaPort _udmBuilder;

        // SignalR connections
        private HubConnection _invokeConnection;
        private HubConnection _eventsConnection;

        // Reconnection
        private CancellationTokenSource _reconnectCts;
        private bool _isDisposed = false;
        private const int ReconnectDelayMs = 10000;

        private ScanatorConfiguration _configuration;
        public bool IsConfigured => _configuration != null;
        public bool IsBoardIndexValid => _boardIndex != -1;
        public bool IsUdmBuilderInitialized => _udmBuilder != null;
        public bool IsDownloadFinish = false;
        public bool IsMarkComplete = false;
        public ConnectState ConnectState = ConnectState.Disconnected;
        public UdmBuilderJavaPort UdmBuilder => _udmBuilder;

        private bool _isConnected;
        public bool IsConnected => _isConnected && _invokeConnection?.State == HubConnectionState.Connected;

        public event EventHandler<HansHostProviderEvent> EventReceived;

        private readonly IEventAggregator _eventAggregator;

        public int MarkProgress = 0;
        public int DownloadProgress = 0;

        public ScanatorProxyClient(string address)
        {
            _ipAddress = address;

            // Получаем URL сервиса из конфигурации Services
            _serviceUrl = GetServiceUrlByAddress(address);

            _eventAggregator = Bootstrapper.ContainerProvider.Resolve<IEventAggregator>();

            // Загружаем конфигурацию сканатора
            if (_ipAddress == "172.18.34.227")
            {
                LoadConfiguration(Bootstrapper.Configuration.Get<ScannerSettings>().GetConfigurationByAddress("172.18.34.227"));
            }
            else
            {
                LoadConfiguration(Bootstrapper.Configuration.Get<ScannerSettings>().GetConfigurationByAddress("172.18.34.228"));
            }
            

            Bootstrapper.ContainerProvider.Resolve<IEventAggregator>()
                .GetEvent<OnScanatorsConfigurationChangedEvent>()
                .Subscribe(OnConfigurationChanged);

            // Запускаем подключение к SignalR
            Task.Run(async () => await ConnectAsync());
        }

        private string GetServiceUrlByAddress(string address)
        {
            // Находим сервис по IP адресу в StartupArguments
            if (address == "172.18.34.227")
                return Services.Hans1.Url;
            if (address == "172.18.34.228")
                return Services.Hans2.Url;

            throw new ArgumentException($"Unknown scanner address: {address}");
        }

        private void OnConfigurationChanged()
        {
            if (_ipAddress == "172.18.34.227")
            {
                LoadConfiguration(Bootstrapper.Configuration.Get<ScannerSettings>().GetConfigurationByAddress("172.18.34.227"));
            }
            else
            {
                LoadConfiguration(Bootstrapper.Configuration.Get<ScannerSettings>().GetConfigurationByAddress("172.18.34.228"));
            }
            Console.WriteLine($"ScanatorProxyClient: [{_ipAddress}] Обновленная конфигурация успешно загружена");

            // Отправляем новую конфигурацию на сервер
            Task.Run(async () => await LoadConfigurationAsync(_configuration));
        }

        public void LoadConfiguration(ScanatorConfiguration configuration)
        {
            _configuration = configuration;
            _boardIndex = _configuration.CardInfo.SeqIndex;
            _udmBuilder = new UdmBuilderJavaPort(_configuration);
        }

        /// <summary>
        /// Подключение к SignalR хабам с бесконечными попытками
        /// </summary>
        public async Task ConnectAsync()
        {
            _reconnectCts?.Cancel();
            _reconnectCts = new CancellationTokenSource();

            while (!_reconnectCts.IsCancellationRequested && !_isDisposed)
            {
                try
                {
                    //Console.WriteLine($"[{_ipAddress}] Подключение к SignalR...");

                    // Создаем подключение к Invoke Hub
                    _invokeConnection = new HubConnectionBuilder()
                        .WithUrl($"{_serviceUrl}/invoke")
                        .WithAutomaticReconnect()
                        .Build();

                    // Создаем подключение к Events Hub
                    _eventsConnection = new HubConnectionBuilder()
                        .WithUrl($"{_serviceUrl}/events")
                        .WithAutomaticReconnect()
                        .Build();

                    // Подписываемся на события
                    SubscribeToEvents();

                    // Подключаемся
                    await _invokeConnection.StartAsync();
                    Console.WriteLine($"[{_ipAddress}] ✓ Invoke Hub подключен");

                    await _eventsConnection.StartAsync();
                    Console.WriteLine($"[{_ipAddress}] ✓ Events Hub подключен");

                    _isConnected = true;

                    // Загружаем конфигурацию на сервер
                    if (_configuration != null)
                    {
                        await LoadConfigurationAsync(_configuration);
                    }

                    Console.WriteLine($"[{_ipAddress}] ✓ SignalR подключение установлено");
                    return;
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"[{_ipAddress}] Ошибка подключения: {ex.Message}");
                    //Console.WriteLine($"[{_ipAddress}] Повторная попытка через {ReconnectDelayMs}мс...");

                    try
                    {
                        await Task.Delay(ReconnectDelayMs, _reconnectCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private void SubscribeToEvents()
        {
            // Подписываемся на все события
            _eventsConnection.On<HansHostProviderEvent>(EventId.ReadyToConnect, (e) =>
            {
                ConnectState = ConnectState.ReadyToConnect;
                EventReceived?.Invoke(this, e);
                Console.WriteLine("ReadyToConnect");
            });

            _eventsConnection.On<HansHostProviderEvent>(EventId.Connected, (e) =>
            {
                ConnectState = ConnectState.Connected;
                EventReceived?.Invoke(this, e);
                Console.WriteLine("Connected");
            });

            _eventsConnection.On<HansHostProviderEvent>(EventId.Disconnected, (e) =>
            {
                ConnectState = ConnectState.Disconnected;
                EventReceived?.Invoke(this, e);
                Console.WriteLine("Disconnected");
            });

            _eventsConnection.On<HansHostProviderEvent>(EventId.StreamProgress, (e) =>
            {
                if (!string.IsNullOrEmpty(e.EventArgsJson))
                {
                    try
                    {
                        // Пробуем десериализовать как одно число (новый формат)
                        DownloadProgress = JsonConvert.DeserializeObject<int>(e.EventArgsJson);
                    }
                    catch
                    {
                        // Если не получилось, пробуем как массив (старый формат для совместимости)
                        var args = JsonConvert.DeserializeObject<int[]>(e.EventArgsJson);
                        if (args?.Length > 0)
                        {
                            DownloadProgress = args[0];
                        }
                    }
                }
                EventReceived?.Invoke(this, e);
            });

            _eventsConnection.On<HansHostProviderEvent>(EventId.StreamEnd, (e) =>
            {
                IsDownloadFinish = true;
                EventReceived?.Invoke(this, e);
            });

            _eventsConnection.On<HansHostProviderEvent>(EventId.MarkingProgress, (e) =>
            {
                if (!string.IsNullOrEmpty(e.EventArgsJson))
                {
                    try
                    {
                        // Пробуем десериализовать как одно число (новый формат)
                        MarkProgress = JsonConvert.DeserializeObject<int>(e.EventArgsJson);
                    }
                    catch
                    {
                        // Если не получилось, пробуем как массив (старый формат)
                        var args = JsonConvert.DeserializeObject<int[]>(e.EventArgsJson);
                        if (args?.Length > 0)
                        {
                            MarkProgress = args[0];
                        }
                    }

                    _eventAggregator.GetEvent<OnMarkingProgressEvent>().Publish(this);

                    //if (PrintService.Instance.Mode == PrintServiceMode.Automatic)
                    //{
                    //    _eventAggregator.GetEvent<OnMarkingProgressEvent>().Publish(this);
                    //}
                    //else
                    //{
                    //    _eventAggregator.GetEvent<OnSingleModeMarkingProgressEvent>().Publish(this);
                    //}
                }
                EventReceived?.Invoke(this, e);
            });

            _eventsConnection.On<HansHostProviderEvent>(EventId.MarkingComplete, (e) =>
            {
                IsMarkComplete = true;
                MarkProgress = 100;

                if (PrintService.Instance.Mode == PrintServiceMode.Automatic)
                {
                    _eventAggregator.GetEvent<OnMarkingProgressEvent>().Publish(this);
                }
                else
                {
                    _eventAggregator.GetEvent<OnSingleModeMarkingProgressEvent>().Publish(this);
                }

                EventReceived?.Invoke(this, e);
            });

            _eventsConnection.On<HansHostProviderEvent>(EventId.Status, (e) =>
            {
                try
                {
                    // Десериализуем статус напрямую как объект
                    ScanatorStatus status = JsonConvert.DeserializeObject<ScanatorStatus>(e.EventArgsJson);
                    //IsMarkComplete = status.IsMarkFinish;
                    MarkProgress = status.MarkProgress;
                    if (status.IsConnected)
                    {
                        ConnectState = ConnectState.Connected;
                    }
                    else
                    {
                        ConnectState = ConnectState.Disconnected;
                    }
                    //if (status != null)
                    //{
                    //    Console.WriteLine($"Status from {_ipAddress}:");
                    //    Console.WriteLine($"  IsConnected: {status.IsConnected}");
                    //    Console.WriteLine($"  IsMarking: {status.IsMarking}");
                    //    Console.WriteLine($"  WorkingStatus: {status.WorkingStatus}");
                    //    Console.WriteLine($"  MarkProgress: {status.MarkProgress}");
                    //    Console.WriteLine($"  DownloadProgress: {status.DownloadProgress}");
                    //}
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка десериализации статуса: {ex.Message}");
                    Console.WriteLine($"JSON: {e.EventArgsJson}");
                }
            });

            // Обработка переподключения
            _invokeConnection.Reconnected += async (connectionId) =>
            {
                Console.WriteLine($"[{_ipAddress}] Invoke Hub переподключен");
                _isConnected = true;

                if (_configuration != null)
                {
                    await LoadConfigurationAsync(_configuration);
                }
            };

            _invokeConnection.Closed += async (error) =>
            {
                Console.WriteLine($"[{_ipAddress}] Invoke Hub отключен: {error?.Message}");
                _isConnected = false;
                ConnectState = ConnectState.Disconnected;

                // Запускаем переподключение
                StartReconnectLoop();
            };

            _eventsConnection.Reconnected += async (connectionId) =>
            {
                Console.WriteLine($"[{_ipAddress}] Events Hub переподключен");
            };

            _eventsConnection.Closed += async (error) =>
            {
                Console.WriteLine($"[{_ipAddress}] Events Hub отключен: {error?.Message}");
            };
        }

        /// <summary>
        /// Запускает цикл бесконечного переподключения
        /// </summary>
        private void StartReconnectLoop()
        {
            if (_isDisposed) return;

            // Отменяем предыдущий цикл если есть
            _reconnectCts?.Cancel();
            _reconnectCts = new CancellationTokenSource();

            Task.Run(async () => await ReconnectLoopAsync(_reconnectCts.Token));
        }

        private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !_isDisposed)
            {
                try
                {
                    Console.WriteLine($"[{_ipAddress}] Попытка переподключения через {ReconnectDelayMs}мс...");
                    await Task.Delay(ReconnectDelayMs, cancellationToken);

                    if (cancellationToken.IsCancellationRequested || _isDisposed) break;

                    // Пробуем переподключиться (1 попытка)
                    bool success = await TryReconnectOnceAsync();
                    if (success)
                    {
                        Console.WriteLine($"[{_ipAddress}] Переподключение успешно!");
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{_ipAddress}] Ошибка при переподключении: {ex.Message}");
                }
            }
        }

        private async Task<bool> TryReconnectOnceAsync()
        {
            try
            {
                // Закрываем старые подключения если есть
                if (_invokeConnection != null)
                {
                    try { await _invokeConnection.DisposeAsync(); } catch { }
                }
                if (_eventsConnection != null)
                {
                    try { await _eventsConnection.DisposeAsync(); } catch { }
                }

                // Создаем новые подключения
                _invokeConnection = new HubConnectionBuilder()
                    .WithUrl($"{_serviceUrl}/invoke")
                    .WithAutomaticReconnect()
                    .Build();

                _eventsConnection = new HubConnectionBuilder()
                    .WithUrl($"{_serviceUrl}/events")
                    .WithAutomaticReconnect()
                    .Build();

                // Подписываемся на события
                SubscribeToEvents();

                // Подключаемся
                await _invokeConnection.StartAsync();
                await _eventsConnection.StartAsync();

                if (_invokeConnection.State == HubConnectionState.Connected)
                {
                    Console.WriteLine("_invokeConnection connected");
                }
                if (_eventsConnection.State == HubConnectionState.Connected)
                {
                    Console.WriteLine("_eventsConnection connected");

                }

                _isConnected = true;

                // Загружаем конфигурацию на сервер
                if (_configuration != null)
                {
                    await LoadConfigurationAsync(_configuration);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_ipAddress}] Не удалось переподключиться: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        #region Invoke Methods

        /// <summary>
        /// Загрузить конфигурацию на сервер
        /// </summary>
        public async Task LoadConfigurationAsync(ScanatorConfiguration config)
        {
            if (!await _invokeConnection.InvokeAsync<bool>(Methods.IsConnected))
            {
                Console.WriteLine("Устройство не подключено, конфигурация не может быть применена !");
                return;
            }

            await _invokeConnection.InvokeAsync(Methods.LoadConfiguration, config);
            Console.WriteLine($"[{_ipAddress}] Конфигурация загружена на сервер");
        }

        /// <summary>
        /// Проверить, загружена ли конфигурация
        /// </summary>
        public async Task<bool> IsConfigurationLoadedAsync()
        {
            if (!IsConnected) return false;
            return await _invokeConnection.InvokeAsync<bool>(Methods.IsConfigurationLoaded);
        }

        /// <summary>
        /// Получить текущую конфигурацию с сервера
        /// </summary>
        public async Task<ScanatorConfiguration> GetConfigurationAsync()
        {
            if (!IsConnected) return null;
            return await _invokeConnection.InvokeAsync<ScanatorConfiguration>(Methods.GetConfiguration);
        }


        /// <summary>
        /// Получить адрес хоста
        /// </summary>
        public async Task<string> GetHostAddressAsync()
        {
            return _ipAddress;
        }

        /// <summary>
        /// Получить фиксированный индекс
        /// </summary>
        public async Task<int> GetFixedIndexAsync()
        {
            if (!IsConnected) return -1;
            return await _invokeConnection.InvokeAsync<int>(Methods.GetFixedIndex);
        }

        /// <summary>
        /// Проверить инициализацию Hans SDK
        /// </summary>
        public async Task<bool> IsHansSdkInitializedAsync()
        {
            if (!IsConnected) return false;
            return await _invokeConnection.InvokeAsync<bool>(Methods.IsHansSdkInitialized);
        }

        /// <summary>
        /// Получить прогресс загрузки
        /// </summary>
        public async Task<int> GetDownloadProgressAsync()
        {
            if (!IsConnected) return 0;
            return await _invokeConnection.InvokeAsync<int>(Methods.GetDownloadProgress);
        }

        /// <summary>
        /// Получить прогресс маркировки
        /// </summary>
        public async Task<int> GetMarkingProgressAsync()
        {
            if (!IsConnected) return 0;
            return await _invokeConnection.InvokeAsync<int>(Methods.GetMarkingProgress);
        }

        /// <summary>
        /// Проверить подключение к плате
        /// </summary>
        public async Task<bool> IsConnectedToBoard()
        {
            if (!IsConnected) return false;
            return await _invokeConnection.InvokeAsync<bool>(Methods.IsConnected);
        }

        /// <summary>
        /// Получить индекс платы
        /// </summary>
        public async Task<int> GetBoardIndexAsync()
        {
            if (!IsConnected) return -1;
            return await _invokeConnection.InvokeAsync<int>(Methods.GetBoardIndex);
        }

        /// <summary>
        /// Проверить завершение загрузки файла
        /// </summary>
        public async Task<bool> IsDownloadMarkFileFinishAsync()
        {
            if (!IsConnected) return false;
            return await _invokeConnection.InvokeAsync<bool>(Methods.IsDownloadMarkFileFinish);
        }

        /// <summary>
        /// Загрузить UDM файл в контроллер
        /// </summary>
        public async Task<bool> DownloadMarkFileAsync(string udmFilePath)
        {
            if (!IsConnected) return false;

            IsDownloadFinish = false;
            IsMarkComplete = false;
            MarkProgress = 0;
            DownloadProgress = 0;

            return await _invokeConnection.InvokeAsync<bool>(Methods.DownloadMarkFile, udmFilePath);
        }

        /// <summary>
        /// Начать маркировку
        /// </summary>
        public async Task StartMarkAsync()
        {
            if (!IsConnected) return;
            Console.WriteLine("НАЧАЛО СКАНИРОВАНИЯ");
            await _invokeConnection.InvokeAsync(Methods.StartMark);
        }

        /// <summary>
        /// Остановить маркировку
        /// </summary>
        public async Task StopMarkAsync()
        {
            if (!IsConnected) return;
            await _invokeConnection.InvokeAsync(Methods.StopMark);
        }

        /// <summary>
        /// Приостановить маркировку
        /// </summary>
        public async Task PauseMarkAsync()
        {
            if (!IsConnected) return;
            await _invokeConnection.InvokeAsync(Methods.PauseMark);
        }

        /// <summary>
        /// Получить статус сканатора
        /// </summary>
        public async Task<HansHostProvider.Shared.ScanatorStatus?> GetStatusAsync()
        {
            if (!IsConnected) return null;
            return await _invokeConnection.InvokeAsync<HansHostProvider.Shared.ScanatorStatus>(Methods.GetStatus);
        }

        #endregion

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            // Останавливаем цикл переподключения
            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();

            try
            {
                _invokeConnection?.StopAsync().Wait(TimeSpan.FromSeconds(2));
                _eventsConnection?.StopAsync().Wait(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_ipAddress}] Ошибка при отключении: {ex.Message}");
            }

            _invokeConnection?.DisposeAsync();
            _eventsConnection?.DisposeAsync();
            _isConnected = false;

            GC.SuppressFinalize(this);
        }
    }
}
