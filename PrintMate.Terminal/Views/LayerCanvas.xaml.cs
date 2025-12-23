using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PrintMate.Terminal.Views.Modals;
using CliRegion = ProjectParserTest.Parsers.Shared.Models.Region;
using CliPoint = ProjectParserTest.Parsers.Shared.Models.Point;
using ProjectParserTest.Parsers.Shared.Models;
using ProjectParserTest.Parsers.Shared.Enums;

namespace PrintMate.Terminal.Views
{
    public partial class LayerCanvas : UserControl
    {
        private const double FIELD_SIZE = 320.0; // Реальный размер платформы в мм (320x320)
        private const double DEFAULT_CANVAS_SIZE = 600.0; // Размер по умолчанию для инициализации

        // Динамический размер канваса (растягивается на весь экран)
        private double CanvasSize
        {
            get
            {
                if (ViewportCanvas == null) return DEFAULT_CANVAS_SIZE;
                double width = ViewportCanvas.ActualWidth;
                double height = ViewportCanvas.ActualHeight;
                if (width <= 0 || height <= 0) return DEFAULT_CANVAS_SIZE;
                return Math.Min(width, height);
            }
        }

        private double ScaleFactor
        {
            get
            {
                var size = CanvasSize;
                if (size <= 0) return DEFAULT_CANVAS_SIZE / FIELD_SIZE;
                return size / FIELD_SIZE;
            }
        }
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

        // Для отложенного рендеринга при смене LOD (оптимизация производительности)
        private DispatcherTimer _deferredRenderTimer;
        private bool _needsLodRender = false;

        // Визуальные элементы для рендеринга
        private readonly List<DrawingVisual> _visuals = new List<DrawingVisual>();
        private readonly VisualHost _visualHost;

        // Выделенная деталь и её визуализация
        private Part _selectedPart;
        private DrawingVisual _selectionVisual;
        private readonly Dictionary<Part, List<DrawingVisual>> _partVisualsMap = new Dictionary<Part, List<DrawingVisual>>();

        // Онлайн-режим прожига
        private double _markingProgress = 0.0; // Процент прожига текущего слоя (0.0 - 1.0)
        private double _scanner1Progress = 0.0; // Прогресс сканатора 1 (LaserNum=1)
        private double _scanner2Progress = 0.0; // Прогресс сканатора 2 (LaserNum=0)
        private DrawingVisual _markingProgressVisual;
        private bool _isOnlineMode = false; // Включен ли онлайн-режим

        public LayerCanvas()
        {
            InitializeComponent();

            // Создаём хост для DrawingVisual элементов (размер определяется динамически)
            _visualHost = new VisualHost
            {
                ClipToBounds = true
            };
            Canvas.SetLeft(_visualHost, 0);
            Canvas.SetTop(_visualHost, 0);
            MainCanvas.Children.Add(_visualHost);

            // Инициализируем таймер для отложенного рендеринга (200мс задержка)
            _deferredRenderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _deferredRenderTimer.Tick += OnDeferredRenderTimer;

            // Рисуем сетку сразу при загрузке
            RenderLayer();
        }

        /// <summary>
        /// Обработчик таймера отложенного рендеринга
        /// </summary>
        private void OnDeferredRenderTimer(object sender, EventArgs e)
        {
            _deferredRenderTimer.Stop();
            if (_needsLodRender)
            {
                _needsLodRender = false;
                RenderLayer();
            }
        }

