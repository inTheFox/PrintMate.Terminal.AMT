using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ProjectParserTest.Parsers.Shared.Models;
using WpfPoint = System.Windows.Point;
using WinFormsPanel = System.Windows.Forms.Panel;

namespace PrintMate.Terminal.Controls
{
    /// <summary>
    /// WPF UserControl для GPU-ускоренного рендеринга CLI проектов через Veldrid.
    /// Использует OpenGL бэкенд, оптимизированный для Intel UHD графики.
    /// </summary>
    public partial class VeldridViewportControl : System.Windows.Controls.UserControl
    {
        #region Callbacks

        public Action<bool> OnLoadingStateChanged { get; set; }
        public Action<int?> OnPartClicked { get; set; }
        public Action<int> OnCachingProgress { get; set; }

        #endregion

        #region Приватные поля

        private VeldridLayerRenderer _renderer;
        private WinFormsPanel _renderPanel;
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
            DependencyProperty.Register(nameof(ShowInfo), typeof(bool), typeof(VeldridViewportControl),
                new PropertyMetadata(false, OnShowInfoChanged));

        public bool ShowInfo
        {
            get => (bool)GetValue(ShowInfoProperty);
            set => SetValue(ShowInfoProperty, value);
        }

        private static void OnShowInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VeldridViewportControl control)
            {
                control.FpsText.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion

        #region Конструктор

        public VeldridViewportControl()
        {
            InitializeComponent();
            _renderer = new VeldridLayerRenderer();
            _fpsStopwatch = new Stopwatch();
        }

        #endregion

        #region Публичные методы

        public void SetCurrentLayerGetter(Func<int> getCurrentLayerFunc)
        {
            _getCurrentLayerFunc = getCurrentLayerFunc;
            Console.WriteLine("[VeldridViewport] CurrentLayer getter configured (PULL model enabled)");
        }

        public void LoadProject(Project project)
        {
            _currentProject = project;
            OnLoadingStateChanged?.Invoke(true);

            if (_renderer.IsInitialized)
            {
                _renderer.LoadProject(project);
            }

            OnLoadingStateChanged?.Invoke(false);

            if (project?.Layers != null)
            {
                OnCachingProgress?.Invoke(project.Layers.Count);
            }

            Console.WriteLine($"[VeldridViewport] Project loaded: {project?.Layers?.Count ?? 0} layers");
        }

        public void UpdateLayerVisualization(int layerCount)
        {
            _renderer?.SetCurrentLayer(layerCount);
        }

        public void HighlightPart(int? partId)
        {
            Console.WriteLine($"[VeldridViewport] HighlightPart: {partId}");
        }

        #endregion

        #region Обработчики событий

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Создаём WinForms панель для Veldrid
            _renderPanel = new WinFormsPanel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Black
            };

            // Добавляем обработчики мыши
            _renderPanel.MouseDown += RenderPanel_MouseDown;
            _renderPanel.MouseUp += RenderPanel_MouseUp;
            _renderPanel.MouseMove += RenderPanel_MouseMove;
            _renderPanel.MouseWheel += RenderPanel_MouseWheel;
            _renderPanel.Resize += RenderPanel_Resize;

            WinFormsHost.Child = _renderPanel;

            // Даём время на инициализацию WinForms контрола
            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitializeVeldrid();
            }), DispatcherPriority.Loaded);
        }

        private void InitializeVeldrid()
        {
            try
            {
                if (_renderPanel.Handle == IntPtr.Zero)
                {
                    Console.WriteLine("[VeldridViewport] Panel handle not ready, retrying...");
                    Dispatcher.BeginInvoke(new Action(InitializeVeldrid), DispatcherPriority.Background);
                    return;
                }

                int width = Math.Max(1, _renderPanel.Width);
                int height = Math.Max(1, _renderPanel.Height);

                _renderer.Initialize(_renderPanel.Handle, width, height);

                // Загружаем проект если уже установлен
                if (_currentProject != null)
                {
                    _renderer.LoadProject(_currentProject);
                }

                // Запускаем рендер-цикл
                StartRenderLoop();

                Console.WriteLine("[VeldridViewport] Initialized with GPU acceleration");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VeldridViewport] Initialization failed: {ex.Message}");
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopRenderLoop();
            _renderer?.Dispose();

            if (_renderPanel != null)
            {
                _renderPanel.MouseDown -= RenderPanel_MouseDown;
                _renderPanel.MouseUp -= RenderPanel_MouseUp;
                _renderPanel.MouseMove -= RenderPanel_MouseMove;
                _renderPanel.MouseWheel -= RenderPanel_MouseWheel;
                _renderPanel.Resize -= RenderPanel_Resize;
            }
        }

        private void StartRenderLoop()
        {
            _fpsStopwatch.Start();

            _renderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();

            Console.WriteLine("[VeldridViewport] Render loop started (60 FPS target)");
        }

        private void StopRenderLoop()
        {
            _renderTimer?.Stop();
            _fpsStopwatch?.Stop();
        }

        private void OnRenderTick(object sender, EventArgs e)
        {
            if (!_renderer.IsInitialized) return;

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

            // Рендерим
            _renderer.Render();

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

        private void RenderPanel_Resize(object sender, EventArgs e)
        {
            if (_renderer.IsInitialized && _renderPanel.Width > 0 && _renderPanel.Height > 0)
            {
                _renderer.Resize(_renderPanel.Width, _renderPanel.Height);
            }
        }

        #endregion

        #region Управление мышью

        private void RenderPanel_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            _lastMousePosition = new WpfPoint(e.X, e.Y);

            if (e.Button == MouseButtons.Left)
            {
                _isLeftMouseDown = true;
                _renderPanel.Capture = true;
            }
            else if (e.Button == MouseButtons.Right)
            {
                _isRightMouseDown = true;
                _renderPanel.Capture = true;
            }

            _renderPanel.Focus();
        }

        private void RenderPanel_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isLeftMouseDown = false;
            }
            else if (e.Button == MouseButtons.Right)
            {
                _isRightMouseDown = false;
            }

            if (!_isLeftMouseDown && !_isRightMouseDown)
            {
                _renderPanel.Capture = false;
            }
        }

        private void RenderPanel_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!_isLeftMouseDown && !_isRightMouseDown)
                return;

            float deltaX = e.X - (float)_lastMousePosition.X;
            float deltaY = e.Y - (float)_lastMousePosition.Y;

            if (_isLeftMouseDown)
            {
                // Вращение камеры
                _renderer.Rotate(deltaX * 0.5f, deltaY * 0.3f);
            }

            if (_isRightMouseDown)
            {
                // Панорамирование
                _renderer.Pan(deltaX, deltaY);
            }

            _lastMousePosition = new WpfPoint(e.X, e.Y);
        }

        private void RenderPanel_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            _renderer.ZoomBy(e.Delta);
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.R)
            {
                _renderer.ResetCamera();
                Console.WriteLine("[VeldridViewport] Camera reset");
            }
        }

        #endregion
    }
}
