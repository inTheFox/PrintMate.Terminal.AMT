using PrintMate.Terminal.Interfaces;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using HandyControl.Tools.Command;

namespace PrintMate.Terminal.Services
{
    /// <summary>
    /// Асинхронный сервис для отображения модальных окон через Canvas overlay.
    /// Не блокирует UI поток в отличие от DialogService.ShowDialog().
    /// </summary>
    public class ModalService
    {
        public static ModalService Instance { get; private set; }

        private Canvas _modalContainer;
        private Canvas _backgroundOverlay;
        private BlurEffect _backgroundBlurEffect;
        private readonly Stack<ModalContext> _modalStack = new Stack<ModalContext>();
        private int _zIndexCounter = 200; // Начинаем с 200, чтобы быть выше основного контента

        // Настройки анимации размытия
        private const double BlurRadius = 15.0; // Максимальный радиус размытия
        private const int BlurAnimationDurationMs = 350; // Длительность анимации размытия

        // Пул для переиспользования View (ключ - имя типа View)
        private readonly Dictionary<string, UIElement> _viewPool = new Dictionary<string, UIElement>();

        /// <summary>
        /// Событие, которое вызывается после завершения анимации открытия модального окна
        /// Параметр - ID открытого модального окна
        /// </summary>
        public event Action<string> OnOpenAnimationFinish;

        /// <summary>
        /// Событие, которое вызывается после закрытия модального окна
        /// </summary>
        public event Action<string> OnModalClosed;

        public ModalService()
        {
            Instance = this;
        }

        /// <summary>
        /// Инициализация сервиса с Canvas контейнерами из MainWindow
        /// </summary>
        public void Initialize(Canvas modalContainer, Canvas backgroundOverlay)
        {
            Initialize(modalContainer, backgroundOverlay, null);
        }

        /// <summary>
        /// Инициализация сервиса с Canvas контейнерами из MainWindow и эффектом размытия
        /// </summary>
        /// <param name="modalContainer">Canvas для модальных окон</param>
        /// <param name="backgroundOverlay">Canvas для затемнения фона</param>
        /// <param name="backgroundBlurEffect">BlurEffect для размытия основного контента (опционально)</param>
        public void Initialize(Canvas modalContainer, Canvas backgroundOverlay, BlurEffect backgroundBlurEffect)
        {
            _modalContainer = modalContainer ?? throw new ArgumentNullException(nameof(modalContainer));
            _backgroundOverlay = backgroundOverlay ?? throw new ArgumentNullException(nameof(backgroundOverlay));
            _backgroundBlurEffect = backgroundBlurEffect; // Может быть null

            // Устанавливаем начальные параметры для overlay
            _backgroundOverlay.Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
            _backgroundOverlay.Visibility = Visibility.Collapsed;
            _backgroundOverlay.Opacity = 0;

            // Сбрасываем размытие при инициализации
            if (_backgroundBlurEffect != null)
            {
                _backgroundBlurEffect.Radius = 0;
            }
        }

        /// <summary>
        /// Показывает модальное окно без ожидания (fire-and-forget)
        /// </summary>
        /// <typeparam name="ViewType">Тип View (UserControl)</typeparam>
        /// <typeparam name="ViewModelType">Тип ViewModel</typeparam>
        /// <param name="modalId">Пользовательский ID модального окна (если null - будет сгенерирован автоматически)</param>
        /// <param name="options">Опциональные параметры для инициализации ViewModel</param>
        /// <param name="showOverlay">Показывать ли затемнённый фон</param>
        /// <param name="closeOnBackgroundClick">Закрывать ли при клике на фон</param>
        /// <returns>ID модального окна для последующего закрытия</returns>
        public string Show<ViewType, ViewModelType>(
            string modalId = null,
            Dictionary<string, object> options = null,
            bool showOverlay = true,
            bool closeOnBackgroundClick = true)
        {
            return ShowInternal<ViewType, ViewModelType>(modalId, options, showOverlay, closeOnBackgroundClick, false);
        }

