namespace LoggingService.Shared.Models
{
    public class LogQueryResponse
    {
        public List<LogEntry> Logs { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
