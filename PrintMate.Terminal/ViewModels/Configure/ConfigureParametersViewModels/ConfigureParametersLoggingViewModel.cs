using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LoggingService.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Observer.Shared.Models;
using Prism.Commands;
using Prism.Mvvm;
using LogLevel = LoggingService.Shared.Models.LogLevel;
using LogEntry = LoggingService.Shared.Models.LogEntry;
using LogQueryRequest = LoggingService.Shared.Models.LogQueryRequest;

namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels
{
    public class LogLevelItem
    {
        public string Name { get; set; }
        public LogLevel? Value { get; set; }
    }

    public class ConfigureParametersLoggingViewModel : BindableBase
    {
        private readonly LoggingClient _loggingClient;
        private bool _isRealTimeEnabled = true;

        #region Observable Properties

        private ObservableCollection<LogEntryViewModel> _logs = new();
        public ObservableCollection<LogEntryViewModel> Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }

        public ObservableCollection<LogLevelItem> LogLevels { get; } = new()
        {
            new LogLevelItem { Name = "All", Value = null },
            new LogLevelItem { Name = "Trace", Value = LogLevel.Trace },
            new LogLevelItem { Name = "Debug", Value = LogLevel.Debug },
            new LogLevelItem { Name = "Information", Value = LogLevel.Information },
            new LogLevelItem { Name = "Warning", Value = LogLevel.Warning },
            new LogLevelItem { Name = "Error", Value = LogLevel.Error },
            new LogLevelItem { Name = "Critical", Value = LogLevel.Critical }
        };

        private LogLevelItem _selectedLogLevelItem;
        public LogLevelItem SelectedLogLevelItem
        {
            get => _selectedLogLevelItem;
            set
            {
                if (SetProperty(ref _selectedLogLevelItem, value))
                {
                    SelectedLogLevel = value?.Value;
                }
            }
        }

        private DateTime _startDate = DateTime.Now.AddDays(-1);
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        private LogLevel? _selectedLogLevel;
        public LogLevel? SelectedLogLevel
        {
            get => _selectedLogLevel;
            set => SetProperty(ref _selectedLogLevel, value);
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private int _pageSize = 100;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                    _ = LoadLogsAsync();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand LoadLogsCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        public ConfigureParametersLoggingViewModel()
        {
            _loggingClient = new LoggingClient("PrintMate.Terminal");
            _selectedLogLevelItem = LogLevels[0]; // "All"

            LoadLogsCommand = new DelegateCommand(async () => await LoadLogsAsync());
            PreviousPageCommand = new DelegateCommand(PreviousPage, CanGoPrevious).ObservesProperty(() => CurrentPage);
            NextPageCommand = new DelegateCommand(NextPage, CanGoNext).ObservesProperty(() => CurrentPage).ObservesProperty(() => TotalPages);
            RefreshCommand = new DelegateCommand(async () => await LoadLogsAsync());

            // Подписка на событие получения новых логов
            _loggingClient.LogReceived += OnNewLogEntry;

            // Запуск SignalR подключения
            _ = _loggingClient.StartRealTimeConnectionAsync();

            // Автозагрузка при инициализации
            _ = LoadLogsAsync();
        }

        private void OnNewLogEntry(object? sender, LoggingService.Shared.Models.LogEntry logEntry)
        {
            if (!_isRealTimeEnabled) return;

            // Проверяем фильтры
            if (SelectedLogLevel.HasValue && logEntry.Level < SelectedLogLevel.Value)
                return;

            if (!string.IsNullOrWhiteSpace(SearchText) &&
                !logEntry.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
                !logEntry.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                return;

            // Добавляем лог в начало списка в UI потоке
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var viewModel = new LogEntryViewModel(logEntry);
                Logs.Insert(0, viewModel);

                // Ограничиваем размер коллекции
                if (Logs.Count > PageSize)
                {
                    Logs.RemoveAt(Logs.Count - 1);
                }

                TotalCount++;
            });
        }

        private async Task LoadLogsAsync()
        {
            IsLoading = true;
            try
            {
                var request = new LogQueryRequest
                {
                    StartDate = StartDate,
                    EndDate = EndDate,
                    SearchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                    MinLevel = SelectedLogLevel,
                    Skip = (CurrentPage - 1) * PageSize,
                    Take = PageSize
                };

                var response = await _loggingClient.QueryLogsAsync(request);

                Logs = new ObservableCollection<LogEntryViewModel>(
                    response.Logs.Select(log => new LogEntryViewModel(log))
                );

                TotalCount = response.TotalCount;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading logs: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void PreviousPage()
        {
            if (CanGoPrevious())
            {
                CurrentPage--;
                _ = LoadLogsAsync();
            }
        }

        private void NextPage()
        {
            if (CanGoNext())
            {
                CurrentPage++;
                _ = LoadLogsAsync();
            }
        }

        private bool CanGoPrevious() => CurrentPage > 1;
        private bool CanGoNext() => CurrentPage < TotalPages;

        public void Dispose()
        {
            // Отписываемся от события
            _loggingClient.LogReceived -= OnNewLogEntry;
            _loggingClient?.Dispose();
        }
    }

    public class LogEntryViewModel
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Application { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public string? Exception { get; set; }

        public string LevelText => Level switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "FATAL",
            _ => "UNKNOWN"
        };

        public string LevelColor => Level switch
        {
            LogLevel.Trace => "#999999",
            LogLevel.Debug => "#6A9955",
            LogLevel.Information => "#4EC9B0",
            LogLevel.Warning => "#DCDCAA",
            LogLevel.Error => "#F48771",
            LogLevel.Critical => "#FF0000",
            _ => "#FFFFFF"
        };

        public LogEntryViewModel(LoggingService.Shared.Models.LogEntry log)
        {
            Timestamp = log.Timestamp;
            Level = log.Level;
            Application = log.Application ?? string.Empty;
            Category = log.Category ?? string.Empty;
            Message = log.Message ?? string.Empty;
            Exception = log.Exception;
        }
    }
}
