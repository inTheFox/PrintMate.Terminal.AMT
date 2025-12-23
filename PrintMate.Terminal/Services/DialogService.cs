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
    public class DialogService
    {
        public static DialogService Instance { get; private set; }
        private Dictionary<string, Window> _windows = new Dictionary<string, Window>();
        private Stack<string> _windowIdStack = new Stack<string>();
        private bool _isClosed = false;
        public event Action OnOpenAnimationFinish;

        public DialogService()
        {
            Instance = this;
        }

        private Window CurrentWindow => _windowIdStack.Count > 0 && _windows.ContainsKey(_windowIdStack.Peek())
            ? _windows[_windowIdStack.Peek()]
            : null;

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

            if (options != null && options.Count > 0)
            {
                foreach (var keyValuePair in options)
                {
                    var property = model.GetType().GetProperties()
                        .FirstOrDefault(p => p.Name.ToLower() == keyValuePair.Key.ToLower());
                    if (property != null)
                    {
                        if (property.PropertyType != keyValuePair.Value.GetType())
                        {
                            Console.WriteLine($"Неудачная попытка установить значение типа {keyValuePair.Value.GetType().Name} для свойства {property.Name} типа {property.PropertyType.Name} ");
                            continue;
                        }
                        property.SetValue(model, keyValuePair.Value);
                    }
                }
            }

            if (model.GetType().GetProperty("View") != null)
            {
                model.GetType().GetProperty("View").SetValue(model, content);
            }

            var contentContainer = new Grid { Opacity = 0 };
            contentContainer.Children.Add((UIElement)(object)content);

            // Генерируем уникальный ID окна
            var windowId = Guid.NewGuid().ToString();

            // Определяем Owner - если есть окна в стеке, используем последнее, иначе MainWindow
            Window owner = CurrentWindow ?? Application.Current.MainWindow;

            var newWindow = new Window
            {
                Width = 1024,
                Height = 768,
                Title = "Клавиатура",
                DataContext = model,
                Content = contentContainer,
                AllowsTransparency = true,
                WindowStyle = WindowStyle.None,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Owner = owner,
                Left = owner.Left,
                Top = owner.Top,
                Opacity = 0,
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                Tag = windowId  // Сохраняем ID в Tag окна
            };

            // Добавляем окно в коллекцию и стек
            _windows[windowId] = newWindow;
            _windowIdStack.Push(windowId);

            // Если ViewModel имеет свойство WindowId, устанавливаем его
            var windowIdProperty = model.GetType().GetProperty("WindowId");
            if (windowIdProperty != null && windowIdProperty.CanWrite)
            {
                windowIdProperty.SetValue(model, windowId);
            }

            newWindow.Loaded += OnDialogLoaded;
            newWindow.Closed += (s, e) =>
            {
                // Удаляем окно из коллекции и стека при закрытии
                var id = newWindow.Tag as string;
                if (!string.IsNullOrEmpty(id))
                {
                    _windows.Remove(id);

                    // Удаляем из стека (может быть не на вершине, если закрыли не последнее окно)
                    var tempStack = new Stack<string>();
                    while (_windowIdStack.Count > 0)
                    {
                        var topId = _windowIdStack.Pop();
                        if (topId != id)
                        {
                            tempStack.Push(topId);
                        }
                    }
                    while (tempStack.Count > 0)
                    {
                        _windowIdStack.Push(tempStack.Pop());
                    }
                }
            };

            newWindow.ShowDialog();

            return new DialogResult<ViewModelType>
            {
                IsSuccess = !_isClosed,
                Result = model
            };
        }

        private void OnCloseCommand(object obj)
        {
            _isClosed = true;
            Close();
        }


        private void OnDialogLoaded(object sender, RoutedEventArgs e)
        {
            var dialog = (Window)sender;
            var contentContainer = (Grid)dialog.Content;

            // Создаём группу трансформаций для комбинированной анимации
            var transformGroup = new TransformGroup();
            var scaleTransform = new ScaleTransform(0.5, 0.5);
            var translateTransform = new TranslateTransform(0, -80);
            var rotateTransform = new RotateTransform(-8);

            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(translateTransform);
            transformGroup.Children.Add(rotateTransform);

            contentContainer.RenderTransform = transformGroup;
            contentContainer.RenderTransformOrigin = new Point(0.5, 0.5);

            // Анимация прозрачности окна
            var fadeInWindow = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            dialog.BeginAnimation(UIElement.OpacityProperty, fadeInWindow);

            // Анимация прозрачности контента
            var fadeInContent = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            //fadeInContent.Completed += (e, f) => OnOpenAnimationFinish?.Invoke();
            contentContainer.BeginAnimation(UIElement.OpacityProperty, fadeInContent);

            // Анимация масштаба с bounce эффектом
            var scaleAnimation = new DoubleAnimation
            {
                From = 0.5,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(700),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }
            };

            // Анимация сдвига сверху вниз
            var translateAnimation = new DoubleAnimation
            {
                From = -80,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(700),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 7 }
            };

            // Анимация вращения
            var rotateAnimation = new DoubleAnimation
            {
                From = -8,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(700),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };
            rotateAnimation.Completed += (e, f) => OnOpenAnimationFinish?.Invoke();

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, translateAnimation);
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }

        public void Close()
        {
            Close(null);
        }

        public void Close(string windowId)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Window window;

                // Если передан ID, ищем окно по ID, иначе берём текущее
                if (!string.IsNullOrEmpty(windowId))
                {
                    if (!_windows.ContainsKey(windowId))
                    {
                        Console.WriteLine($"[DialogService] Window with ID {windowId} not found");
                        return;
                    }
                    window = _windows[windowId];
                }
                else
                {
                    window = CurrentWindow;
                }

                if (window == null || !window.IsLoaded) return;

                var contentContainer = (Grid)window.Content;

                // Получаем или создаём группу трансформаций
                TransformGroup transformGroup;
                ScaleTransform scaleTransform;
                TranslateTransform translateTransform;
                RotateTransform rotateTransform;

                if (contentContainer.RenderTransform is TransformGroup existingGroup)
                {
                    transformGroup = existingGroup;
                    scaleTransform = transformGroup.Children[0] as ScaleTransform;
                    translateTransform = transformGroup.Children[1] as TranslateTransform;
                    rotateTransform = transformGroup.Children[2] as RotateTransform;
                }
                else
                {
                    transformGroup = new TransformGroup();
                    scaleTransform = new ScaleTransform(1, 1);
                    translateTransform = new TranslateTransform(0, 0);
                    rotateTransform = new RotateTransform(0);

                    transformGroup.Children.Add(scaleTransform);
                    transformGroup.Children.Add(translateTransform);
                    transformGroup.Children.Add(rotateTransform);

                    contentContainer.RenderTransform = transformGroup;
                    contentContainer.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                // Анимация уменьшения и сдвига вниз с поворотом
                var scaleAnimation = new DoubleAnimation
                {
                    To = 0.3,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseIn, Amplitude = 0.5 }
                };

                var translateAnimation = new DoubleAnimation
                {
                    To = 100,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseIn, Exponent = 5 }
                };

                var rotateAnimation = new DoubleAnimation
                {
                    To = 10,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseIn, Amplitude = 0.3 }
                };

                // Анимация прозрачности
                var fadeOut = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                fadeOut.Completed += (s, e) =>
                {
                    window?.Close();
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                translateTransform.BeginAnimation(TranslateTransform.YProperty, translateAnimation);
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
                window.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            });
        }
    }
}