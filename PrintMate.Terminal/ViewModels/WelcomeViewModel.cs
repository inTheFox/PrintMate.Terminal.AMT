using HandyControl.Controls;
using HandyControl.Tools.Command;
using Microsoft.AspNetCore.Authorization;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PrintMate.Terminal.ViewModels
{
    public class WelcomeViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly KeyboardService _keyboardService;

        private string _login;
        private string _password;

        // Событие для запуска анимации приветствия
        public event Action<string> OnLoginSuccess;

        public string Login
        {
            get => _login;
            set => SetProperty(ref _login, value);
        }
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public ICommand NextCommand { get; }
        public RelayCommand ClickLoginCommand { get; set; }
        public RelayCommand ClickPasswordCommand { get; set; }
        public RelayCommand LoginCommand { get; set; }

        private readonly NotificationService _notificationService;
        private readonly PrintSessionService _printSessionService;
        private readonly IEventAggregator _eventAggregator;
        private readonly AuthorizationService _authorizationService;

        public WelcomeViewModel(
            IRegionManager regionManager,
            KeyboardService keyboardService,
            NotificationService notificationService,
            PrintSessionService printSessionService,
            AuthorizationService authorizationService,
            IEventAggregator eventAggregator)
        {
            _authorizationService = authorizationService;
            _regionManager = regionManager;
            _keyboardService = keyboardService;
            _notificationService = notificationService;
            _printSessionService = printSessionService;
            _eventAggregator = eventAggregator;

            ClickLoginCommand = new RelayCommand(ClickToLoginCallback);
            ClickPasswordCommand = new RelayCommand(ClickToPasswordCallback);
            LoginCommand = new RelayCommand(LoginCommandCallback);

            // Подписываемся на событие выхода из системы
            _eventAggregator.GetEvent<OnUserQuit>().Subscribe(OnUserLogout);
        }

        private void OnUserLogout()
        {
            // Сбрасываем поля при выходе
            Login = string.Empty;
            Password = string.Empty;
        }

        private async void LoginCommandCallback(object obj)
        {
            if (string.IsNullOrEmpty(Login))
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Вы не ввели логин");
                return;
            }
            if (string.IsNullOrEmpty(Password))
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Вы не ввели пароль");
                return;
            }
            if (await _authorizationService.LoginAsync(Login, Password))
            {
                var user = _authorizationService.GetUser();

                // Запускаем анимацию приветствия
                Console.WriteLine("SUCCESS LOGIN");
                OnLoginSuccess?.Invoke(user.Name);
            }
            else
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Неверный логин или пароль");
            }
        }

        private void ClickToPasswordCallback(object obj)
        {
            Password = _keyboardService.Show(KeyboardType.Full, "Введите пароль", Password);
        }

        private void ClickToLoginCallback(object obj)
        {
            Login = _keyboardService.Show(KeyboardType.Full, "Введите логин", Login);
        }

        /// <summary>
        /// Завершает анимацию приветствия и переходит к основному приложению
        /// </summary>
        public async Task CompleteWelcomeAnimationAsync()
        {
            await _notificationService.AddInfoAsync("Система", $"{_authorizationService.GetUser().Name} авторизовался в системе");

            // Переключаем UI, чтобы создались все view
            _regionManager.RequestNavigate("RootRegion", nameof(RootContainer));
            _regionManager.RequestNavigate(Bootstrapper.LeftBarRegion, nameof(LeftBarView));
            _regionManager.RequestNavigate(Bootstrapper.RightBarRegion, nameof(RightBarView));
            _regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(ManualControl));

            // Публикуем событие авторизации для обновления UI
            _authorizationService.Join();

            // Проверяем незавершённые сессии печати после авторизации
            await CheckForInterruptedSessionsAsync();
        }

        /// <summary>
        /// Проверяет наличие незавершённых сессий печати и уведомляет оператора
        /// </summary>
        private async Task CheckForInterruptedSessionsAsync()
        {
            var interruptedSession = await _printSessionService.GetUnfinishedSessionAsync();
            if (interruptedSession != null)
            {
                System.Console.WriteLine($"[WelcomeViewModel] Обнаружена прерванная сессия печати!");
                System.Console.WriteLine($"  Проект: {interruptedSession.ProjectName}");
                System.Console.WriteLine($"  Начало: {interruptedSession.StartedAt}");
                System.Console.WriteLine($"  Последний слой: {interruptedSession.LastCompletedLayer + 1}/{interruptedSession.TotalLayers}");

                // Публикуем событие для UI (модальное окно или уведомление)
                _eventAggregator.GetEvent<OnInterruptedSessionDetectedEvent>().Publish(interruptedSession);

                await CustomMessageBox.ShowErrorAsync("Прерванная печать",
                    $"Обнаружена незавершённая печать проекта \"{interruptedSession.ProjectName}\". " +
                    $"Напечатано {interruptedSession.LastCompletedLayer + 1} из {interruptedSession.TotalLayers} слоёв.");
                await _printSessionService.ShowOn(interruptedSession);
            }
        }
    }
}