        /// <summary>
        /// Запланировать отложенный рендеринг при смене LOD
        /// </summary>
        private void ScheduleDeferredRender()
        {
            _needsLodRender = true;
            _deferredRenderTimer.Stop();
            _deferredRenderTimer.Start();
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

        /// <summary>
        /// Публичный метод для выделения детали (используется из ViewModel)
        /// </summary>
        public void HighlightPart(int? partId)
        {
            if (partId == null)
            {
                ClearSelection();
                return;
            }

            // Находим деталь по ID в текущем слое
            if (_currentLayer != null)
            {
                foreach (var region in _currentLayer.Regions)
                {
                    if (region.Part != null && region.Part.Id == partId.Value)
                    {
                        SelectPart(region.Part);
                        return;
                    }
                }
            }

            // Если деталь не найдена на текущем слое, снимаем выделение
            ClearSelection();
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

            // В онлайн-режиме не рисуем основной слой
            if (_isOnlineMode)
                return;

            bool showDetails = _zoom >= DETAIL_ZOOM_THRESHOLD;

            // Проходим по всем регионам
            foreach (var region in _currentLayer.Regions)
            {
                if (!ShouldRenderRegion(region))
                    continue;

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
                return;

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
                if (lineIndex % 5 != 0)
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
                var canvasSize = CanvasSize;

                // Радиус закругления углов
                double cornerRadius = 15.0;

                // Создаём закругленный прямоугольник для платформы
                var platformRect = new RectangleGeometry(new Rect(0, 0, canvasSize, canvasSize), cornerRadius, cornerRadius);
                platformRect.Freeze();

                // Фон платформы - серый
                var backgroundBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
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

                // Шаг сетки в canvas координатах (с учетом ScaleFactor)
                double gridStep = gridStepMm * ScaleFactor;
                double majorGridStep = majorGridStepMm * ScaleFactor;

                // Перо для основных линий (более тёмное на сером фоне)
                var majorGridPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 60, 60, 60)), 1.0 / _zoom);
                majorGridPen.Freeze();

                // Рисуем вертикальные линии
                for (double x = 0; x <= canvasSize; x += gridStep)
                {
                    bool isMajor = Math.Abs(x % majorGridStep) < 0.1;
                    var pen = isMajor ? majorGridPen : gridPen;
                    dc.DrawLine(pen, new System.Windows.Point(x, 0), new System.Windows.Point(x, canvasSize));
                }

                // Рисуем горизонтальные линии
                for (double y = 0; y <= canvasSize; y += gridStep)
                {
                    bool isMajor = Math.Abs(y % majorGridStep) < 0.1;
                    var pen = isMajor ? majorGridPen : gridPen;
                    dc.DrawLine(pen, new System.Windows.Point(0, y), new System.Windows.Point(canvasSize, y));
                }

