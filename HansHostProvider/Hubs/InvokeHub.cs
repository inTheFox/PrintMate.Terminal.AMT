using Hans.NET.Models;
using HansHostProvider.Services;
using HansHostProvider.Shared;
using Microsoft.AspNetCore.SignalR;
using ScanatorStatus = HansHostProvider.Shared.ScanatorStatus;

namespace HansHostProvider.Hubs
{
    public class InvokeHub : Hub
    {
        private readonly HansService _hansService;

        public InvokeHub(HansService hansService)
        {
            _hansService = hansService ?? throw new ArgumentNullException(nameof(hansService));
        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"[InvokeHub] Client connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"[InvokeHub] Client disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }

        // Configuration
        public Task LoadConfiguration(ScanatorConfiguration config)
        {
            ServiceContext.Configuration = config;
            _hansService.ApplyConfiguration(config);
            return Task.CompletedTask;
        }

        public bool IsConfigurationLoaded() => ServiceContext.Configuration != null;
        public ScanatorConfiguration? GetConfiguration() => ServiceContext.Configuration;

        // Connection info
        public string GetHostAddress() => ServiceContext.Configuration?.CardInfo.IpAddress ?? string.Empty;
        public int GetFixedIndex() => ServiceContext.Configuration?.CardInfo.SeqIndex ?? -1;
        public bool IsHansSdkInitialized() => _hansService.IsSdkInitialized;
        public bool IsConnected() => _hansService.IsConnected;
        public int GetBoardIndex() => _hansService.BoardIndex;

        // Progress
        public int GetDownloadProgress() => _hansService.DownloadProgress;
        public int GetMarkingProgress() => _hansService.MarkProgress;
        public bool IsDownloadMarkFileFinish() => _hansService.IsDownloadMarkFileFinish;

        // Mark file operations
        public Task<bool> DownloadMarkFile(string path) =>
            Task.FromResult(_hansService.DownloadMarkFile(path));

        // Marking control
        public Task StartMark()
        {
            _hansService.StartMark();
            return Task.CompletedTask;
        }

        public Task StopMark()
        {
            _hansService.StopMark();
            return Task.CompletedTask;
        }

        public Task PauseMark()
        {
            _hansService.Pause();
            return Task.CompletedTask;
        }

        // Status
        public ScanatorStatus GetStatus() => new()
        {
            IsConnected = _hansService.IsConnected,
            IsMarking = _hansService.IsMarking,
            IsMarkFinish = _hansService.IsMarkComplete,
            MarkProgress = _hansService.MarkProgress,
            DownloadProgress = _hansService.DownloadProgress,
            IsDownloadFinish = _hansService.IsDownloadMarkFileFinish,
            WorkingStatus = (int)_hansService.WorkingStatus
        };
    }
}
