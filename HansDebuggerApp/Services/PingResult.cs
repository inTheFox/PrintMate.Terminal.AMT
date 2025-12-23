namespace HansDebuggerApp.Services;

public class PingResult
{
    public bool Success { get; set; }
    public string Status { get; set; }
    public long RoundtripTime { get; set; }
    public string Address { get; set; }
    public int Ttl { get; set; }
}