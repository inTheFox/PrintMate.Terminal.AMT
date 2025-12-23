using System;
using System.Windows;
using PrintMate.Terminal.Events;
using Prism.Events;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels;

public class AddProjectLoadingProgressViewModel : BindableBase
{
    private string _status;
    private int _statusProgress;
    private DateTime _lastProgressUpdate = DateTime.MinValue;

    public string Status
    {
        get => _status;
        set
        {
            Console.WriteLine($"[Status] {value}");
            SetProperty(ref _status, value);
        }
    }

    public int StatusProgress
    {
        get => _statusProgress;
        set
        {
            if (value > 100) value = 100;

            // Throttling: обновляем UI не чаще чем раз в 50ms
            var now = DateTime.Now;
            if ((now - _lastProgressUpdate).TotalMilliseconds < 50 && value < 100)
                return;

            _lastProgressUpdate = now;
            Console.WriteLine($"[Progress] StatusProgress = {value}%");
            SetProperty(ref _statusProgress, value);
        }
    }

    private readonly IEventAggregator _eventAggregator;
    public AddProjectLoadingProgressViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        _eventAggregator.GetEvent<OnProjectImportStatusChangedEvent>().Subscribe((status) =>
        {
            Application.Current.Dispatcher.InvokeAsync(() => Status = status);
        });
        _eventAggregator.GetEvent<OnProjectImportStatusProgressChangedEvent>().Subscribe((progress) =>
        {
            Application.Current.Dispatcher.InvokeAsync(() => StatusProgress = progress);
        });
    }
}