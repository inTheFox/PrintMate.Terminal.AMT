using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Services;
using Prism.Events;
using Prism.Ioc;

namespace PrintMate.Terminal.Views.Modals
{
    /// <summary>
    /// Wizard для добавления проекта с современными анимациями.
    /// Полностью автономный - не зависит от событий ModalService.
    /// </summary>
    public partial class AddProjectWizard : UserControl
    {
        private int _currentStep = 0;
        private string _selectedFormat;
        private SubscriptionToken _projectAnalyzeToken;

        // Lazy-создание страниц
        private AddProjectModalSelectProjectType _projectTypeSelector;
        private ProjectDirectoryPicker _directoryPicker;
        private AddProjectLoadingProgressView _loadingProgress;
        private AddProjectSuccessfullView _successView;

        public AddProjectWizard()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Вызывается при каждом показе UserControl (включая переиспользование из пула)
        /// </summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Полный сброс состояния при каждом показе
            ResetWizard();

            // Запускаем анимацию появления
            PlayEntranceAnimation();
        }

        /// <summary>
        /// Полный сброс wizard к начальному состоянию
        /// </summary>
        private void ResetWizard()
        {
            _currentStep = 0;
            _selectedFormat = null;

            // Отписываемся от предыдущей подписки если была
            if (_projectAnalyzeToken != null)
            {
                Bootstrapper.ContainerProvider.Resolve<IEventAggregator>()
                    .GetEvent<OnProjectAnalyzeFinishEvent>()
                    .Unsubscribe(_projectAnalyzeToken);
                _projectAnalyzeToken = null;
            }

            // Создаём первую страницу заново для чистого состояния
            //_projectTypeSelector = new AddProjectModalSelectProjectType();
            //_projectTypeSelector.OnNext += OnProjectTypeSelected;

            // ВАЖНО: Создаём страницу загрузки заранее, чтобы не было задержки при показе
            _loadingProgress = new AddProjectLoadingProgressView();

            // Очищаем другие страницы
            _directoryPicker = null;
            _successView = null;

            _selectedFormat = ".cli";

            // Создаём страницу выбора директории
            _directoryPicker = new ProjectDirectoryPicker();
            _directoryPicker.Format = ".cli";
            _directoryPicker.OnSelected += OnDirectorySelected;

            // Показываем первую страницу
            CurrentPage.Content = _directoryPicker;
            NextPage.Content = null;
            NextPage.Visibility = Visibility.Collapsed;

            // Обновляем индикаторы
            UpdateStepIndicators();
        }

        /// <summary>
        /// Анимация появления wizard
        /// </summary>
        private void PlayEntranceAnimation()
        {
            MainBorder.Opacity = 0;

            var storyboard = (Storyboard)Resources["FadeInStoryboard"];
            storyboard.Begin(MainBorder);
        }

        /// <summary>
        /// Обновляет индикаторы шагов
        /// </summary>
        private void UpdateStepIndicators()
        {
            var indicators = new[] { Step1Indicator, Step2Indicator, Step3Indicator, Step4Indicator };
            for (int i = 0; i < indicators.Length; i++)
            {
                indicators[i].Fill = new SolidColorBrush(i == _currentStep ? Color.FromRgb(0x66, 0x66, 0x66) : Color.FromRgb(0x33, 0x33, 0x33));
            }
        }

        #region Навигация между шагами

        private void OnProjectTypeSelected(string format)
        {
            _selectedFormat = format;

            // Создаём страницу выбора директории
            _directoryPicker = new ProjectDirectoryPicker();
            _directoryPicker.Format = format;
            _directoryPicker.OnSelected += OnDirectorySelected;

            // Показываем страницу выбора директории с анимацией
            NavigateToStep(1, _directoryPicker);
        }

        private void OnDirectorySelected(string path)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [OnDirectorySelected] START - path={path}");

            // Подписываемся на завершение анализа
            _projectAnalyzeToken = Bootstrapper.ContainerProvider.Resolve<IEventAggregator>()
                .GetEvent<OnProjectAnalyzeFinishEvent>()
                .Subscribe(OnProjectAnalyzeFinish, ThreadOption.UIThread);

            // МГНОВЕННО показываем страницу загрузки БЕЗ ОЖИДАНИЯ
            NavigateToStep(1, _loadingProgress);
            UpdateStepIndicators();

