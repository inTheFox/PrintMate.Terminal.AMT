using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Interfaces;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Services;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class NotificationsCenterViewModel : BindableBase, IViewModelForm
    {
        private readonly NotificationService _notificationService;
        private readonly DispatcherTimer _timeTimer;

        private ObservableCollection<Notification> _notifications;
        private string _currentTime;
        private string _currentDate;
        private int _unreadCount;
        private Visibility _emptyStateVisibility;
        private Visibility _unreadBadgeVisibility;
        private Visibility _clearButtonVisibility;

        private RelayCommand _deleteNotificationCommand;
        private RelayCommand _markAsReadCommand;
        private RelayCommand _clearAllCommand;
        private RelayCommand _closeFormCommand;

        public NotificationsCenterViewModel(NotificationService notificationService)
        {
            _notificationService = notificationService;
            _notifications = new ObservableCollection<Notification>();

            // Инициализация времени
            UpdateTime();

            // Таймер для обновления времени каждую секунду
            _timeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timeTimer.Tick += (s, e) => UpdateTime();
            _timeTimer.Start();

            // Загружаем уведомления асинхронно
            _ = LoadNotificationsAsync();
        }

        public ObservableCollection<Notification> Notifications
        {
            get => _notifications;
            set => SetProperty(ref _notifications, value);
        }

        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public string CurrentDate
        {
            get => _currentDate;
            set => SetProperty(ref _currentDate, value);
        }

        public int UnreadCount
        {
            get => _unreadCount;
            set
            {
                SetProperty(ref _unreadCount, value);
                UnreadBadgeVisibility = value > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility EmptyStateVisibility
        {
            get => _emptyStateVisibility;
            set => SetProperty(ref _emptyStateVisibility, value);
        }

        public Visibility UnreadBadgeVisibility
        {
            get => _unreadBadgeVisibility;
            set => SetProperty(ref _unreadBadgeVisibility, value);
        }

        public Visibility ClearButtonVisibility
        {
            get => _clearButtonVisibility;
            set => SetProperty(ref _clearButtonVisibility, value);
        }

        public RelayCommand DeleteNotificationCommand
        {
            get => _deleteNotificationCommand ??= new RelayCommand(async obj =>
            {
                if (obj is int notificationId)
                {
                    await DeleteNotificationAsync(notificationId);
                }
            });
        }

        public RelayCommand MarkAsReadCommand
        {
            get => _markAsReadCommand ??= new RelayCommand(async obj =>
            {
                if (obj is int notificationId)
                {
                    await MarkAsReadAsync(notificationId);
                }
            });
        }

        public RelayCommand ClearAllCommand
        {
            get => _clearAllCommand ??= new RelayCommand(async obj =>
            {
                await ClearAllNotificationsAsync();
            });
        }

        public RelayCommand CloseFormCommand
        {
            get => _closeFormCommand ??= new RelayCommand(obj =>
            {
                _timeTimer?.Stop();
                CloseCommand?.Execute(null);
            });
        }

        public RelayCommand CloseCommand { get; set; }

        private void UpdateTime()
        {
            var now = DateTime.Now;
            CurrentTime = now.ToString("HH:mm");
            CurrentDate = now.ToString("dddd, d MMMM", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));
        }

        private async Task LoadNotificationsAsync()
        {
            try
            {
                var notifications = await _notificationService.GetAllNotificationsAsync();
                var unreadCount = await _notificationService.GetUnreadCountAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Notifications.Clear();
                    foreach (var notification in notifications)
                    {
                        Notifications.Add(notification);
                    }

                    UnreadCount = unreadCount;
                    UpdateVisibility();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationsCenterViewModel] Ошибка загрузки уведомлений: {ex.Message}");
            }
        }

        private async Task DeleteNotificationAsync(int notificationId)
        {
            try
            {
                await _notificationService.DeleteNotificationAsync(notificationId);
                await LoadNotificationsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationsCenterViewModel] Ошибка удаления уведомления: {ex.Message}");
            }
        }

        private async Task MarkAsReadAsync(int notificationId)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(notificationId);

                // Обновляем локальное состояние
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var notification in Notifications)
                    {
                        if (notification.Id == notificationId && !notification.IsRead)
                        {
                            notification.IsRead = true;
                            UnreadCount--;
                            break;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationsCenterViewModel] Ошибка отметки прочитанного: {ex.Message}");
            }
        }

        private async Task ClearAllNotificationsAsync()
        {
            try
            {
                await _notificationService.ClearAllNotificationsAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Notifications.Clear();
                    UnreadCount = 0;
                    UpdateVisibility();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationsCenterViewModel] Ошибка очистки уведомлений: {ex.Message}");
            }
        }

        private void UpdateVisibility()
        {
            EmptyStateVisibility = Notifications.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ClearButtonVisibility = Notifications.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
