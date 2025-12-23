using HandyControl.Tools.Command;
using Prism.Mvvm;
using Prism.Events;
using Prism.Commands;
using ProjectParserTest.Parsers.CliParser;
using ProjectParserTest.Parsers.Shared.Enums;
using ProjectParserTest.Parsers.Shared.Models;
using PrintMate.Terminal.Parsers.Shared.Models;
using PrintMate.Terminal.ViewModels.PagesViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using SkiaSharp;
using System.Windows.Threading;

namespace PrintMate.Terminal.ViewModels
{
    public class ProjectPreviewViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly CliProvider _cliProvider;
        private DispatcherTimer _simulationTimer;
        private Project _currentProject;
        public Project CurrentProject
        {
            get => _currentProject;
            set
            {
                SetProperty(ref _currentProject, value);
                if (_currentProject != null && _currentProject.Layers.Count > 0)
                {
                    SelectedLayerIndex = 0;
                }
            }
        }

        private int _selectedLayerIndex;
        public int SelectedLayerIndex
        {
            get => _selectedLayerIndex;
            set
            {
                SetProperty(ref _selectedLayerIndex, value);
                UpdateCurrentLayer();
                RenderLayer();
            }
        }

        private Layer _currentLayer;
        public Layer CurrentLayer
        {
            get => _currentLayer;
            set => SetProperty(ref _currentLayer, value);
        }

        private List<System.Windows.Shapes.Path> _renderedPaths;
        public List<System.Windows.Shapes.Path> RenderedPaths
        {
            get => _renderedPaths;
            set => SetProperty(ref _renderedPaths, value);
        }

        private ImageSource _layerImage;
        public ImageSource LayerImage
        {
            get => _layerImage;
            set => SetProperty(ref _layerImage, value);
        }

        public event Action PathsUpdated;
        public event EventHandler LayerChanged;

        private bool _showDownskinPreview = true;
        public bool ShowDownskinPreview
        {
            get => _showDownskinPreview;
            set
            {
                if (SetProperty(ref _showDownskinPreview, value))
                {
                    Console.WriteLine($"[FILTER] ShowDownskinPreview changed to {value}");
                    RenderLayer();
                }
            }
        }

        private bool _showInfillPreview = true;
        public bool ShowInfillPreview
        {
            get => _showInfillPreview;
            set
            {
                if (SetProperty(ref _showInfillPreview, value))
                {
                    Console.WriteLine($"[FILTER] ShowInfillPreview changed to {value}");
                    RenderLayer();
                }
            }
        }

        private bool _showUpskinPreview = true;
        public bool ShowUpskinPreview
        {
            get => _showUpskinPreview;
            set
            {
                if (SetProperty(ref _showUpskinPreview, value))
                {
                    Console.WriteLine($"[FILTER] ShowUpskinPreview changed to {value}");
                    RenderLayer();
                }
            }
        }

        private bool _showOtherGeometry = true;
        public bool ShowOtherGeometry
        {
            get => _showOtherGeometry;
            set
            {
                if (SetProperty(ref _showOtherGeometry, value))
                {
                    Console.WriteLine($"[FILTER] ShowOtherGeometry changed to {value}");
                    RenderLayer();
                }
            }
        }