            // Запускаем парсинг в фоне (НЕ ЖДЁМ)
            var parser = Bootstrapper.ContainerProvider.Resolve<ProjectManager>();
            parser.Load(path);

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [OnDirectorySelected] END - page shown, parsing started");
        }

        private async void OnProjectAnalyzeFinish(ProjectParserTest.Parsers.Shared.Models.Project project)
        {
            Console.WriteLine("[AddProjectWizard] OnProjectAnalyzeFinish called");

            // Отписываемся
            if (_projectAnalyzeToken != null)
            {
                Bootstrapper.ContainerProvider.Resolve<IEventAggregator>()
                    .GetEvent<OnProjectAnalyzeFinishEvent>()
                    .Unsubscribe(_projectAnalyzeToken);
                _projectAnalyzeToken = null;
            }

            // Задержка перед показом успеха, чтобы пользователь успел увидеть "100%"
            await Task.Delay(800);

            // Создаём страницу успеха и показываем МГНОВЕННО
            _successView = new AddProjectSuccessfullView();
            NavigateToStep(2, _successView);
            UpdateStepIndicators();

            // Запускаем анимацию успеха
            await Task.Delay(100);
            _successView?.StartFadeInAnimation();
        }

        #endregion

        #region Анимации переходов

        /// <summary>
        /// Анимированный переход к следующему шагу (НЕ БЛОКИРУЕТ UI)
        /// </summary>
        private async void NavigateToStep(int step, UIElement newPage, bool fastAnimation = false)
        {
            // Подготавливаем новую страницу
            InitializePageTransform(newPage, startFromRight: true);
            NextPage.Content = newPage;
            NextPage.Visibility = Visibility.Visible;

            // Запускаем параллельные анимации (быстрая или обычная)
            var duration = TimeSpan.FromMilliseconds(fastAnimation ? 150 : 500);

            // Текущая страница уходит влево
            AnimatePageOut(CurrentPage.Content as UIElement, duration);

            // Новая страница приходит справа
            AnimatePageIn(newPage, duration);

            // Ждём завершения анимации в фоне
            await Task.Delay(duration);

            // Меняем местами
            CurrentPage.Content = newPage;
            NextPage.Content = null;
            NextPage.Visibility = Visibility.Collapsed;

            _currentStep = step;
            UpdateStepIndicators();
        }

        private void InitializePageTransform(UIElement element, bool startFromRight)
        {
            if (element == null) return;

            var transform = new TransformGroup();
            transform.Children.Add(new TranslateTransform(startFromRight ? 900 : -900, 0));
            transform.Children.Add(new ScaleTransform(0.85, 0.85));
            transform.Children.Add(new RotateTransform(startFromRight ? 5 : -5));

            element.RenderTransform = transform;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.Opacity = 0;
        }

        private void AnimatePageOut(UIElement element, TimeSpan duration)
        {
            if (element == null) return;

            var transform = element.RenderTransform as TransformGroup;
            if (transform == null)
            {
                transform = new TransformGroup();
                transform.Children.Add(new TranslateTransform(0, 0));
                transform.Children.Add(new ScaleTransform(1, 1));
                transform.Children.Add(new RotateTransform(0));
                element.RenderTransform = transform;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            var translate = transform.Children[0] as TranslateTransform;
            var scale = transform.Children[1] as ScaleTransform;
            var rotate = transform.Children[2] as RotateTransform;

            var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

            // Уходит влево с уменьшением и поворотом
            translate?.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(-900, duration) { EasingFunction = ease });
            scale?.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(0.7, duration) { EasingFunction = ease });
            scale?.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(0.7, duration) { EasingFunction = ease });
            rotate?.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(-8, duration) { EasingFunction = ease });
            element.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, TimeSpan.FromMilliseconds(300)) { EasingFunction = ease });
        }

        private void AnimatePageIn(UIElement element, TimeSpan duration)
        {
            if (element == null) return;

            var transform = element.RenderTransform as TransformGroup;
            if (transform == null) return;

            var translate = transform.Children[0] as TranslateTransform;
            var scale = transform.Children[1] as ScaleTransform;
            var rotate = transform.Children[2] as RotateTransform;

            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            // Приходит справа с увеличением и выравниванием
            translate?.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(0, duration) { EasingFunction = ease });
            scale?.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(1, duration) { EasingFunction = ease });
            scale?.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(1, duration) { EasingFunction = ease });
            rotate?.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(0, duration) { EasingFunction = ease });
            element.BeginAnimation(OpacityProperty,
                new DoubleAnimation(1, TimeSpan.FromMilliseconds(400)) { EasingFunction = ease });
        }

        #endregion
    }
}
