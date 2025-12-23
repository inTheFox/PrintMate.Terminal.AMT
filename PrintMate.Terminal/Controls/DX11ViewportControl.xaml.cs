using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpDX;
using D3D9 = SharpDX.Direct3D9;
using PrintMate.Terminal.Rendering;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Controls
{
    /// <summary>
    /// WPF UserControl для DirectX 11 рендеринга CLI проектов
    /// </summary>
    public partial class DX11ViewportControl : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty ShowInfoProperty =
            DependencyProperty.Register(nameof(ShowInfo), typeof(bool), typeof(DX11ViewportControl),
                new PropertyMetadata(true));

        public bool ShowInfo
        {
            get => (bool)GetValue(ShowInfoProperty);
            set => SetValue(ShowInfoProperty, value);
        }

        #endregion

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
        /// Callback для уведомления о прогрессе кеширования (текущий закешированный слой)
        /// </summary>
        public Action<int> OnCachingProgress { get; set; }

        #endregion

        #region Приватные поля

        private D3DImage _d3dImage;
        private D3D9.Direct3DEx _d3d9;
        private D3D9.DeviceEx _d3d9Device;
        private D3D9.Texture _d3d9RenderTarget;

        private DX11Renderer _renderer;
        private OrbitCamera _camera;
        private CliGeometryBuilder _geometryBuilder;

        // Меши
        private CliMesh _platformMesh;
        private CliMesh _platformEdgesMesh;
        private CliMesh _platformShadowMesh;
        private CliMesh _platformGridMesh;
        private CliMesh _platformAxesMesh;
        private CliMesh _boundaryWireframeMesh;
        private CliMesh _printedLayersMesh;
        private CliMesh _currentLayerHatchMesh;

        // Управление мышью
        private System.Windows.Point _lastMousePosition;
        private System.Windows.Point _mouseDownPosition;
        private bool _isLeftMouseDown;
        private bool _isMiddleMouseDown;
        private bool _mouseMoved;

        // Данные проекта
        private Project _currentProject;
        private int _currentLayerIndex;
        private int? _highlightedPartId = null;

        // PULL модель: Viewport запрашивает текущий слой из ViewModel
        private Func<int> _getCurrentLayerFunc;

        // Picking data - храним треугольники с привязкой к деталям
        private List<(Vector3 v0, Vector3 v1, Vector3 v2, int? partId)> _pickingTriangles = new List<(Vector3, Vector3, Vector3, int?)>();

        // Состояние
        private bool _isInitialized;
        private bool _firstRenderLogged;

        // Async mesh building
        private CancellationTokenSource _meshBuildCancellation;
        private bool _isBuilding;
        private int? _pendingLayerIndex = null; // Запомненный запрос на обновление слоя

        // Данные для построения геометрии
        private int _lastBuiltLayer = -1;
        private (float centerX, float centerY, float maxRadius) _projectCenter;
        private List<Vertex> _accumulatedVertices = new List<Vertex>();
        private List<uint> _accumulatedIndices = new List<uint>();

        // КЕШ геометрии по слоям для быстрого переключения
        private Dictionary<int, (List<Vertex> vertices, List<uint> indices)> _layerGeometryCache = new Dictionary<int, (List<Vertex>, List<uint>)>();
        private Task _cachePreloadTask;
        private CancellationTokenSource _cachePreloadCancellation;
        
        // КУМУЛЯТИВНЫЙ КЕШ: хранит объединённую геометрию для слоёв 0..N
        private Dictionary<int, (Vertex[] vertices, uint[] indices)> _cumulativeGeometryCache = new Dictionary<int, (Vertex[], uint[])>();
        private const int MaxCumulativeCacheSize = 100;

        #endregion

        #region Конструктор

        public DX11ViewportControl()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        #endregion

        #region Инициализация

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            
            InitializeDirectX();
            StartRenderLoop();

            // Подписываемся на события окна для обработки сворачивания/разворачивания
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.StateChanged += OnWindowStateChanged;
                window.Activated += OnWindowActivated;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Отписываемся от событий окна
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.StateChanged -= OnWindowStateChanged;
                window.Activated -= OnWindowActivated;
            }

            // Отменяем фоновые задачи построения мешей
            _meshBuildCancellation?.Cancel();
            _meshBuildCancellation?.Dispose();

            StopRenderLoop();
            CleanupDirectX();
        }

        private void InitializeDirectX()
        {
            try
            {
                // Создаём D3DImage для интеграции DirectX с WPF
                _d3dImage = new D3DImage();
                D3DImageHost.Source = _d3dImage;

                // Подписываемся на события мыши и клавиатуры
                D3DImageHost.MouseDown += OnImageMouseDown;
                D3DImageHost.MouseUp += OnImageMouseUp;
                D3DImageHost.MouseMove += OnImageMouseMove;
                D3DImageHost.MouseWheel += OnImageMouseWheel;
                this.SizeChanged += OnSizeChanged;

                // Получаем размеры
                int width = Math.Max((int)ActualWidth, 800);
                int height = Math.Max((int)ActualHeight, 600);

                // Инициализируем DX9 для D3DImage (требуется для WPF interop)
                InitializeD3D9(width, height);

                // Инициализируем DirectX 11 рендерер (offscreen)
                _renderer = new DX11Renderer();
                _renderer.InitializeOffscreen(width, height);

                // Открываем shared resource между DX11 и DX9
                OpenSharedResource();

                // Инициализируем камеру
                _camera = new OrbitCamera(distance: 600f, azimuth: 45f, elevation: 30f);
                _camera.AspectRatio = (float)width / height;

                // Создаём geometry builder
                _geometryBuilder = new CliGeometryBuilder(_renderer.Device);

                // Создаём платформу 320x320 мм с толщиной 10мм (1см), чёрный цвет, Alpha=0 для partId=0
                _platformMesh = _geometryBuilder.BuildPlatformMesh(320f, 10f, new Color4(0f, 0f, 0f, 0.0f));

                // Создаём контур платформы для визуализации краёв (тёмно-серый RGB 60,60,60)
                _platformEdgesMesh = _geometryBuilder.BuildPlatformEdgesMesh(320f, 10f, new Color4(60f / 255f, 60f / 255f, 60f / 255f, 1.0f));

                // Создаём координатные оси X и Y (цвет #595959 - RGB 89,89,89)
                _platformAxesMesh = _geometryBuilder.BuildPlatformAxesMesh(320f);

                // Тень отключена (не работает корректно с текущей системой рендеринга)
                // _platformShadowMesh = _geometryBuilder.BuildPlatformShadowMesh(320f, 450f, 20f);

                // Сетка и границы отключены
                _platformGridMesh = null;
                _boundaryWireframeMesh = null;

                // Устанавливаем DX9 surface в D3DImage
                SetD3DImageBackBuffer();

                _isInitialized = true;

                

                // Если проект был загружен до инициализации, загружаем его сейчас
                if (_currentProject != null && _currentProject.Layers != null && _currentProject.Layers.Count > 0)
                {
                    
                    OnLoadingStateChanged?.Invoke(true); // Начинаем загрузку

                    // Запускаем фоновое кеширование (теперь _geometryBuilder готов!)
                    StartBackgroundCaching();

                    FocusOnProject();
                    // НЕ вызываем UpdateLayerVisualization - это делается через ViewModel.CurrentLayer
                }
            }
            catch (Exception ex)
            {
                
                
                MessageBox.Show($"Ошибка инициализации DirectX:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeD3D9(int width, int height)
        {
            // Создаём DirectX 9 для WPF D3DImage interop
            _d3d9 = new D3D9.Direct3DEx();

            var presentParams = new D3D9.PresentParameters
            {
                BackBufferWidth = width,
                BackBufferHeight = height,
                BackBufferFormat = D3D9.Format.A8R8G8B8,
                BackBufferCount = 1,
                SwapEffect = D3D9.SwapEffect.Discard,
                DeviceWindowHandle = IntPtr.Zero,
                Windowed = true,
                PresentationInterval = D3D9.PresentInterval.Immediate
            };

            _d3d9Device = new D3D9.DeviceEx(
                _d3d9,
                0,
                D3D9.DeviceType.Hardware,
                IntPtr.Zero,
                D3D9.CreateFlags.HardwareVertexProcessing | D3D9.CreateFlags.Multithreaded | D3D9.CreateFlags.FpuPreserve,
                presentParams
            );

            
        }

        private void OpenSharedResource()
        {
            // Открываем DX11 текстуру как DX9 текстуру через shared handle
            IntPtr sharedHandle = _renderer.BackBufferPtr;

            _d3d9RenderTarget = new D3D9.Texture(
                _d3d9Device,
                _renderer.BackBufferWidth,
                _renderer.BackBufferHeight,
                1,
                D3D9.Usage.RenderTarget,
                D3D9.Format.A8R8G8B8,
                D3D9.Pool.Default,
                ref sharedHandle
            );

            
        }

        private void SetD3DImageBackBuffer()
        {
            using (var surface = _d3d9RenderTarget.GetSurfaceLevel(0))
            {
                _d3dImage.Lock();
                _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                _d3dImage.Unlock();
            }

            
        }

        private void CleanupDirectX()
        {
            _isInitialized = false;

            _platformMesh?.Dispose();
            _printedLayersMesh?.Dispose();
            _currentLayerHatchMesh?.Dispose();

            _geometryBuilder = null;
            _renderer?.Dispose();

            _d3d9RenderTarget?.Dispose();
            _d3d9Device?.Dispose();
            _d3d9?.Dispose();

            _d3dImage = null;

            
        }

        #endregion

        #region Рендер цикл

        private void StartRenderLoop()
        {
            // Используем CompositionTarget.Rendering для sync с WPF (vsync)
            CompositionTarget.Rendering += OnCompositionTargetRendering;

            
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            if (!_isInitialized)
                return;

            try
            {
                Render();
            }
            catch (Exception ex)
            {
                
                
            }
        }

        private void StopRenderLoop()
        {
            CompositionTarget.Rendering -= OnCompositionTargetRendering;

            
        }

        private void Render()
        {
            if (!_isInitialized || _renderer == null || _d3dImage == null)
            {
                if (!_firstRenderLogged)
                {
                    
                    _firstRenderLogged = true;
                }
                return;
            }

            if (!_firstRenderLogged)
            {
                
                _firstRenderLogged = true;
            }

            // ===== PULL МОДЕЛЬ: Опрашиваем текущий слой из ViewModel =====
            if (_getCurrentLayerFunc != null && _currentProject != null)
            {
                int requestedLayer = _getCurrentLayerFunc();

                // Конвертируем 1-based в 0-based для сравнения
                int requestedLayerIndex = requestedLayer - 1;

                // Если слой изменился, обновляем визуализацию
                if (requestedLayerIndex != _currentLayerIndex && requestedLayer >= 1)
                {
                    _currentLayerIndex = requestedLayerIndex;

                    // Запускаем асинхронное обновление геометрии
                    UpdateLayerVisualizationInternal(requestedLayerIndex);
                }
            }

            // Получаем матрицы
            SharpDX.Matrix world = SharpDX.Matrix.Identity;
            SharpDX.Matrix view = _camera.GetViewMatrix();
            SharpDX.Matrix projection = _camera.GetProjectionMatrix();

            _renderer.SetMatrices(world, view, projection, _camera.Position);

            // ===== SHADOW PASS: Рендерим в shadow map =====
            _renderer.BeginShadowPass();

            // Рендерим все объекты в shadow map
            if (_platformMesh != null)
                _renderer.DrawMeshShadow(_platformMesh);

            if (_printedLayersMesh != null)
                _renderer.DrawMeshShadow(_printedLayersMesh);

            if (_currentLayerHatchMesh != null)
                _renderer.DrawMeshShadow(_currentLayerHatchMesh);

            _renderer.EndShadowPass();

            // ===== MAIN PASS: Рендерим с тенями =====
            // Фон RGB(20,20,20)
            _renderer.BeginFrame(new Color4(20f / 255f, 20f / 255f, 20f / 255f, 1.0f));

            // Рендерим платформу
            if (_platformMesh != null)
            {
                _renderer.DrawMesh(_platformMesh);
            }

            // Рендерим контур платформы (wireframe для визуализации краёв)
            if (_platformEdgesMesh != null)
            {
                _renderer.DrawWireframe(_platformEdgesMesh);
            }

            // Рендерим координатные оси X и Y (как solid прямоугольники)
            if (_platformAxesMesh != null)
            {
                _renderer.DrawMesh(_platformAxesMesh);
            }

            // Рендерим сетку на платформе
            if (_platformGridMesh != null)
            {
                _renderer.DrawWireframe(_platformGridMesh);
            }

            // Рендерим рамку границ рабочей области (wireframe)
            if (_boundaryWireframeMesh != null)
            {
                _renderer.DrawWireframe(_boundaryWireframeMesh);
            }

            // Рендерим напечатанные слои
            if (_printedLayersMesh != null)
            {
                _renderer.DrawMesh(_printedLayersMesh);
            }

            // Рендерим штриховку текущего слоя
            if (_currentLayerHatchMesh != null)
            {
                _renderer.DrawMesh(_currentLayerHatchMesh);
            }

            _renderer.EndFrame();

            // ВАЖНО: Lock/Unlock D3DImage для синхронизации с DirectX
            _d3dImage.Lock();
            _d3dImage.AddDirtyRect(new Int32Rect(0, 0, _d3dImage.PixelWidth, _d3dImage.PixelHeight));
            _d3dImage.Unlock();
        }

        #endregion

        #region Управление проектом

        /// <summary>
        /// Устанавливает функцию для получения текущего слоя (PULL модель)
        /// </summary>
        public void SetCurrentLayerGetter(Func<int> getCurrentLayerFunc)
        {
            _getCurrentLayerFunc = getCurrentLayerFunc;
            
        }

        /// <summary>
        /// Загружает CLI проект для визуализации
        /// </summary>
        public void LoadProject(Project project)
        {
            _currentProject = project;

            // Сбрасываем состояние при загрузке нового проекта
            _lastBuiltLayer = -1;
            _accumulatedVertices.Clear();
            _accumulatedIndices.Clear();
            _layerGeometryCache.Clear();

            // Очищаем кумулятивный кеш (ВАЖНО при смене проекта!)
            lock (_cumulativeGeometryCache)
            {
                _cumulativeGeometryCache.Clear();
            }

            // Освобождаем старые меши
            _printedLayersMesh?.Dispose();
            _printedLayersMesh = null;
            _currentLayerHatchMesh?.Dispose();
            _currentLayerHatchMesh = null;

            // ВАЖНО: Очищаем статический кеш в CliGeometryBuilder для применения новых алгоритмов
            CliGeometryBuilder.ClearGeometryCache();

            // Очищаем пул буферов геометрии
            _geometryBuilder?.ClearPool();

            // Отменяем предыдущую задачу кеширования
            _cachePreloadCancellation?.Cancel();

            if (project != null && project.Layers != null && project.Layers.Count > 0)
            {
                

                // Вычисляем центр проекта для градиента
                _projectCenter = CalculateProjectCenter(project, project.Layers.Count);
                

                // Запускаем фоновое кеширование ВСЕХ слоёв (РАНЬШЕ проверки _isInitialized)
                // Кеширование геометрии не требует DirectX, только парсинга данных
                StartBackgroundCaching();

                // Проверяем, что DirectX инициализирован
                if (!_isInitialized)
                {
                    
                    return;
                }

                // Автоматически фокусируемся на проекте
                FocusOnProject();

                // НЕ вызываем UpdateLayerVisualization - это делается через ViewModel.CurrentLayer
            }
        }

        /// <summary>
        /// Обновляет визуализацию до указанного слоя (1-based индекс из UI)
        /// </summary>
        public void UpdateLayerVisualization(int layerCount)
        {
            if (_currentProject == null || !_isInitialized || layerCount < 1)
                return;

            // Конвертируем 1-based в 0-based: layerCount=1 → показать слой 0
            int layerIndex = layerCount - 1;
            _currentLayerIndex = layerIndex;
            UpdateLayerVisualizationInternal(layerIndex);
        }


        /// <summary>
        /// Внутренний метод для обновления визуализации слоя (без анимации)
        /// </summary>
        private async void UpdateLayerVisualizationInternal(int layerIndex)
        {
            if (_currentProject == null || !_isInitialized || layerIndex < 0)
                return;

            // Отменяем предыдущую задачу построения, если она ещё выполняется
            var oldCancellation = _meshBuildCancellation;
            _meshBuildCancellation = new CancellationTokenSource();
            var cancellationToken = _meshBuildCancellation.Token;

            // Отменяем старую задачу и освобождаем ресурсы
            try
            {
                oldCancellation?.Cancel();
                oldCancellation?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Игнорируем, если уже освобождён
            }

            _currentLayerIndex = layerIndex;

            // Если уже идёт построение, запоминаем новый запрос и выходим
            if (_isBuilding)
            {
                _pendingLayerIndex = layerIndex;
                return;
            }

            _isBuilding = true;
            _pendingLayerIndex = null; // Сбрасываем pending при начале обработки

            try
            {
                // Строим геометрию в фоновом потоке
                var meshes = await Task.Run(() => BuildMeshesAsync(layerIndex, cancellationToken), cancellationToken);

                // Проверяем, не была ли задача отменена
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Если meshes == null, значит геометрия не изменилась (переиспользуем существующий меш)
                // Это нормально и избегает мигания
                if (meshes.HasValue)
                {
                    // Обновляем меши в UI потоке (DirectX ресурсы уже созданы в фоновом потоке)
                    await Dispatcher.InvokeAsync(() =>
                    {
                        // Только если пришёл новый меш - заменяем старый
                        if (meshes.Value.printedLayers != null)
                        {
                            _printedLayersMesh?.Dispose();
                            _printedLayersMesh = meshes.Value.printedLayers;
                        }

                        if (meshes.Value.currentHatch != null)
                        {
                            _currentLayerHatchMesh?.Dispose();
                            _currentLayerHatchMesh = meshes.Value.currentHatch;
                        }
                    });
                }

                OnLoadingStateChanged?.Invoke(false); // Загрузка завершена

                // Предзагрузка соседних слоёв в фоне (не блокирует UI)
                _ = PreloadAdjacentLayersAsync(layerIndex, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                OnLoadingStateChanged?.Invoke(false); // Загрузка отменена
            }
            catch (Exception ex)
            {
                
                OnLoadingStateChanged?.Invoke(false); // Загрузка завершена с ошибкой
            }
            finally
            {
                _isBuilding = false;

                // Если во время построения пришёл новый запрос, обрабатываем его
                if (_pendingLayerIndex.HasValue)
                {
                    int pending = _pendingLayerIndex.Value;
                    _pendingLayerIndex = null;
                    UpdateLayerVisualizationInternal(pending);
                }
            }
        }

        /// <summary>
        /// Предзагружает геометрию соседних слоёв в кумулятивный кеш
        /// Вызывается в фоне после успешного построения текущего слоя
        /// </summary>
        private async Task PreloadAdjacentLayersAsync(int currentLayer, CancellationToken cancellationToken)
        {
            if (_currentProject?.Layers == null)
                return;

            int maxLayer = _currentProject.Layers.Count - 1;

            // Предзагружаем слои: +1, +2, +3 (вперёд более приоритетно)
            int[] layersToPreload = new[]
            {
                currentLayer + 1,
                currentLayer + 2,
                currentLayer + 3,
                currentLayer - 1
            };

            await Task.Run(() =>
            {
                foreach (int layerIndex in layersToPreload)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (layerIndex < 0 || layerIndex > maxLayer)
                        continue;

                    // Проверяем, есть ли уже в кеше
                    bool alreadyCached;
                    lock (_cumulativeGeometryCache)
                    {
                        alreadyCached = _cumulativeGeometryCache.ContainsKey(layerIndex);
                    }

                    if (alreadyCached)
                        continue;

                    // Строим геометрию для этого слоя (тихо, без обновления UI)
                    BuildCumulativeGeometryForLayer(layerIndex, cancellationToken);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Строит кумулятивную геометрию для указанного слоя и кеширует её
        /// </summary>
        private void BuildCumulativeGeometryForLayer(int layerIndex, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            // Ищем ближайший закешированный слой
            int nearestCachedLayer = -1;
            Vertex[] baseVertices = null;
            uint[] baseIndices = null;

            lock (_cumulativeGeometryCache)
            {
                for (int i = layerIndex - 1; i >= 0; i--)
                {
                    if (_cumulativeGeometryCache.TryGetValue(i, out var cached))
                    {
                        nearestCachedLayer = i;
                        baseVertices = cached.vertices;
                        baseIndices = cached.indices;
                        break;
                    }
                }
            }

            // Строим инкрементально
            var tempVertices = new List<Vertex>();
            var tempIndices = new List<uint>();

            if (baseVertices != null)
            {
                tempVertices.AddRange(baseVertices);
                tempIndices.AddRange(baseIndices);
            }

            int startLayer = nearestCachedLayer + 1;
            lock (_layerGeometryCache)
            {
                for (int i = startLayer; i <= layerIndex; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (_layerGeometryCache.TryGetValue(i, out var layerGeometry))
                    {
                        uint baseIndex = (uint)tempVertices.Count;
                        tempVertices.AddRange(layerGeometry.vertices);
                        for (int j = 0; j < layerGeometry.indices.Count; j++)
                        {
                            tempIndices.Add(layerGeometry.indices[j] + baseIndex);
                        }
                    }
                }
            }

            if (tempVertices.Count > 0)
            {
                var vertices = tempVertices.ToArray();
                var indices = tempIndices.ToArray();

                lock (_cumulativeGeometryCache)
                {
                    if (_cumulativeGeometryCache.Count >= MaxCumulativeCacheSize)
                    {
                        // Удаляем самый дальний от текущего слоя
                        int toRemove = _cumulativeGeometryCache.Keys
                            .OrderByDescending(k => Math.Abs(k - layerIndex))
                            .First();
                        _cumulativeGeometryCache.Remove(toRemove);
                    }
                    _cumulativeGeometryCache[layerIndex] = (vertices, indices);
                }
            }
        }

        /// <summary>
        /// Строит меши из КЕША (мгновенно)
        /// Кеш гарантированно заполнен, т.к. анимация загрузки ждёт его завершения
        /// </summary>
        private (CliMesh printedLayers, CliMesh currentHatch)? BuildMeshesAsync(int layerIndex, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            Vertex[] vertices = null;
            uint[] indices = null;

            // ОПТИМИЗАЦИЯ 1: Проверяем кумулятивный кеш (мгновенное попадание)
            lock (_cumulativeGeometryCache)
            {
                if (_cumulativeGeometryCache.TryGetValue(layerIndex, out var cached))
                {
                    vertices = cached.vertices;
                    indices = cached.indices;
                }
            }

            // ОПТИМИЗАЦИЯ 2: Инкрементальное построение от ближайшего закешированного слоя
            if (vertices == null)
            {
                int nearestCachedLayer = -1;
                Vertex[] baseVertices = null;
                uint[] baseIndices = null;

                lock (_cumulativeGeometryCache)
                {
                    for (int i = layerIndex - 1; i >= 0; i--)
                    {
                        if (_cumulativeGeometryCache.TryGetValue(i, out var cached))
                        {
                            nearestCachedLayer = i;
                            baseVertices = cached.vertices;
                            baseIndices = cached.indices;
                            break;
                        }
                    }
                }

                _accumulatedVertices.Clear();
                _accumulatedIndices.Clear();

                if (baseVertices != null)
                {
                    _accumulatedVertices.AddRange(baseVertices);
                    _accumulatedIndices.AddRange(baseIndices);
                }

                int startLayer = nearestCachedLayer + 1;
                lock (_layerGeometryCache)
                {
                    for (int i = startLayer; i <= layerIndex; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return null;

                        if (_layerGeometryCache.TryGetValue(i, out var layerGeometry))
                        {
                            uint baseIndex = (uint)_accumulatedVertices.Count;
                            _accumulatedVertices.AddRange(layerGeometry.vertices);
                            // Используем for вместо LINQ Select для скорости
                            for (int j = 0; j < layerGeometry.indices.Count; j++)
                            {
                                _accumulatedIndices.Add(layerGeometry.indices[j] + baseIndex);
                            }
                        }
                    }
                }

                if (_accumulatedVertices.Count > 0)
                {
                    vertices = _accumulatedVertices.ToArray();
                    indices = _accumulatedIndices.ToArray();

                    lock (_cumulativeGeometryCache)
                    {
                        if (_cumulativeGeometryCache.Count >= MaxCumulativeCacheSize)
                        {
                            int toRemove = _cumulativeGeometryCache.Keys
                                .OrderByDescending(k => Math.Abs(k - layerIndex))
                                .First();
                            _cumulativeGeometryCache.Remove(toRemove);
                        }
                        _cumulativeGeometryCache[layerIndex] = (vertices, indices);
                    }
                }
            }

            // Создаём меш из закешированной геометрии
            CliMesh currentLayerHatchMesh = null;
            if (vertices != null && vertices.Length > 0 && indices != null && indices.Length > 0)
            {
                currentLayerHatchMesh = _geometryBuilder.CreateMesh(vertices, indices);
            }

            return (null, currentLayerHatchMesh);

            // ... остальной закомментированный код можно удалить или оставить
        }

        /// <summary>
        /// Fallback: строит геометрию напрямую если кеш неполный
        /// НОВАЯ ЛОГИКА: Использует глобальную триангуляцию Triangle.NET для литого вида
        /// </summary>
        private (CliMesh printedLayers, CliMesh currentHatch)? BuildMeshesDirectly(int layerIndex, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            // НОВЫЙ ПОДХОД: Используем глобальную триангуляцию для всех слоёв до текущего
            var (vertices, indices) = _geometryBuilder.BuildGlobalUnifiedMesh(
                _currentProject,
                layerIndex - 1, // Строим до текущего слоя (не включая текущий)
                _projectCenter.centerX,
                _projectCenter.centerY,
                _projectCenter.maxRadius
            );

            _lastBuiltLayer = layerIndex;

            CliMesh printedLayersMesh = null;
            if (vertices.Count > 0 && indices.Count > 0)
            {
                var geometryList = new List<(List<Vertex> vertices, List<uint> indices)>
                {
                    (vertices, indices)
                };
                printedLayersMesh = _geometryBuilder.MergeGeometryToMesh(geometryList);
            }

            BuildPickingData(layerIndex);

            // ОПТИМИЗИРОВАННАЯ ЛОГИКА: Рисуем контуры и Hatch для ВСЕХ слоёв (от 0 до текущего)
            CliMesh currentLayerHatchMesh = null;

            // Строим контуры и Hatch для всех слоёв от 0 до layerIndex одним вызовом (цвета определяются внутри метода)
            var (allContourVerts, allContourInds) = _geometryBuilder.BuildAllLayersContoursAndHatch(
                _currentProject,
                layerIndex,
                _projectCenter.centerX,
                _projectCenter.centerY,
                _projectCenter.maxRadius
            );

            if (allContourVerts.Count > 0 && allContourInds.Count > 0)
            {
                var combinedGeometries = new List<(List<Vertex> vertices, List<uint> indices)>
                {
                    (allContourVerts, allContourInds)
                };
                currentLayerHatchMesh = _geometryBuilder.MergeGeometryToMesh(combinedGeometries);
            }

            return (printedLayersMesh, currentLayerHatchMesh);
        }

        /// <summary>
        /// Строит данные для ray picking (треугольники всех видимых граней с привязкой к деталям)
        /// </summary>
        private void BuildPickingData(int layerIndex)
        {
            _pickingTriangles.Clear();

            if (_currentProject == null || _currentProject.Layers == null || _currentProject.Layers.Count == 0)
                return;

            // layerIndex начинается с 0, проверяем что он в допустимых пределах
            if (layerIndex < 0 || layerIndex >= _currentProject.Layers.Count)
                return;

            // Собираем контуры для каждого слоя с информацией о детали
            var layerContours = new List<(float zBottom, float zTop, List<(List<ProjectParserTest.Parsers.Shared.Models.Point> points, int? partId)> contours)>();

            // layer.Height содержит абсолютную Z позицию в мм
            float layerThickness = _currentProject.GetLayerThicknessInMillimeters();

            // Строим picking для всех напечатанных слоёв (от 0 до текущего включительно)
            for (int i = 0; i <= layerIndex; i++)
            {
                var layer = _currentProject.Layers[i];
                float zTop = (float)layer.Height;
                if (zTop < 0.001f) zTop = (i + 1) * layerThickness;
                float zBottom = i == 0 ? 0 : (float)_currentProject.Layers[i - 1].Height;
                if (zBottom < 0.001f) zBottom = i * layerThickness;

                var contoursAtThisZ = new List<(List<ProjectParserTest.Parsers.Shared.Models.Point> points, int? partId)>();

                if (layer.Regions != null)
                {
                    foreach (var region in layer.Regions)
                    {
                        if (region.PolyLines == null)
                            continue;

                        // Берём preview регионы для гладкой заливки, либо обычные регионы если preview нет
                        // Также включаем контуры и края для отображения
                        if (region.GeometryRegion == ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.InfillRegionPreview ||
                            region.GeometryRegion == ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.UpskinRegionPreview ||
                            region.GeometryRegion == ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.DownskinRegionPreview ||
                            region.GeometryRegion == ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.Infill ||
                            region.GeometryRegion == ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.Upskin ||
                            region.GeometryRegion == ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.Downskin ||
                            region.GeometryRegion == ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.Edges ||
                            region.GeometryRegion == ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.Contour ||
                            region.GeometryRegion == ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.ContourUpskin ||
                            region.GeometryRegion == ProjectParserTest.Parsers.Shared.Enums.GeometryRegion.ContourDownskin)
                        {
                            foreach (var polyline in region.PolyLines)
                            {
                                if (polyline.Points != null && polyline.Points.Count >= 3)
                                {
                                    contoursAtThisZ.Add((polyline.Points, region.Part?.Id));
                                }
                            }
                        }
                    }
                }

                if (contoursAtThisZ.Count > 0)
                {
                    layerContours.Add((zBottom, zTop, contoursAtThisZ));
                }
            }

            // Добавляем треугольники для picking
            int topTriangles = 0;
            int sideTriangles = 0;

            foreach (var layerContour in layerContours)
            {
                foreach (var contour in layerContour.contours)
                {
                    // Верхняя поверхность (веерная триангуляция)
                    for (int j = 1; j < contour.points.Count - 1; j++)
                    {
                        Vector3 v0 = new Vector3(contour.points[0].X, contour.points[0].Y, layerContour.zTop);
                        Vector3 v1 = new Vector3(contour.points[j].X, contour.points[j].Y, layerContour.zTop);
                        Vector3 v2 = new Vector3(contour.points[j + 1].X, contour.points[j + 1].Y, layerContour.zTop);
                        _pickingTriangles.Add((v0, v1, v2, contour.partId));
                        topTriangles++;
                    }

                    // Боковые грани (вертикальные стенки)
                    for (int j = 0; j < contour.points.Count; j++)
                    {
                        int next = (j + 1) % contour.points.Count;
                        var p1 = contour.points[j];
                        var p2 = contour.points[next];

                        // Два треугольника для каждой боковой грани
                        // Создаём оба порядка обхода для двусторонней видимости
                        Vector3 v0 = new Vector3(p1.X, p1.Y, layerContour.zBottom);
                        Vector3 v1 = new Vector3(p1.X, p1.Y, layerContour.zTop);
                        Vector3 v2 = new Vector3(p2.X, p2.Y, layerContour.zTop);
                        Vector3 v3 = new Vector3(p2.X, p2.Y, layerContour.zBottom);

                        // Наружная сторона (против часовой стрелки)
                        _pickingTriangles.Add((v0, v1, v2, contour.partId));
                        _pickingTriangles.Add((v0, v2, v3, contour.partId));

                        // Внутренняя сторона (по часовой стрелке) - для двусторонней видимости
                        _pickingTriangles.Add((v2, v1, v0, contour.partId));
                        _pickingTriangles.Add((v3, v2, v0, contour.partId));

                        sideTriangles += 4;
                    }
                }
            }

            
        }

        /// <summary>
        /// Фокусирует камеру на проекте
        /// </summary>
        private void FocusOnProject()
        {
            if (_currentProject == null || _currentProject.Layers == null)
                return;

            // Вычисляем bounding box проекта
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = 0;

            // layer.Height содержит абсолютную Z позицию в мм
            float layerThickness = _currentProject.GetLayerThicknessInMillimeters();
            var lastLayer = _currentProject.Layers[^1];
            float maxZ = (float)lastLayer.Height;
            if (maxZ < 0.001f) maxZ = _currentProject.Layers.Count * layerThickness;

            foreach (var layer in _currentProject.Layers)
            {

                if (layer.Regions == null)
                    continue;

                foreach (var region in layer.Regions)
                {
                    if (region.PolyLines == null)
                        continue;

                    foreach (var polyline in region.PolyLines)
                    {
                        if (polyline.Points == null)
                            continue;

                        foreach (var point in polyline.Points)
                        {
                            if (point.X < minX) minX = point.X;
                            if (point.X > maxX) maxX = point.X;
                            if (point.Y < minY) minY = point.Y;
                            if (point.Y > maxY) maxY = point.Y;
                        }
                    }
                }
            }

            var boundingBox = new BoundingBox(
                new Vector3(minX, minY, minZ),
                new Vector3(maxX, maxY, maxZ)
            );

            _camera.FocusOn(boundingBox);

            
        }

        /// <summary>
        /// Запускает фоновое кеширование геометрии всех слоёв
        /// ОПТИМИЗИРОВАНО: последовательная обработка с низким приоритетом для Intel UHD
        /// </summary>
        private void StartBackgroundCaching()
        {
            if (_currentProject == null || _currentProject.Layers == null)
                return;

            if (_geometryBuilder == null)
                return;

            // Отменяем предыдущую задачу
            _cachePreloadCancellation?.Cancel();
            _cachePreloadCancellation = new CancellationTokenSource();
            var cancellationToken = _cachePreloadCancellation.Token;

            int totalLayers = _currentProject.Layers.Count;

            // Предвычисляем Z позиции для всех слоёв (один раз)
            // zPositions[i] содержит Z координату слоя i (layer.Height - абсолютная позиция в мм)
            float layerThickness = _currentProject.GetLayerThicknessInMillimeters();
            var zPositions = new float[totalLayers];
            for (int i = 0; i < totalLayers; i++)
            {
                float z = (float)_currentProject.Layers[i].Height;
                if (z < 0.001f) z = (i + 1) * layerThickness;
                zPositions[i] = z;
            }

            // Запускаем ПОСЛЕДОВАТЕЛЬНОЕ кеширование с низким приоритетом
            _cachePreloadTask = Task.Run(async () =>
            {
                try
                {
                    for (int layerIndex = 0; layerIndex < totalLayers; layerIndex++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        // Проверяем, не закеширован ли уже
                        bool alreadyCached;
                        lock (_layerGeometryCache)
                        {
                            alreadyCached = _layerGeometryCache.ContainsKey(layerIndex);
                        }

                        if (alreadyCached)
                            continue;

                        // Кешируем слой
                        var layerGeometry = _geometryBuilder.BuildSingleLayerContoursAndHatch(
                            _currentProject,
                            layerIndex,
                            zPositions[layerIndex]
                        );

                        lock (_layerGeometryCache)
                        {
                            _layerGeometryCache[layerIndex] = (layerGeometry.vertices, layerGeometry.indices);
                        }

                        // Уведомляем о прогрессе каждые 10 слоёв
                        if (layerIndex % 10 == 0)
                        {
                            OnCachingProgress?.Invoke(layerIndex);
                        }

                        // Даём UI потоку дышать - небольшая пауза каждые 5 слоёв
                        if (layerIndex % 5 == 0)
                        {
                            await Task.Delay(1, cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Кеширование отменено
                }
                catch (Exception)
                {
                    // Игнорируем ошибки кеширования
                }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        OnLoadingStateChanged?.Invoke(false);
                    });
                }
            }, cancellationToken);
        }

        #endregion

        #region Управление мышью

        private void OnImageMouseDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePosition = e.GetPosition(D3DImageHost);
            _mouseDownPosition = _lastMousePosition;
            _mouseMoved = false;
            D3DImageHost.CaptureMouse();

            // Устанавливаем фокус для обработки клавиатуры
            this.Focus();

            if (e.ChangedButton == MouseButton.Left)
                _isLeftMouseDown = true;

            if (e.ChangedButton == MouseButton.Middle)
                _isMiddleMouseDown = true;
        }

        private void OnImageMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Если мышь не сдвинулась (или сдвинулась незначительно), это клик
                if (!_mouseMoved || GetMouseMovementDistance(_mouseDownPosition, _lastMousePosition) < 5.0)
                {
                    // Выполняем ray picking для определения выбранной детали
                    PerformPicking(_lastMousePosition);
                }

                _isLeftMouseDown = false;
            }

            if (e.ChangedButton == MouseButton.Middle)
                _isMiddleMouseDown = false;

            if (!_isLeftMouseDown && !_isMiddleMouseDown)
                D3DImageHost.ReleaseMouseCapture();
        }

        private double GetMouseMovementDistance(System.Windows.Point p1, System.Windows.Point p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private void OnImageMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isLeftMouseDown && !_isMiddleMouseDown)
                return;

            System.Windows.Point currentPosition = e.GetPosition(D3DImageHost);
            double deltaX = currentPosition.X - _lastMousePosition.X;
            double deltaY = currentPosition.Y - _lastMousePosition.Y;

            // Отмечаем что мышь двигалась
            if (Math.Abs(deltaX) > 0.1 || Math.Abs(deltaY) > 0.1)
                _mouseMoved = true;

            if (_isLeftMouseDown)
            {
                // Вращение камеры
                float azimuthDelta = -(float)deltaX * 0.5f;
                float elevationDelta = -(float)deltaY * 0.5f;
                _camera.Rotate(azimuthDelta, elevationDelta);
            }

            if (_isMiddleMouseDown)
            {
                // Панорамирование
                float panSpeed = 0.5f;
                _camera.Pan((float)deltaX * panSpeed, (float)deltaY * panSpeed);
            }

            _lastMousePosition = currentPosition;
        }

        private void OnImageMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Зум
            float zoomDelta = e.Delta * 0.1f;
            _camera.Zoom(zoomDelta);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isInitialized || _renderer == null)
                return;

            int width = Math.Max((int)e.NewSize.Width, 1);
            int height = Math.Max((int)e.NewSize.Height, 1);

            try
            {
                // Resize DX11 renderer
                _renderer.Resize(width, height);
                _camera.AspectRatio = (float)width / height;

                // Пересоздаём DX9 shared resource
                _d3d9RenderTarget?.Dispose();
                OpenSharedResource();
                SetD3DImageBackBuffer();

                
            }
            catch (Exception ex)
            {
                
            }
        }

        #endregion

        #region Клавиатурное управление

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.R)
            {
                // Сброс камеры
                _camera.Reset();
                if (_currentProject != null)
                    FocusOnProject();

                
            }
        }

        /// <summary>
        /// Выполняет ray picking для определения выбранной детали
        /// </summary>
        private void PerformPicking(System.Windows.Point mousePosition)
        {
            if (_camera == null || D3DImageHost == null || _pickingTriangles.Count == 0)
            {
                
                return;
            }

            // Создаём луч из экранных координат
            float screenX = (float)mousePosition.X;
            float screenY = (float)mousePosition.Y;
            float screenWidth = (float)D3DImageHost.ActualWidth;
            float screenHeight = (float)D3DImageHost.ActualHeight;

            Ray pickingRay = _camera.GetPickingRay(screenX, screenY, screenWidth, screenHeight);

            

            // Создаём копию коллекции для безопасной итерации (защита от модификации в фоновом потоке)
            List<(Vector3 v0, Vector3 v1, Vector3 v2, int? partId)> trianglesCopy;
            lock (_pickingTriangles)
            {
                trianglesCopy = new List<(Vector3, Vector3, Vector3, int?)>(_pickingTriangles);
            }

            // Ищем ближайшее пересечение с треугольниками
            float closestDistance = float.MaxValue;
            int? hitPartId = null;
            int intersectionCount = 0;

            foreach (var triangle in trianglesCopy)
            {
                if (RayIntersectsTriangle(pickingRay, triangle.v0, triangle.v1, triangle.v2, out float distance))
                {
                    intersectionCount++;
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        hitPartId = triangle.partId;
                    }
                }
            }

            

            // Уведомляем ViewModel о выборе детали
            if (hitPartId.HasValue)
            {
                
                OnPartClicked?.Invoke(hitPartId.Value);
            }
            else
            {
                
                OnPartClicked?.Invoke(null);
            }
        }

        /// <summary>
        /// Проверяет пересечение луча с треугольником (алгоритм Möller–Trumbore)
        /// </summary>
        private bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float distance)
        {
            distance = 0;

            const float EPSILON = 0.0000001f;

            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;

            Vector3 h = Vector3.Cross(ray.Direction, edge2);
            float a = Vector3.Dot(edge1, h);

            if (a > -EPSILON && a < EPSILON)
                return false; // Луч параллелен треугольнику

            float f = 1.0f / a;
            Vector3 s = ray.Position - v0;
            float u = f * Vector3.Dot(s, h);

            if (u < 0.0f || u > 1.0f)
                return false;

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(ray.Direction, q);

            if (v < 0.0f || u + v > 1.0f)
                return false;

            // Вычисляем расстояние до точки пересечения
            float t = f * Vector3.Dot(edge2, q);

            if (t > EPSILON) // Пересечение впереди луча
            {
                distance = t;
                return true;
            }

            return false;
        }

        #endregion

        #region Выделение детали

        /// <summary>
        /// Выделяет деталь через шейдер (быстро, без перестроения геометрии)
        /// </summary>
        /// <param name="partId">ID детали для выделения, или null для сброса</param>
        public void HighlightPart(int? partId)
        {
            if (_renderer == null)
                return;

            _highlightedPartId = partId;

            // Устанавливаем highlighted part в рендерере через шейдер
            _renderer.SetHighlightedPart(partId);

            // Принудительно перерисовываем на следующем кадре
            InvalidateVisual();

            if (partId.HasValue)
            {
                
            }
            else
            {
                
            }
        }

        #endregion

        #region Публичное API камеры

        /// <summary>
        /// Сбрасывает камеру в начальную позицию
        /// </summary>
        public void ResetCamera()
        {
            _camera?.Reset();
            if (_currentProject != null)
                FocusOnProject();

            
        }

        /// <summary>
        /// Устанавливает вид сверху
        /// </summary>
        public void SetTopView()
        {
            if (_camera == null) return;

            _camera.Azimuth = 0f;
            _camera.Elevation = 89f; // Почти сверху (не 90, чтобы избежать gimbal lock)

            
        }

        /// <summary>
        /// Устанавливает изометрический вид
        /// </summary>
        public void SetIsometricView()
        {
            if (_camera == null) return;

            _camera.Azimuth = 45f;
            _camera.Elevation = 30f;

            
        }

        #endregion

        #region Обработка сворачивания/разворачивания окна

        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            var window = sender as Window;
            if (window == null) return;

            if (window.WindowState == WindowState.Minimized)
            {
                
            }
            else if (window.WindowState == WindowState.Normal || window.WindowState == WindowState.Maximized)
            {
                
                // Восстанавливаем D3DImage после разворачивания
                RestoreD3DImage();
            }
        }

        private void OnWindowActivated(object sender, EventArgs e)
        {
            
            // Восстанавливаем D3DImage при активации окна
            RestoreD3DImage();
        }

        private void RestoreD3DImage()
        {
            if (_d3dImage == null || _d3d9RenderTarget == null || !_isInitialized)
                return;

            try
            {
                _d3dImage.Lock();
                _d3dImage.AddDirtyRect(new Int32Rect(0, 0, _d3dImage.PixelWidth, _d3dImage.PixelHeight));
                _d3dImage.Unlock();
                
            }
            catch (Exception ex)
            {
                
            }
        }

        /// <summary>
        /// Вычисляет центр проекта и максимальный радиус для градиента
        /// </summary>
        private (float centerX, float centerY, float maxRadius) CalculateProjectCenter(Project project, int layerCount)
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            for (int i = 0; i < layerCount && i < project.Layers.Count; i++)
            {
                var layer = project.Layers[i];
                if (layer.Regions == null) continue;

                foreach (var region in layer.Regions)
                {
                    if (region.PolyLines == null) continue;

                    foreach (var polyline in region.PolyLines)
                    {
                        if (polyline.Points == null) continue;

                        foreach (var point in polyline.Points)
                        {
                            if (point.X < minX) minX = point.X;
                            if (point.X > maxX) maxX = point.X;
                            if (point.Y < minY) minY = point.Y;
                            if (point.Y > maxY) maxY = point.Y;
                        }
                    }
                }
            }

            float centerX = (minX + maxX) / 2f;
            float centerY = (minY + maxY) / 2f;
            float maxRadius = Math.Max(maxX - minX, maxY - minY) / 2f;

            return (centerX, centerY, maxRadius);
        }

        /// <summary>
        /// Вычисляет Z позицию верхней поверхности указанного слоя (0-based индекс)
        /// </summary>
        private double CalculateLayerZPosition(int layerIndex)
        {
            if (_currentProject == null || layerIndex < 0)
                return 0.0;

            // layer.Height содержит абсолютную Z позицию в мм
            float z = (float)_currentProject.Layers[layerIndex].Height;
            if (z < 0.001f)
            {
                float layerThickness = _currentProject.GetLayerThicknessInMillimeters();
                z = (layerIndex + 1) * layerThickness;
            }
            return z;
        }

        #endregion
    }
}
