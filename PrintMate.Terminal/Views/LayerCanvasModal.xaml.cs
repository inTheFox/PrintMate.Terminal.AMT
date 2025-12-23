using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PrintMate.Terminal.Views.Modals;
using CliRegion = ProjectParserTest.Parsers.Shared.Models.Region;
using CliPoint = ProjectParserTest.Parsers.Shared.Models.Point;
using ProjectParserTest.Parsers.Shared.Models;
using ProjectParserTest.Parsers.Shared.Enums;

namespace PrintMate.Terminal.Views
{
    public partial class LayerCanvasModal : UserControl
    {
        private const double FIELD_SIZE = 320.0; // Реальный размер платформы в мм (320x320)
        private const double CANVAS_SIZE = 700; // Размер канваса в пикселях (600x600)
        private const double SCALE_FACTOR = CANVAS_SIZE / FIELD_SIZE; // Коэффициент масштабирования (~1.875)
        private const double DETAIL_ZOOM_THRESHOLD = 4.0; // Порог для показа hatch линий
        private const double MIN_ZOOM = 0.1;
        private const double MAX_ZOOM = 100.0;
        private const double ZOOM_SPEED = 0.1;

        private Layer _currentLayer;
        private System.Windows.Point _lastMousePosition;
        private bool _isDragging;
        private double _zoom = 1.0;

        // Для сенсорных жестов
        private double _initialManipulationScale = 1.0;
        private System.Windows.Point _initialManipulationTranslate = new System.Windows.Point(0, 0);

        // Визуальные элементы для рендеринга
        private readonly List<DrawingVisual> _visuals = new List<DrawingVisual>();
        private readonly VisualHost _visualHost;

        // Выделенная деталь и её визуализация
        private Part _selectedPart;
        private DrawingVisual _selectionVisual;
        private readonly Dictionary<Part, List<DrawingVisual>> _partVisualsMap = new Dictionary<Part, List<DrawingVisual>>();

        // Плавная анимация зума
        private double _targetZoom = 1.0;
        private System.Windows.Point _zoomMousePosition;
        private System.Threading.Timer _zoomTimer;
        private readonly object _zoomLock = new object();

        public LayerCanvasModal()
        {
            InitializeComponent();

            // Создаём хост для DrawingVisual элементов (увеличенный размер)
            _visualHost = new VisualHost
            {
                Width = CANVAS_SIZE,
                Height = CANVAS_SIZE,
                ClipToBounds = true
            };
            Canvas.SetLeft(_visualHost, 0);
            Canvas.SetTop(_visualHost, 0);
            MainCanvas.Children.Add(_visualHost);

            // Рисуем сетку сразу при загрузке
            RenderLayer();
        }

        public void SetLayer(Layer layer)
        {
            _currentLayer = layer;
            RenderLayer();
        }

        /// <summary>
        /// Публичный метод для загрузки слоя (используется из ViewModel)
        /// </summary>
        public void LoadLayer(Layer layer)
        {
            SetLayer(layer);
        }

        private void RenderLayer()
        {
            // Очищаем старые визуалы
            _visualHost.ClearVisuals();
            _visuals.Clear();
            _partVisualsMap.Clear();

            // Рисуем сетку (всегда, даже если нет слоя)
            RenderGrid();

            if (_currentLayer == null)
                return;

            bool showDetails = _zoom >= DETAIL_ZOOM_THRESHOLD;

            Console.WriteLine($"[RENDER] Rendering layer, Zoom: {_zoom:F2}, ShowDetails: {showDetails}, Regions: {_currentLayer.Regions?.Count ?? 0}");

            int renderedCount = 0;
            int skippedCount = 0;

            // Проходим по всем регионам
            foreach (var region in _currentLayer.Regions)
            {
                if (!ShouldRenderRegion(region))
                {
                    skippedCount++;
                    continue;
                }

                renderedCount++;
                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    RenderRegion(dc, region, showDetails);
                }

                _visuals.Add(visual);
                _visualHost.AddVisual(visual);

                // Сохраняем маппинг Part -> Visual для hit-testing
                if (region.Part != null)
                {
                    if (!_partVisualsMap.ContainsKey(region.Part))
                    {
                        _partVisualsMap[region.Part] = new List<DrawingVisual>();
                    }
                    _partVisualsMap[region.Part].Add(visual);
                }
            }

            Console.WriteLine($"[RENDER] Rendered: {renderedCount}, Skipped: {skippedCount}");

