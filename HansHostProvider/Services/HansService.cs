using Hans.NET.libs;
using Hans.NET.Models;
using HansHostProvider.Hubs;
using HansHostProvider.Shared;
using HansHostProvider.Utils;
using LoggingService.Client;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using static Hans.NET.libs.HM_HashuScanDLL;
using EventId = HansHostProvider.Shared.EventId;
using ScanatorStatus = HansHostProvider.Shared.ScanatorStatus;

namespace HansHostProvider.Services
{
    public class HansService : IHostedService, IDisposable
    {
        private const int ObserverIntervalMs = 500;
        private const int ReconnectDelayMs = 10000;
        private const int InitTimeoutSeconds = 10;

        public string Address => ServiceContext.BoardAddress;
        public int BoardIndex { get; private set; } = -1;
        public bool IsConnected { get; private set; }
        public bool IsMarking { get; private set; }
        public bool IsMarkComplete { get; private set; }
        public int MarkProgress { get; private set; }
        public MarkingState MarkingState { get; private set; } = MarkingState.Stop;
        public bool IsDownloadMarkFileFinish { get; private set; }
        public int DownloadProgress { get; private set; }
        public ConnectState ConnectState { get; private set; } = ConnectState.Disconnected;
        public WorkingStatus WorkingStatus { get; private set; } = WorkingStatus.Unknown;
        public bool IsSdkInitialized => _isSdkInitialized;

        private readonly IHubContext<EventsHub> _eventsHubContext;
        private readonly SdkMessagePump _messagePump;
        private readonly ManualResetEventSlim _initializationComplete = new(false);
        private readonly LoggingClient _loggingClient;
        private readonly CancellationTokenSource _observerCts = new();
        private volatile bool _isSdkInitialized;
        private bool _disposed;

        public HansService(IHubContext<EventsHub> eventsHubContext)
        {
            _eventsHubContext = eventsHubContext ?? throw new ArgumentNullException(nameof(eventsHubContext));
            _loggingClient = new LoggingClient($"HansHost_{Address}");
            _messagePump = new SdkMessagePump();
            _messagePump.MessageReceived += OnWindowMessage;
        }

        private async Task InitializeSdk()
        {
            _messagePump.Start();

            // Ждём создания окна с таймаутом
            SpinWait.SpinUntil(() => _messagePump.IsInitialized, TimeSpan.FromMilliseconds(500));

            if (!_messagePump.IsInitialized)
            {
                await Log("Ошибка: SdkMessagePump не инициализирован за отведённое время");
                return;
            }

            //await Log($"SdkMessagePump запущен, Handle: 0x{_messagePump.WindowHandle.ToInt64():X}");

            _messagePump.Invoke(() =>
            {
                _isSdkInitialized = IsSuccess(() => HM_InitBoard(_messagePump.WindowHandle));
                Log(_isSdkInitialized
                    ? "Hans SDK успешно инициализирована"
                    : "Ошибка при инициализации Hans SDK").Wait();

                _initializationComplete.Set();
            });
        }

        private void OnWindowMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case MessageType.ConnectStateUpdate:
                    HandleConnectStateUpdate((int)lParam, (ulong)wParam);
                    break;

                case MessageType.StreamProgress:
                    DownloadProgress = (int)wParam;
                    Task.Run(async () => await Log($"Download progress: {DownloadProgress}"));
                    _ = PublishEventAsync(EventId.StreamProgress, DownloadProgress);
                    break;

                case MessageType.StreamEnd:
                    Task.Run(async () => await Log("Download complete"));
                    IsDownloadMarkFileFinish = true;
                    _ = PublishEventAsync(EventId.StreamEnd);
                    break;

                case MessageType.MarkingProgress:
                    var progress = (int)wParam;
                    if (progress > 0)
                    {
                        MarkProgress = progress;
                        Task.Run(async () => await Log($"Marking progress: {progress}%"));
                        _ = PublishEventAsync(EventId.MarkingProgress, progress);
                    }
                    break;

