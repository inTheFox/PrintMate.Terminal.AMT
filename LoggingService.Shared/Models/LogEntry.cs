namespace LoggingService.Shared.Models
{
    public class LogEntry
    {
        public Guid SessionId { get; set; } = Guid.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; }
        public string Application { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
    }
}