        private string _layerInfo;
        public string LayerInfo
        {
            get => _layerInfo;
            set => SetProperty(ref _layerInfo, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Новые свойства для 3D и симуляции
        private Layer3DViewModel _layer3DViewModel;
        public Layer3DViewModel Layer3DViewModel
        {
            get => _layer3DViewModel;
            set => SetProperty(ref _layer3DViewModel, value);
        }

        private int _currentSimulationLayer;
        public int CurrentSimulationLayer
        {
            get => _currentSimulationLayer;
            set
            {
                if (SetProperty(ref _currentSimulationLayer, value))
                {
                    // Обновляем 3D геометрию для текущего количества слоев
                    Layer3DViewModel?.BuildLayersGeometry(value);
                    Layer3DViewModel?.OnLayerStarted(value);
                    Console.WriteLine($"[3D] Построена геометрия для {value} слоев");
                }
            }
        }

        // Команды
        public Prism.Commands.DelegateCommand LoadTestProjectCommand { get; }
        public Prism.Commands.DelegateCommand StartSimulationCommand { get; }
        public Prism.Commands.DelegateCommand PauseSimulationCommand { get; }
        public Prism.Commands.DelegateCommand StopSimulationCommand { get; }

        // Размер поля сканатора в мм
        private const float FIELD_SIZE = 320f;

        public RelayCommand NextLayerCommand { get; set; }
        public RelayCommand PreviousLayerCommand { get; set; }
        public RelayCommand LoadProjectCommand { get; set; }

        public ProjectPreviewViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _cliProvider = new CliProvider();

            RenderedPaths = new List<System.Windows.Shapes.Path>();

            // Инициализация Layer3DViewModel
            Layer3DViewModel = new Layer3DViewModel(eventAggregator);

            // Команды
            NextLayerCommand = new RelayCommand(_ => NextLayer(), _ => CanGoToNextLayer());
            PreviousLayerCommand = new RelayCommand(_ => PreviousLayer(), _ => CanGoToPreviousLayer());
            LoadProjectCommand = new RelayCommand(async _ => await LoadDefaultProject());

            LoadTestProjectCommand = new Prism.Commands.DelegateCommand(async () => await LoadTestProject());
            StartSimulationCommand = new Prism.Commands.DelegateCommand(StartSimulation);
            PauseSimulationCommand = new Prism.Commands.DelegateCommand(PauseSimulation);
            StopSimulationCommand = new Prism.Commands.DelegateCommand(StopSimulation);

            // Таймер для симуляции
            _simulationTimer = new DispatcherTimer();
            _simulationTimer.Interval = TimeSpan.FromMilliseconds(500); // 2 слоя в секунду
            _simulationTimer.Tick += SimulationTimer_Tick;
        }

        private async System.Threading.Tasks.Task LoadDefaultProject()
        {
            try
            {
                var cliProvider = new CliProvider();
                FileDialog dialog = new OpenFileDialog();

                bool? isSuccess = dialog.ShowDialog(Application.Current.MainWindow);
                if (isSuccess == true)
                {
                    CurrentProject = await cliProvider.ParseAsync(dialog.FileName);
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading project: {ex.Message}");
            }
        }

        private void NextLayer()
        {
            if (CanGoToNextLayer())
            {
                SelectedLayerIndex++;
            }
        }

        private void PreviousLayer()
        {
            if (CanGoToPreviousLayer())
            {
                SelectedLayerIndex--;
            }
        }

        private bool CanGoToNextLayer()
        {
            return CurrentProject != null && SelectedLayerIndex < CurrentProject.Layers.Count - 1;
        }

        private bool CanGoToPreviousLayer()
        {
            return CurrentProject != null && SelectedLayerIndex > 0;
        }

        private void UpdateCurrentLayer()
        {
            if (CurrentProject != null && SelectedLayerIndex >= 0 && SelectedLayerIndex < CurrentProject.Layers.Count)
            {
                CurrentLayer = CurrentProject.Layers[SelectedLayerIndex];
                UpdateLayerInfo();
                LayerChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool _isRendering = false;
        private double _currentZoomLevel = 1.0;
        private string _layerInfoBase; // Кэш базовой информации без зума
        private string _layerInfoDetails; // Кэш детальной информации
        private CancellationTokenSource _zoomUpdateCts;

        // Порог масштабирования, при котором показываем детальную заливку (Hatch)
        private const double DETAIL_ZOOM_THRESHOLD = 15.0;

        public void UpdateZoomLevel(double zoomLevel)
        {
            bool wasAboveThreshold = _currentZoomLevel >= DETAIL_ZOOM_THRESHOLD;
            bool isAboveThreshold = zoomLevel >= DETAIL_ZOOM_THRESHOLD;

            _currentZoomLevel = zoomLevel;

            // Если пересекли порог детализации - перерендерить
            if (wasAboveThreshold != isAboveThreshold)
            {
                Console.WriteLine($"[ZOOM] Threshold crossed at {zoomLevel:F2}. Mode: {(isAboveThreshold ? "LINES" : "FILL")}");
                RenderLayer();
                return;
            }

            // Отменяем предыдущее обновление (дебаунс)
            _zoomUpdateCts?.Cancel();
            _zoomUpdateCts = new CancellationTokenSource();
            var token = _zoomUpdateCts.Token;

            // Обновляем с задержкой 50мс чтобы не спамить UI
            Task.Delay(50, token).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (!token.IsCancellationRequested)
                        {
                            UpdateZoomInLayerInfo();
                        }
                    });
                }
            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        private void UpdateLayerInfo()
        {
            if (CurrentLayer != null && CurrentProject != null)
            {
                int totalRegions = CurrentLayer.Regions?.Count ?? 0;
                int previewRegions = CurrentLayer.Regions?.Count(r => IsPreviewRegion(r.GeometryRegion)) ?? 0;
                int hatchCount = CurrentLayer.Regions?.Count(r => r.Type == BlockType.Hatch) ?? 0;
                int polylineCount = CurrentLayer.Regions?.Count(r => r.Type == BlockType.PolyLine) ?? 0;

                // Подсчитываем регионы по типам для отладки
                var regionTypes = CurrentLayer.Regions?
                    .GroupBy(r => r.GeometryRegion)
                    .Select(g => $"{g.Key}:{g.Count()}")
                    .ToList();

                string regionDetails = regionTypes != null ? string.Join(", ", regionTypes) : "";

                // Кэшируем базовую информацию и детали
                _layerInfoBase = $"Layer {SelectedLayerIndex + 1} / {CurrentProject.Layers.Count} | " +
                           $"Total: {totalRegions} (Polyline: {polylineCount}, Hatch: {hatchCount}) | ";
                _layerInfoDetails = regionDetails;

                LayerInfo = _layerInfoBase + $"Zoom: {_currentZoomLevel:F2}x\n{_layerInfoDetails}";
            }
            else
            {
                _layerInfoBase = "";
                _layerInfoDetails = "";
                LayerInfo = "No layer loaded";
            }
        }

        private void UpdateZoomInLayerInfo()
        {
            // Быстрое обновление только значения зума без LINQ операций
            if (!string.IsNullOrEmpty(_layerInfoBase))
            {
                LayerInfo = _layerInfoBase + $"Zoom: {_currentZoomLevel:F2}x\n{_layerInfoDetails}";
            }
        }

        private bool IsPreviewRegion(GeometryRegion region)
        {
            return region == GeometryRegion.DownskinRegionPreview ||
                   region == GeometryRegion.InfillRegionPreview ||
                   region == GeometryRegion.UpskinRegionPreview;
        }

        private async void RenderLayer()
        {
            if (_isRendering)
                return;

            _isRendering = true;
            IsLoading = true;

            try
            {
                if (CurrentLayer?.Regions == null)
                {
                    LayerImage = null;
                    return;
                }

                // Рендерим с помощью SkiaSharp в фоновом потоке
                var bitmap = await Task.Run(() =>
                {
                    // Создаем bitmap с фиксированным разрешением 3000x3000
                    int width = 3000;
                    int height = 3000;
                    float scale = width / FIELD_SIZE; // Вычисляем масштаб (~9.375)

                    // Создаем SkiaSharp surface
                    var imageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
                    using (var surface = SKSurface.Create(imageInfo))
                    {
                        var canvas = surface.Canvas;

                        // Очищаем фон
                        canvas.Clear(SKColors.Transparent);

                        // Определяем уровень детализации
                        bool showDetails = _currentZoomLevel >= DETAIL_ZOOM_THRESHOLD;

                        // Рендерим каждый регион
                        foreach (var region in CurrentLayer.Regions)
                        {
                            if (!ShouldRenderRegion(region))
                                continue;

                            var wpfColor = GetRegionColor(region.GeometryRegion);
                            var skColor = new SKColor(wpfColor.R, wpfColor.G, wpfColor.B, 255);
                            bool isHatch = region.Type == BlockType.Hatch;

                            if (region.PolyLines == null)
                                continue;

                            // При большом зуме (>=15) Hatch регионы рисуем линиями с прореживанием
                            if (isHatch && showDetails)
                            {
                                // Определяем цвет в зависимости от LaserNum
                                var lineColor = region.LaserNum == 0
                                    ? new SKColor(255, 0, 0, 255) // Красный для LaserNum = 0
                                    : skColor; // Обычный цвет для остальных

                                // Детальный режим - рисуем линии с прореживанием
                                using (var paint = new SKPaint())
                                {
                                    paint.Color = lineColor;
                                    paint.IsAntialias = true; // Включаем сглаживание для плавных линий
                                    paint.FilterQuality = SKFilterQuality.High;
                                    paint.StrokeWidth = 0.5f; // Очень тонкие линии
                                    paint.Style = SKPaintStyle.Stroke;
                                    paint.StrokeCap = SKStrokeCap.Square; // Квадратные концы для стыковки
                                    paint.StrokeJoin = SKStrokeJoin.Miter; // Острые углы

                                    // Объединяем все линии региона в один путь
                                    using (var path = new SKPath())
                                    {
                                        int lineIndex = 0;
                                        foreach (var polyLine in region.PolyLines)
                                        {
                                            if (polyLine?.Points == null || polyLine.Points.Count < 2)
                                            {
                                                lineIndex++;
                                                continue;
                                            }

                                            // Пропускаем 9 из 10 линий (оставляем каждую 10-ю)
                                            if (lineIndex % 10 != 0)
                                            {
                                                lineIndex++;
                                                continue;
                                            }

                                            // Добавляем polyline в общий путь
                                            var firstPoint = TransformPoint(polyLine.Points[0]);
                                            path.MoveTo((float)(firstPoint.X * scale), (float)(firstPoint.Y * scale));

                                            for (int i = 1; i < polyLine.Points.Count; i++)
                                            {
                                                var point = TransformPoint(polyLine.Points[i]);
                                                path.LineTo((float)(point.X * scale), (float)(point.Y * scale));
                                            }

                                            lineIndex++;
                                        }

                                        // Рисуем весь путь одной операцией
                                        canvas.DrawPath(path, paint);
                                    }
                                }
                            }
                            // При маленьком зуме (<15) для Infill регионов заполняем соответствующий Contour
                            else if (isHatch && !showDetails)
                            {
                                // Находим соответствующий Contour регион (по Part.Id) и заполняем его цветом Infill
                                var contourRegion = CurrentLayer.Regions.FirstOrDefault(r =>
                                    r.GeometryRegion == GeometryRegion.Contour &&
                                    r.Type == BlockType.PolyLine &&
                                    r.Part?.Id == region.Part?.Id);

                                Console.WriteLine($"[FILL] Infill region: {region.GeometryRegion}, PartId: {region.Part?.Id}, Contour found: {contourRegion != null}");

                                using (var fillPaint = new SKPaint())
                                {
                                    fillPaint.Color = new SKColor(wpfColor.R, wpfColor.G, wpfColor.B, 255);
                                    fillPaint.Style = SKPaintStyle.Fill;
                                    fillPaint.IsAntialias = true;

                                    // Если есть Contour - рисуем по контуру
                                    if (contourRegion != null && contourRegion.PolyLines != null)
                                    {
                                        using (var path = new SKPath())
                                        {
                                            foreach (var polyLine in contourRegion.PolyLines)
                                            {
                                                if (polyLine?.Points == null || polyLine.Points.Count < 2)
                                                    continue;

                                                var firstPoint = TransformPoint(polyLine.Points[0]);
                                                path.MoveTo((float)(firstPoint.X * scale), (float)(firstPoint.Y * scale));

                                                for (int i = 1; i < polyLine.Points.Count; i++)
                                                {
                                                    var point = TransformPoint(polyLine.Points[i]);
                                                    path.LineTo((float)(point.X * scale), (float)(point.Y * scale));
                                                }

                                                path.Close();
                                            }

                                            canvas.DrawPath(path, fillPaint);
                                        }
                                    }
                                    // Если нет Contour - рисуем bounding box Infill региона
                                    else
                                    {
                                        Console.WriteLine($"[FILL] No Contour - drawing bounding box");

                                        // Находим границы региона
                                        float minX = float.MaxValue, minY = float.MaxValue;
                                        float maxX = float.MinValue, maxY = float.MinValue;

                                        foreach (var polyLine in region.PolyLines)
                                        {
                                            if (polyLine?.Points == null)
                                                continue;

                                            foreach (var point in polyLine.Points)
                                            {
                                                var transformedPoint = TransformPoint(point);
                                                float x = (float)(transformedPoint.X * scale);
                                                float y = (float)(transformedPoint.Y * scale);

                                                if (x < minX) minX = x;
                                                if (x > maxX) maxX = x;
                                                if (y < minY) minY = y;
                                                if (y > maxY) maxY = y;
                                            }
                                        }

                                        // Рисуем прямоугольник
                                        if (minX != float.MaxValue && maxX != float.MinValue)
                                        {
                                            Console.WriteLine($"[FILL] BBox: ({minX}, {minY}) - ({maxX}, {maxY})");
                                            canvas.DrawRect(minX, minY, maxX - minX, maxY - minY, fillPaint);
                                        }
                                    }
                                }
                            }
                            // Для Contour регионов - всегда рисуем только контур линиями
                            else if (!isHatch)
                            {
                                using (var paint = new SKPaint())
                                {
                                    paint.Color = skColor;
                                    paint.IsAntialias = false; // Отключаем сглаживание для чётких линий
                                    paint.FilterQuality = SKFilterQuality.None;
                                    paint.StrokeWidth = 1.0f; // Тонкие линии
                                    paint.Style = SKPaintStyle.Stroke;
                                    paint.StrokeCap = SKStrokeCap.Butt; // Прямые концы
                                    paint.StrokeJoin = SKStrokeJoin.Miter; // Острые углы

                                    foreach (var polyLine in region.PolyLines)
                                    {
                                        if (polyLine?.Points == null || polyLine.Points.Count < 2)
                                            continue;

                                        using (var path = new SKPath())
                                        {
                                            var firstPoint = TransformPoint(polyLine.Points[0]);
                                            path.MoveTo((float)(firstPoint.X * scale), (float)(firstPoint.Y * scale));

                                            for (int i = 1; i < polyLine.Points.Count; i++)
                                            {
                                                var point = TransformPoint(polyLine.Points[i]);
                                                path.LineTo((float)(point.X * scale), (float)(point.Y * scale));
                                            }

                                            canvas.DrawPath(path, paint);
                                        }
                                    }
                                }
                            }
                        }

                        // Конвертируем в WPF BitmapSource
                        using (var image = surface.Snapshot())
                        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                        {
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = data.AsStream();
                            bitmapImage.EndInit();
                            bitmapImage.Freeze();
                            return bitmapImage;
                        }
                    }
                });

                // Устанавливаем изображение в UI потоке
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    LayerImage = bitmap;
                });
            }
            finally
            {
                _isRendering = false;
                IsLoading = false;
            }
        }


