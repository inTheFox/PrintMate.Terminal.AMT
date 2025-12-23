using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ProjectParserTest.Parsers.Shared.Models;
using ProjectParserTest.Parsers.Shared.Enums;
using PrintMate.Terminal.Parsers.Shared.Models;
using CliRegion = ProjectParserTest.Parsers.Shared.Models.Region;
using CliPoint = ProjectParserTest.Parsers.Shared.Models.Point;

namespace PrintMate.Terminal.Controls
{
    /// <summary>
    /// CPU-рендерер с изометрической проекцией для слабых ПК без дискретной видеокарты.
    /// Полностью рендерит на процессоре через WriteableBitmap.
    /// </summary>
    public class IsometricLayerRenderer
    {
        #region Константы

        // Изометрические углы (стандартная изометрия 30°)
        private const double ISO_ANGLE_X = 30.0 * Math.PI / 180.0; // 30 градусов
        private const double ISO_ANGLE_Y = 30.0 * Math.PI / 180.0;

        // Коэффициенты проекции
        private static readonly double CosX = Math.Cos(ISO_ANGLE_X);
        private static readonly double SinX = Math.Sin(ISO_ANGLE_X);
        private static readonly double CosY = Math.Cos(ISO_ANGLE_Y);
        private static readonly double SinY = Math.Sin(ISO_ANGLE_Y);

        // Размер рабочего поля в мм
        private const float FIELD_SIZE = 320f;
        private const float HALF_FIELD = FIELD_SIZE / 2f;

        // Цвета по умолчанию
        private static readonly Color BackgroundColor = Color.FromRgb(30, 30, 30);
        private static readonly Color PlatformColor = Color.FromRgb(50, 50, 50);
        private static readonly Color GridColor = Color.FromRgb(60, 60, 60);
        private static readonly Color AxisColor = Color.FromRgb(80, 80, 80);
        private static readonly Color ContourColor = Color.FromRgb(255, 100, 30);
        private static readonly Color HatchColor = Color.FromRgb(200, 80, 20);
        private static readonly Color PreviousLayersColor = Color.FromRgb(100, 100, 100);

        #endregion

        #region Приватные поля

        private WriteableBitmap _bitmap;
        private int _width;
        private int _height;
        private double _scale = 1.5;
        private double _offsetX;
        private double _offsetY;
        private double _rotationAngle = 45.0; // Поворот вокруг оси Z в градусах
        private double _elevationAngle = 30.0; // Угол наклона камеры

        // Данные проекта
        private Project _currentProject;
        private int _currentLayerIndex;
        private int _maxVisibleLayers = 10; // Количество видимых предыдущих слоёв

        // Z-buffer для корректной отрисовки
        private double[] _zBuffer;

        #endregion

        #region Публичные свойства

        /// <summary>
        /// Масштаб отображения
        /// </summary>
        public double Scale
        {
            get => _scale;
            set => _scale = Math.Clamp(value, 0.1, 10.0);
        }

        /// <summary>
        /// Угол поворота вокруг оси Z (в градусах)
        /// </summary>
        public double RotationAngle
        {
            get => _rotationAngle;
            set => _rotationAngle = value % 360.0;
        }

        /// <summary>
        /// Угол наклона камеры (в градусах, 0 = вид сверху, 90 = вид сбоку)
        /// </summary>
        public double ElevationAngle
        {
            get => _elevationAngle;
            set => _elevationAngle = Math.Clamp(value, 5.0, 85.0);
        }

        /// <summary>
        /// Смещение по X
        /// </summary>
        public double OffsetX
        {
            get => _offsetX;
            set => _offsetX = value;
        }

        /// <summary>
        /// Смещение по Y
        /// </summary>
        public double OffsetY
        {
            get => _offsetY;
            set => _offsetY = value;
        }

        /// <summary>
        /// Количество видимых предыдущих слоёв
        /// </summary>
        public int MaxVisibleLayers
        {
            get => _maxVisibleLayers;
            set => _maxVisibleLayers = Math.Max(1, value);
        }

        /// <summary>
        /// Текущее изображение для отображения
        /// </summary>
        public WriteableBitmap Bitmap => _bitmap;

        #endregion

        #region Конструктор

