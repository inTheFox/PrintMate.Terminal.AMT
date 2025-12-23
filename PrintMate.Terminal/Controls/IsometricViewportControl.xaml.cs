using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ProjectParserTest.Parsers.Shared.Models;
using WpfPoint = System.Windows.Point;

namespace PrintMate.Terminal.Controls
{
    /// <summary>
    /// WPF UserControl для CPU-рендеринга CLI проектов с изометрической проекцией.
    /// Оптимизирован для работы на ПК без дискретной видеокарты.
    /// </summary>
    public partial class IsometricViewportControl : UserControl
    {
        #region Callbacks

        /// <summary>
        /// Callback для уведомления о начале/окончании загрузки геометрии
        /// </summary>
        public Action<bool> OnLoadingStateChanged { get; set; }

        /// <summary>
        /// Callback для уведомления о выборе детали при клике
        /// </summary>
        public Action<int?> OnPartClicked { get; set; }

        /// <summary>
        /// Callback для уведомления о прогрессе кеширования
        /// </summary>
        public Action<int> OnCachingProgress { get; set; }

        #endregion

        #region Приватные поля

        private IsometricLayerRenderer _renderer;
        private DispatcherTimer _renderTimer;
        private Stopwatch _fpsStopwatch;
        private int _frameCount;
        private double _fps;

        // Управление мышью
        private WpfPoint _lastMousePosition;
        private bool _isLeftMouseDown;
        private bool _isRightMouseDown;

        // PULL модель
        private Func<int> _getCurrentLayerFunc;
        private int _lastRequestedLayer = -1;

        // Проект
        private Project _currentProject;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty ShowInfoProperty =
            DependencyProperty.Register(nameof(ShowInfo), typeof(bool), typeof(IsometricViewportControl),
                new PropertyMetadata(false, OnShowInfoChanged));

        public bool ShowInfo
        {
            get => (bool)GetValue(ShowInfoProperty);
            set => SetValue(ShowInfoProperty, value);
        }

        private static void OnShowInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is IsometricViewportControl control)
            {
                control.InfoPanel.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion

        #region Конструктор

        public IsometricViewportControl()
        {
            InitializeComponent();

            _fpsStopwatch = new Stopwatch();
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Устанавливает функцию для получения текущего слоя (PULL модель)
        /// </summary>
        public void SetCurrentLayerGetter(Func<int> getCurrentLayerFunc)
        {
            _getCurrentLayerFunc = getCurrentLayerFunc;
            Console.WriteLine("[IsometricViewport] CurrentLayer getter configured (PULL model enabled)");
        }

        /// <summary>
        /// Загружает CLI проект для визуализации
        /// </summary>
        public void LoadProject(Project project)
        {
            _currentProject = project;

            if (_renderer != null)
            {
                OnLoadingStateChanged?.Invoke(true);
                _renderer.LoadProject(project);
                OnLoadingStateChanged?.Invoke(false);

                // Уведомляем о "прогрессе кеширования" - для CPU рендерера всё доступно сразу
                if (project?.Layers != null)
                {
                    OnCachingProgress?.Invoke(project.Layers.Count);
                }
            }

            Console.WriteLine($"[IsometricViewport] Project loaded: {project?.Layers?.Count ?? 0} layers");
        }

        /// <summary>
        /// Обновляет визуализацию до указанного слоя (1-based)
        /// </summary>
        public void UpdateLayerVisualization(int layerCount)
        {
            _renderer?.SetCurrentLayer(layerCount);
        }

        /// <summary>
        /// Выделяет деталь (заглушка для совместимости с DX11ViewportControl)
        /// </summary>
        public void HighlightPart(int? partId)
        {
            // TODO: Реализовать подсветку детали в CPU рендерере
            Console.WriteLine($"[IsometricViewport] HighlightPart: {partId}");
        }

        #endregion

        #region Обработчики событий

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Инициализируем рендерер
            int width = Math.Max((int)ActualWidth, 100);
            int height = Math.Max((int)ActualHeight, 100);

            _renderer = new IsometricLayerRenderer(width, height);

            // Привязываем bitmap к Image
            RenderImage.Source = _renderer.Bitmap;

            // Загружаем проект если он уже установлен
            if (_currentProject != null)
            {
                _renderer.LoadProject(_currentProject);
            }

            // Запускаем рендер-цикл
            StartRenderLoop();

            // Фокусируемся для обработки клавиатуры
            Focus();

            Console.WriteLine($"[IsometricViewport] Initialized {width}x{height}");
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_renderer == null)
                return;

            int width = Math.Max((int)e.NewSize.Width, 100);
            int height = Math.Max((int)e.NewSize.Height, 100);

            _renderer.Resize(width, height);
            RenderImage.Source = _renderer.Bitmap;

            Console.WriteLine($"[IsometricViewport] Resized to {width}x{height}");
        }

        private void StartRenderLoop()
        {
            _fpsStopwatch.Start();

            // Используем DispatcherTimer для рендеринга ~30 FPS
            // Это достаточно для плавности и не нагружает CPU
            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
            };
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();

            Console.WriteLine("[IsometricViewport] Render loop started (30 FPS target)");
        }

        private void OnRenderTick(object sender, EventArgs e)
        {
            if (_renderer == null)
                return;

            // PULL модель: опрашиваем текущий слой
            if (_getCurrentLayerFunc != null)
            {
                int requestedLayer = _getCurrentLayerFunc();
                if (requestedLayer != _lastRequestedLayer && requestedLayer >= 1)
                {
                    _lastRequestedLayer = requestedLayer;
                    _renderer.SetCurrentLayer(requestedLayer);
                }
            }

            // Рендерим кадр
            _renderer.Render();

            // Обновляем FPS
            _frameCount++;
            if (_fpsStopwatch.ElapsedMilliseconds >= 1000)
            {
                _fps = _frameCount * 1000.0 / _fpsStopwatch.ElapsedMilliseconds;
                _frameCount = 0;
                _fpsStopwatch.Restart();

                if (ShowInfo)
                {
                    FpsText.Text = $"FPS: {_fps:F1}";
                    LayerText.Text = $"Layer: {_lastRequestedLayer}";
                }
            }
        }

        #endregion

        #region Управление мышью

        private void RenderImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePosition = e.GetPosition(RenderImage);
            _isLeftMouseDown = true;
            RenderImage.CaptureMouse();
            Focus();
        }

        private void RenderImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isLeftMouseDown = false;
            if (!_isRightMouseDown)
                RenderImage.ReleaseMouseCapture();
        }

        private void RenderImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePosition = e.GetPosition(RenderImage);
            _isRightMouseDown = true;
            RenderImage.CaptureMouse();
        }

        private void RenderImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isRightMouseDown = false;
            if (!_isLeftMouseDown)
                RenderImage.ReleaseMouseCapture();
        }

        private void RenderImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isLeftMouseDown && !_isRightMouseDown)
                return;

            WpfPoint currentPosition = e.GetPosition(RenderImage);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;

            if (_isLeftMouseDown)
            {
                // Вращение камеры
                _renderer?.Rotate(deltaX * 0.5, deltaY * 0.3);
            }

            if (_isRightMouseDown)
            {
                // Панорамирование
                _renderer?.Pan(deltaX, deltaY);
            }

            _lastMousePosition = currentPosition;
        }

        private void RenderImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Масштабирование
            _renderer?.Zoom(e.Delta);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.R)
            {
                // Сброс камеры
                _renderer?.ResetCamera();
                Console.WriteLine("[IsometricViewport] Camera reset");
            }
        }

        #endregion
    }
}
