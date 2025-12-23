using PrintMate.Terminal.Events;
using PrintMate.Terminal.ViewModels;
using PrintMate.Terminal.Views;
using Prism.Events;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LoggingService.Client;
using LoggingService.Shared.Models;
using Application = System.Windows.Application;

namespace PrintMate.Terminal.Services
{
    public class LoggerService : LoggingClient
    {
        public LoggerService() : base("Terminal")
        {
        }

        public Task TraceAsync(object source, string message, Dictionary<string, object>? properties = null)
        {
            return LogAsync(LogLevel.Trace, source.GetType().Name, message, null, properties);
        }

        public Task DebugAsync(object source, string message, Dictionary<string, object>? properties = null)
        {
            return LogAsync(LogLevel.Debug, source.GetType().Name, message, null, properties);
        }

        public Task InformationAsync(object source, string message, Dictionary<string, object>? properties = null)
        {
            return LogAsync(LogLevel.Information, source.GetType().Name, message, null, properties);
        }

        public Task WarningAsync(object source, string message, Dictionary<string, object>? properties = null)
        {
            return LogAsync(LogLevel.Warning, source.GetType().Name, message, null, properties);
        }

        public Task ErrorAsync(object source, string message, Exception? exception = null, Dictionary<string, object>? properties = null)
        {
            return LogAsync(LogLevel.Error, source.GetType().Name, message, exception, properties);
        }

        public Task CriticalAsync(object source, string message, Exception? exception = null, Dictionary<string, object>? properties = null)
        {
            return LogAsync(LogLevel.Critical, source.GetType().Name, message, exception, properties);
        }
    }
}