        /// <summary>
        /// Показывает модальное окно асинхронно (с ожиданием результата)
        /// </summary>
        /// <typeparam name="ViewType">Тип View (UserControl)</typeparam>
        /// <typeparam name="ViewModelType">Тип ViewModel</typeparam>
        /// <param name="modalId">Пользовательский ID модального окна (если null - будет сгенерирован автоматически)</param>
        /// <param name="options">Опциональные параметры для инициализации ViewModel</param>
        /// <param name="showOverlay">Показывать ли затемнённый фон</param>
        /// <param name="closeOnBackgroundClick">Закрывать ли при клике на фон</param>
        /// <returns>Результат с ViewModel после закрытия</returns>
        public Task<ModalResult<ViewModelType>> ShowAsync<ViewType, ViewModelType>(
            string modalId = null,
            Dictionary<string, object> options = null,
            bool showOverlay = true,
            bool closeOnBackgroundClick = true)
        {
            var resultModalId = ShowInternal<ViewType, ViewModelType>(modalId, options, showOverlay, closeOnBackgroundClick, true);
            var context = _modalStack.FirstOrDefault(c => c.Id == resultModalId);

            if (context == null)
            {
                throw new InvalidOperationException($"Контекст модального окна '{resultModalId}' не найден в стеке");
            }

            if (context.TaskCompletionSource == null)
            {
                throw new InvalidOperationException($"TaskCompletionSource для модального окна '{resultModalId}' равен null");
            }

            // TaskCompletionSource<T> - это object, получаем Task через рефлексию
            var tcsType = context.TaskCompletionSource.GetType();
            var taskProperty = tcsType.GetProperty("Task");

            if (taskProperty == null)
            {
                throw new InvalidOperationException($"У типа {tcsType.Name} нет свойства Task");
            }

            var task = taskProperty.GetValue(context.TaskCompletionSource) as Task<ModalResult<ViewModelType>>;
            return task;
        }

        /// <summary>
        /// Внутренний метод для показа модального окна
        /// </summary>
        private string ShowInternal<ViewType, ViewModelType>(
            string modalId,
            Dictionary<string, object> options,
            bool showOverlay,
            bool closeOnBackgroundClick,
            bool createTask)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                return Application.Current.Dispatcher.Invoke(() =>
                    ShowInternal<ViewType, ViewModelType>(modalId, options, showOverlay, closeOnBackgroundClick, createTask));
            }

            if (_modalContainer == null)
            {
                throw new InvalidOperationException("ModalService не инициализирован. Вызовите Initialize() перед использованием.");
            }

            // Генерируем ID если не указан
            if (string.IsNullOrEmpty(modalId))
            {
                modalId = Guid.NewGuid().ToString();
            }
            else
            {
                // Проверяем, что такой ID ещё не используется
                if (_modalStack.Any(c => c.Id == modalId))
                {
                    throw new InvalidOperationException($"Модальное окно с ID '{modalId}' уже открыто. Используйте уникальный ID или оставьте null для автогенерации.");
                }
            }

            // TaskCompletionSource создаётся только если нужно ждать результат
            object tcs = createTask ? new TaskCompletionSource<ModalResult<ViewModelType>>() : null;

            // Пытаемся получить View из пула для переиспользования
            var viewTypeName = typeof(ViewType).Name;
            UIElement view;

            if (_viewPool.TryGetValue(viewTypeName, out var cachedView))
            {
                // Используем кешированную View
                view = cachedView;
                System.Diagnostics.Debug.WriteLine($"[ModalService] Переиспользуем View из пула: {viewTypeName}");

                // ВАЖНО: Если View уже был добавлен в другой контейнер, удаляем его оттуда
                if (view is FrameworkElement fe && fe.Parent is Panel parentPanel)
                {
                    parentPanel.Children.Remove(view);
                }
            }
            else
            {
                // Создаём новую View и добавляем в пул
                view = Bootstrapper.ContainerProvider.Resolve<ViewType>() as UIElement;
                if (view == null)
                {
                    throw new InvalidOperationException($"ViewType {typeof(ViewType).Name} должен наследовать UIElement");
                }
                _viewPool[viewTypeName] = view;
                System.Diagnostics.Debug.WriteLine($"[ModalService] Создана новая View и добавлена в пул: {viewTypeName}");
            }