        public IsometricLayerRenderer(int width = 800, int height = 600)
        {
            Resize(width, height);
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Изменяет размер рендер-буфера
        /// </summary>
        public void Resize(int width, int height)
        {
            _width = Math.Max(width, 100);
            _height = Math.Max(height, 100);
            _bitmap = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Bgra32, null);
            _zBuffer = new double[_width * _height];

            // Центрируем изображение
            _offsetX = _width / 2.0;
            _offsetY = _height / 2.0;
        }

        /// <summary>
        /// Загружает проект для визуализации
        /// </summary>
        public void LoadProject(Project project)
        {
            _currentProject = project;
            _currentLayerIndex = 0;

            // Автоматически подстраиваем масштаб под размер проекта
            if (project != null && project.Layers != null && project.Layers.Count > 0)
            {
                AutoFitScale();
            }
        }

        /// <summary>
        /// Устанавливает текущий слой для отображения (1-based)
        /// </summary>
        public void SetCurrentLayer(int layerNumber)
        {
            if (_currentProject == null || _currentProject.Layers == null)
                return;

            _currentLayerIndex = Math.Clamp(layerNumber - 1, 0, _currentProject.Layers.Count - 1);
        }

        /// <summary>
        /// Рендерит текущий кадр
        /// </summary>
        public void Render()
        {
            if (_bitmap == null)
                return;

            try
            {
                _bitmap.Lock();

                unsafe
                {
                    IntPtr backBuffer = _bitmap.BackBuffer;
                    int stride = _bitmap.BackBufferStride;

                    // Очищаем буфер
                    ClearBuffer(backBuffer, stride);

                    // Сбрасываем Z-buffer
                    Array.Fill(_zBuffer, double.MaxValue);

                    // Рисуем платформу
                    RenderPlatform(backBuffer, stride);

                    // Рисуем слои проекта
                    if (_currentProject != null && _currentProject.Layers != null)
                    {
                        RenderLayers(backBuffer, stride);
                    }
                }

                _bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
            }
            finally
            {
                _bitmap.Unlock();
            }
        }

        /// <summary>
        /// Поворачивает камеру
        /// </summary>
        public void Rotate(double deltaAzimuth, double deltaElevation)
        {
            RotationAngle += deltaAzimuth;
            ElevationAngle += deltaElevation;
        }

        /// <summary>
        /// Перемещает камеру (панорамирование)
        /// </summary>
        public void Pan(double deltaX, double deltaY)
        {
            _offsetX += deltaX;
            _offsetY += deltaY;
        }

        /// <summary>
        /// Изменяет масштаб
        /// </summary>
        public void Zoom(double delta)
        {
            Scale *= (1.0 + delta * 0.001);
        }

        /// <summary>
        /// Сбрасывает камеру в начальное положение
        /// </summary>
        public void ResetCamera()
        {
            _rotationAngle = 45.0;
            _elevationAngle = 30.0;
            _offsetX = _width / 2.0;
            _offsetY = _height / 2.0;
            AutoFitScale();
        }

        #endregion

        #region Приватные методы - Рендеринг

        private unsafe void ClearBuffer(IntPtr backBuffer, int stride)
        {
            byte* pixels = (byte*)backBuffer;
            int bytesPerPixel = 4; // BGRA

            for (int y = 0; y < _height; y++)
            {
                byte* row = pixels + y * stride;
                for (int x = 0; x < _width; x++)
                {
                    int offset = x * bytesPerPixel;
                    row[offset + 0] = BackgroundColor.B; // Blue
                    row[offset + 1] = BackgroundColor.G; // Green
                    row[offset + 2] = BackgroundColor.R; // Red
                    row[offset + 3] = 255;               // Alpha
                }
            }
        }

        private unsafe void RenderPlatform(IntPtr backBuffer, int stride)
        {
            // Рисуем платформу как прямоугольник
            var corners = new (float x, float y, float z)[]
            {
                (-HALF_FIELD, -HALF_FIELD, 0),
                (HALF_FIELD, -HALF_FIELD, 0),
                (HALF_FIELD, HALF_FIELD, 0),
                (-HALF_FIELD, HALF_FIELD, 0)
            };

            // Проецируем углы
            var projected = corners.Select(c => Project3DTo2D(c.x, c.y, c.z)).ToArray();

            // Рисуем заполненный полигон платформы
            FillPolygon(backBuffer, stride, projected, PlatformColor);

            // Рисуем сетку на платформе
            RenderGrid(backBuffer, stride);

            // Рисуем оси
            RenderAxes(backBuffer, stride);
        }

