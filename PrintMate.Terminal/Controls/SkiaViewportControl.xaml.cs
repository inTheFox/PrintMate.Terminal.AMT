using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using ProjectParserTest.Parsers.Shared.Models;
using WpfPoint = System.Windows.Point;

namespace PrintMate.Terminal.Controls
{
    /// <summary>
    /// WPF UserControl для GPU-ускоренного рендеринга CLI проектов через SkiaSharp.
    /// Оптимизирован для работы на Intel UHD графике.
    /// </summary>
    public partial class SkiaViewportControl : UserControl
    {
        #region Callbacks

        public Action<bool> OnLoadingStateChanged { get; set; }
        public Action<int?> OnPartClicked { get; set; }
        public Action<int> OnCachingProgress { get; set; }

        #endregion

        #region Приватные поля

        private SkiaLayerRenderer _renderer;
        private DispatcherTimer _renderTimer;
        private Stopwatch _fpsStopwatch;
        private int _frameCount;

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
            DependencyProperty.Register(nameof(ShowInfo), typeof(bool), typeof(SkiaViewportControl),
                new PropertyMetadata(false, OnShowInfoChanged));

        public bool ShowInfo
        {
            get => (bool)GetValue(ShowInfoProperty);
            set => SetValue(ShowInfoProperty, value);
        }

        private static void OnShowInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkiaViewportControl control)
            {
                control.FpsText.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion

        #region Конструктор

        public SkiaViewportControl()
        {
            InitializeComponent();
            _renderer = new SkiaLayerRenderer();
            _fpsStopwatch = new Stopwatch();
        }

        #endregion

        #region Публичные методы

        public void SetCurrentLayerGetter(Func<int> getCurrentLayerFunc)
        {
            _getCurrentLayerFunc = getCurrentLayerFunc;
            Console.WriteLine("[SkiaViewport] CurrentLayer getter configured (PULL model enabled)");
        }

        public void LoadProject(Project project)
        {
            _currentProject = project;
            OnLoadingStateChanged?.Invoke(true);

            _renderer.LoadProject(project);

            OnLoadingStateChanged?.Invoke(false);

            if (project?.Layers != null)
            {
                OnCachingProgress?.Invoke(project.Layers.Count);
            }

            // Перерисовываем
            SkiaCanvas.InvalidateVisual();

            Console.WriteLine($"[SkiaViewport] Project loaded: {project?.Layers?.Count ?? 0} layers");
        }

        public void UpdateLayerVisualization(int layerCount)
        {
            _renderer?.SetCurrentLayer(layerCount);
            SkiaCanvas.InvalidateVisual();
        }

        public void HighlightPart(int? partId)
        {
            // TODO: Реализовать подсветку детали
            Console.WriteLine($"[SkiaViewport] HighlightPart: {partId}");
        }

        #endregion

        #region Обработчики событий

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем проект если уже установлен
            if (_currentProject != null)
            {
                _renderer.LoadProject(_currentProject);
            }

            // Запускаем рендер-цикл для анимации/обновления
            StartRenderLoop();

            Focus();

            Console.WriteLine("[SkiaViewport] Initialized with GPU acceleration");
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopRenderLoop();
            _renderer?.Dispose();
        }

        private void StartRenderLoop()
        {
            _fpsStopwatch.Start();

            // Таймер для обновления (проверка PULL модели)
            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();

            Console.WriteLine("[SkiaViewport] Render loop started (60 FPS target)");
        }

        private void StopRenderLoop()
        {
            _renderTimer?.Stop();
            _fpsStopwatch?.Stop();
        }

        private void OnRenderTick(object sender, EventArgs e)
        {
            // PULL модель: опрашиваем текущий слой
            if (_getCurrentLayerFunc != null)
            {
                int requestedLayer = _getCurrentLayerFunc();
                if (requestedLayer != _lastRequestedLayer && requestedLayer >= 1)
                {
                    _lastRequestedLayer = requestedLayer;
                    _renderer.SetCurrentLayer(requestedLayer);
                    SkiaCanvas.InvalidateVisual();
                }
            }

            // Обновляем FPS
            _frameCount++;
            if (_fpsStopwatch.ElapsedMilliseconds >= 1000)
            {
                double fps = _frameCount * 1000.0 / _fpsStopwatch.ElapsedMilliseconds;
                _frameCount = 0;
                _fpsStopwatch.Restart();

                if (ShowInfo)
                {
                    FpsText.Text = $"FPS: {fps:F1} | Layer: {_lastRequestedLayer}";
                }
            }
        }

        private void SkiaCanvas_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            var info = e.Info;

            _renderer.Render(canvas, info.Width, info.Height);
        }

        #endregion

        #region Управление мышью

        private void SkiaCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePosition = e.GetPosition(SkiaCanvas);
            _isLeftMouseDown = true;
            SkiaCanvas.CaptureMouse();
            Focus();
        }

        private void SkiaCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isLeftMouseDown = false;
            if (!_isRightMouseDown)
                SkiaCanvas.ReleaseMouseCapture();
        }

        private void SkiaCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePosition = e.GetPosition(SkiaCanvas);
            _isRightMouseDown = true;
            SkiaCanvas.CaptureMouse();
        }

        private void SkiaCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isRightMouseDown = false;
            if (!_isLeftMouseDown)
                SkiaCanvas.ReleaseMouseCapture();
        }

        private void SkiaCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isLeftMouseDown && !_isRightMouseDown)
                return;

            WpfPoint currentPosition = e.GetPosition(SkiaCanvas);
            float deltaX = (float)(currentPosition.X - _lastMousePosition.X);
            float deltaY = (float)(currentPosition.Y - _lastMousePosition.Y);

            if (_isLeftMouseDown)
            {
                // Вращение камеры
                _renderer.Rotate(deltaX * 0.5f, deltaY * 0.3f);
                SkiaCanvas.InvalidateVisual();
            }

            if (_isRightMouseDown)
            {
                // Панорамирование
                _renderer.Pan(deltaX, deltaY);
                SkiaCanvas.InvalidateVisual();
            }

            _lastMousePosition = currentPosition;
        }

        private void SkiaCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _renderer.Zoom(e.Delta);
            SkiaCanvas.InvalidateVisual();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.R)
            {
                _renderer.ResetCamera((int)ActualWidth, (int)ActualHeight);
                SkiaCanvas.InvalidateVisual();
                Console.WriteLine("[SkiaViewport] Camera reset");
            }
        }

        #endregion
    }
}
