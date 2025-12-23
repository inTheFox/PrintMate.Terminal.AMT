using HandyControl.Tools.Command;
using PrintMate.Terminal.Services;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using PrintMate.Terminal.Events;

namespace PrintMate.Terminal.ViewModels
{
    public class LogViewModel : BindableBase
    {
        private ObservableCollection<LogEntry> _logEntries;
        public ObservableCollection<LogEntry> LogEntries
        {
            get => _logEntries;
            set => SetProperty(ref _logEntries, value);
        }

        private ObservableCollection<LogEntry> _logEntriesMemory;
        public ObservableCollection<LogEntry> LogEntriesMemory
        {
            get => _logEntriesMemory;
            set => SetProperty(ref _logEntriesMemory, value);
        }

        private static object LockEntries = new object();

        private readonly OnLoggerMessageEvent _loggerMessageEvent;

        private bool _infoEnabled = true;
        private bool _warningEnabled = true;
        private bool _errorEnabled = true;
        private bool _successEnabled = true;

        public bool InfoEnabled
        {
            set
            {
                SetProperty(ref _infoEnabled, value);
                CheckedUpdated();
            }
            get => _infoEnabled;
        }
        public bool WarningEnabled
        {
            set
            {
                SetProperty(ref _warningEnabled, value);
                CheckedUpdated();
            }
            get => _warningEnabled;
        }
        public bool ErrorEnabled
        {
            set
            {
                SetProperty(ref _errorEnabled, value);
                CheckedUpdated();
            }
            get => _errorEnabled;
        }
        public bool SuccessEnabled
        {
            set
            {
                SetProperty(ref _successEnabled, value);
                CheckedUpdated();
            }
            get => _successEnabled;
        }

        public RelayCommand<bool> OnCheckedChanged;


        public LogViewModel(IEventAggregator eventAggregator)
        {
            LogEntriesMemory = new ObservableCollection<LogEntry>();
            LogEntries = new ObservableCollection<LogEntry>();

            _loggerMessageEvent = eventAggregator.GetEvent<OnLoggerMessageEvent>();
            _loggerMessageEvent.Subscribe((message) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AddLog(message);
                });
            });
        }

        private void CheckedUpdated()
        {
            lock (LockEntries)
            {
                LogEntries.Clear();
                LogEntries.AddRange(LogEntriesMemory.Where((p) =>
                {
                    switch (p.EntryType)
                    {
                        case LogMessageType.Error:
                            if (ErrorEnabled) return true;
                            break;
                        case LogMessageType.Info:
                            if (InfoEnabled) return true;
                            break;
                        case LogMessageType.Success:
                            if (SuccessEnabled) return true;
                            break;
                        case LogMessageType.Warning:
                            if (WarningEnabled) return true;
                            break;
                        default:
                            break;
                    }
                    return false;
                }));
            }
        }

        // Метод для добавления сообщений
        private void AddLog(LoggerMessage message)
        {
            lock (LockEntries)
            {
                var entry = new LogEntry(message.Message, message.MessageType);

                LogEntriesMemory.Add(entry);
                if (LogEntriesMemory.Count > 100000)
                {
                    LogEntriesMemory.RemoveAt(0);
                }
                if (IsLogMessageTypeEnabled(message.MessageType))
                {
                    LogEntries.Add(entry);
                }
            }
            CheckedUpdated();
        }

        private bool IsLogMessageTypeEnabled(LogMessageType logMessageType)
        {
            switch (logMessageType)
            {
                case LogMessageType.Error:
                    if (ErrorEnabled) return true;
                    break;
                case LogMessageType.Info:
                    if (InfoEnabled) return true;
                    break;
                case LogMessageType.Success:
                    if (SuccessEnabled) return true;
                    break;
                case LogMessageType.Warning:
                    if (WarningEnabled) return true;
                    break;
                default:
                    break;
            }

            return false;
        }
    }
}