            // Перерисовываем выделение, если деталь была выбрана
            if (_selectedPart != null)
            {
                RenderSelection();
            }
        }

        private void RenderRegion(DrawingContext dc, CliRegion region, bool showDetails)
        {
            var color = GetRegionColor(region.GeometryRegion);
            bool isHatch = region.Type == BlockType.Hatch;

            if (region.PolyLines == null || region.PolyLines.Count == 0)
            {
                Console.WriteLine($"[RenderRegion] Skipping region with no polylines: {region.GeometryRegion}");
                return;
            }

            // При высоком зуме (>= 4) для Infill - рисуем линии штриховки + контур
            if (isHatch && showDetails)
            {
                RenderHatchLines(dc, region, color);

                // Дополнительно рисуем контур для Infill регионов
                var contourRegion = FindContourForRegion(region);
                if (contourRegion != null)
                {
                    RenderContourLines(dc, contourRegion, GetRegionColor(GeometryRegion.Contour));
                }
            }
            // При низком зуме (< 4) для Infill - заполняем Contour или bounding box
            else if (isHatch && !showDetails)
            {
                Console.WriteLine($"[RenderRegion] Calling RenderHatchFill for {region.GeometryRegion}, PolyLines: {region.PolyLines.Count}");
                RenderHatchFill(dc, region, color);
            }
            // Для Contour - всегда рисуем только линии
            else if (!isHatch)
            {
                RenderContourLines(dc, region, color);
            }
        }

        private void RenderHatchLines(DrawingContext dc, CliRegion region, Color color)
        {
            // Цвет в зависимости от LaserNum - делаем ярче
            var lineColor = GetColorByLaserNum(region.LaserNum, color);
            lineColor = MakeBrighter(lineColor, 1.5); // Увеличиваем яркость в 1.5 раза

            // Увеличенная толщина линии для лучшей видимости штриховки
            var pen = new Pen(new SolidColorBrush(lineColor), 0.3 / _zoom);
            pen.Freeze();

            int lineIndex = 0;
            foreach (var polyLine in region.PolyLines)
            {
                if (polyLine?.Points == null || polyLine.Points.Count < 2)
                {
                    lineIndex++;
                    continue;
                }

                // Показываем каждую 7-ю линию для большего расстояния между линиями
                if (lineIndex % 3 != 0)
                {
                    lineIndex++;
                    continue;
                }

                for (int i = 0; i < polyLine.Points.Count - 1; i++)
                {
                    var p1 = TransformPoint(polyLine.Points[i]);
                    var p2 = TransformPoint(polyLine.Points[i + 1]);
                    dc.DrawLine(pen, p1, p2);
                }

                lineIndex++;
            }
        }

        private void RenderHatchFill(DrawingContext dc, CliRegion region, Color color)
        {
            // Ищем соответствующий Contour регион
            var contourRegion = FindContourForRegion(region);

            var brush = new SolidColorBrush(color);
            brush.Freeze();

            if (contourRegion != null && contourRegion.PolyLines != null)
            {
                // Заполняем по контуру
                var geometry = CreateGeometryFromRegion(contourRegion);
                if (geometry != null && geometry.Figures.Count > 0)
                {
                    dc.DrawGeometry(brush, null, geometry);
                    return;
                }
            }

            // Пробуем заполнить по геометрии самого региона (только если есть полилинии с > 2 точками - это замкнутые контуры)
            if (region.PolyLines != null && region.PolyLines.Count > 0)
            {
                // Проверяем, есть ли хотя бы одна полилиния с более чем 2 точками (замкнутый контур)
                bool hasClosedContours = region.PolyLines.Any(pl => pl?.Points != null && pl.Points.Count > 2);

                if (hasClosedContours)
                {
                    var geometry = CreateGeometryFromRegion(region);
                    if (geometry != null && geometry.Figures.Count > 0)
                    {
                        dc.DrawGeometry(brush, null, geometry);
                        return;
                    }
                }
            }

            // Fallback: рисуем bounding box (для штриховки без контура)
            var bounds = GetRegionBounds(region);
            if (!bounds.IsEmpty)
            {
                dc.DrawRectangle(brush, null, bounds);
            }
        }

