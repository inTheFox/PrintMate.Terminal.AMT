using HandyControl.Tools.Command;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Opc2Lib;
using PrintMate.Terminal.Opc;
using Prism.Mvvm;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HandyControl.Tools.Extension;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Services;
using Prism.Events;
using CommandResponse = PrintMate.Terminal.Opc.CommandResponse;
using PrintMate.Terminal.Models;

namespace PrintMate.Terminal.ViewModels
{
    public class LeftBarViewModel : BindableBase
    {
        private readonly Random _random = new Random();
        private ObservableCollection<CommandInfo> _indicators = new ObservableCollection<CommandInfo>();
        public ObservableCollection<CommandInfo> Indicators
        {
            get => _indicators;
            set => SetProperty(ref _indicators, value);
        }

        private readonly ILogicControllerProvider _client;
        private const int MaxPoints = 20;      // всего храним точек
        private const int VisiblePoints = 5;   // сколько видно на графике

        private readonly IEventAggregator _eventAggregator;
        private readonly MonitoringManager _monitoringManager;
        private readonly ILogicControllerObserver _observer;
        private readonly AuthorizationService _authorizationService;
        private readonly RolesService _rolesService;
        private readonly NotificationService _notificationService;

        private int _unreadNotificationsCount;
        public int UnreadNotificationsCount
        {
            get => _unreadNotificationsCount;
            set
            {
                SetProperty(ref _unreadNotificationsCount, value);
                RaisePropertyChanged(nameof(UnreadBadgeVisibility));
                RaisePropertyChanged(nameof(UnreadCountText));
            }
        }

        public Visibility UnreadBadgeVisibility => UnreadNotificationsCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        public string UnreadCountText => UnreadNotificationsCount > 99 ? "99+" : UnreadNotificationsCount.ToString();

        private Visibility _notFavouritesInfoVisibility = Visibility.Visible;
        public Visibility NotFavouritesInfoVisibility
        {
            get => _notFavouritesInfoVisibility;
            set => SetProperty(ref _notFavouritesInfoVisibility, value);
        }

        private string _userFullName = "Гость";
        public string UserFullName
        {
            get => _userFullName;
            set => SetProperty(ref _userFullName, value);
        }

        private string _userRole = "Не авторизован";
        public string UserRole
        {
            get => _userRole;
            set => SetProperty(ref _userRole, value);
        }

        public ObservableCollection<string> DataList { get; set; }

        public LeftBarViewModel(ILogicControllerProvider client, ILogicControllerObserver observer, IEventAggregator eventAggregator, MonitoringManager monitoringManager, AuthorizationService authorizationService, RolesService rolesService, NotificationService notificationService)
        {
            _observer = observer;
            _eventAggregator = eventAggregator;
            _monitoringManager = monitoringManager;
            _client = client;
            _authorizationService = authorizationService;
            _rolesService = rolesService;
            _notificationService = notificationService;

            eventAggregator.GetEvent<OnCommandAddToFavouritesEvent>().Subscribe((command) =>
            {
                Indicators.AddIfNotExists(command);
                if (Indicators.Count <= 0)
                {
                    NotFavouritesInfoVisibility = Visibility.Visible;
                }
                else
                {
                    NotFavouritesInfoVisibility = Visibility.Collapsed;
                }
            });
            eventAggregator.GetEvent<OnCommandRemoveFromFavouritesEvent>().Subscribe((command) =>
            {
                Indicators.Remove(Indicators.FirstOrDefault(p=>p == command));
                if (Indicators.Count <= 0)
                {
                    NotFavouritesInfoVisibility = Visibility.Visible;
                }
                else
                {
                    NotFavouritesInfoVisibility = Visibility.Collapsed;
                }
            });

            var saved = monitoringManager.GetGroups().First();

            Indicators = new ObservableCollection<CommandInfo>(saved.Value.Commands);
            if (Indicators.Count > 0)
            {
                NotFavouritesInfoVisibility = Visibility.Collapsed;
            }

            // Подписываемся на события авторизации
            _eventAggregator.GetEvent<OnUserAuthorized>().Subscribe(OnUserAuthorized);
            _eventAggregator.GetEvent<OnUserQuit>().Subscribe(OnUserQuit);

            // Загружаем текущего пользователя при инициализации
            UpdateUserProfile();

            // Загружаем счётчик непрочитанных уведомлений
            _ = UpdateUnreadCountAsync();

            //MessageBox.ShowDialog($"IndicatorsCount: {Indicators.Count}");

            //var subscription = _observer.Subscribe(this,
            //    async (e) => await OnOpcData(e),
            //    Indicators.Select(p => p).ToArray()
            //);
        }

        //private async Task OnOpcData(CommandResponse command)
        //{
        //    var indicator = Indicators.FirstOrDefault(p => p == command.CommandInfo);
        //    if (indicator == null) return;

        //    var newValue = Math.Round((float)command.Value, 2);

        //    Application.Current.Dispatcher.InvokeAsync(() =>
        //    {
        //        indicator.ChartValues.Add(new DateTimePoint(now, newValue));

        //        // Ограничиваем общее количество точек
        //        if (indicator.ChartValues.Count > MaxPoints)
        //        {
        //            indicator.ChartValues.RemoveAt(0);
        //        }

        //        indicator.Value = newValue;
        //    });
        //}

        private void OnUserAuthorized(User user)
        {
            UpdateUserProfile();
        }

        private void OnUserQuit()
        {
            UserFullName = "Гость";
            UserRole = "Не авторизован";
        }

        public async Task UpdateUnreadCountAsync()
        {
            try
            {
                UnreadNotificationsCount = await _notificationService.GetUnreadCountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LeftBarViewModel] Ошибка загрузки счётчика уведомлений: {ex.Message}");
            }
        }

        private async void UpdateUserProfile()
        {
            var user = _authorizationService.GetUser();

            if (user == null)
            {
                UserFullName = "Гость";
                UserRole = "Не авторизован";
                return;
            }

            // Формируем полное имя
            var fullName = $"{user.Family} {user.Name}".Trim();
            UserFullName = string.IsNullOrEmpty(fullName) ? user.Login : fullName;

            // Получаем роль пользователя
            if (_authorizationService.IsRootAuthorized())
            {
                UserRole = "Администратор";
            }
            else if (user.RoleId != Guid.Empty)
            {
                var role = await _rolesService.GetUserRole(user.Id);
                UserRole = role?.DisplayName ?? "Без роли";
            }
            else
            {
                UserRole = "Без роли";
            }
        }
    }
}