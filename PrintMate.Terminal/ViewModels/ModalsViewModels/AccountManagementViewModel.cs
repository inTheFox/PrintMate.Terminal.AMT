using System.Threading.Tasks;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Interfaces;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views;
using PrintMate.Terminal.Views.Modals;
using Prism.Mvvm;
using Prism.Regions;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class AccountManagementViewModel : BindableBase, IViewModelForm
    {
        private readonly AuthorizationService _authorizationService;
        private readonly IRegionManager _regionManager;
        private User _currentUser;

        private string _name;
        private string _family;
        private string _login;

        private RelayCommand _changePasswordCommand;
        private RelayCommand _logoutCommand;
        private RelayCommand _closeFormCommand;

        public AccountManagementViewModel(AuthorizationService authorizationService, IRegionManager regionManager)
        {
            _authorizationService = authorizationService;
            _regionManager = regionManager;

            _currentUser = _authorizationService.GetUser();

            if (_currentUser != null)
            {
                Name = _currentUser.Name;
                Family = _currentUser.Family;
                Login = _currentUser.Login;
            }
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Family
        {
            get => _family;
            set => SetProperty(ref _family, value);
        }

        public string Login
        {
            get => _login;
            set => SetProperty(ref _login, value);
        }

        public RelayCommand ChangePasswordCommand
        {
            get => _changePasswordCommand ??= new RelayCommand(async obj =>
            {
                await OpenChangePasswordModal();
            });
        }

        public RelayCommand LogoutCommand
        {
            get => _logoutCommand ??= new RelayCommand(async obj =>
            {
                await Logout();
            });
        }

        public RelayCommand CloseFormCommand
        {
            get => _closeFormCommand ??= new RelayCommand(obj =>
            {
                CloseCommand?.Execute(null);
            });
        }

        public RelayCommand CloseCommand { get; set; }

        private async Task OpenChangePasswordModal()
        {
            await ModalService.Instance.ShowAsync<ChangePasswordView, ChangePasswordViewModel>(
                modalId: null,
                options: null,
                showOverlay: true,
                closeOnBackgroundClick: true
            );
        }

        private async Task Logout()
        {
            var result = await CustomMessageBox.ShowQuestionAsync("Подтверждение", "Вы уверены, что хотите выйти?");
            if (result == Models.MessageBoxResult.Yes)
            {
                await _authorizationService.Logout();

                // Закрываем модальное окно
                CloseCommand?.Execute(null);

                // Сбрасываем состояние WelcomeView и показываем его
                WelcomeView.Instance?.ResetState();

                // Возвращаемся на экран авторизации
                _regionManager.RequestNavigate("RootRegion", nameof(WelcomeView));
            }
        }
    }
}