        private unsafe void RenderGrid(IntPtr backBuffer, int stride)
        {
            const float gridStep = 20f; // 20 мм между линиями

            // Вертикальные линии
            for (float x = -HALF_FIELD; x <= HALF_FIELD; x += gridStep)
            {
                var p1 = Project3DTo2D(x, -HALF_FIELD, 0);
                var p2 = Project3DTo2D(x, HALF_FIELD, 0);
                DrawLine(backBuffer, stride, p1.x, p1.y, p2.x, p2.y, GridColor);
            }

            // Горизонтальные линии
            for (float y = -HALF_FIELD; y <= HALF_FIELD; y += gridStep)
            {
                var p1 = Project3DTo2D(-HALF_FIELD, y, 0);
                var p2 = Project3DTo2D(HALF_FIELD, y, 0);
                DrawLine(backBuffer, stride, p1.x, p1.y, p2.x, p2.y, GridColor);
            }
        }

        private unsafe void RenderAxes(IntPtr backBuffer, int stride)
        {
            // Центральные оси (более яркие)
            var center = Project3DTo2D(0, 0, 0);

            // Ось X
            var xEnd = Project3DTo2D(HALF_FIELD, 0, 0);
            DrawLine(backBuffer, stride, center.x, center.y, xEnd.x, xEnd.y, AxisColor, 2);

            // Ось Y
            var yEnd = Project3DTo2D(0, HALF_FIELD, 0);
            DrawLine(backBuffer, stride, center.x, center.y, yEnd.x, yEnd.y, AxisColor, 2);
        }

        private unsafe void RenderLayers(IntPtr backBuffer, int stride)
        {
            if (_currentProject.Layers == null || _currentProject.Layers.Count == 0)
                return;

            // Вычисляем Z-позиции слоёв
            var layerZPositions = new List<double>();
            double currentZ = 0;
            foreach (var layer in _currentProject.Layers)
            {
                double height = layer.Height > 0 ? layer.Height : 0.05;
                currentZ += height;
                layerZPositions.Add(currentZ);
            }

            // Рендерим ВСЕ слои от 0 до текущего (показываем всю "стопку")
            // Для оптимизации: если слоёв много, рисуем каждый N-й слой для предыдущих
            int totalLayers = _currentLayerIndex + 1;
            int skipFactor = totalLayers > 100 ? totalLayers / 50 : 1; // Не более 50 слоёв для отрисовки

            for (int i = 0; i <= _currentLayerIndex && i < _currentProject.Layers.Count; i++)
            {
                bool isCurrentLayer = (i == _currentLayerIndex);

                // Для предыдущих слоёв пропускаем некоторые для оптимизации
                if (!isCurrentLayer && skipFactor > 1 && i % skipFactor != 0)
                    continue;

                var layer = _currentProject.Layers[i];
                float zPos = (float)layerZPositions[i];

                RenderSingleLayer(backBuffer, stride, layer, zPos, isCurrentLayer);
            }
        }

        private unsafe void RenderSingleLayer(IntPtr backBuffer, int stride, Layer layer, float zPosition, bool isCurrentLayer)
        {
            if (layer.Regions == null)
                return;

            foreach (var region in layer.Regions)
            {
                if (region.PolyLines == null || region.PolyLines.Count == 0)
                    continue;

                // Определяем цвет в зависимости от типа региона
                Color color = GetRegionColor(region, isCurrentLayer);

                // Рендерим полилинии
                foreach (var polyLine in region.PolyLines)
                {
                    if (polyLine.Points == null || polyLine.Points.Count < 2)
                        continue;

                    RenderPolyLine(backBuffer, stride, polyLine, zPosition, color, region.Type == BlockType.Hatch);
                }
            }
        }

