using System;
using System.Collections.Generic;
using SkiaSharp;
using ProjectParserTest.Parsers.Shared.Models;
using ProjectParserTest.Parsers.Shared.Enums;
using PrintMate.Terminal.Parsers.Shared.Models;
using CliRegion = ProjectParserTest.Parsers.Shared.Models.Region;

namespace PrintMate.Terminal.Controls
{
    /// <summary>
    /// SkiaSharp рендерер с GPU ускорением для визуализации CLI проектов.
    /// Оптимизирован для работы на Intel UHD графике.
    /// </summary>
    public class SkiaLayerRenderer
    {
        #region Константы

        private const float FIELD_SIZE = 320f;
        private const float HALF_FIELD = FIELD_SIZE / 2f;

        // Максимальное количество отрисовываемых слоёв для производительности
        private const int MAX_VISIBLE_LAYERS = 20;

        // Цвета
        private static readonly SKColor BackgroundColor = new SKColor(30, 30, 30);
        private static readonly SKColor PlatformColor = new SKColor(50, 50, 50);
        private static readonly SKColor GridColor = new SKColor(60, 60, 60);
        private static readonly SKColor AxisColor = new SKColor(90, 90, 90);
        private static readonly SKColor ContourColor = new SKColor(255, 100, 30);
        private static readonly SKColor HatchColor = new SKColor(200, 80, 20);
        private static readonly SKColor PreviousLayersColor = new SKColor(100, 100, 100);

        #endregion

        #region Приватные поля

        private Project _currentProject;
        private int _currentLayerIndex;

        // Камера
        private float _scale = 1.5f;
        private float _offsetX;
        private float _offsetY;
        private float _rotationAngle = 45f;
        private float _elevationAngle = 30f;

        // Предварительно вычисленные sin/cos для проекции
        private float _cosR, _sinR, _cosE, _sinE, _cosIso, _sinIso;
        private bool _trigNeedsUpdate = true;

        // Кеш скомпилированных путей слоёв (в локальных координатах)
        private Dictionary<int, SKPath> _layerContourCache = new Dictionary<int, SKPath>();
        private List<float> _layerZPositions;

        // Paint объекты (переиспользуем для производительности)
        private SKPaint _linePaint;
        private SKPaint _fillPaint;
        private SKPaint _gridPaint;
        private bool _paintsInitialized = false;

        #endregion

        #region Публичные свойства

        public float Scale
        {
            get => _scale;
            set => _scale = Math.Clamp(value, 0.1f, 20f);
        }

        public float RotationAngle
        {
            get => _rotationAngle;
            set
            {
                _rotationAngle = value % 360f;
                _trigNeedsUpdate = true;
            }
        }

        public float ElevationAngle
        {
            get => _elevationAngle;
            set
            {
                _elevationAngle = Math.Clamp(value, 5f, 85f);
                _trigNeedsUpdate = true;
            }
        }

        public float OffsetX
        {
            get => _offsetX;
            set => _offsetX = value;
        }

        public float OffsetY
        {
            get => _offsetY;
            set => _offsetY = value;
        }

        #endregion

        #region Конструктор

        public SkiaLayerRenderer()
        {
            // Paints будут инициализированы лениво при первом рендере
        }

        private void EnsurePaintsInitialized()
        {
            if (_paintsInitialized) return;

            try
            {
                _linePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1f,
                    IsAntialias = true,
                    StrokeCap = SKStrokeCap.Round
                };

                _fillPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                _gridPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 0.5f,
                    IsAntialias = false,
                    Color = GridColor
                };

