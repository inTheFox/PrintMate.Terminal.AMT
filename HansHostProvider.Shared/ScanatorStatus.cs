namespace HansHostProvider.Shared
{
    public class ScanatorStatus
    {
        public bool IsConnected { get; set; }
        public bool IsMarking { get; set; }
        public string? LastError { get; set; }
        public bool IsMarkFinish { get; set; }
        public int WorkingStatus { get; set; }
        public int MarkProgress { get; set; }
        public int DownloadProgress { get; set; }
        public bool IsDownloadFinish { get; set; }
    }
}
