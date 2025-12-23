using PrintMate.Terminal.Interfaces;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HandyControl.Tools.Command;
using Newtonsoft.Json;

namespace PrintMate.Terminal.Services
{
    /// <summary>
    /// Оптимизированный DialogService с быстрым отображением диалогов
    /// </summary>
    public class DialogService
    {
        public static Window ActiveWindow;
        private bool _isClosed = false;

        // Кэшируем замороженные ресурсы для переиспользования
        private static readonly SolidColorBrush CachedBackgroundBrush;
        private static readonly CubicEase CachedEaseOut;
        private static readonly CubicEase CachedEaseIn;

        static DialogService()
        {
            // Инициализируем и замораживаем ресурсы один раз
            CachedBackgroundBrush = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
            CachedBackgroundBrush.Freeze();

            CachedEaseOut = new CubicEase { EasingMode = EasingMode.EaseOut };
            CachedEaseOut.Freeze();

            CachedEaseIn = new CubicEase { EasingMode = EasingMode.EaseIn };
            CachedEaseIn.Freeze();
        }

        public Services.DialogResult<ViewModelType> ShowDialog<ViewType, ViewModelType>(Dictionary<string, object> options = null)
        {
            if (ActiveWindow != null)
            {
                ActiveWindow.ContentRendered -= ActiveWindowOnContentRendered;
            }

            _isClosed = false;
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                return Application.Current.Dispatcher.Invoke(() => ShowDialog<ViewType, ViewModelType>(options));
            }

            var content = Bootstrapper.ContainerProvider.Resolve<ViewType>();
            var model = Bootstrapper.ContainerProvider.Resolve<ViewModelType>();

            if (model is IViewModelForm closeable)
            {
                closeable.CloseCommand = new RelayCommand(OnCloseCommand);
            }

            // Оптимизация: применяем опции до создания UI
            ApplyOptions(model, options);

            // Создаём трансформации заранее с начальными значениями
            var scaleTransform = new ScaleTransform(0.8, 0.8); // Начинаем с 80% вместо 0%
            var contentContainer = new Border
            {
                Opacity = 0,
                RenderTransform = scaleTransform,
                RenderTransformOrigin = new Point(0.5, 0.5),
                UseLayoutRounding = true, // Улучшает производительность рендеринга
                SnapsToDevicePixels = true
            };
            contentContainer.Child = (UIElement)(object)content;

            ActiveWindow = new Window
            {
                Width = 1920,
                Height = 1080,
                Title = "Клавиатура",
                DataContext = model,
                Content = contentContainer,
                AllowsTransparency = true,
                WindowStyle = WindowStyle.None,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Owner = Application.Current.MainWindow,
                Opacity = 0,
                Background = CachedBackgroundBrush, // Используем закэшированную кисть
                ShowInTaskbar = false, // Ускоряет открытие
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };

            // Запускаем анимацию сразу после Loaded, не ждём ContentRendered
            ActiveWindow.Loaded += ActiveWindowOnLoaded;
            ActiveWindow.ShowDialog();

            return new DialogResult<ViewModelType>
            {
                IsSuccess = !_isClosed,
                Result = model
            };
        }

        private void ApplyOptions<ViewModelType>(ViewModelType model, Dictionary<string, object> options)
        {
            if (options == null || options.Count == 0) return;

            var modelType = model.GetType();

            foreach (var keyValuePair in options)
            {
                var property = modelType.GetProperty(keyValuePair.Key,
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.IgnoreCase);

                if (property != null && property.CanWrite)
                {
                    try
                    {
                        if (property.PropertyType == keyValuePair.Value.GetType() ||
                            property.PropertyType.IsAssignableFrom(keyValuePair.Value.GetType()))
                        {
                            property.SetValue(model, keyValuePair.Value);
                        }
                        else
                        {
                            Console.WriteLine($"Неудачная попытка установить значение типа {keyValuePair.Value.GetType().Name} для свойства {property.Name} типа {property.PropertyType.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при установке свойства {property.Name}: {ex.Message}");
                    }
                }
            }
        }

        private void ActiveWindowOnLoaded(object sender, EventArgs e)
        {
            var dialog = (Window)sender;
            dialog.Loaded -= ActiveWindowOnLoaded;

            var contentContainer = (Border)dialog.Content;
            var scaleTransform = (ScaleTransform)contentContainer.RenderTransform;

            // Более быстрая анимация: 0.3s вместо 0.5s и 0.8s
            var duration = TimeSpan.FromSeconds(0.3);

            // Анимация прозрачности окна
            var fadeInWindow = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = duration,
                EasingFunction = CachedEaseOut
            };

            // Анимация прозрачности контента
            var fadeInContent = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = duration,
                EasingFunction = CachedEaseOut
            };