                // Рисуем центральные оси (оранжевые)
                var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 111, 0)), 1.5 / _zoom);
                axisPen.Freeze();
                double center = canvasSize / 2.0;

                // Вертикальная ось (X=0)
                dc.DrawLine(axisPen, new System.Windows.Point(center, 0), new System.Windows.Point(center, canvasSize));
                // Горизонтальная ось (Y=0)
                dc.DrawLine(axisPen, new System.Windows.Point(0, center), new System.Windows.Point(canvasSize, center));

                // Снимаем clip
                dc.Pop();

                // Обводка платформы - оранжевая с закругленными углами
                var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 255, 111, 0)), 3.0 / _zoom);
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
            double x = (point.X + FIELD_SIZE / 2.0) * ScaleFactor;
            double y = (FIELD_SIZE / 2.0 - point.Y) * ScaleFactor; // Инвертируем Y

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
            return Color.FromRgb(255, 106, 22);
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
            var mousePosition = e.GetPosition(ViewportCanvas);
            var delta = e.Delta > 0 ? 1 + ZOOM_SPEED : 1 - ZOOM_SPEED;

            double oldZoom = _zoom;
            _zoom = Math.Clamp(_zoom * delta, MIN_ZOOM, MAX_ZOOM);

            // Применяем зум мгновенно
            ApplyZoom(mousePosition, _zoom);

            // Проверяем порог LOD - используем отложенный рендеринг
            bool wasDetails = oldZoom >= DETAIL_ZOOM_THRESHOLD;
            bool nowDetails = _zoom >= DETAIL_ZOOM_THRESHOLD;

            if (wasDetails != nowDetails)
            {
                ScheduleDeferredRender();
            }

            e.Handled = true;
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

            // Если включён онлайн-режим, перерисовываем прогресс с новым зумом
            if (_isOnlineMode)
            {
                RenderMarkingProgress();
            }
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
            // Получаем центр манипуляции (точка между пальцами)
            var manipulationOrigin = e.ManipulationOrigin;

            // Обработка масштабирования (pinch-to-zoom)
            if (e.DeltaManipulation.Scale.X != 1.0 || e.DeltaManipulation.Scale.Y != 1.0)
            {
                double scaleDelta = (e.DeltaManipulation.Scale.X + e.DeltaManipulation.Scale.Y) / 2.0;
                double oldZoom = _zoom;
                double newZoom = Math.Clamp(_zoom * scaleDelta, MIN_ZOOM, MAX_ZOOM);

                // Применяем зум относительно точки касания (центра pinch-жеста)
                ApplyTouchZoom(manipulationOrigin, oldZoom, newZoom);

                // Проверяем порог LOD - используем отложенный рендеринг
                bool wasDetails = oldZoom >= DETAIL_ZOOM_THRESHOLD;
                bool nowDetails = newZoom >= DETAIL_ZOOM_THRESHOLD;

                if (wasDetails != nowDetails)
                {
                    ScheduleDeferredRender();
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

        /// <summary>
        /// Применяет зум относительно точки касания (для сенсорного pinch-to-zoom)
        /// </summary>
        private void ApplyTouchZoom(System.Windows.Point touchOrigin, double oldZoom, double newZoom)
        {
            // Центр трансформации из ScaleTransform
            double centerX = ScaleTransform.CenterX;
            double centerY = ScaleTransform.CenterY;

            // Вычисляем точку на MainCanvas, на которую указывает центр касания (до изменения зума)
            double touchCanvasX = (touchOrigin.X - TranslateTransform.X - centerX) / oldZoom + centerX;
            double touchCanvasY = (touchOrigin.Y - TranslateTransform.Y - centerY) / oldZoom + centerY;

            // Применяем новый зум
            _zoom = newZoom;
            ScaleTransform.ScaleX = newZoom;
            ScaleTransform.ScaleY = newZoom;

            // Корректируем смещение так, чтобы точка под центром касания осталась на месте
            TranslateTransform.X = touchOrigin.X - (touchCanvasX - centerX) * newZoom - centerX;
            TranslateTransform.Y = touchOrigin.Y - (touchCanvasY - centerY) * newZoom - centerY;

            // Если включён онлайн-режим, перерисовываем прогресс с новым зумом
            if (_isOnlineMode)
            {
                RenderMarkingProgress();
            }
        }

        private void Canvas_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            e.Handled = true;
        }

        // Обработка изменения размера viewport для адаптивного масштабирования
        private void ViewportCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                // Вычисляем квадратный размер (минимальное измерение)
                double squareSize = Math.Min(e.NewSize.Width, e.NewSize.Height);

                // Вычисляем смещение для центрирования
                double offsetX = (e.NewSize.Width - squareSize) / 2;
                double offsetY = (e.NewSize.Height - squareSize) / 2;

                // Обновляем размер MainCanvas (квадратный)
                MainCanvas.Width = squareSize;
                MainCanvas.Height = squareSize;
                Canvas.SetLeft(MainCanvas, offsetX);
                Canvas.SetTop(MainCanvas, offsetY);

                // Обновляем размер VisualHost (квадратный)
                _visualHost.Width = squareSize;
                _visualHost.Height = squareSize;

                // Обновляем центр масштабирования (центр квадрата)
                ScaleTransform.CenterX = squareSize / 2;
                ScaleTransform.CenterY = squareSize / 2;

                // Обновляем размеры OverlayCanvas (квадратный, в тех же координатах)
                OverlayCanvas.Width = squareSize;
                OverlayCanvas.Height = squareSize;
                Canvas.SetLeft(OverlayCanvas, offsetX);
                Canvas.SetTop(OverlayCanvas, offsetY);

                // Перерисовываем слой с новыми размерами
                RenderLayer();
            }
        }

        // Обработка выделения детали по клику
        private void HandlePartSelection(System.Windows.Point clickPosition)
        {
            if (_currentLayer == null)
                return;

            // Преобразуем координаты клика в координаты MainCanvas с учётом трансформаций
            var transformedPoint = TransformClickToCanvas(clickPosition);

            // Радиус поиска в пикселях экрана (очень большой радиус для удобства)
            double searchRadiusPixels = 100.0;
            // Преобразуем радиус в координаты canvas с учётом zoom
            double searchRadius = searchRadiusPixels / _zoom;

            // Ищем деталь по клику с расширенным радиусом
            Part foundPart = null;
            double minDistance = double.MaxValue;

            foreach (var partEntry in _partVisualsMap)
            {
                // Сначала точный HitTest
                foreach (var visual in partEntry.Value)
                {
                    var hitTestResult = VisualTreeHelper.HitTest(visual, transformedPoint);
                    if (hitTestResult != null)
                    {
                        foundPart = partEntry.Key;
                        minDistance = 0;
                        break;
                    }
                }

                if (foundPart != null)
                    break;

                // Если точное попадание не найдено, ищем в радиусе
                var part = partEntry.Key;
                var partRegions = _currentLayer.Regions.Where(r => r.Part?.Id == part.Id).ToList();

                foreach (var region in partRegions)
                {
                    if (region.PolyLines == null) continue;

                    foreach (var polyline in region.PolyLines)
                    {
                        for (int i = 0; i < polyline.Points.Count; i++)
                        {
                            var p1 = polyline.Points[i];

                            // Расстояние от точки клика до вершины полилинии (в координатах canvas)
                            double dx = p1.X - transformedPoint.X;
                            double dy = p1.Y - transformedPoint.Y;
                            double distance = Math.Sqrt(dx * dx + dy * dy);

                            if (distance < searchRadius && distance < minDistance)
                            {
                                minDistance = distance;
                                foundPart = part;
                            }
                        }
                    }
                }
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

            RenderSelection();
            ShowPartName();
        }

        // Снятие выделения
        private void ClearSelection()
        {
            if (_selectedPart == null)
                return;

            _selectedPart = null;

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
            var canvasSize = CanvasSize;
            double scaledWidth = canvasSize * _zoom;
            double scaledHeight = canvasSize * _zoom;

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

        #region Онлайн-режим прожига

        /// <summary>
        /// Обновление прогресса маркировки в онлайн-режиме (старый метод для совместимости)
        /// </summary>
        public void UpdateMarkingProgress(double progressPercent)
        {
            // Используем одинаковый прогресс для обоих сканаторов
            UpdateMarkingProgress(progressPercent, progressPercent);
        }

        /// <summary>
        /// Обновление прогресса маркировки в онлайн-режиме с раздельными прогрессами сканаторов
        /// </summary>
        /// <param name="scanner1ProgressPercent">Прогресс сканатора 1 (LaserNum=1) в процентах (0-100)</param>
        /// <param name="scanner2ProgressPercent">Прогресс сканатора 2 (LaserNum=0) в процентах (0-100)</param>
        public void UpdateMarkingProgress(double scanner1ProgressPercent, double scanner2ProgressPercent)
        {
            // Включаем онлайн-режим, если он ещё не включен
            if (!_isOnlineMode)
            {
                _isOnlineMode = true;
                // При первом включении сразу устанавливаем начальные значения
                _scanner1Progress = 0.0;
                _scanner2Progress = 0.0;
                RenderLayer(); // Скрываем основной слой
            }

            // Напрямую устанавливаем прогрессы (приводим к диапазону 0.0 - 1.0)
            _scanner1Progress = Math.Clamp(scanner1ProgressPercent / 100.0, 0.0, 1.0);
            _scanner2Progress = Math.Clamp(scanner2ProgressPercent / 100.0, 0.0, 1.0);

            // Обновляем общий прогресс
            _markingProgress = (_scanner1Progress + _scanner2Progress) / 2.0;

            // Перерисовываем
            RenderMarkingProgress();
        }

        /// <summary>
        /// Сброс прогресса маркировки
        /// </summary>
        public void ClearMarkingProgress()
        {
            _markingProgress = 0.0;
            _scanner1Progress = 0.0;
            _scanner2Progress = 0.0;
            _isOnlineMode = false;

            // Убираем визуал прогресса
            if (_markingProgressVisual != null)
            {
                _visualHost.RemoveVisual(_markingProgressVisual);
                _markingProgressVisual = null;
            }

            // Восстанавливаем основной слой
            RenderLayer();
        }

        /// <summary>
        /// Отрисовка прогресса маркировки (штриховка линия за линией в реальном времени)
        /// Каждый сканатор прожигает свои регионы независимо
        /// </summary>
        private void RenderMarkingProgress()
        {
            // Убираем старый визуал прогресса
            if (_markingProgressVisual != null)
            {
                _visualHost.RemoveVisual(_markingProgressVisual);
                _markingProgressVisual = null;
            }

            // Если слоя нет, не рисуем ничего (но онлайн-режим остаётся активным)
            if (_currentLayer == null)
                return;

            // Создаём новый визуал для прогресса маркировки
            _markingProgressVisual = new DrawingVisual();
            using (var dc = _markingProgressVisual.RenderOpen())
            {
                // Собираем линии для каждого сканатора отдельно
                var scanner1Lines = new List<LineSegmentInfo>(); // LaserNum=1
                var scanner2Lines = new List<LineSegmentInfo>(); // LaserNum=0

                foreach (var region in _currentLayer.Regions.Where(r => ShouldRenderRegion(r)))
                {
                    if (region.PolyLines == null || region.PolyLines.Count == 0)
                        continue;

                    // Определяем цвет в зависимости от LaserNum
                    Color lineColor;
                    List<LineSegmentInfo> targetList;

                    if (region.LaserNum == 1)
                    {
                        // Сканатор 1 (227) - тёмно-серый
                        lineColor = Color.FromArgb(255, 60, 60, 60);
                        targetList = scanner1Lines;
                    }
                    else
                    {
                        // Сканатор 2 (228) - тёмно-серый
                        lineColor = Color.FromArgb(255, 40, 40, 40);
                        targetList = scanner2Lines;
                    }

                    // Для каждой полилинии региона
                    foreach (var polyLine in region.PolyLines)
                    {
                        if (polyLine?.Points == null || polyLine.Points.Count < 2)
                            continue;

                        // Добавляем все сегменты полилинии
                        for (int i = 0; i < polyLine.Points.Count - 1; i++)
                        {
                            targetList.Add(new LineSegmentInfo
                            {
                                P1 = polyLine.Points[i],
                                P2 = polyLine.Points[i + 1],
                                Color = lineColor
                            });
                        }

                        // Для контуров - замыкаем
                        if (region.Type == BlockType.PolyLine && polyLine.Points.Count > 2)
                        {
                            targetList.Add(new LineSegmentInfo
                            {
                                P1 = polyLine.Points[polyLine.Points.Count - 1],
                                P2 = polyLine.Points[0],
                                Color = lineColor
                            });
                        }
                    }
                }

                // 3. Рисуем прожжённые линии сканатора 1 + красное "перо" на краю
                int scanner1LinesToShow = (int)(scanner1Lines.Count * _scanner1Progress);
                for (int i = 0; i < scanner1LinesToShow && i < scanner1Lines.Count; i++)
                {
                    var lineInfo = scanner1Lines[i];
                    var p1 = TransformPoint(lineInfo.P1);
                    var p2 = TransformPoint(lineInfo.P2);

                    // Обычная линия
                    var pen = new Pen(new SolidColorBrush(lineInfo.Color), 1.5 / _zoom);
                    pen.Freeze();
                    dc.DrawLine(pen, p1, p2);

                    // Если это последняя линия - рисуем красное "перо" поверх всей линии
                    if (i == scanner1LinesToShow - 1)
                    {
                        var redPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 255, 50, 50)), 1.0 / _zoom);
                        redPen.Freeze();

                        // Рисуем всю линию красным цветом
                        dc.DrawLine(redPen, p1, p2);
                    }
                }

                // 4. Рисуем прожжённые линии сканатора 2 + красное "перо" на краю
                int scanner2LinesToShow = (int)(scanner2Lines.Count * _scanner2Progress);
                for (int i = 0; i < scanner2LinesToShow && i < scanner2Lines.Count; i++)
                {
                    var lineInfo = scanner2Lines[i];
                    var p1 = TransformPoint(lineInfo.P1);
                    var p2 = TransformPoint(lineInfo.P2);

                    // Обычная линия
                    var pen = new Pen(new SolidColorBrush(lineInfo.Color), 1.5 / _zoom);
                    pen.Freeze();
                    dc.DrawLine(pen, p1, p2);

                    // Если это последняя линия - рисуем красное "перо" поверх всей линии
                    if (i == scanner2LinesToShow - 1)
                    {
                        var redPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 255, 50, 50)), 1.0 / _zoom);
                        redPen.Freeze();

                        // Рисуем всю линию красным цветом
                        dc.DrawLine(redPen, p1, p2);
                    }
                }
            }

            _visualHost.AddVisual(_markingProgressVisual);
        }

        /// <summary>
        /// Отрисовка региона с заданным цветом для фоновой полупрозрачной фигуры
        /// </summary>
        private void RenderRegion(DrawingContext dc, CliRegion region, bool showDetails, Color color)
        {
            bool isHatch = region.Type == BlockType.Hatch;

            if (region.PolyLines == null || region.PolyLines.Count == 0)
                return;

            // Для штриховки при высоком зуме - рисуем линии
            if (isHatch && showDetails)
            {
                var pen = new Pen(new SolidColorBrush(color), 0.3 / _zoom);
                pen.Freeze();

                int lineIndex = 0;
                foreach (var polyLine in region.PolyLines)
                {
                    if (polyLine?.Points == null || polyLine.Points.Count < 2)
                    {
                        lineIndex++;
                        continue;
                    }

                    if (lineIndex % 5 != 0)
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
            // Для штриховки при низком зуме - заполняем
            else if (isHatch && !showDetails)
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();

                var contourRegion = FindContourForRegion(region);
                if (contourRegion != null && contourRegion.PolyLines != null)
                {
                    var geometry = CreateGeometryFromRegion(contourRegion);
                    if (geometry != null)
                    {
                        dc.DrawGeometry(brush, null, geometry);
                    }
                }
                else
                {
                    var bounds = GetRegionBounds(region);
                    if (!bounds.IsEmpty)
                    {
                        dc.DrawRectangle(brush, null, bounds);
                    }
                }
            }
            // Для контуров - рисуем линии
            else if (!isHatch)
            {
                var pen = new Pen(new SolidColorBrush(color), 0.5 / _zoom);
                pen.Freeze();

                foreach (var polyLine in region.PolyLines)
                {
                    if (polyLine?.Points == null || polyLine.Points.Count < 2)
                        continue;

                    for (int i = 0; i < polyLine.Points.Count - 1; i++)
                    {
                        var p1 = TransformPoint(polyLine.Points[i]);
                        var p2 = TransformPoint(polyLine.Points[i + 1]);
                        dc.DrawLine(pen, p1, p2);
                    }

                    // Замыкаем контур
                    if (polyLine.Points.Count > 2)
                    {
                        var pLast = TransformPoint(polyLine.Points[polyLine.Points.Count - 1]);
                        var pFirst = TransformPoint(polyLine.Points[0]);
                        dc.DrawLine(pen, pLast, pFirst);
                    }
                }
            }
        }

        /// <summary>
        /// Информация о сегменте линии для отрисовки
        /// </summary>
        private class LineSegmentInfo
        {
            public CliPoint P1 { get; set; }
            public CliPoint P2 { get; set; }
            public Color Color { get; set; }
        }

        #endregion
    }
}