                _paintsInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SkiaRenderer] Failed to initialize paints: {ex.Message}");
            }
        }

        #endregion

        #region Публичные методы

        public void LoadProject(Project project)
        {
            _currentProject = project;
            _currentLayerIndex = 0;
            InvalidateCache();

            if (project != null && project.Layers != null && project.Layers.Count > 0)
            {
                // Предварительно вычисляем Z-позиции всех слоёв
                _layerZPositions = new List<float>(project.Layers.Count);
                float currentZ = 0;
                foreach (var layer in project.Layers)
                {
                    float height = layer.Height > 0 ? (float)layer.Height : 0.05f;
                    currentZ += height;
                    _layerZPositions.Add(currentZ);
                }

                AutoFitScale();
            }
        }

        public void SetCurrentLayer(int layerNumber)
        {
            if (_currentProject == null || _currentProject.Layers == null)
                return;

            int newIndex = Math.Clamp(layerNumber - 1, 0, _currentProject.Layers.Count - 1);
            if (newIndex != _currentLayerIndex)
            {
                _currentLayerIndex = newIndex;
            }
        }

        public void Render(SKCanvas canvas, int width, int height)
        {
            if (canvas == null) return;

            EnsurePaintsInitialized();

            if (!_paintsInitialized)
            {
                canvas.Clear(BackgroundColor);
                return;
            }

            // Устанавливаем центр
            if (_offsetX == 0 && _offsetY == 0)
            {
                _offsetX = width / 2f;
                _offsetY = height / 2f;
            }

            // Обновляем тригонометрию если нужно
            if (_trigNeedsUpdate)
            {
                UpdateTrigCache();
                _trigNeedsUpdate = false;
            }

            // Очищаем фон
            canvas.Clear(BackgroundColor);

            // Сохраняем состояние
            canvas.Save();

            // Применяем трансформации камеры
            canvas.Translate(_offsetX, _offsetY);
            canvas.Scale(_scale, _scale);

            // Рисуем платформу
            RenderPlatform(canvas);

            // Рисуем слои
            if (_currentProject != null && _currentProject.Layers != null)
            {
                RenderLayersOptimized(canvas);
            }

            canvas.Restore();
        }

        public void Rotate(float deltaAzimuth, float deltaElevation)
        {
            RotationAngle += deltaAzimuth;
            ElevationAngle += deltaElevation;
        }

        public void Pan(float deltaX, float deltaY)
        {
            _offsetX += deltaX;
            _offsetY += deltaY;
        }

        public void Zoom(float delta)
        {
            Scale *= (1f + delta * 0.001f);
        }

        public void ResetCamera(int width, int height)
        {
            _rotationAngle = 45f;
            _elevationAngle = 30f;
            _offsetX = width / 2f;
            _offsetY = height / 2f;
            _trigNeedsUpdate = true;
            AutoFitScale();
        }

        public void InvalidateCache()
        {
            foreach (var path in _layerContourCache.Values)
            {
                path?.Dispose();
            }
            _layerContourCache.Clear();
        }

        public void Dispose()
        {
            _paintsInitialized = false;
            _linePaint?.Dispose();
            _linePaint = null;
            _fillPaint?.Dispose();
            _fillPaint = null;
            _gridPaint?.Dispose();
            _gridPaint = null;
            InvalidateCache();
        }

        #endregion

        #region Приватные методы - Рендеринг

        private void UpdateTrigCache()
        {
            float radAngle = _rotationAngle * MathF.PI / 180f;
            _cosR = MathF.Cos(radAngle);
            _sinR = MathF.Sin(radAngle);

            float radElevation = _elevationAngle * MathF.PI / 180f;
            _cosE = MathF.Cos(radElevation);
            _sinE = MathF.Sin(radElevation);

            float isoAngle = 30f * MathF.PI / 180f;
            _cosIso = MathF.Cos(isoAngle);
            _sinIso = MathF.Sin(isoAngle);
        }

        private void RenderPlatform(SKCanvas canvas)
        {
            if (_fillPaint == null || _gridPaint == null || _linePaint == null)
                return;

            var corners = new SKPoint[]
            {
                ProjectToScreenFast(-HALF_FIELD, -HALF_FIELD, 0),
                ProjectToScreenFast(HALF_FIELD, -HALF_FIELD, 0),
                ProjectToScreenFast(HALF_FIELD, HALF_FIELD, 0),
                ProjectToScreenFast(-HALF_FIELD, HALF_FIELD, 0)
            };

            using (var path = new SKPath())
            {
                path.MoveTo(corners[0]);
                path.LineTo(corners[1]);
                path.LineTo(corners[2]);
                path.LineTo(corners[3]);
                path.Close();

                _fillPaint.Color = PlatformColor;
                canvas.DrawPath(path, _fillPaint);
            }

            RenderGridSimple(canvas);
            RenderAxes(canvas);
        }

        private void RenderGridSimple(SKCanvas canvas)
        {
            // Упрощённая сетка - только границы и центральные линии
            var p1 = ProjectToScreenFast(-HALF_FIELD, 0, 0);
            var p2 = ProjectToScreenFast(HALF_FIELD, 0, 0);
            canvas.DrawLine(p1, p2, _gridPaint);

            p1 = ProjectToScreenFast(0, -HALF_FIELD, 0);
            p2 = ProjectToScreenFast(0, HALF_FIELD, 0);
            canvas.DrawLine(p1, p2, _gridPaint);
        }

        private void RenderAxes(SKCanvas canvas)
        {
            var center = ProjectToScreenFast(0, 0, 0);
            var xEnd = ProjectToScreenFast(HALF_FIELD * 0.8f, 0, 0);
            var yEnd = ProjectToScreenFast(0, HALF_FIELD * 0.8f, 0);

            _linePaint.Color = AxisColor;
            _linePaint.StrokeWidth = 2f;
            canvas.DrawLine(center, xEnd, _linePaint);
            canvas.DrawLine(center, yEnd, _linePaint);
            _linePaint.StrokeWidth = 1f;
        }

        private void RenderLayersOptimized(SKCanvas canvas)
        {
            if (_currentProject.Layers == null || _currentProject.Layers.Count == 0)
                return;

            // Рисуем ТОЛЬКО текущий слой для максимальной производительности
            // История слоёв отключена для Intel UHD
            if (_currentLayerIndex < _currentProject.Layers.Count)
            {
                float zPos = _layerZPositions[_currentLayerIndex];
                RenderCurrentLayerFull(canvas, _currentProject.Layers[_currentLayerIndex], zPos);
            }
        }

        private void RenderLayerContoursOnly(SKCanvas canvas, Layer layer, float zPosition)
        {
            if (layer.Regions == null) return;

            foreach (var region in layer.Regions)
            {
                // Только контуры для предыдущих слоёв
                if (region.Type == BlockType.Hatch) continue;
                if (region.PolyLines == null || region.PolyLines.Count == 0) continue;

                foreach (var polyLine in region.PolyLines)
                {
                    RenderPolyLineSimple(canvas, polyLine, zPosition);
                }
            }
        }

        private void RenderCurrentLayerFull(SKCanvas canvas, Layer layer, float zPosition)
        {
            if (layer.Regions == null) return;

            foreach (var region in layer.Regions)
            {
                if (region.PolyLines == null || region.PolyLines.Count == 0) continue;

                SKColor color = GetRegionColor(region);
                _linePaint.Color = color;

                bool isHatch = region.Type == BlockType.Hatch;

                // Для штриховки - сильное прореживание
                // Для контуров - небольшое прореживание
                int step = isHatch ? 5 : 2;
                _linePaint.StrokeWidth = isHatch ? 0.8f : 1.5f;

                foreach (var polyLine in region.PolyLines)
                {
                    RenderPolyLineWithStep(canvas, polyLine, zPosition, step, !isHatch);
                }
            }
        }

        private void RenderPolyLineSimple(SKCanvas canvas, PolyLine polyLine, float z)
        {
            var points = polyLine.Points;
            if (points == null || points.Count < 2) return;

            // Упрощённый рендеринг - каждую 4-ю точку
            int step = Math.Max(1, points.Count / 20);

            var prev = ProjectToScreenFast(points[0].X, points[0].Y, z);
            for (int i = step; i < points.Count; i += step)
            {
                var curr = ProjectToScreenFast(points[i].X, points[i].Y, z);
                canvas.DrawLine(prev, curr, _linePaint);
                prev = curr;
            }

            // Замыкаем
            if (points.Count > 2)
            {
                var first = ProjectToScreenFast(points[0].X, points[0].Y, z);
                canvas.DrawLine(prev, first, _linePaint);
            }
        }

        private void RenderPolyLineWithStep(SKCanvas canvas, PolyLine polyLine, float z, int step, bool close)
        {
            var points = polyLine.Points;
            if (points == null || points.Count < 2) return;

            using (var path = new SKPath())
            {
                var firstPoint = ProjectToScreenFast(points[0].X, points[0].Y, z);
                path.MoveTo(firstPoint);

                for (int i = step; i < points.Count; i += step)
                {
                    var p = ProjectToScreenFast(points[i].X, points[i].Y, z);
                    path.LineTo(p);
                }

                // Добавляем последнюю точку если пропустили
                if ((points.Count - 1) % step != 0)
                {
                    var last = ProjectToScreenFast(points[points.Count - 1].X, points[points.Count - 1].Y, z);
                    path.LineTo(last);
                }

                if (close && points.Count > 2)
                {
                    path.Close();
                }

                canvas.DrawPath(path, _linePaint);
            }
        }

        private SKColor GetRegionColor(CliRegion region)
        {
            return region.GeometryRegion switch
            {
                GeometryRegion.Contour => ContourColor,
                GeometryRegion.ContourUpskin => new SKColor(255, 150, 50),
                GeometryRegion.ContourDownskin => new SKColor(255, 120, 40),
                GeometryRegion.Infill => HatchColor,
                GeometryRegion.Upskin => new SKColor(220, 100, 30),
                GeometryRegion.Downskin => new SKColor(180, 80, 20),
                _ => new SKColor(150, 150, 150)
            };
        }

        #endregion

        #region Приватные методы - Проекция

        private SKPoint ProjectToScreenFast(float x3d, float y3d, float z3d)
        {
            // Поворот вокруг Z (используем кешированные значения)
            float rotX = x3d * _cosR - y3d * _sinR;
            float rotY = x3d * _sinR + y3d * _cosR;

            // Изометрическая проекция
            float screenX = rotX * _cosIso + rotY * _sinIso;
            float screenY = z3d * _cosE - (rotY * _cosIso - rotX * _sinIso) * _sinE;

            return new SKPoint(screenX, -screenY);
        }

        private void AutoFitScale()
        {
            if (_currentProject == null || _currentProject.Layers == null)
                return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            // Проверяем только первые несколько слоёв для скорости
            int layersToCheck = Math.Min(10, _currentProject.Layers.Count);

            for (int l = 0; l < layersToCheck; l++)
            {
                var layer = _currentProject.Layers[l];
                if (layer.Regions == null) continue;

                foreach (var region in layer.Regions)
                {
                    if (region.PolyLines == null) continue;

                    foreach (var polyLine in region.PolyLines)
                    {
                        if (polyLine.Points == null) continue;

                        // Проверяем только каждую 10-ю точку
                        for (int i = 0; i < polyLine.Points.Count; i += 10)
                        {
                            var point = polyLine.Points[i];
                            if (point.X < minX) minX = point.X;
                            if (point.X > maxX) maxX = point.X;
                            if (point.Y < minY) minY = point.Y;
                            if (point.Y > maxY) maxY = point.Y;
                        }
                    }
                }
            }

            float projectSize = MathF.Max(maxX - minX, maxY - minY);
            if (projectSize > 0)
            {
                _scale = 400f / projectSize;
            }
        }

        #endregion
    }
}
