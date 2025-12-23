using LoggingService.Shared.Models;
using LogViewerApp.Models;
using LogViewerApp.Services;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace LogViewerApp.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private readonly LoggingApiService _loggingApiService;
        private readonly DispatcherTimer _pollingTimer;
        private readonly object _lockObject = new object();
        private DateTime _lastLogTimestamp = DateTime.MinValue;

        private ObservableCollection<LogEntryViewModel> _allLogs;
        private ObservableCollection<LogEntryViewModel> _filteredLogs;
        private string _searchText = string.Empty;
        private string? _selectedApplication;
        private string? _selectedCategory;
        private LogLevelFilter? _selectedLogLevel;
        private DateTime? _selectedDate;
        private bool _isConnected;
        private bool _autoScroll = true;
        private int _totalLogsCount;

        public ObservableCollection<LogEntryViewModel> FilteredLogs
        {
            get => _filteredLogs;
            set => SetProperty(ref _filteredLogs, value);
        }

        public ObservableCollection<string> Applications { get; }
        public ObservableCollection<string> Categories { get; }
        public ObservableCollection<LogLevelFilter> LogLevels { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string? SelectedApplication
        {
            get => _selectedApplication;
            set
            {
                if (SetProperty(ref _selectedApplication, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ApplyFilters();
                }
            }
        }

        public LogLevelFilter? SelectedLogLevel
        {
            get => _selectedLogLevel;
            set
            {
                if (SetProperty(ref _selectedLogLevel, value))
                {
                    ApplyFilters();
                }
            }
        }

        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    ApplyFilters();
                }
            }
        }

        private string? _selectedSessionId;
        public string? SelectedSessionId
        {
            get => _selectedSessionId;
            set
            {
                if (SetProperty(ref _selectedSessionId, value))
                {
                    ApplyFilters();
                }
            }
        }

        public ObservableCollection<string> SessionIds { get; }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public bool AutoScroll
        {
            get => _autoScroll;
            set => SetProperty(ref _autoScroll, value);
        }

        public int TotalLogsCount
        {
            get => _totalLogsCount;
            set => SetProperty(ref _totalLogsCount, value);
        }

        public string ConnectionStatusText => IsConnected ? "Подключено" : "Отключено";
        public string ConnectionStatusColor => IsConnected ? "#4CAF50" : "#F44336";

        public DelegateCommand ClearLogsCommand { get; }
        public DelegateCommand ReconnectCommand { get; }

        public MainViewModel(LoggingApiService loggingApiService)
        {
            _loggingApiService = loggingApiService;
            _allLogs = new ObservableCollection<LogEntryViewModel>();
            _filteredLogs = new ObservableCollection<LogEntryViewModel>();

            Applications = new ObservableCollection<string> { "Все" };
            Categories = new ObservableCollection<string> { "Все" };
            SessionIds = new ObservableCollection<string> { "Все" };
            LogLevels = new ObservableCollection<LogLevelFilter>
            {
                LogLevelFilter.All,
                LogLevelFilter.Trace,
                LogLevelFilter.Debug,
                LogLevelFilter.Information,
                LogLevelFilter.Warning,
                LogLevelFilter.Error,
                LogLevelFilter.Critical
            };

            ClearLogsCommand = new DelegateCommand(ClearLogs);
            ReconnectCommand = new DelegateCommand(async () => await ReloadLogsAsync());

            // Таймер для постоянного опроса новых логов (каждую секунду)
            _pollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _pollingTimer.Tick += async (s, e) => await PollNewLogsAsync();

            // Включаем коллекционную синхронизацию для многопоточности
            BindingOperations.EnableCollectionSynchronization(_filteredLogs, _lockObject);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            // Загружаем все исторические логи
            await LoadHistoricalLogsAsync();

            // Запускаем таймер для постоянного опроса новых логов
            _pollingTimer.Start();
            IsConnected = true;
        }

        private async Task LoadHistoricalLogsAsync()
        {
            try
            {
                Console.WriteLine("[API] Loading historical logs...");
                var response = await _loggingApiService.QueryLogsAsync();

                if (response.Logs.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Сортируем по времени: старые сначала, т.к. Insert(0) перевернёт порядок
                        foreach (var logEntry in response.Logs.OrderBy(l => l.Timestamp))
                        {
                            var logViewModel = new LogEntryViewModel(logEntry);
                            _allLogs.Insert(0, logViewModel);

                            // Добавляем приложение в список
                            if (!Applications.Contains(logEntry.Application) && !string.IsNullOrEmpty(logEntry.Application))
                            {
                                Applications.Add(logEntry.Application);
                            }

                            // Добавляем категорию в список
                            if (!Categories.Contains(logEntry.Category) && !string.IsNullOrEmpty(logEntry.Category))
                            {
                                Categories.Add(logEntry.Category);
                            }

                            // Добавляем SessionId в список
                            var sessionIdStr = logEntry.SessionId.ToString();
                            if (logEntry.SessionId != Guid.Empty && !SessionIds.Contains(sessionIdStr))
                            {
                                SessionIds.Add(sessionIdStr);
                            }

                            // Проверяем фильтры
                            if (PassesFilters(logViewModel))
                            {
                                lock (_lockObject)
                                {
                                    FilteredLogs.Insert(0, logViewModel);
                                }
                            }
                        }

                        TotalLogsCount = _allLogs.Count;

                        // Сохраняем timestamp последнего лога
                        if (_allLogs.Count > 0)
                        {
                            _lastLogTimestamp = _allLogs.Max(l => l.Timestamp);
                        }

                        Console.WriteLine($"[API] Loaded {_allLogs.Count} historical logs");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error loading historical logs: {ex.Message}");
                IsConnected = false;
            }
        }

        private async Task PollNewLogsAsync()
        {
            try
            {
                // Запрашиваем только новые логи после последнего timestamp
                var request = new LogQueryRequest
                {
                    StartDate = _lastLogTimestamp,
                    Take = 1000
                };

                var response = await _loggingApiService.QueryLogsAsync(request);

                if (response.Logs.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var logEntry in response.Logs.Where(l => l.Timestamp > _lastLogTimestamp).OrderBy(l => l.Timestamp))
                        {
                            var logViewModel = new LogEntryViewModel(logEntry);
                            _allLogs.Insert(0, logViewModel);

                            // Добавляем приложение в список
                            if (!Applications.Contains(logEntry.Application) && !string.IsNullOrEmpty(logEntry.Application))
                            {
                                Applications.Add(logEntry.Application);
                            }

                            // Добавляем категорию в список
                            if (!Categories.Contains(logEntry.Category) && !string.IsNullOrEmpty(logEntry.Category))
                            {
                                Categories.Add(logEntry.Category);
                            }

                            // Добавляем SessionId в список
                            var sessionIdStr = logEntry.SessionId.ToString();
                            if (logEntry.SessionId != Guid.Empty && !SessionIds.Contains(sessionIdStr))
                            {
                                SessionIds.Add(sessionIdStr);
                            }

                            // Проверяем фильтры
                            if (PassesFilters(logViewModel))
                            {
                                lock (_lockObject)
                                {
                                    FilteredLogs.Insert(0, logViewModel);
                                }
                            }

                            // Обновляем timestamp
                            if (logEntry.Timestamp > _lastLogTimestamp)
                            {
                                _lastLogTimestamp = logEntry.Timestamp;
                            }
                        }

                        TotalLogsCount = _allLogs.Count;

                        // Ограничиваем количество логов в памяти
                        if (_allLogs.Count > 10000)
                        {
                            var toRemove = _allLogs.Skip(10000).ToList();
                            foreach (var log in toRemove)
                            {
                                _allLogs.Remove(log);
                            }
                        }
                    });
                }

                IsConnected = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Polling] Error: {ex.Message}");
                IsConnected = false;
            }
        }

        private async Task ReloadLogsAsync()
        {
            lock (_lockObject)
            {
                _allLogs.Clear();
                FilteredLogs.Clear();
                Applications.Clear();
                Categories.Clear();
                SessionIds.Clear();
                Applications.Add("Все");
                Categories.Add("Все");
                SessionIds.Add("Все");
                TotalLogsCount = 0;
                _lastLogTimestamp = DateTime.MinValue;
            }

            await LoadHistoricalLogsAsync();
        }

        private bool PassesFilters(LogEntryViewModel log)
        {
            // Фильтр по дате (только выбранный день)
            if (SelectedDate.HasValue)
            {
                var logDate = log.Timestamp.Date;
                var selectedDate = SelectedDate.Value.Date;
                if (logDate != selectedDate)
                {
                    return false;
                }
            }

            // Фильтр по приложению
            if (!string.IsNullOrEmpty(SelectedApplication) &&
                SelectedApplication != "Все" &&
                log.Application != SelectedApplication)
            {
                return false;
            }

            // Фильтр по категории
            if (!string.IsNullOrEmpty(SelectedCategory) &&
                SelectedCategory != "Все" &&
                log.Category != SelectedCategory)
            {
                return false;
            }

            // Фильтр по SessionId
            if (!string.IsNullOrEmpty(SelectedSessionId) &&
                SelectedSessionId != "Все" &&
                log.SessionId.ToString() != SelectedSessionId)
            {
                return false;
            }

            // Фильтр по уровню логирования
            if (SelectedLogLevel?.Level.HasValue == true && log.Level < SelectedLogLevel.Level.Value)
            {
                return false;
            }

            // Поиск по тексту
            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLower();
                return log.Message.ToLower().Contains(searchLower) ||
                       log.Category.ToLower().Contains(searchLower) ||
                       (log.Exception?.ToLower().Contains(searchLower) ?? false);
            }

            return true;
        }

        private void ApplyFilters()
        {
            lock (_lockObject)
            {
                FilteredLogs.Clear();
                foreach (var log in _allLogs.Where(PassesFilters))
                {
                    FilteredLogs.Add(log);
                }
            }
        }

        private void ClearLogs()
        {
            lock (_lockObject)
            {
                _allLogs.Clear();
                FilteredLogs.Clear();
                TotalLogsCount = 0;
            }
        }
    }
}