            // Анимация масштаба: от 0.8 → 1.0 (меньший диапазон = быстрее)
            var scaleUpX = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = duration,
                EasingFunction = CachedEaseOut
            };

            var scaleUpY = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = duration,
                EasingFunction = CachedEaseOut
            };

            // Запускаем все анимации параллельно
            dialog.BeginAnimation(UIElement.OpacityProperty, fadeInWindow);
            contentContainer.BeginAnimation(UIElement.OpacityProperty, fadeInContent);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUpX);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUpY);
        }

        private void ActiveWindowOnContentRendered(object sender, EventArgs e)
        {
            // Оставляем этот метод для обратной совместимости, но он больше не используется
        }

        private void OnCloseCommand(object obj)
        {
            _isClosed = true;
            Close();
        }

        public Services.DialogResult<ViewModelType> Show<ViewType, ViewModelType>(Dictionary<string, object> options = null)
        {
            _isClosed = false;
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                return Application.Current.Dispatcher.Invoke(() => Show<ViewType, ViewModelType>(options));
            }

            var content = Bootstrapper.ContainerProvider.Resolve<ViewType>();
            var model = Bootstrapper.ContainerProvider.Resolve<ViewModelType>();

            if (model is IViewModelForm closeable)
            {
                closeable.CloseCommand = new RelayCommand(OnCloseCommand);
            }

            ApplyOptions(model, options);

            var scaleTransform = new ScaleTransform(0.8, 0.8);
            var contentContainer = new Border
            {
                Opacity = 0,
                RenderTransform = scaleTransform,
                RenderTransformOrigin = new Point(0.5, 0.5),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            contentContainer.Child = (UIElement)(object)content;

            ActiveWindow = new Window
            {
                Width = 1920,
                Height = 1080,
                Title = "Клавиатура",
                DataContext = model,
                Content = contentContainer,
                AllowsTransparency = true,
                WindowStyle = WindowStyle.None,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Opacity = 0,
                Topmost = true,
                Background = CachedBackgroundBrush,
                ShowInTaskbar = false,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };

            ActiveWindow.Loaded += ActiveWindowOnLoaded;
            ActiveWindow.Show();

            return new DialogResult<ViewModelType>
            {
                IsSuccess = !_isClosed,
                Result = model
            };
        }

        public static void Close()
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (ActiveWindow == null || !ActiveWindow.IsLoaded) return;

                var contentContainer = (Border)ActiveWindow.Content;
                var scaleTransform = contentContainer.RenderTransform as ScaleTransform;

                if (scaleTransform == null)
                {
                    scaleTransform = new ScaleTransform(1, 1);
                    contentContainer.RenderTransform = scaleTransform;
                    contentContainer.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                // Более быстрая анимация закрытия: 0.15s вместо 0.2s
                var duration = TimeSpan.FromSeconds(0.15);

                // Анимация уменьшения до 0.7 вместо 0.5 (меньше эффект = быстрее)
                var scaleDownX = new DoubleAnimation
                {
                    To = 0.7,
                    Duration = duration,
                    EasingFunction = CachedEaseIn
                };

                var scaleDownY = new DoubleAnimation
                {
                    To = 0.7,
                    Duration = duration,
                    EasingFunction = CachedEaseIn
                };

                // Анимация прозрачности
                var fadeOut = new DoubleAnimation
                {
                    To = 0,
                    Duration = duration,
                    EasingFunction = CachedEaseIn
                };

                fadeOut.Completed += (s, e) =>
                {
                    ActiveWindow?.Close();
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDownX);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDownY);
                ActiveWindow.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            });
        }
    }
}