                case MessageType.MarkingComplete:
                    Task.Run(async () => await Log("Mark complete"));
                    IsMarkComplete = true;
                    MarkProgress = 0;
                    _ = PublishEventAsync(EventId.MarkingComplete);
                    break;
            }
        }

        private void HandleConnectStateUpdate(int ipIndex, ulong ipValue)
        {
            var deviceInfo = HansServiceUtils.DeviceRefresh(ipIndex, ipValue);
            if (deviceInfo.DeviceName != ServiceContext.BoardAddress)
                return;

            BoardIndex = ipIndex;
            ConnectState = (ConnectState)HM_GetConnectStatus(ipIndex);

            switch (ConnectState)
            {
                case ConnectState.ReadyToConnect:
                    HM_ConnectByIpStr(ServiceContext.BoardAddress);
                    ConnectState = (ConnectState)HM_GetConnectStatus(ipIndex);
                    IsConnected = ConnectState == ConnectState.Connected;
                    _ = PublishEventAsync(EventId.ReadyToConnect);
                    break;

                case ConnectState.Connected:
                    IsConnected = true;
                    _ = PublishEventAsync(EventId.Connected);
                    break;

                case ConnectState.Disconnected:
                    IsConnected = false;
                    _ = PublishEventAsync(EventId.Disconnected);
                    break;
            }

            Log($"Connect state changed: {ConnectState}, BoardIndex: {BoardIndex}").Wait();
        }

        private async Task PublishEventAsync(string eventName, object? arg = null)
        {
            try
            {
                var eventData = new HansHostProviderEvent
                {
                    Id = Guid.NewGuid(),
                    EventName = eventName,
                    EventArgsJson = arg != null ? JsonConvert.SerializeObject(arg) : string.Empty
                };

                await _eventsHubContext.Clients.All.SendAsync(eventName, eventData);
            }
            catch (Exception ex)
            {
                Log($"Ошибка публикации события {eventName}: {ex.Message}");
            }
        }

        public void ApplyConfiguration(ScanatorConfiguration scanatorConfiguration)
        {
            if (!_isSdkInitialized)
            {
                Log("Невозможно применить конфигурацию: SDK не инициализирован");
                return;
            }

            if (BoardIndex == -1)
            {
                Log("Невозможно применить конфигурацию: устройство не подключено");
                return;
            }

            if (_messagePump.InvokeRequired)
            {
                _messagePump.Invoke(() => ApplyConfiguration(scanatorConfiguration));
                return;
            }

            try
            {
                Log($"OffsetX: {scanatorConfiguration.ScannerConfig.OffsetX}, OffsetY: {scanatorConfiguration.ScannerConfig.OffsetY}");

                //HM_SetMarkRegion(BoardIndex, (int)400);
                //HM_SetCoordinate(BoardIndex, scanatorConfiguration.ScannerConfig.CoordinateTypeCode);
                Log("Конфигурация успешно применена");
            }
            catch (Exception ex)
            {
                Log($"Ошибка при применении конфигурации: {ex.Message}");
            }
        }

        private async Task RunObserverAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!IsConnected)
                    {
                        await TryConnectAsync(cancellationToken);
                        continue;
                    }

                    if (BoardIndex == -1)
                    {
                        IsConnected = false;
                        continue;
                    }

                    await UpdateStatusAsync();
                    await Task.Delay(ObserverIntervalMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Ошибка в Observer: {ex.Message}");
                    await Task.Delay(ReconnectDelayMs, cancellationToken);
                }
            }

            Log("Observer остановлен");
        }

        private async Task TryConnectAsync(CancellationToken cancellationToken)
        {
            //Log($"Попытка подключения к {ServiceContext.BoardAddress}...");

            HM_ConnectByIpStr(ServiceContext.BoardAddress);
            int index = HM_GetIndexByIpAddr(ServiceContext.BoardAddress);

            if (index >= 0)
            {
                BoardIndex = index;
                ConnectState = (ConnectState)HM_GetConnectStatus(index);
                Log($"Состояние подключения: {ConnectState}");

                if (ConnectState == ConnectState.Connected)
                {
                    IsConnected = true;
                    Log($"Успешное подключение к {ServiceContext.BoardAddress} (индекс: {BoardIndex})");
                    await PublishEventAsync(EventId.Connected);
                    return;
                }
            }

            await Task.Delay(ReconnectDelayMs, cancellationToken);
        }

        private async Task UpdateStatusAsync()
        {
            WorkingStatus = GetWorkingStatus(BoardIndex);
            IsMarking = WorkingStatus == WorkingStatus.Run;

            if (IsMarking)
            {
                HM_ExecuteProgress(BoardIndex);
            }

            var status = new ScanatorStatus
            {
                IsConnected = true,
                IsMarking = IsMarking,
                IsMarkFinish = IsMarkComplete,
                LastError = string.Empty,
                WorkingStatus = (int)WorkingStatus,
                MarkProgress = MarkProgress
            };

            await PublishEventAsync(EventId.Status, status);
        }

        public bool DownloadMarkFile(string udmFilePath)
        {
            if (_messagePump.InvokeRequired)
                return _messagePump.Invoke(() => DownloadMarkFile(udmFilePath));

            if (!IsConnected)
            {
                Log("Ошибка: отсутствует подключение к плате");
                return false;
            }

            if (!File.Exists(udmFilePath))
            {
                Log($"Ошибка: файл не существует: {udmFilePath}");
                return false;
            }

            var fileInfo = new FileInfo(udmFilePath);
            Log($"Загрузка файла: {Path.GetFileName(udmFilePath)} ({fileInfo.Length:N0} bytes), BoardIndex: {BoardIndex}");

            IsDownloadMarkFileFinish = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            int result = HM_DownloadMarkFile(BoardIndex, udmFilePath, _messagePump.WindowHandle);
            HM_BurnMarkFile(BoardIndex, false);

            sw.Stop();

            if (result == 0)
            {
                Log($"Файл загружен успешно ({sw.ElapsedMilliseconds}ms)");
                return true;
            }

            Log($"Ошибка загрузки файла. Код: {result}, время: {sw.ElapsedMilliseconds}ms");
            return false;
        }

        public void StartMark()
        {
            if (_messagePump.InvokeRequired)
            {
                _messagePump.Invoke(StartMark);
                return;
            }

            IsMarkComplete = false;
            MarkProgress = 0;
            HM_StartMark(BoardIndex);
        }

        public void StopMark()
        {
            if (_messagePump.InvokeRequired)
            {
                _messagePump.Invoke(StopMark);
                return;
            }

            HM_StopMark(BoardIndex);
        }

        public void Pause()
        {
            if (_messagePump.InvokeRequired)
            {
                _messagePump.Invoke(Pause);
                return;
            }

            HM_PauseMark(BoardIndex);
        }

        private async Task Log(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            Console.WriteLine($"[{Address}][{timestamp}] {message}");
            await _loggingClient.LogInformationAsync("HansService", message);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Log("HansService запускается...");
            await InitializeSdk();

            _ = Task.Run(async () =>
            {
                if (_initializationComplete.Wait(TimeSpan.FromSeconds(InitTimeoutSeconds)))
                {
                    await Log($"HansService инициализирован. SDK: {_isSdkInitialized}");
                    await RunObserverAsync(_observerCts.Token);
                }
                else
                {
                    await Log("Таймаут инициализации HansService");
                }
            }, cancellationToken);

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Log("Остановка HansService...");

            _observerCts.Cancel();

            try
            {
                _messagePump.Dispose();
                await Log("SdkMessagePump остановлен");
            }
            catch (Exception ex)
            {
                await Log($"Ошибка при остановке SdkMessagePump: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _observerCts.Cancel();
            _observerCts.Dispose();
            _initializationComplete.Dispose();
            _messagePump.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