        private void RenderContourLines(DrawingContext dc, CliRegion region, Color color)
        {
            // Цвет в зависимости от LaserNum
            var lineColor = GetColorByLaserNum(region.LaserNum, color);
            // Толщина пера обратно пропорциональна зуму для фиксированной визуальной толщины
            var pen = new Pen(new SolidColorBrush(lineColor), 0.5 / _zoom);
            pen.Freeze();

            foreach (var polyLine in region.PolyLines)
            {
                if (polyLine?.Points == null || polyLine.Points.Count < 2)
                    continue;

                // Рисуем все линии между точками
                for (int i = 0; i < polyLine.Points.Count - 1; i++)
                {
                    var p1 = TransformPoint(polyLine.Points[i]);
                    var p2 = TransformPoint(polyLine.Points[i + 1]);
                    dc.DrawLine(pen, p1, p2);
                }

                // Замыкаем контур: соединяем последнюю точку с первой
                if (polyLine.Points.Count > 2)
                {
                    var pLast = TransformPoint(polyLine.Points[polyLine.Points.Count - 1]);
                    var pFirst = TransformPoint(polyLine.Points[0]);
                    dc.DrawLine(pen, pLast, pFirst);
                }
            }
        }

        private void RenderGrid()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                // Радиус закругления углов
                double cornerRadius = 15.0;

                // Создаём закругленный прямоугольник для платформы
                var platformRect = new RectangleGeometry(new Rect(0, 0, CANVAS_SIZE, CANVAS_SIZE), cornerRadius, cornerRadius);
                platformRect.Freeze();