            // ViewModel создаём всегда новый
            var viewModel = Bootstrapper.ContainerProvider.Resolve<ViewModelType>();

            // Установка опций в ViewModel (оптимизировано - кешируем PropertyInfo)
            if (options != null && options.Count > 0)
            {
                var viewModelType = viewModel.GetType();
                var properties = viewModelType.GetProperties();

                foreach (var kvp in options)
                {
                    var property = properties.FirstOrDefault(p =>
                        p.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) && p.CanWrite);

                    if (property != null)
                    {
                        try
                        {
                            property.SetValue(viewModel, kvp.Value);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ModalService] Не удалось установить свойство {property.Name}: {ex.Message}");
                        }
                    }
                }
            }

            // Связываем View с ViewModel
            if (view is FrameworkElement frameworkElement)
            {
                frameworkElement.DataContext = viewModel;
            }

            // Создаём контекст модального окна (ID уже установлен выше)
            var currentZIndex = _zIndexCounter;
            _zIndexCounter += 2; // +2 чтобы оставить место между overlay и контентом

            var modalContext = new ModalContext
            {
                Id = modalId,
                View = view,
                ViewModel = viewModel,
                TaskCompletionSource = tcs,
                ZIndex = currentZIndex,
                ShowOverlay = showOverlay
            };

            // Устанавливаем дополнительные свойства асинхронно (не блокируя UI)
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var viewModelType = viewModel.GetType();

                // Устанавливаем View в ViewModel если есть такое свойство
                var viewProperty = viewModelType.GetProperty("View");
                if (viewProperty != null && viewProperty.CanWrite)
                {
                    viewProperty.SetValue(viewModel, view);
                }

                // Устанавливаем WindowId в ViewModel если есть такое свойство
                var windowIdProperty = viewModelType.GetProperty("WindowId");
                if (windowIdProperty != null && windowIdProperty.CanWrite)
                {
                    windowIdProperty.SetValue(viewModel, modalId);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);

            // Настраиваем команду закрытия если ViewModel поддерживает IViewModelForm
            if (viewModel is IViewModelForm closeable)
            {
                closeable.CloseCommand = new RelayCommand(_ => CloseAsync(modalId, false));
            }

            // Контейнер для view с трансформациями
            var contentContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0
            };
            contentContainer.Children.Add(view);

            // Добавляем контейнер на Canvas
            _modalContainer.Children.Add(contentContainer);
            Canvas.SetZIndex(contentContainer, currentZIndex + 1);
            Canvas.SetLeft(contentContainer, 0);
            Canvas.SetTop(contentContainer, 0);

            modalContext.ContentContainer = contentContainer;

            // Показываем overlay если нужно
            if (showOverlay)
            {
                ShowOverlay(currentZIndex, closeOnBackgroundClick ? modalId : null);
            }

            // Добавляем в стек
            _modalStack.Push(modalContext);

            // Запускаем анимацию появления
            AnimateShow(contentContainer, modalId);

            return modalId;
        }

        /// <summary>
        /// Закрывает модальное окно (fire-and-forget)
        /// </summary>
        /// <param name="modalId">ID модального окна (null = закрыть последнее)</param>
        /// <param name="isSuccess">Успешное ли закрытие</param>
        public void Close(string modalId = null, bool isSuccess = true)
        {
            // Fire-and-forget вызов CloseAsync
            _ = CloseAsync(modalId, isSuccess);
        }

        /// <summary>
        /// Закрывает модальное окно асинхронно
        /// </summary>
        /// <param name="modalId">ID модального окна (null = закрыть последнее)</param>
        /// <param name="isSuccess">Успешное ли закрытие</param>
        public Task CloseAsync(string modalId = null, bool isSuccess = true)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                return Application.Current.Dispatcher.InvokeAsync(() => CloseAsync(modalId, isSuccess)).Task;
            }

            var tcs = new TaskCompletionSource<bool>();

            ModalContext contextToClose;

            if (string.IsNullOrEmpty(modalId))
            {
                // Закрываем последнее модальное окно
                if (_modalStack.Count == 0)
                {
                    tcs.SetResult(false);
                    return tcs.Task;
                }
                contextToClose = _modalStack.Pop();
            }
            else
            {
                // Ищем конкретное модальное окно
                contextToClose = _modalStack.FirstOrDefault(c => c.Id == modalId);
                if (contextToClose == null)
                {
                    tcs.SetResult(false);
                    return tcs.Task;
                }

                // Удаляем из стека
                var tempStack = new Stack<ModalContext>();
                while (_modalStack.Count > 0)
                {
                    var ctx = _modalStack.Pop();
                    if (ctx.Id != modalId)
                    {
                        tempStack.Push(ctx);
                    }
                }
                while (tempStack.Count > 0)
                {
                    _modalStack.Push(tempStack.Pop());
                }
            }

            // Анимация закрытия
            AnimateHide(contextToClose.ContentContainer, () =>
            {
                // Удаляем из Canvas
                _modalContainer.Children.Remove(contextToClose.ContentContainer);

                // Скрываем overlay если больше нет модальных окон
                if (_modalStack.Count == 0)
                {
                    HideOverlay();
                }

                // Завершаем Task с результатом через рефлексию (только если есть TaskCompletionSource)
                if (contextToClose.TaskCompletionSource != null)
                {
                    var tcsType = contextToClose.TaskCompletionSource.GetType();
                    var resultType = tcsType.GetGenericArguments()[0];

                    // Создаём экземпляр ModalResult<T>
                    var result = Activator.CreateInstance(resultType);

                    // Устанавливаем свойства через рефлексию
                    var isSuccessProperty = resultType.GetProperty("IsSuccess");
                    var resultProperty = resultType.GetProperty("Result");

                    isSuccessProperty?.SetValue(result, isSuccess);
                    resultProperty?.SetValue(result, contextToClose.ViewModel);

                    // Вызываем TrySetResult через рефлексию
                    var trySetResultMethod = tcsType.GetMethod("TrySetResult");
                    trySetResultMethod?.Invoke(contextToClose.TaskCompletionSource, new[] { result });
                }

                // Вызываем событие закрытия модального окна
                OnModalClosed?.Invoke(contextToClose.Id);

                tcs.SetResult(true);
            });

            return tcs.Task;
        }

        /// <summary>
        /// Показывает затемнённый фон с эффектом размытия
        /// </summary>
        private void ShowOverlay(int zIndex, string modalIdForClick)
        {
            _backgroundOverlay.Visibility = Visibility.Visible;
            Panel.SetZIndex(_backgroundOverlay, zIndex);

            // Устанавливаем обработчик клика если нужно
            if (!string.IsNullOrEmpty(modalIdForClick))
            {
                MouseButtonEventHandler handler = null;
                handler = (s, e) =>
                {
                    _backgroundOverlay.MouseLeftButtonDown -= handler;
                    CloseAsync(modalIdForClick, false);
                };
                _backgroundOverlay.MouseLeftButtonDown += handler;
            }

            // Анимация затемнения
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            _backgroundOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            // Анимация размытия фона (только если это первое модальное окно)
            if (_backgroundBlurEffect != null && _modalStack.Count == 0)
            {
                AnimateBlur(0, BlurRadius);
            }
        }

        /// <summary>
        /// Скрывает затемнённый фон и убирает размытие
        /// </summary>
        private void HideOverlay()
        {
            // Анимация скрытия затемнения
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (s, e) =>
            {
                _backgroundOverlay.Visibility = Visibility.Collapsed;
            };

            _backgroundOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);

            // Анимация убирания размытия
            if (_backgroundBlurEffect != null)
            {
                AnimateBlur(_backgroundBlurEffect.Radius, 0);
            }
        }

        /// <summary>
        /// Анимирует эффект размытия от начального до конечного значения
        /// </summary>
        /// <param name="fromRadius">Начальный радиус размытия</param>
        /// <param name="toRadius">Конечный радиус размытия</param>
        private void AnimateBlur(double fromRadius, double toRadius)
        {
            if (_backgroundBlurEffect == null) return;

            var blurAnimation = new DoubleAnimation
            {
                From = fromRadius,
                To = toRadius,
                Duration = TimeSpan.FromMilliseconds(BlurAnimationDurationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            _backgroundBlurEffect.BeginAnimation(BlurEffect.RadiusProperty, blurAnimation);
        }

        /// <summary>
        /// Анимация появления модального окна
        /// </summary>
        private void AnimateShow(Grid contentContainer, string modalId)
        {
            // Трансформации
            var transformGroup = new TransformGroup();
            var scaleTransform = new ScaleTransform(0.7, 0.7);
            var translateTransform = new TranslateTransform(0, -50);

            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(translateTransform);

            contentContainer.RenderTransform = transformGroup;
            contentContainer.RenderTransformOrigin = new Point(0.5, 0.5);

            // Анимация прозрачности (быстрая)
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Анимация масштаба (более плавная и короткая)
            var scaleAnimation = new DoubleAnimation
            {
                From = 0.85,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Анимация сдвига (минимальная)
            var translateAnimation = new DoubleAnimation
            {
                From = -30,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Вызываем событие после завершения самой длинной анимации (250ms)
            translateAnimation.Completed += (s, e) =>
            {
                OnOpenAnimationFinish?.Invoke(modalId);
            };

            contentContainer.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, translateAnimation);
        }

        /// <summary>
        /// Анимация скрытия модального окна
        /// </summary>
        private void AnimateHide(Grid contentContainer, Action onComplete)
        {
            var transformGroup = contentContainer.RenderTransform as TransformGroup;
            ScaleTransform scaleTransform;
            TranslateTransform translateTransform;

            if (transformGroup == null)
            {
                transformGroup = new TransformGroup();
                scaleTransform = new ScaleTransform(1, 1);
                translateTransform = new TranslateTransform(0, 0);
                transformGroup.Children.Add(scaleTransform);
                transformGroup.Children.Add(translateTransform);
                contentContainer.RenderTransform = transformGroup;
                contentContainer.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            else
            {
                scaleTransform = transformGroup.Children[0] as ScaleTransform;
                translateTransform = transformGroup.Children[1] as TranslateTransform;
            }

            // Анимация прозрачности
            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            // Анимация уменьшения
            var scaleAnimation = new DoubleAnimation
            {
                To = 0.8,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            // Анимация сдвига вниз
            var translateAnimation = new DoubleAnimation
            {
                To = 30,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (s, e) => onComplete?.Invoke();

            contentContainer.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, translateAnimation);
        }

        /// <summary>
        /// Внутренний класс для хранения контекста модального окна
        /// </summary>
        private class ModalContext
        {
            public string Id { get; set; }
            public UIElement View { get; set; }
            public object ViewModel { get; set; }
            public Grid ContentContainer { get; set; }
            public object TaskCompletionSource { get; set; }
            public int ZIndex { get; set; }
            public bool ShowOverlay { get; set; }
        }
    }

    /// <summary>
    /// Результат закрытия модального окна
    /// </summary>
    public class ModalResult<T>
    {
        public bool IsSuccess { get; set; }
        public T Result { get; set; }
    }
}