        private bool ShouldRenderRegion(ProjectParserTest.Parsers.Shared.Models.Region region)
        {
            // Отображаем только Contour и Infill регионы
            bool result = region.GeometryRegion == GeometryRegion.Contour ||
                         region.GeometryRegion == GeometryRegion.Infill;

            Console.WriteLine($"[FILTER] Region type: {region.GeometryRegion} - Show: {result}");
            return result;
        }

        /// <summary>
        /// Определяет, является ли регион детальной заливкой (штриховкой),
        /// которая должна отображаться только при увеличении
        /// BlockType.Hatch - это линии заполнения (штриховка)
        /// BlockType.PolyLine - это контуры
        /// </summary>
        private bool IsDetailFillRegion(ProjectParserTest.Parsers.Shared.Models.Region region)
        {
            // Hatch-регионы (штриховка) показываем только при зуме
            return region.Type == BlockType.Hatch;
        }

        private Color GetRegionColor(GeometryRegion region)
        {
            switch (region)
            {
                case GeometryRegion.DownskinRegionPreview:
                    return Color.FromRgb(255, 100, 100); // Красноватый
                case GeometryRegion.InfillRegionPreview:
                    return Color.FromRgb(100, 255, 100); // Зеленоватый
                case GeometryRegion.UpskinRegionPreview:
                    return Color.FromRgb(100, 100, 255); // Синеватый
                case GeometryRegion.Edges:
                    return Color.FromRgb(255, 255, 0); // Желтый
                case GeometryRegion.Contour:
                case GeometryRegion.ContourDownskin:
                case GeometryRegion.ContourUpskin:
                    return Color.FromRgb(255, 165, 0); // Оранжевый
                case GeometryRegion.Infill:
                    return Color.FromRgb(150, 150, 150); // Серый
                case GeometryRegion.Downskin:
                    return Color.FromRgb(200, 100, 100); // Темно-красный
                case GeometryRegion.Upskin:
                    return Color.FromRgb(100, 100, 200); // Темно-синий
                case GeometryRegion.Support:
                case GeometryRegion.SupportFill:
                    return Color.FromRgb(128, 0, 128); // Фиолетовый
                default:
                    return Color.FromRgb(200, 200, 200); // Светло-серый
            }
        }

