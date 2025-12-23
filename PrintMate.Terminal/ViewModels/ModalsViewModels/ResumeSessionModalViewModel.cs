using HandyControl.Tools.Command;
using PrintMate.Terminal.Services;
using PrintSpectator.Shared.Models;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    /// <summary>
    /// Результат выбора в модальном окне возобновления сессии
    /// </summary>
    public enum ResumeSessionResult
    {
        None,
        Resume,
        StartNew,
        Cancel
    }

    /// <summary>
    /// ViewModel для модального окна возобновления прерванной сессии печати
    /// </summary>
    public class ResumeSessionModalViewModel : BindableBase
    {
        private PrintSession _session;
        public PrintSession Session
        {
            get => _session;
            set
            {
                if (SetProperty(ref _session, value))
                {
                    RaisePropertyChanged(nameof(CompletedLayers));
                    RaisePropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        public int CompletedLayers => Session?.LastCompletedLayer + 1 ?? 0;

        public double ProgressPercentage
        {
            get
            {
                if (Session == null || Session.TotalLayers == 0)
                    return 0;
                return ((double)CompletedLayers / Session.TotalLayers) * 100;
            }
        }

        public ResumeSessionResult Result { get; set; } = ResumeSessionResult.None;

        public RelayCommand ResumeCommand { get; }
        public RelayCommand StartNewCommand { get; }
        public RelayCommand CancelCommand { get; }

        public ResumeSessionModalViewModel()
        {
            ResumeCommand = new RelayCommand(_ => OnResume());
            StartNewCommand = new RelayCommand(_ => OnStartNew());
            CancelCommand = new RelayCommand(_ => OnCancel());
        }

        private void OnResume()
        {
            Result = ResumeSessionResult.Resume;
            CloseModal();
        }

        private void OnStartNew()
        {
            Result = ResumeSessionResult.StartNew;
            CloseModal();
        }

        private void OnCancel()
        {
            Result = ResumeSessionResult.Cancel;
            CloseModal();
        }

        private void CloseModal()
        {
            ModalService.Instance?.Close();
        }
    }
}