                // Фон платформы - серый
                var backgroundBrush = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60));
                backgroundBrush.Freeze();
                dc.DrawGeometry(backgroundBrush, null, platformRect);

                // Устанавливаем clip для сетки, чтобы она не выходила за закругленные углы
                dc.PushClip(platformRect);

                // Цвет сетки - темно-серый, полупрозрачный
                var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 80, 80, 80)), 0.5 / _zoom);
                gridPen.Freeze();

                // Шаг сетки в мм (реальных координатах)
                const double gridStepMm = 10.0; // 10мм
                const double majorGridStepMm = 50.0; // 50мм для основных линий

                // Шаг сетки в canvas координатах (с учетом SCALE_FACTOR)
                double gridStep = gridStepMm * SCALE_FACTOR;
                double majorGridStep = majorGridStepMm * SCALE_FACTOR;

                // Перо для основных линий (более тёмное на сером фоне)
                var majorGridPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 60, 60, 60)), 1.0 / _zoom);
                majorGridPen.Freeze();

                // Рисуем вертикальные линии
                for (double x = 0; x <= CANVAS_SIZE; x += gridStep)
                {
                    bool isMajor = Math.Abs(x % majorGridStep) < 0.1;
                    var pen = isMajor ? majorGridPen : gridPen;
                    dc.DrawLine(pen, new System.Windows.Point(x, 0), new System.Windows.Point(x, CANVAS_SIZE));
                }

                // Рисуем горизонтальные линии
                for (double y = 0; y <= CANVAS_SIZE; y += gridStep)
                {
                    bool isMajor = Math.Abs(y % majorGridStep) < 0.1;
                    var pen = isMajor ? majorGridPen : gridPen;
                    dc.DrawLine(pen, new System.Windows.Point(0, y), new System.Windows.Point(CANVAS_SIZE, y));
                }

                // Рисуем центральные оси (оранжевые)
                var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 111, 0)), 1.5 / _zoom);
                axisPen.Freeze();
                double center = CANVAS_SIZE / 2.0;

                // Вертикальная ось (X=0)
                dc.DrawLine(axisPen, new System.Windows.Point(center, 0), new System.Windows.Point(center, CANVAS_SIZE));
                // Горизонтальная ось (Y=0)
                dc.DrawLine(axisPen, new System.Windows.Point(0, center), new System.Windows.Point(CANVAS_SIZE, center));

                // Снимаем clip
                dc.Pop();

                // Обводка платформы - оранжевая с закругленными углами
                var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 255, 111, 0)), 2.0 / _zoom);
                borderPen.Freeze();
                dc.DrawGeometry(null, borderPen, platformRect);
            }

            _visuals.Add(visual);
            _visualHost.AddVisual(visual);
        }

        private CliRegion FindContourForRegion(CliRegion region)
        {
            if (_currentLayer?.Regions == null)
                return null;

            // Определяем какой тип контура искать в зависимости от типа региона
            var contourTypes = GetMatchingContourTypes(region.GeometryRegion);

            // Сначала ищем контур с типом PolyLine
            foreach (var r in _currentLayer.Regions)
            {
                if (contourTypes.Contains(r.GeometryRegion) &&
                    r.Type == BlockType.PolyLine &&
                    r.Part?.Id == region.Part?.Id &&
                    r.PolyLines != null && r.PolyLines.Count > 0)
                {
                    // Проверяем, что есть хотя бы одна полилиния с точками
                    foreach (var pl in r.PolyLines)
                    {
                        if (pl?.Points != null && pl.Points.Count >= 2)
                            return r;
                    }
                }
            }

            // Если не нашли PolyLine контур, ищем любой контур с точками
            foreach (var r in _currentLayer.Regions)
            {
                if (contourTypes.Contains(r.GeometryRegion) &&
                    r.Part?.Id == region.Part?.Id &&
                    r.PolyLines != null && r.PolyLines.Count > 0)
                {
                    foreach (var pl in r.PolyLines)
                    {
                        if (pl?.Points != null && pl.Points.Count >= 2)
                            return r;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Возвращает типы контуров, которые могут соответствовать данному типу региона
        /// </summary>
        private GeometryRegion[] GetMatchingContourTypes(GeometryRegion regionType)
        {
            return regionType switch
            {
                GeometryRegion.Infill => new[] { GeometryRegion.Contour },
                GeometryRegion.Downskin => new[] { GeometryRegion.ContourDownskin, GeometryRegion.Contour },
                GeometryRegion.Upskin => new[] { GeometryRegion.ContourUpskin, GeometryRegion.Contour },
                GeometryRegion.SupportFill => new[] { GeometryRegion.Support, GeometryRegion.Contour },
                _ => new[] { GeometryRegion.Contour }
            };
        }

        private PathGeometry CreateGeometryFromRegion(CliRegion region)
        {
            var pathGeometry = new PathGeometry();

            foreach (var polyLine in region.PolyLines)
            {
                if (polyLine?.Points == null || polyLine.Points.Count < 2)
                    continue;

                var figure = new PathFigure();
                figure.StartPoint = TransformPoint(polyLine.Points[0]);
                figure.IsClosed = true;

                for (int i = 1; i < polyLine.Points.Count; i++)
                {
                    var segment = new LineSegment(TransformPoint(polyLine.Points[i]), true);
                    figure.Segments.Add(segment);
                }

                pathGeometry.Figures.Add(figure);
            }

            pathGeometry.Freeze();
            return pathGeometry;
        }

        private Rect GetRegionBounds(CliRegion region)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var polyLine in region.PolyLines)
            {
                if (polyLine?.Points == null)
                    continue;

                foreach (var point in polyLine.Points)
                {
                    var transformed = TransformPoint(point);
                    if (transformed.X < minX) minX = transformed.X;
                    if (transformed.X > maxX) maxX = transformed.X;
                    if (transformed.Y < minY) minY = transformed.Y;
                    if (transformed.Y > maxY) maxY = transformed.Y;
                }
            }

            if (minX == double.MaxValue)
                return Rect.Empty;

            return new Rect(new System.Windows.Point(minX, minY), new System.Windows.Point(maxX, maxY));
        }

        private System.Windows.Point TransformPoint(CliPoint point)
        {
            // CLI координаты: центр (0,0), диапазон ±160мм
            // Canvas координаты: верхний левый угол (0,0), размер 600x600 (масштабированный)
            double x = (point.X + FIELD_SIZE / 2.0) * SCALE_FACTOR;
            double y = (FIELD_SIZE / 2.0 - point.Y) * SCALE_FACTOR; // Инвертируем Y

            return new System.Windows.Point(x, y);
        }

        private bool ShouldRenderRegion(CliRegion region)
        {
            // Отображаем Contour, Infill, InfillRegionPreview и Hatch регионы
            return region.GeometryRegion == GeometryRegion.Contour ||
                   region.GeometryRegion == GeometryRegion.Infill ||
                   region.GeometryRegion == GeometryRegion.InfillRegionPreview ||
                   region.Type == BlockType.Hatch;
        }

        private Color GetRegionColor(GeometryRegion geometryRegion)
        {
            return geometryRegion switch
            {
                GeometryRegion.Contour => Color.FromRgb(255, 165, 0), // Оранжевый
                GeometryRegion.Infill => Color.FromRgb(39, 39, 39), // Темно-серый
                _ => Color.FromRgb(200, 200, 200) // Серый
            };
        }

        private Color GetColorByLaserNum(int laserNum, Color defaultColor)
        {
            return laserNum switch
            {
                0 => Colors.Red,           // LaserNum 0 - красный
                1 => Colors.Blue,          // LaserNum 1 - синий
                2 => Colors.Yellow,        // LaserNum 2 - жёлтый
                3 => Colors.Magenta,       // LaserNum 3 - пурпурный
                _ => defaultColor          // Остальные - цвет по умолчанию
            };
        }

        private Color MakeBrighter(Color color, double factor)
        {
            // Увеличиваем компоненты RGB, ограничивая максимумом 255
            byte r = (byte)Math.Min(255, color.R * factor);
            byte g = (byte)Math.Min(255, color.G * factor);
            byte b = (byte)Math.Min(255, color.B * factor);

            return Color.FromArgb(color.A, r, g, b);
        }

        // Zoom и Pan
        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            lock (_zoomLock)
            {
                var delta = e.Delta > 0 ? 1 + ZOOM_SPEED : 1 - ZOOM_SPEED;
                _targetZoom = Math.Clamp(_targetZoom * delta, MIN_ZOOM, MAX_ZOOM);

                // Сохраняем позицию мыши для плавного зума
                _zoomMousePosition = e.GetPosition(ViewportCanvas);

                Console.WriteLine($"[ZOOM] Target: {_targetZoom:F2}, Current: {_zoom:F2}, Mouse: ({_zoomMousePosition.X:F1}, {_zoomMousePosition.Y:F1})");

                // Запускаем или перезапускаем таймер анимации
                if (_zoomTimer == null)
                {
                    _zoomTimer = new System.Threading.Timer(ZoomAnimationTick, null, 0, 16); // ~60 FPS
                }
            }

            e.Handled = true;
        }

        // Тик анимации зума
        private void ZoomAnimationTick(object state)
        {
            Dispatcher.Invoke(() =>
            {
                lock (_zoomLock)
                {
                    // Проверяем, достигли ли целевого зума
                    double diff = _targetZoom - _zoom;
                    if (Math.Abs(diff) < 0.001)
                    {
                        // Анимация завершена
                        _zoom = _targetZoom;
                        ApplyZoom(_zoomMousePosition, _zoom);

                        // Останавливаем таймер
                        _zoomTimer?.Dispose();
                        _zoomTimer = null;
                        return;
                    }

                    // Плавная интерполяция (easing out)
                    const double interpolationFactor = 0.2;
                    double oldZoom = _zoom;
                    _zoom += diff * interpolationFactor;

                    // Применяем зум
                    ApplyZoom(_zoomMousePosition, _zoom);

                    // Проверяем порог LOD
                    bool wasDetails = oldZoom >= DETAIL_ZOOM_THRESHOLD;
                    bool nowDetails = _zoom >= DETAIL_ZOOM_THRESHOLD;

                    if (wasDetails != nowDetails)
                    {
                        Console.WriteLine($"[LOD] Switching detail level: {nowDetails}");
                        RenderLayer();
                    }
                }
            });
        }

        // Применение зума относительно позиции мыши
        private void ApplyZoom(System.Windows.Point mousePosition, double zoom)
        {
            // Центр трансформации (из XAML: CenterX="300" CenterY="300")
            double centerX = ScaleTransform.CenterX;
            double centerY = ScaleTransform.CenterY;

            // Вычисляем точку на MainCanvas, на которую указывает курсор (до изменения зума)
            // Используем текущий ScaleTransform для вычисления
            double oldZoom = ScaleTransform.ScaleX;
            double mouseCanvasX = (mousePosition.X - TranslateTransform.X - centerX) / oldZoom + centerX;
            double mouseCanvasY = (mousePosition.Y - TranslateTransform.Y - centerY) / oldZoom + centerY;

            // Применяем новый зум
            ScaleTransform.ScaleX = zoom;
            ScaleTransform.ScaleY = zoom;

            // Корректируем смещение так, чтобы точка под курсором осталась на месте
            TranslateTransform.X = mousePosition.X - (mouseCanvasX - centerX) * zoom - centerX;
            TranslateTransform.Y = mousePosition.Y - (mouseCanvasY - centerY) * zoom - centerY;

            // Применяем ограничения
            ClampTransform();
        }

        // Плавно приводит MainCanvas к центру ViewportCanvas
        private void CenterMainCanvas()
        {
            // Размеры ViewportCanvas
            double viewportWidth = ViewportCanvas.ActualWidth;
            double viewportHeight = ViewportCanvas.ActualHeight;

            // Центр масштабирования
            double centerX = ScaleTransform.CenterX;
            double centerY = ScaleTransform.CenterY;

            // Вычисляем целевое положение для центрирования
            // Центр MainCanvas должен совпасть с центром ViewportCanvas
            double targetCenterX = viewportWidth / 2.0;
            double targetCenterY = viewportHeight / 2.0;

            // Текущий центр MainCanvas после трансформации
            double currentCenterX = TranslateTransform.X + centerX;
            double currentCenterY = TranslateTransform.Y + centerY;

            // Вычисляем разницу между текущей и целевой позицией
            double deltaX = targetCenterX - currentCenterX;
            double deltaY = targetCenterY - currentCenterY;

            // Плавное движение к центру: сдвигаем на 20% от расстояния
            const double interpolationFactor = 0.2;
            TranslateTransform.X += deltaX * interpolationFactor;
            TranslateTransform.Y += deltaY * interpolationFactor;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePosition = e.GetPosition(ViewportCanvas);
            _isDragging = false; // Будет установлено в true только при движении мыши
            ViewportCanvas.CaptureMouse();
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var currentPosition = e.GetPosition(ViewportCanvas);

            // Если мышь не двигалась (или двигалась незначительно), это клик для выделения
            var delta = currentPosition - _lastMousePosition;
            if (Math.Abs(delta.X) < 3 && Math.Abs(delta.Y) < 3 && !_isDragging)
            {
                HandlePartSelection(currentPosition);
            }

            _isDragging = false;
            ViewportCanvas.ReleaseMouseCapture();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!ViewportCanvas.IsMouseCaptured)
                return;

            var currentPosition = e.GetPosition(ViewportCanvas);
            var delta = currentPosition - _lastMousePosition;

            // Если мышь сдвинулась достаточно, начинаем драг
            if (Math.Abs(delta.X) > 3 || Math.Abs(delta.Y) > 3)
            {
                _isDragging = true;
            }

            if (_isDragging)
            {
                TranslateTransform.X += delta.X;
                TranslateTransform.Y += delta.Y;

                // Применяем ограничения
                ClampTransform();

                _lastMousePosition = currentPosition;
            }
        }

        // Обработка сенсорных жестов
        private void Canvas_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = ViewportCanvas;
            e.Mode = ManipulationModes.Scale | ManipulationModes.Translate;

            // Сохраняем начальное состояние
            _initialManipulationScale = _zoom;
            _initialManipulationTranslate = new System.Windows.Point(TranslateTransform.X, TranslateTransform.Y);

            e.Handled = true;
        }

        private void Canvas_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            // Обработка масштабирования (pinch-to-zoom)
            if (e.DeltaManipulation.Scale.X != 1.0 || e.DeltaManipulation.Scale.Y != 1.0)
            {
                double scaleDelta = (e.DeltaManipulation.Scale.X + e.DeltaManipulation.Scale.Y) / 2.0;
                double oldZoom = _zoom;
                double newZoom = Math.Clamp(_zoom * scaleDelta, MIN_ZOOM, MAX_ZOOM);

                _zoom = newZoom;
                ScaleTransform.ScaleX = newZoom;
                ScaleTransform.ScaleY = newZoom;

                Console.WriteLine($"[TOUCH ZOOM] Old: {oldZoom:F2}, New: {newZoom:F2}");

                // Проверяем порог LOD
                bool wasDetails = oldZoom >= DETAIL_ZOOM_THRESHOLD;
                bool nowDetails = newZoom >= DETAIL_ZOOM_THRESHOLD;

                if (wasDetails != nowDetails)
                {
                    Console.WriteLine($"[LOD] Switching detail level: {nowDetails}");
                    RenderLayer();
                }
            }

            // Обработка перемещения (pan)
            if (e.DeltaManipulation.Translation.X != 0 || e.DeltaManipulation.Translation.Y != 0)
            {
                TranslateTransform.X += e.DeltaManipulation.Translation.X;
                TranslateTransform.Y += e.DeltaManipulation.Translation.Y;
            }

            // Применяем ограничения
            ClampTransform();

            e.Handled = true;
        }

        private void Canvas_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            Console.WriteLine($"[TOUCH] Manipulation completed, Final Zoom: {_zoom:F2}");
            e.Handled = true;
        }

        // Обработка выделения детали по клику
        private void HandlePartSelection(System.Windows.Point clickPosition)
        {
            if (_currentLayer == null)
                return;

            // Преобразуем координаты клика в координаты MainCanvas с учётом трансформаций
            var transformedPoint = TransformClickToCanvas(clickPosition);

            // Ищем деталь по клику
            Part foundPart = null;

            foreach (var partEntry in _partVisualsMap)
            {
                foreach (var visual in partEntry.Value)
                {
                    var hitTestResult = VisualTreeHelper.HitTest(visual, transformedPoint);
                    if (hitTestResult != null)
                    {
                        foundPart = partEntry.Key;
                        break;
                    }
                }

                if (foundPart != null)
                    break;
            }

            // Если нашли деталь, выделяем её
            if (foundPart != null)
            {
                SelectPart(foundPart);
            }
            else
            {
                // Если кликнули не на деталь, снимаем выделение
                ClearSelection();
            }
        }

        // Преобразование координат клика в координаты MainCanvas
        private System.Windows.Point TransformClickToCanvas(System.Windows.Point viewportPoint)
        {
            // Применяем обратную трансформацию
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new TranslateTransform(-TranslateTransform.X, -TranslateTransform.Y));
            transformGroup.Children.Add(new ScaleTransform(1.0 / _zoom, 1.0 / _zoom, ScaleTransform.CenterX, ScaleTransform.CenterY));

            return transformGroup.Transform(viewportPoint);
        }

        // Выделение детали
        private void SelectPart(Part part)
        {
            if (_selectedPart == part)
                return;

            _selectedPart = part;
            Console.WriteLine($"[SELECTION] Selected part: {part.Name} (ID: {part.Id})");

            RenderSelection();
            ShowPartName();
        }

        // Снятие выделения
        private void ClearSelection()
        {
            if (_selectedPart == null)
                return;

            _selectedPart = null;
            Console.WriteLine($"[SELECTION] Cleared selection");

            // Убираем визуал выделения
            if (_selectionVisual != null)
            {
                _visualHost.RemoveVisual(_selectionVisual);
                _selectionVisual = null;
            }

            // Скрываем название детали
            PartNameBorder.Visibility = Visibility.Collapsed;
        }

        // Отрисовка выделения детали
        private void RenderSelection()
        {
            // Убираем старый визуал выделения
            if (_selectionVisual != null)
            {
                _visualHost.RemoveVisual(_selectionVisual);
                _selectionVisual = null;
            }

            if (_selectedPart == null || _currentLayer == null)
                return;

            // Создаём новый визуал для выделения
            _selectionVisual = new DrawingVisual();
            using (var dc = _selectionVisual.RenderOpen())
            {
                // Цвет выделения - яркий зелёный с прозрачностью
                var selectionColor = Color.FromArgb(180, 0, 255, 0);
                var selectionPen = new Pen(new SolidColorBrush(selectionColor), 2.0 / _zoom);
                selectionPen.Freeze();

                // Рисуем контуры всех регионов выбранной детали
                foreach (var region in _currentLayer.Regions)
                {
                    if (region.Part?.Id != _selectedPart.Id)
                        continue;

                    if (!ShouldRenderRegion(region))
                        continue;

                    // Рисуем контур
                    foreach (var polyLine in region.PolyLines)
                    {
                        if (polyLine?.Points == null || polyLine.Points.Count < 2)
                            continue;

                        for (int i = 0; i < polyLine.Points.Count - 1; i++)
                        {
                            var p1 = TransformPoint(polyLine.Points[i]);
                            var p2 = TransformPoint(polyLine.Points[i + 1]);
                            dc.DrawLine(selectionPen, p1, p2);
                        }
                    }
                }
            }

            _visualHost.AddVisual(_selectionVisual);
        }

        // Отображение названия детали
        private void ShowPartName()
        {
            if (_selectedPart == null || _currentLayer == null)
            {
                PartNameBorder.Visibility = Visibility.Collapsed;
                return;
            }

            // Устанавливаем текст названия
            PartNameText.Text = _selectedPart.Name ?? $"Part {_selectedPart.Id}";

            // Находим центр bounding box выделенной детали
            var bounds = GetPartBounds(_selectedPart);
            if (bounds.IsEmpty)
            {
                PartNameBorder.Visibility = Visibility.Collapsed;
                return;
            }

            // Позиционируем текст над деталью (по центру сверху)
            double centerX = (bounds.Left + bounds.Right) / 2.0;
            double topY = bounds.Top;

            // Смещаем вверх на 20 пикселей (с учётом зума)
            topY -= 20.0 / _zoom;

            Canvas.SetLeft(PartNameBorder, centerX);
            Canvas.SetTop(PartNameBorder, topY);

            // Корректируем масштаб текста для читаемости
            var textScale = 1.0 / _zoom;
            PartNameBorder.RenderTransform = new ScaleTransform(textScale, textScale);
            PartNameBorder.RenderTransformOrigin = new System.Windows.Point(0.5, 1.0); // Центр снизу

            PartNameBorder.Visibility = Visibility.Visible;
        }

        // Получение границ детали
        private Rect GetPartBounds(Part part)
        {
            if (_currentLayer == null)
                return Rect.Empty;

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var region in _currentLayer.Regions)
            {
                if (region.Part?.Id != part.Id)
                    continue;

                if (!ShouldRenderRegion(region))
                    continue;

                foreach (var polyLine in region.PolyLines)
                {
                    if (polyLine?.Points == null)
                        continue;

                    foreach (var point in polyLine.Points)
                    {
                        var transformed = TransformPoint(point);
                        if (transformed.X < minX) minX = transformed.X;
                        if (transformed.X > maxX) maxX = transformed.X;
                        if (transformed.Y < minY) minY = transformed.Y;
                        if (transformed.Y > maxY) maxY = transformed.Y;
                    }
                }
            }

            if (minX == double.MaxValue)
                return Rect.Empty;

            return new Rect(new System.Windows.Point(minX, minY), new System.Windows.Point(maxX, maxY));
        }

        // Ограничиваем трансформацию, чтобы MainCanvas не мог полностью выйти за границы ViewportCanvas
        private void ClampTransform()
        {
            // Размеры ViewportCanvas
            double viewportWidth = ViewportCanvas.ActualWidth;
            double viewportHeight = ViewportCanvas.ActualHeight;

            // Размеры MainCanvas после масштабирования
            double scaledWidth = CANVAS_SIZE * _zoom;
            double scaledHeight = CANVAS_SIZE * _zoom;

            // Центр масштабирования MainCanvas (CenterX, CenterY из ScaleTransform)
            double centerX = ScaleTransform.CenterX;
            double centerY = ScaleTransform.CenterY;

            // Вычисляем границы MainCanvas после трансформации
            // Левый верхний угол после масштабирования относительно центра
            double left = TranslateTransform.X + centerX * (1 - _zoom);
            double top = TranslateTransform.Y + centerY * (1 - _zoom);
            double right = left + scaledWidth;
            double bottom = top + scaledHeight;

            // Минимальная видимая часть (например, 50 пикселей должны быть видны)
            const double minVisibleSize = 50.0;

            // Ограничиваем смещение
            double newX = TranslateTransform.X;
            double newY = TranslateTransform.Y;

            // Не даём MainCanvas уйти полностью влево (правая граница должна быть видна)
            if (right < minVisibleSize)
            {
                newX = minVisibleSize - scaledWidth - centerX * (1 - _zoom);
            }

            // Не даём MainCanvas уйти полностью вправо (левая граница должна быть видна)
            if (left > viewportWidth - minVisibleSize)
            {
                newX = viewportWidth - minVisibleSize - centerX * (1 - _zoom);
            }

            // Не даём MainCanvas уйти полностью вверх (нижняя граница должна быть видна)
            if (bottom < minVisibleSize)
            {
                newY = minVisibleSize - scaledHeight - centerY * (1 - _zoom);
            }

            // Не даём MainCanvas уйти полностью вниз (верхняя граница должна быть видна)
            if (top > viewportHeight - minVisibleSize)
            {
                newY = viewportHeight - minVisibleSize - centerY * (1 - _zoom);
            }

            TranslateTransform.X = newX;
            TranslateTransform.Y = newY;
        }

        // Вспомогательный класс для хранения DrawingVisual элементов
        private class VisualHost : FrameworkElement
        {
            private readonly VisualCollection _visuals;

            public VisualHost()
            {
                _visuals = new VisualCollection(this);
            }

            public void AddVisual(DrawingVisual visual)
            {
                _visuals.Add(visual);
            }

            public void RemoveVisual(DrawingVisual visual)
            {
                _visuals.Remove(visual);
            }

            public void ClearVisuals()
            {
                _visuals.Clear();
            }

            protected override int VisualChildrenCount => _visuals.Count;

            protected override Visual GetVisualChild(int index)
            {
                if (index < 0 || index >= _visuals.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _visuals[index];
            }
        }
    }
}
