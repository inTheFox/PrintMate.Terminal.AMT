using LoggingService.Shared.Models;
using Prism.Mvvm;

namespace LogViewerApp.Models
{
    public class LogEntryViewModel : BindableBase
    {
        private readonly LogEntry _logEntry;

        public LogEntryViewModel(LogEntry logEntry)
        {
            _logEntry = logEntry;
        }

        public Guid SessionId => _logEntry.SessionId;
        public DateTime Timestamp => _logEntry.Timestamp;
        public LogLevel Level => _logEntry.Level;
        public string Application => _logEntry.Application;
        public string Category => _logEntry.Category;
        public string Message => _logEntry.Message;
        public string? Exception => _logEntry.Exception;
        public Dictionary<string, object>? Properties => _logEntry.Properties;

        public string SessionIdShort => SessionId == Guid.Empty ? "-" : SessionId.ToString().Substring(0, 8);
        public string TimeFormatted => Timestamp.ToString("HH:mm:ss.fff");
        public string DateFormatted => Timestamp.ToString("dd.MM.yyyy");

        public string LevelText => Level.ToString();

        public string LevelColor => Level switch
        {
            LogLevel.Trace => "#9E9E9E",
            LogLevel.Debug => "#2196F3",
            LogLevel.Information => "#4CAF50",
            LogLevel.Warning => "#FF9800",
            LogLevel.Error => "#F44336",
            LogLevel.Critical => "#B71C1C",
            _ => "#000000"
        };

        public bool HasException => !string.IsNullOrEmpty(Exception);
    }
}
