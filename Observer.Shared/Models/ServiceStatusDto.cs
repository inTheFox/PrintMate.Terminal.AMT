namespace Observer.Shared.Models
{
    public class ServiceStatusDto
    {
        public string Id { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string StartupArguments { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public int? ProcessId { get; set; }
        public bool AutoRestartEnabled { get; set; } = true;
    }
}
