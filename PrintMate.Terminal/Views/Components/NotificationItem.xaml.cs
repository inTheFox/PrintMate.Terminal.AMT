using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PrintMate.Terminal.Models;
using System.Windows.Media;

namespace PrintMate.Terminal.Views.Components
{
    public partial class NotificationItem : UserControl
    {
        public event EventHandler CloseRequested;
        private DispatcherTimer _autoCloseTimer;

        public NotificationItem()
        {
            InitializeComponent();
            Loaded += NotificationItem_Loaded;
        }

        private void NotificationItem_Loaded(object sender, RoutedEventArgs e)
        {
            // Простая анимация появления без Storyboard
            NotificationBorder.Opacity = 0;
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            NotificationBorder.BeginAnimation(OpacityProperty, fadeIn);

            // Настраиваем автозакрытие, если указано
            if (DataContext is Notification notification && notification.AutoCloseSeconds.HasValue)
            {
                _autoCloseTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(notification.AutoCloseSeconds.Value)
                };
                _autoCloseTimer.Tick += (s, args) =>
                {
                    _autoCloseTimer.Stop();
                    Close();
                };
                _autoCloseTimer.Start();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_autoCloseTimer != null)
            {
                _autoCloseTimer.Stop();
                _autoCloseTimer = null;
            }
            Close();
        }

        public void Close()
        {
            // Анимация затухания
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            // Анимация схлопывания высоты
            var collapseHeight = new DoubleAnimation
            {
                From = ActualHeight,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            // Анимация уменьшения margin
            var collapseMargin = new ThicknessAnimation
            {
                From = new Thickness(10),
                To = new Thickness(10, 0, 10, 0),
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            collapseHeight.Completed += (s, e) =>
            {
                CloseRequested?.Invoke(this, EventArgs.Empty);
            };

            NotificationBorder.BeginAnimation(OpacityProperty, fadeOut);
            BeginAnimation(HeightProperty, collapseHeight);
            NotificationBorder.BeginAnimation(MarginProperty, collapseMargin);
        }
    }
}
