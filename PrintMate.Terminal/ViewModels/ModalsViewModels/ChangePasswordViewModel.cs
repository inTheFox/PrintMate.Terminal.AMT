using System;
using System.Threading.Tasks;
using System.Windows;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Interfaces;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Services;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class ChangePasswordViewModel : BindableBase, IViewModelForm
    {
        private readonly AuthorizationService _authorizationService;
        private readonly UserService _userService;
        private User _currentUser;

        private string _oldPassword;
        private string _newPassword;
        private string _confirmPassword;

        private Visibility _oldPasswordDangerVisibility;
        private Visibility _newPasswordDangerVisibility;
        private Visibility _confirmPasswordDangerVisibility;

        private RelayCommand _changePasswordCommand;
        private RelayCommand _cancelCommand;

        public ChangePasswordViewModel(AuthorizationService authorizationService, UserService userService)
        {
            _authorizationService = authorizationService;
            _userService = userService;

            _currentUser = _authorizationService.GetUser();

            OldPasswordDangerVisibility = Visibility.Collapsed;
            NewPasswordDangerVisibility = Visibility.Collapsed;
            ConfirmPasswordDangerVisibility = Visibility.Collapsed;
        }

        public string OldPassword
        {
            get => _oldPassword;
            set
            {
                OldPasswordDangerVisibility = string.IsNullOrEmpty(value) ? Visibility.Visible : Visibility.Collapsed;
                SetProperty(ref _oldPassword, value);
            }
        }

        public string NewPassword
        {
            get => _newPassword;
            set
            {
                NewPasswordDangerVisibility = string.IsNullOrEmpty(value) ? Visibility.Visible : Visibility.Collapsed;
                SetProperty(ref _newPassword, value);
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                ConfirmPasswordDangerVisibility = string.IsNullOrEmpty(value) || value != NewPassword
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                SetProperty(ref _confirmPassword, value);
            }
        }

        public Visibility OldPasswordDangerVisibility
        {
            get => _oldPasswordDangerVisibility;
            set => SetProperty(ref _oldPasswordDangerVisibility, value);
        }

        public Visibility NewPasswordDangerVisibility
        {
            get => _newPasswordDangerVisibility;
            set => SetProperty(ref _newPasswordDangerVisibility, value);
        }

        public Visibility ConfirmPasswordDangerVisibility
        {
            get => _confirmPasswordDangerVisibility;
            set => SetProperty(ref _confirmPasswordDangerVisibility, value);
        }

        public RelayCommand ChangePasswordCommand
        {
            get => _changePasswordCommand ??= new RelayCommand(async obj =>
            {
                await ChangePassword();
            });
        }

        public RelayCommand CancelCommand
        {
            get => _cancelCommand ??= new RelayCommand(obj =>
            {
                CloseCommand?.Execute(null);
            });
        }

        public RelayCommand CloseCommand { get; set; }

        private async Task ChangePassword()
        {
            if (string.IsNullOrEmpty(OldPassword) || string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
            {
                await CustomMessageBox.ShowWarningAsync("Ошибка", "Заполните все поля для смены пароля");
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                await CustomMessageBox.ShowWarningAsync("Ошибка", "Пароли не совпадают");
                return;
            }

            // Проверка для root-профиля
            if (_authorizationService.IsRootAuthorized())
            {
                await CustomMessageBox.ShowWarningAsync("Ошибка", "Невозможно изменить пароль root-профиля");
                return;
            }

            // Проверка старого пароля
            if (_currentUser.Password != OldPassword)
            {
                await CustomMessageBox.ShowWarningAsync("Ошибка", "Неверный текущий пароль");
                return;
            }

            try
            {
                _currentUser.Password = NewPassword;
                await _userService.Update(_currentUser);
                await CustomMessageBox.ShowSuccessAsync("Успех", "Пароль успешно изменен");

                // Закрываем окно после успешной смены пароля
                CloseCommand?.Execute(null);
            }
            catch (Exception ex)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", $"Ошибка при смене пароля: {ex.Message}");
            }
        }
    }
}
