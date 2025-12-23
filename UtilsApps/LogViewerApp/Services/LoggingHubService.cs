using LoggingService.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;

namespace LogViewerApp.Services
{
    public class LoggingHubService : IDisposable
    {
        private HubConnection? _hubConnection;
        private readonly string _hubUrl;
        private bool _isConnecting;

        public event EventHandler<LogEntry>? LogReceived;
        public event EventHandler<bool>? ConnectionStateChanged;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public LoggingHubService()
        {
            _hubUrl = $"{Observer.Shared.Models.Services.LoggingService.Url}/logsHub";
        }

        public async Task ConnectAsync()
        {
            if (_isConnecting || IsConnected)
                return;

            _isConnecting = true;

            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl)
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                    .Build();

                _hubConnection.On<LogEntry>("ReceiveLogEntry", (logEntry) =>
                {
                    Console.WriteLine($"[SignalR] Received log: {logEntry.Level} - {logEntry.Message}");
                    LogReceived?.Invoke(this, logEntry);
                });

                _hubConnection.Reconnecting += error =>
                {
                    ConnectionStateChanged?.Invoke(this, false);
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += connectionId =>
                {
                    ConnectionStateChanged?.Invoke(this, true);
                    return Task.CompletedTask;
                };

                _hubConnection.Closed += error =>
                {
                    ConnectionStateChanged?.Invoke(this, false);
                    return Task.CompletedTask;
                };

                await _hubConnection.StartAsync();
                Console.WriteLine($"[SignalR] Connected successfully to {_hubUrl}");
                ConnectionStateChanged?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to SignalR hub: {ex.Message}");
                ConnectionStateChanged?.Invoke(this, false);
            }
            finally
            {
                _isConnecting = false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
                ConnectionStateChanged?.Invoke(this, false);
            }
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
        }
    }
}
