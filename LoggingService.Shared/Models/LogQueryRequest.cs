namespace LoggingService.Shared.Models
{
    public class LogQueryRequest
    {
        public string? Application { get; set; }
        public LogLevel? MinLevel { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchText { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 100;
    }
}
