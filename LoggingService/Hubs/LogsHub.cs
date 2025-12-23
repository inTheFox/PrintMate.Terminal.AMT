using LoggingService.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using LogLevel = LoggingService.Shared.Models.LogLevel;

namespace LoggingService.Hubs
{
    public class LogsHub : Hub
    {
        /// <summary>
        /// Отправляет новый лог всем подключенным клиентам
        /// </summary>
        public async Task BroadcastLogEntry(LogEntry logEntry)
        {
            await Clients.All.SendAsync("ReceiveLogEntry", logEntry);
        }

        /// <summary>
        /// Подписка на логи с определенным фильтром
        /// </summary>
        public async Task SubscribeToLogs(string? application = null, LogLevel? minLevel = null)
        {
            var filter = $"{application ?? "all"}:{minLevel?.ToString() ?? "all"}";
            await Groups.AddToGroupAsync(Context.ConnectionId, filter);
        }

        /// <summary>
        /// Отписка от логов
        /// </summary>
        public async Task UnsubscribeFromLogs(string? application = null, LogLevel? minLevel = null)
        {
            var filter = $"{application ?? "all"}:{minLevel?.ToString() ?? "all"}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, filter);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        }
    }
}
