using System;
using PrintMate.Terminal.Services;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels;

public class LogEntry : BindableBase
{
    public DateTime Timestamp { get; }
    public string Message { get; }

    public LogMessageType EntryType { get; set; }

    public string TimestampString => Timestamp.ToString("HH:mm:ss.fff");

    public LogEntry(string message, LogMessageType entryType = LogMessageType.Info)
    {
        Message = message;
        EntryType = entryType;
        Timestamp = DateTime.Now;
    }
}