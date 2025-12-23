using LoggingService.Shared.Models;

namespace LogViewerApp.Models
{
    public class LogLevelFilter
    {
        public LogLevel? Level { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        public static LogLevelFilter All => new LogLevelFilter { Level = null, DisplayName = "Все уровни" };
        public static LogLevelFilter Trace => new LogLevelFilter { Level = LogLevel.Trace, DisplayName = "Trace" };
        public static LogLevelFilter Debug => new LogLevelFilter { Level = LogLevel.Debug, DisplayName = "Debug" };
        public static LogLevelFilter Information => new LogLevelFilter { Level = LogLevel.Information, DisplayName = "Information" };
        public static LogLevelFilter Warning => new LogLevelFilter { Level = LogLevel.Warning, DisplayName = "Warning" };
        public static LogLevelFilter Error => new LogLevelFilter { Level = LogLevel.Error, DisplayName = "Error" };
        public static LogLevelFilter Critical => new LogLevelFilter { Level = LogLevel.Critical, DisplayName = "Critical" };
    }
}
