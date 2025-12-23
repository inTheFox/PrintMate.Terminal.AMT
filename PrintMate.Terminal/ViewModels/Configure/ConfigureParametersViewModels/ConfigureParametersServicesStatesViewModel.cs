using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using HandyControl.Tools.Command;
using Observer.Shared.Models;
using PrintMate.Terminal.Services;
using Prism.Mvvm;
using Prism.Regions;

namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels
{
    public class ConfigureParametersServicesStatesViewModel : BindableBase, IRegionMemberLifetime, INavigationAware
    {
        public bool KeepAlive => false;

        private const string EnabledIcon = "/images/indicator_green_32.png";
        private const string DisabledIcon = "/images/indicator_red_32.png";

        private readonly ObserverApiClient _apiClient;
        private Timer _refreshTimer;
        private bool _isObserverAvailable;

        public ObservableCollection<ServiceStatusViewModel> Services { get; } = new();

        public bool IsObserverAvailable
        {
            get => _isObserverAvailable;
            set
            {
                if (SetProperty(ref _isObserverAvailable, value))
                {
                    RaisePropertyChanged(nameof(ObserverStatusText));
                    RaisePropertyChanged(nameof(ObserverStatusColor));
                    RaisePropertyChanged(nameof(ObserverStatusIcon));
                    RaisePropertyChanged(nameof(EmptyMessage));
                    RaisePropertyChanged(nameof(ShowEmptyMessage));
                }
            }
        }

        // Observer status properties
        public string ObserverStatusText => IsObserverAvailable ? "Доступен" : "Недоступен";
        public Brush ObserverStatusColor => IsObserverAvailable ? Brushes.LimeGreen : Brushes.IndianRed;
        public string ObserverStatusIcon => IsObserverAvailable ? EnabledIcon : DisabledIcon;

        // Empty message
        public string EmptyMessage => IsObserverAvailable ? "Нет доступных сервисов" : "Observer недоступен";
        public Visibility ShowEmptyMessage => Services.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // Statistics
        public int TotalCount => Services.Count;
        public int RunningCount => Services.Count(s => s.IsRunning);
        public int StoppedCount => Services.Count(s => !s.IsRunning);

        public RelayCommand RefreshCommand { get; }

        public ConfigureParametersServicesStatesViewModel()
        {
            _apiClient = new ObserverApiClient();
            RefreshCommand = new RelayCommand(async _ => await RefreshServicesAsync());
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // Запускаем загрузку сервисов
            _ = RefreshServicesAsync();

            // Автообновление каждые 3 секунды
            _refreshTimer = new Timer(async _ =>
            {
                await RefreshServicesAsync();
            }, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // Останавливаем таймер при уходе со страницы
            _refreshTimer?.Dispose();
            _refreshTimer = null;
        }

        private async Task RefreshServicesAsync()
        {
            try
            {
                var isAvailable = await _apiClient.IsAvailableAsync();
                IsObserverAvailable = isAvailable;

                if (!isAvailable)
                {
                    Application.Current?.Dispatcher?.Invoke(() => Services.Clear());
                    UpdateStatistics();
                    return;
                }

                var statuses = await _apiClient.GetServicesStatusAsync();

                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    // Обновляем существующие или добавляем новые
                    foreach (var status in statuses)
                    {
                        var existing = FindService(status.Id);
                        if (existing != null)
                        {
                            existing.Update(status);
                        }
                        else
                        {
                            Services.Add(new ServiceStatusViewModel(status, _apiClient, RefreshServicesAsync));
                        }
                    }

                    // Удаляем сервисы, которых больше нет
                    var toRemove = Services.Where(s => !statuses.Any(st => st.Id == s.Id)).ToList();
                    foreach (var service in toRemove)
                    {
                        Services.Remove(service);
                    }
                });

                UpdateStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicesStatesVM] Ошибка обновления: {ex.Message}");
            }
        }

        private void UpdateStatistics()
        {
            RaisePropertyChanged(nameof(TotalCount));
            RaisePropertyChanged(nameof(RunningCount));
            RaisePropertyChanged(nameof(StoppedCount));
            RaisePropertyChanged(nameof(ShowEmptyMessage));
        }

        private ServiceStatusViewModel FindService(string id)
        {
            foreach (var service in Services)
            {
                if (service.Id == id)
                    return service;
            }
            return null;
        }
    }

    /// <summary>
    /// ViewModel для отдельного сервиса
    /// </summary>
    public class ServiceStatusViewModel : BindableBase
    {
        private const string EnabledIcon = "/images/indicator_green_32.png";
        private const string DisabledIcon = "/images/indicator_red_32.png";

        private readonly ObserverApiClient _apiClient;
        private readonly Func<Task> _refreshCallback;

        private string _id;
        private string _path;
        private string _url;
        private string _startupArguments;
        private bool _isRunning;
        private int? _processId;
        private bool _autoRestartEnabled;
        private bool _isProcessing;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        public string StartupArguments
        {
            get => _startupArguments;
            set
            {
                if (SetProperty(ref _startupArguments, value))
                {
                    RaisePropertyChanged(nameof(HasArguments));
                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    RaisePropertyChanged(nameof(StatusText));
                    RaisePropertyChanged(nameof(StatusColor));
                    RaisePropertyChanged(nameof(StatusIconSource));
                }
            }
        }

        public int? ProcessId
        {
            get => _processId;
            set
            {
                if (SetProperty(ref _processId, value))
                {
                    RaisePropertyChanged(nameof(ProcessIdText));
                }
            }
        }

        public bool AutoRestartEnabled
        {
            get => _autoRestartEnabled;
            set => SetProperty(ref _autoRestartEnabled, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        // Computed properties
        public string StatusText => IsRunning ? "Запущен" : "Остановлен";
        public Brush StatusColor => IsRunning ? Brushes.LimeGreen : Brushes.IndianRed;
        public string StatusIconSource => IsRunning ? EnabledIcon : DisabledIcon;
        public string ProcessIdText => ProcessId.HasValue ? $"PID: {ProcessId}" : string.Empty;
        public Visibility HasArguments => !string.IsNullOrEmpty(StartupArguments) ? Visibility.Visible : Visibility.Collapsed;

        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand RestartCommand { get; }

        public ServiceStatusViewModel(ServiceStatusDto dto, ObserverApiClient apiClient, Func<Task> refreshCallback)
        {
            _apiClient = apiClient;
            _refreshCallback = refreshCallback;

            Update(dto);

            StartCommand = new RelayCommand(async _ => await StartAsync(), _ => !IsRunning && !IsProcessing);
            StopCommand = new RelayCommand(async _ => await StopAsync(), _ => IsRunning && !IsProcessing);
            RestartCommand = new RelayCommand(async _ => await RestartAsync(), _ => IsRunning && !IsProcessing);
        }

        public void Update(ServiceStatusDto dto)
        {
            Id = dto.Id;
            Path = dto.Path;
            Url = dto.Url;
            StartupArguments = dto.StartupArguments;
            IsRunning = dto.IsRunning;
            ProcessId = dto.ProcessId;
            AutoRestartEnabled = dto.AutoRestartEnabled;
        }

        private async Task StartAsync()
        {
            if (IsProcessing) return;

            try
            {
                IsProcessing = true;
                var success = await _apiClient.StartServiceAsync(Id);
                if (success)
                {
                    await Task.Delay(1000);
                    await _refreshCallback();
                }
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task StopAsync()
        {
            if (IsProcessing) return;

            try
            {
                IsProcessing = true;
                var success = await _apiClient.StopServiceAsync(Id);
                if (success)
                {
                    await Task.Delay(1000);
                    await _refreshCallback();
                }
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task RestartAsync()
        {
            if (IsProcessing) return;

            try
            {
                IsProcessing = true;
                var success = await _apiClient.RestartServiceAsync(Id);
                if (success)
                {
                    await Task.Delay(1500);
                    await _refreshCallback();
                }
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