        private unsafe void RenderPolyLine(IntPtr backBuffer, int stride, PolyLine polyLine, float z, Color color, bool isHatch)
        {
            var points = polyLine.Points;
            if (points.Count < 2)
                return;

            // Для штриховки рисуем каждую N-ю линию
            int step = isHatch ? 3 : 1;

            for (int i = 0; i < points.Count - 1; i += step)
            {
                var p1 = Project3DTo2D(points[i].X, points[i].Y, z);
                var p2 = Project3DTo2D(points[i + 1].X, points[i + 1].Y, z);

                // Проверяем Z-buffer для правильного порядка отрисовки
                double avgZ = (p1.z + p2.z) / 2.0;
                DrawLineWithZBuffer(backBuffer, stride, p1.x, p1.y, p1.z, p2.x, p2.y, p2.z, color);
            }

            // Замыкаем контур если это не штриховка
            if (!isHatch && points.Count > 2)
            {
                var pLast = Project3DTo2D(points[points.Count - 1].X, points[points.Count - 1].Y, z);
                var pFirst = Project3DTo2D(points[0].X, points[0].Y, z);
                DrawLineWithZBuffer(backBuffer, stride, pLast.x, pLast.y, pLast.z, pFirst.x, pFirst.y, pFirst.z, color);
            }
        }

        private Color GetRegionColor(CliRegion region, bool isCurrentLayer)
        {
            if (!isCurrentLayer)
            {
                // Предыдущие слои - серые
                return PreviousLayersColor;
            }

            // Текущий слой - яркие цвета
            return region.GeometryRegion switch
            {
                GeometryRegion.Contour => ContourColor,
                GeometryRegion.ContourUpskin => Color.FromRgb(255, 150, 50),
                GeometryRegion.ContourDownskin => Color.FromRgb(255, 120, 40),
                GeometryRegion.Infill => HatchColor,
                GeometryRegion.Upskin => Color.FromRgb(220, 100, 30),
                GeometryRegion.Downskin => Color.FromRgb(180, 80, 20),
                _ => Color.FromRgb(150, 150, 150)
            };
        }

        #endregion

        #region Приватные методы - Проекция

        /// <summary>
        /// Проецирует 3D точку в 2D экранные координаты с изометрической проекцией
        /// </summary>
        private (int x, int y, double z) Project3DTo2D(float x3d, float y3d, float z3d)
        {
            // Поворот вокруг оси Z
            double radAngle = _rotationAngle * Math.PI / 180.0;
            double cosR = Math.Cos(radAngle);
            double sinR = Math.Sin(radAngle);

            double rotX = x3d * cosR - y3d * sinR;
            double rotY = x3d * sinR + y3d * cosR;
            double rotZ = z3d;

            // Изометрическая проекция с наклоном камеры
            double radElevation = _elevationAngle * Math.PI / 180.0;
            double cosE = Math.Cos(radElevation);
            double sinE = Math.Sin(radElevation);

            // Проецируем на экран
            double screenX = rotX * CosX + rotY * SinX;
            double screenY = rotZ * cosE - rotY * sinE * CosY + rotX * sinE * SinY;

            // Применяем масштаб и смещение
            int px = (int)(_offsetX + screenX * _scale);
            int py = (int)(_offsetY - screenY * _scale); // Инвертируем Y для экранных координат

            // Глубина для Z-buffer:
            // Верхние слои (большой z3d) должны быть БЛИЖЕ к камере (меньшая глубина)
            // rotY влияет на глубину в зависимости от угла обзора
            // Используем отрицательный z3d чтобы верхние слои были ближе
            double depth = -z3d * 1000.0 + rotY * sinE - rotX * cosE * 0.1;

            return (px, py, depth);
        }

        #endregion

        #region Приватные методы - Рисование примитивов

        private unsafe void DrawLine(IntPtr backBuffer, int stride, int x1, int y1, int x2, int y2, Color color, int thickness = 1)
        {
            // Алгоритм Брезенхема
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            byte* pixels = (byte*)backBuffer;

            while (true)
            {
                // Рисуем пиксель с учётом толщины
                for (int tx = -thickness / 2; tx <= thickness / 2; tx++)
                {
                    for (int ty = -thickness / 2; ty <= thickness / 2; ty++)
                    {
                        SetPixel(pixels, stride, x1 + tx, y1 + ty, color);
                    }
                }

                if (x1 == x2 && y1 == y2)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }

        private unsafe void DrawLineWithZBuffer(IntPtr backBuffer, int stride,
            int x1, int y1, double z1, int x2, int y2, double z2, Color color)
        {
            // Алгоритм Брезенхема с Z-buffer
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            int steps = Math.Max(dx, dy);
            if (steps == 0) steps = 1;
            double dz = (z2 - z1) / steps;
            double z = z1;

            byte* pixels = (byte*)backBuffer;
            int stepCount = 0;

            while (true)
            {
                // Проверяем Z-buffer
                if (x1 >= 0 && x1 < _width && y1 >= 0 && y1 < _height)
                {
                    int idx = y1 * _width + x1;
                    if (z < _zBuffer[idx])
                    {
                        _zBuffer[idx] = z;
                        SetPixel(pixels, stride, x1, y1, color);
                    }
                }

                if (x1 == x2 && y1 == y2)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }

                z += dz;
                stepCount++;
            }
        }

