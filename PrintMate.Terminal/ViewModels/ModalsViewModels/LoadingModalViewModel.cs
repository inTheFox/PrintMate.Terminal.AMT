using Prism.Mvvm;
using System.Windows;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class LoadingModalViewModel : BindableBase
    {
        private string _statusMessage = "Загрузка...";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private double _progress = 0;
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        private bool _isIndeterminate = true;
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set => SetProperty(ref _isIndeterminate, value);
        }

        private Visibility _progressBarVisibility = Visibility.Visible;
        public Visibility ProgressBarVisibility
        {
            get => _progressBarVisibility;
            set => SetProperty(ref _progressBarVisibility, value);
        }

        public void UpdateProgress(double progress, string message = null)
        {
            Progress = progress;
            IsIndeterminate = false;

            if (!string.IsNullOrEmpty(message))
            {
                StatusMessage = message;
            }
        }

        public void SetIndeterminate(string message = null)
        {
            IsIndeterminate = true;

            if (!string.IsNullOrEmpty(message))
            {
                StatusMessage = message;
            }
        }
    }
}
