using HandyControl.Tools.Command;
using PrintMate.Terminal.Services;
using Prism.Mvvm;
using System.Windows;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    /// <summary>
    /// Результат выбора в модальном окне выхода
    /// </summary>
    public enum AppExitResult
    {
        None,
        Minimize,
        Close,
        Cancel
    }

    /// <summary>
    /// ViewModel для модального окна выхода/сворачивания приложения
    /// </summary>
    public class AppExitModalViewModel : BindableBase
    {
        public AppExitResult Result { get; set; } = AppExitResult.None;

        public RelayCommand MinimizeCommand { get; }
        public RelayCommand CloseAppCommand { get; }
        public RelayCommand CancelCommand { get; }

        public AppExitModalViewModel()
        {
            MinimizeCommand = new RelayCommand(_ => OnMinimize());
            CloseAppCommand = new RelayCommand(_ => OnCloseApp());
            CancelCommand = new RelayCommand(_ => OnCancel());
        }

        private void OnMinimize()
        {
            Result = AppExitResult.Minimize;
            CloseModal();
        }

        private void OnCloseApp()
        {
            Result = AppExitResult.Close;
            CloseModal();
        }

        private void OnCancel()
        {
            Result = AppExitResult.Cancel;
            CloseModal();
        }

        private void CloseModal()
        {
            ModalService.Instance?.Close();
        }
    }
}