        private unsafe void SetPixel(byte* pixels, int stride, int x, int y, Color color)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
                return;

            byte* pixel = pixels + y * stride + x * 4;
            pixel[0] = color.B;
            pixel[1] = color.G;
            pixel[2] = color.R;
            pixel[3] = 255;
        }

        private unsafe void FillPolygon(IntPtr backBuffer, int stride, (int x, int y, double z)[] vertices, Color color)
        {
            if (vertices.Length < 3)
                return;

            // Находим границы полигона
            int minY = vertices.Min(v => v.y);
            int maxY = vertices.Max(v => v.y);
            minY = Math.Max(0, minY);
            maxY = Math.Min(_height - 1, maxY);

            byte* pixels = (byte*)backBuffer;

            // Сканлайн алгоритм заполнения полигона
            for (int y = minY; y <= maxY; y++)
            {
                var intersections = new List<(int x, double z)>();

                // Находим пересечения со всеми рёбрами
                for (int i = 0; i < vertices.Length; i++)
                {
                    int j = (i + 1) % vertices.Length;
                    var v1 = vertices[i];
                    var v2 = vertices[j];

                    if ((v1.y <= y && v2.y > y) || (v2.y <= y && v1.y > y))
                    {
                        // Линейная интерполяция X
                        double t = (y - v1.y) / (double)(v2.y - v1.y);
                        int x = (int)(v1.x + t * (v2.x - v1.x));
                        double z = v1.z + t * (v2.z - v1.z);
                        intersections.Add((x, z));
                    }
                }

                // Сортируем пересечения по X
                intersections.Sort((a, b) => a.x.CompareTo(b.x));

                // Заполняем пары пересечений
                for (int i = 0; i < intersections.Count - 1; i += 2)
                {
                    int x1 = Math.Max(0, intersections[i].x);
                    int x2 = Math.Min(_width - 1, intersections[i + 1].x);
                    double z1 = intersections[i].z;
                    double z2 = intersections[i + 1].z;

                    if (x2 < x1) continue;

                    double dz = (x2 > x1) ? (z2 - z1) / (x2 - x1) : 0;
                    double z = z1;

                    for (int x = x1; x <= x2; x++)
                    {
                        int idx = y * _width + x;
                        if (z < _zBuffer[idx])
                        {
                            _zBuffer[idx] = z;
                            SetPixel(pixels, stride, x, y, color);
                        }
                        z += dz;
                    }
                }
            }
        }

        #endregion

        #region Приватные методы - Утилиты

        private void AutoFitScale()
        {
            if (_currentProject == null || _currentProject.Layers == null)
                return;

            // Вычисляем bounding box проекта
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float maxZ = 0;

            foreach (var layer in _currentProject.Layers)
            {
                if (layer.Regions == null) continue;

                double height = layer.Height > 0 ? layer.Height : 0.05;
                maxZ += (float)height;

                foreach (var region in layer.Regions)
                {
                    if (region.PolyLines == null) continue;

                    foreach (var polyLine in region.PolyLines)
                    {
                        if (polyLine.Points == null) continue;

                        foreach (var point in polyLine.Points)
                        {
                            if (point.X < minX) minX = point.X;
                            if (point.X > maxX) maxX = point.X;
                            if (point.Y < minY) minY = point.Y;
                            if (point.Y > maxY) maxY = point.Y;
                        }
                    }
                }
            }

            // Вычисляем масштаб чтобы проект помещался на экране
            float projectWidth = maxX - minX;
            float projectHeight = maxY - minY;
            float projectSize = Math.Max(projectWidth, projectHeight);

            if (projectSize > 0)
            {
                double targetSize = Math.Min(_width, _height) * 0.7;
                _scale = targetSize / projectSize;
            }
        }

        #endregion
    }
}
