using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using PrintMate.Terminal.Database;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Views.Components;

namespace PrintMate.Terminal.Services
{
    public class NotificationService
    {
        private static Panel _notificationContainer;
        private readonly DatabaseContext _dbContext;

        public NotificationService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Инициализация сервиса уведомлений с контейнером
        /// </summary>
        public static void Initialize(Panel container)
        {
            _notificationContainer = container;
        }

        /// <summary>
        /// Показать уведомление об успехе
        /// </summary>
        public void Success(string title, string message, int? autoCloseSeconds = 5)
        {
            Show(new Notification
            {
                Title = title,
                Message = message,
                Type = NotificationType.Success,
                AutoCloseSeconds = autoCloseSeconds
            });
        }

        /// <summary>
        /// Показать уведомление об ошибке
        /// </summary>
        public void Error(string title, string message, int? autoCloseSeconds = null)
        {
            Show(new Notification
            {
                Title = title,
                Message = message,
                Type = NotificationType.Error,
                AutoCloseSeconds = autoCloseSeconds
            });
        }

        /// <summary>
        /// Показать предупреждение
        /// </summary>
        public void Warning(string title, string message, int? autoCloseSeconds = 7)
        {
            Show(new Notification
            {
                Title = title,
                Message = message,
                Type = NotificationType.Warning,
                AutoCloseSeconds = autoCloseSeconds
            });
        }

        /// <summary>
        /// Показать информационное уведомление
        /// </summary>
        public void Info(string title, string message, int? autoCloseSeconds = 5)
        {
            Show(new Notification
            {
                Title = title,
                Message = message,
                Type = NotificationType.Info,
                AutoCloseSeconds = autoCloseSeconds
            });
        }

        #region Тихие уведомления (только в БД, без всплывающего окна)

        /// <summary>
        /// Добавить уведомление об успехе только в БД (без всплывающего окна)
        /// </summary>
        public async Task AddSuccessAsync(string title, string message)
        {
            await AddSilentAsync(new Notification
            {
                Title = title,
                Message = message,
                Type = NotificationType.Success
            });
        }

        /// <summary>
        /// Добавить уведомление об ошибке только в БД (без всплывающего окна)
        /// </summary>
        public async Task AddErrorAsync(string title, string message)
        {
            await AddSilentAsync(new Notification
            {
                Title = title,
                Message = message,
                Type = NotificationType.Error
            });
        }

        /// <summary>
        /// Добавить предупреждение только в БД (без всплывающего окна)
        /// </summary>
        public async Task AddWarningAsync(string title, string message)
        {
            await AddSilentAsync(new Notification
            {
                Title = title,
                Message = message,
                Type = NotificationType.Warning
            });
        }

        /// <summary>
        /// Добавить информационное уведомление только в БД (без всплывающего окна)
        /// </summary>
        public async Task AddInfoAsync(string title, string message)
        {
            await AddSilentAsync(new Notification
            {
                Title = title,
                Message = message,
                Type = NotificationType.Info
            });
        }

        /// <summary>
        /// Добавить уведомление только в БД без показа всплывающего окна
        /// </summary>
        private async Task AddSilentAsync(Notification notification)
        {
            try
            {
                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationService] Ошибка сохранения уведомления: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Показать уведомление и сохранить в БД
        /// </summary>
        private void Show(Notification notification)
        {
            // Сохраняем уведомление в БД асинхронно
            Task.Run(async () =>
            {
                try
                {
                    _dbContext.Notifications.Add(notification);
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NotificationService] Ошибка сохранения уведомления: {ex.Message}");
                }
            });

            // Показываем всплывающее уведомление
            if (_notificationContainer == null)
            {
                Console.WriteLine("[NotificationService] Контейнер не инициализирован, уведомление только сохранено в БД");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var notificationItem = new NotificationItem
                {
                    DataContext = notification
                };

                notificationItem.CloseRequested += (sender, args) =>
                {
                    _notificationContainer.Children.Remove(notificationItem);
                };

                _notificationContainer.Children.Add(notificationItem);
            });
        }

        /// <summary>
        /// Получить все уведомления из БД
        /// </summary>
        public async Task<List<Notification>> GetAllNotificationsAsync()
        {
            return await _dbContext.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Получить непрочитанные уведомления
        /// </summary>
        public async Task<List<Notification>> GetUnreadNotificationsAsync()
        {
            return await _dbContext.Notifications
                .Where(n => !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Получить количество непрочитанных уведомлений
        /// </summary>
        public async Task<int> GetUnreadCountAsync()
        {
            return await _dbContext.Notifications.CountAsync(n => !n.IsRead);
        }

        /// <summary>
        /// Пометить уведомление как прочитанное
        /// </summary>
        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _dbContext.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Пометить все уведомления как прочитанные
        /// </summary>
        public async Task MarkAllAsReadAsync()
        {
            var unreadNotifications = await _dbContext.Notifications
                .Where(n => !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Удалить уведомление
        /// </summary>
        public async Task DeleteNotificationAsync(int notificationId)
        {
            var notification = await _dbContext.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                _dbContext.Notifications.Remove(notification);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Удалить все уведомления
        /// </summary>
        public async Task ClearAllNotificationsAsync()
        {
            var allNotifications = await _dbContext.Notifications.ToListAsync();
            _dbContext.Notifications.RemoveRange(allNotifications);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Удалить прочитанные уведомления
        /// </summary>
        public async Task ClearReadNotificationsAsync()
        {
            var readNotifications = await _dbContext.Notifications
                .Where(n => n.IsRead)
                .ToListAsync();

            _dbContext.Notifications.RemoveRange(readNotifications);
            await _dbContext.SaveChangesAsync();
        }
    }
}