        /// <summary>
        /// Трансформирует координаты из системы CLI (центр в 0,0)
        /// в систему Canvas (левый верхний угол в 0,0, Y инвертирован)
        /// </summary>
        private System.Windows.Point TransformPoint(ProjectParserTest.Parsers.Shared.Models.Point point)
        {
            // Центр поля
            float centerX = FIELD_SIZE / 2f;
            float centerY = FIELD_SIZE / 2f;

            // Трансформируем координаты:
            // 1. Смещаем в центр поля (CLI координаты обычно центрированы на 0,0)
            // 2. Инвертируем Y (в Canvas Y растет вниз, в CLI обычно вверх)
            double canvasX = centerX + point.X;
            double canvasY = centerY - point.Y; // Инвертируем Y

            return new System.Windows.Point(canvasX, canvasY);
        }

        /// <summary>
        /// Устанавливает проект для визуализации
        /// </summary>
        public void SetProject(Project project)
        {
            CurrentProject = project;
        }

        /// <summary>
        /// Получает размер поля сканатора для расчета масштаба
        /// </summary>
        public float GetFieldSize() => FIELD_SIZE;

        #region Методы для 3D визуализации и симуляции

        private async Task LoadTestProject()
        {
            try
            {
                IsLoading = true;
                Layer3DViewModel.LoadingMessage = "Загрузка mybox.cli...";

                var projectPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Тест 2 лазера (4 детали).cli");

                if (!System.IO.File.Exists(projectPath))
                {
                    Console.WriteLine($"[ERROR] Файл не найден: {projectPath}");
                    MessageBox.Show($"Файл mybox.cli не найден в корне проекта!\n{projectPath}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Console.WriteLine($"[INFO] Загрузка проекта: {projectPath}");
                var project = await _cliProvider.ParseAsync(projectPath);

                CurrentProject = project;
                Layer3DViewModel.LoadProject(project);

                // Автоматически показываем первые 10 слоев (или все, если меньше 10)
                int initialLayers = Math.Min(10, project.Layers.Count);
                CurrentSimulationLayer = initialLayers;

                Console.WriteLine($"[INFO] Проект загружен успешно. Слоев: {project.Layers.Count}");
                Console.WriteLine($"[INFO] Построено начальных слоев: {initialLayers}");
                MessageBox.Show($"Проект загружен!\nСлоев: {project.Layers.Count}\nПостроено начально: {initialLayers} слоев", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Ошибка загрузки проекта: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки проекта:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                Layer3DViewModel.LoadingMessage = string.Empty;
            }
        }

        private void StartSimulation()
        {
            if (CurrentProject == null || CurrentProject.Layers == null || CurrentProject.Layers.Count == 0)
            {
                MessageBox.Show("Сначала загрузите проект!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentSimulationLayer = 0;
            _simulationTimer.Start();
            Console.WriteLine("[SIMULATION] Симуляция запущена");
        }

        private void PauseSimulation()
        {
            _simulationTimer.Stop();
            Console.WriteLine("[SIMULATION] Симуляция приостановлена");
        }

        private void StopSimulation()
        {
            _simulationTimer.Stop();
            CurrentSimulationLayer = 0;
            Layer3DViewModel.OnLayerStarted(0);
            Console.WriteLine("[SIMULATION] Симуляция остановлена");
        }

        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            if (CurrentProject == null || CurrentProject.Layers == null)
            {
                _simulationTimer.Stop();
                return;
            }

            if (CurrentSimulationLayer < Layer3DViewModel.TotalLayers - 1)
            {
                CurrentSimulationLayer++;
                Console.WriteLine($"[SIMULATION] Слой {CurrentSimulationLayer}/{Layer3DViewModel.TotalLayers}");
            }
            else
            {
                _simulationTimer.Stop();
                Console.WriteLine("[SIMULATION] Симуляция завершена");
                MessageBox.Show("Симуляция печати завершена!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion
    }
}
