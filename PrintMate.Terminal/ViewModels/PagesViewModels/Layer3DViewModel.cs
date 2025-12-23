using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Events;
using Prism.Mvvm;
using ProjectParserTest.Parsers.Shared.Models;
using ProjectParserTest.Parsers.Shared.Enums;
using System.Windows.Media;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using Media3D = System.Windows.Media.Media3D;
using HxMeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;

namespace PrintMate.Terminal.ViewModels.PagesViewModels
{
    /// <summary>
    /// ViewModel для 3D визуализации процесса печати
    /// </summary>
    public class Layer3DViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;

        #region Приватные поля

        private Media3D.Camera _camera;
        private IEffectsManager _effectsManager;
        private HxMeshGeometry3D _printedLayersGeometry;
        private object _currentLayerContoursGeometry;
        private object _currentLayerHatchesGeometry;
        private PhongMaterial _printedLayersMaterial;
        private Media3D.Transform3D _modelTransform;
        private Media3D.Transform3D _platformTransform;
        private double _rotationAngle;
        private double _cameraDistance;
        private HxMeshGeometry3D _platformGeometry;
        private int _currentLayerIndex;
        private int _totalLayers;
        private bool _isLoading;
        private string _loadingMessage;
        private System.Windows.Media.Color _contourColor;
        private System.Windows.Media.Color _hatchColor;

        private List<Layer> _allLayers;
        private Media3D.Point3D _modelCenter = new Media3D.Point3D(0, 0, 0);

        #endregion

        #region Публичные свойства

        public Media3D.Camera Camera
        {
            get => _camera;
            set => SetProperty(ref _camera, value);
        }

        public IEffectsManager EffectsManager
        {
            get => _effectsManager;
            set => SetProperty(ref _effectsManager, value);
        }

        public HxMeshGeometry3D PrintedLayersGeometry
        {
            get => _printedLayersGeometry;
            set => SetProperty(ref _printedLayersGeometry, value);
        }

        public object CurrentLayerContoursGeometry
        {
            get => _currentLayerContoursGeometry;
            set => SetProperty(ref _currentLayerContoursGeometry, value);
        }

        public object CurrentLayerHatchesGeometry
        {
            get => _currentLayerHatchesGeometry;
            set => SetProperty(ref _currentLayerHatchesGeometry, value);
        }

        public PhongMaterial PrintedLayersMaterial
        {
            get => _printedLayersMaterial;
            set => SetProperty(ref _printedLayersMaterial, value);
        }

        public Media3D.Transform3D ModelTransform
        {
            get => _modelTransform;
            set => SetProperty(ref _modelTransform, value);
        }

        public Media3D.Transform3D PlatformTransform
        {
            get => _platformTransform;
            set => SetProperty(ref _platformTransform, value);
        }

        public HxMeshGeometry3D PlatformGeometry
        {
            get => _platformGeometry;
            set => SetProperty(ref _platformGeometry, value);
        }

        public double RotationAngle
        {
            get => _rotationAngle;
            set
            {
                if (SetProperty(ref _rotationAngle, value))
                {
                    UpdateModelTransform();
                }
            }
        }

        public double CameraDistance
        {
            get => _cameraDistance;
            set
            {
                // Камера зафиксирована, не обновляем позицию
                SetProperty(ref _cameraDistance, value);
            }
        }

        public int CurrentLayerIndex
        {
            get => _currentLayerIndex;
            set => SetProperty(ref _currentLayerIndex, value);
        }

        public int TotalLayers
        {
            get => _totalLayers;
            set => SetProperty(ref _totalLayers, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            set => SetProperty(ref _loadingMessage, value);
        }

        public System.Windows.Media.Color ContourColor
        {
            get => _contourColor;
            set => SetProperty(ref _contourColor, value);
        }

        public System.Windows.Media.Color HatchColor
        {
            get => _hatchColor;
            set => SetProperty(ref _hatchColor, value);
        }

        #endregion

        #region Конструктор

        public Layer3DViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _allLayers = new List<Layer>();

            InitializeScene();
        }

        #endregion

        #region Инициализация

        private void InitializeScene()
        {
            // Инициализируем EffectsManager для SharpDX
            EffectsManager = new DefaultEffectsManager();

            // Начальное расстояние камеры (для возможного использования)
            _cameraDistance = 600.0;

            // Создаем камеру с фиксированной позицией
            // Камера располагается сверху-сбоку для хорошего обзора платформы
            Camera = new Media3D.PerspectiveCamera
            {
                Position = new Media3D.Point3D(300, 300, 500),  // Фиксированная позиция сверху-сбоку
                LookDirection = new Media3D.Vector3D(-300, -300, -500),  // Смотрит на центр (0,0,0)
                UpDirection = new Media3D.Vector3D(0, 0, 1),  // Верх всегда по оси Z
                FieldOfView = 45
            };

            // Материал для напечатанных слоев (SharpDX PhongMaterial)
            PrintedLayersMaterial = new PhongMaterial
            {
                DiffuseColor = new Color4(0.7f, 0.7f, 0.7f, 0.9f),
                SpecularColor = new Color4(0.2f, 0.2f, 0.2f, 1.0f),
                SpecularShininess = 30f
            };

            ContourColor = System.Windows.Media.Colors.OrangeRed;
            HatchColor = System.Windows.Media.Color.FromRgb(255, 165, 0);

            ModelTransform = Media3D.Transform3D.Identity;
            PlatformTransform = Media3D.Transform3D.Identity;
            RotationAngle = 0;

            PrintedLayersGeometry = new HxMeshGeometry3D();
            CurrentLayerContoursGeometry = null;
            CurrentLayerHatchesGeometry = null;

            // Создаем геометрию платформы 320x320 мм
            CreatePlatformGeometry();

            // Камера уже настроена выше и остается фиксированной
            // UpdateCameraPosition() больше не вызываем

            IsLoading = false;
            CurrentLayerIndex = 0;
            TotalLayers = 0;
        }

        #endregion

        #region Публичные методы

        public void LoadProject(Project project)
        {
            if (project == null || project.Layers == null || !project.Layers.Any())
                return;

            _allLayers = project.Layers.ToList();
            TotalLayers = _allLayers.Count;
            CurrentLayerIndex = 0;

            // Вычисляем центр модели для правильного вращения
            CalculateModelCenter();

            PrintedLayersGeometry = new HxMeshGeometry3D();
            CurrentLayerContoursGeometry = null;
            CurrentLayerHatchesGeometry = null;
        }

        public void OnLayerStarted(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= _allLayers.Count)
                return;

            CurrentLayerIndex = layerIndex + 1;
            // TODO: UpdateCurrentLayerGeometry(_allLayers[layerIndex]);
        }

        public void OnLayerCompleted(int layerIndex)
        {
            CurrentLayerContoursGeometry = null;
            CurrentLayerHatchesGeometry = null;
            // TODO: Add layer to printed layers mesh
        }

        #endregion

        #region Трансформация

        private void UpdateModelTransform()
        {
            // Вращаем только вокруг оси Y (не Z!)
            // Это предотвратит переворачивание
            var rotation = new Media3D.RotateTransform3D(
                new Media3D.AxisAngleRotation3D(
                    new Media3D.Vector3D(0, 1, 0),  // Ось Y
                    RotationAngle
                )
            );

            ModelTransform = rotation;
            PlatformTransform = rotation;
        }

        private void UpdateCameraPosition()
        {
            // Камера располагается под углом 45° относительно осей X и Y
            // и под углом примерно 37° относительно плоскости XY (чтобы смотреть сверху-сбоку)
            double angleXY = -45.0 * Math.PI / 180.0; // -45° в радианах
            double angleZ = 37.0 * Math.PI / 180.0;   // угол наклона от горизонтали

            // Вычисляем позицию камеры на расстоянии CameraDistance от центра
            double x = CameraDistance * Math.Cos(angleXY) * Math.Cos(angleZ);
            double y = CameraDistance * Math.Sin(angleXY) * Math.Cos(angleZ);
            double z = CameraDistance * Math.Sin(angleZ);

            var camera = Camera as Media3D.PerspectiveCamera;
            if (camera != null)
            {
                camera.Position = new Media3D.Point3D(x, y, z);
                // Камера всегда смотрит на центр (0,0,0)
                camera.LookDirection = new Media3D.Vector3D(-x, -y, -z);
                // UpDirection всегда направлен вдоль оси Z (вверх)
                // Это гарантирует, что камера не будет переворачиваться
                camera.UpDirection = new Media3D.Vector3D(0, 0, 1);
            }
        }

        #endregion

        #region Создание геометрии платформы

        /// <summary>
        /// Создает геометрию платформы 320x320 мм
        /// </summary>
        private void CreatePlatformGeometry()
        {
            const double platformSize = 320.0; // мм
            const double platformThickness = 2.0; // мм толщина платформы

            var meshBuilder = new MeshBuilder();
            double halfSize = platformSize / 2;

            // Создаем простой бокс для платформы
            meshBuilder.AddBox(
                new Vector3(0, (float)(platformThickness / 2), 0),
                (float)platformSize,
                (float)platformThickness,
                (float)platformSize
            );

            PlatformGeometry = meshBuilder.ToMesh();
            Console.WriteLine($"[3D] Создана платформа {platformSize}x{platformSize} мм");
        }

        #endregion

        #region Построение 3D геометрии из слоев

        /// <summary>
        /// Вычисляет центр модели для правильного вращения
        /// </summary>
        private void CalculateModelCenter()
        {
            if (_allLayers == null || !_allLayers.Any())
            {
                _modelCenter = new Media3D.Point3D(0, 0, 0);
                return;
            }

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            double totalHeight = 0;

            foreach (var layer in _allLayers)
            {
                totalHeight += layer.Height;

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

            // Если высоты слоев не указаны, вычисляем общую высоту с учетом фиксированной высоты
            if (totalHeight == 0)
            {
                totalHeight = _allLayers.Count * 0.05; // 0.05 мм на слой по умолчанию
            }

            _modelCenter = new Media3D.Point3D(
                (minX + maxX) / 2,
                (minY + maxY) / 2,
                totalHeight / 2
            );

            Console.WriteLine($"[3D] Модель: размер X={maxX-minX:F2}мм, Y={maxY-minY:F2}мм, Z={totalHeight:F2}мм, центр=({_modelCenter.X:F2}, {_modelCenter.Y:F2}, {_modelCenter.Z:F2})");
        }

        /// <summary>
        /// Строит 3D mesh для указанного количества слоев (от 0 до layerCount)
        /// </summary>
        public void BuildLayersGeometry(int layerCount)
        {
            if (_allLayers == null || !_allLayers.Any() || layerCount <= 0)
            {
                PrintedLayersGeometry = new HxMeshGeometry3D();
                Console.WriteLine($"[3D] BuildLayersGeometry: Нет данных или layerCount={layerCount}");
                return;
            }

            var meshBuilder = new MeshBuilder();
            const double platformThickness = 2.0; // Толщина платформы
            double currentZ = platformThickness; // Начинаем сразу над платформой

            // Берем только слои от 0 до layerCount
            int count = Math.Min(layerCount, _allLayers.Count);

            // Собираем все контуры слоёв с их высотами
            var layerContours = new List<(float z, List<List<ProjectParserTest.Parsers.Shared.Models.Point>>)>();

            for (int i = 0; i < count; i++)
            {
                var layer = _allLayers[i];
                double layerHeight = layer.Height > 0 ? layer.Height : 0.05;

                if (layer.Regions == null)
                {
                    currentZ += layerHeight;
                    continue;
                }

                float zTop = (float)(currentZ + layerHeight);

                // Собираем все контуры текущего слоя
                var contoursAtThisZ = new List<List<ProjectParserTest.Parsers.Shared.Models.Point>>();

                foreach (var region in layer.Regions)
                {
                    if (region.PolyLines == null)
                        continue;

                    if (region.GeometryRegion == GeometryRegion.Contour)
                    {
                        foreach (var polyline in region.PolyLines)
                        {
                            if (polyline.Points != null && polyline.Points.Count >= 3)
                            {
                                contoursAtThisZ.Add(polyline.Points);
                            }
                        }
                    }
                }

                if (contoursAtThisZ.Count > 0)
                {
                    layerContours.Add((zTop, contoursAtThisZ));
                }

                currentZ += layerHeight;
            }

            // Собираем штриховку для верхнего слоя
            List<List<ProjectParserTest.Parsers.Shared.Models.Point>> topLayerHatches = null;
            float topLayerZ = 0;

            if (count > 0 && _allLayers[count - 1].Regions != null)
            {
                topLayerHatches = new List<List<ProjectParserTest.Parsers.Shared.Models.Point>>();

                // Вычисляем Z координату верхнего слоя
                double tempZ = platformThickness;
                for (int i = 0; i < count; i++)
                {
                    double layerHeight = _allLayers[i].Height > 0 ? _allLayers[i].Height : 0.05;
                    if (i == count - 1)
                    {
                        topLayerZ = (float)(tempZ + layerHeight);
                    }
                    tempZ += layerHeight;
                }

                // Собираем штриховку верхнего слоя
                foreach (var region in _allLayers[count - 1].Regions)
                {
                    if (region.PolyLines == null)
                        continue;

                    // Рисуем все типы заполнения для верхнего слоя
                    if (region.GeometryRegion == GeometryRegion.Infill ||
                        region.GeometryRegion == GeometryRegion.Upskin ||
                        region.GeometryRegion == GeometryRegion.Downskin ||
                        region.Type == BlockType.Hatch)
                    {
                        foreach (var polyline in region.PolyLines)
                        {
                            if (polyline.Points != null && polyline.Points.Count >= 2)
                            {
                                topLayerHatches.Add(polyline.Points);
                            }
                        }
                    }
                }
            }

            // Теперь создаём геометрию: вертикальные стенки между слоями + верх и низ
            if (layerContours.Count > 0)
            {
                // Нижняя поверхность (первый слой)
                float zBottom = (float)platformThickness;
                foreach (var contour in layerContours[0].Item2)
                {
                    AddFilledPolygon(meshBuilder, contour, zBottom);
                }

                // Вертикальные стенки между всеми слоями + верхняя поверхность каждого слоя
                for (int i = 0; i < layerContours.Count; i++)
                {
                    float zCurrent = i == 0 ? (float)platformThickness : layerContours[i - 1].z;
                    float zNext = layerContours[i].z;

                    foreach (var contour in layerContours[i].Item2)
                    {
                        // Вертикальные стенки
                        AddVerticalWalls(meshBuilder, contour, zCurrent, zNext);

                        // Верхняя поверхность текущего слоя
                        AddFilledPolygon(meshBuilder, contour, zNext);
                    }
                }
            }

            // Добавляем штриховку верхнего слоя (как тонкие линии)
            if (topLayerHatches != null && topLayerHatches.Count > 0)
            {
                const float hatchDiameter = 0.15f; // Немного толще чем контур

                foreach (var hatchLine in topLayerHatches)
                {
                    if (hatchLine.Count < 2)
                        continue;

                    // Создаём путь для трубки
                    var path = new Vector3[hatchLine.Count];
                    for (int i = 0; i < hatchLine.Count; i++)
                    {
                        path[i] = new Vector3(hatchLine[i].X, topLayerZ, hatchLine[i].Y);
                    }

                    // Добавляем трубку для штриховки
                    meshBuilder.AddTube(path, hatchDiameter, 4, false);
                }
            }

            PrintedLayersGeometry = meshBuilder.ToMesh();
        }

        /// <summary>
        /// Добавляет полилинию как тонкую "трубку" в MeshBuilder
        /// </summary>
        private void AddPolyLineToMeshBuilder(MeshBuilder meshBuilder, PrintMate.Terminal.Parsers.Shared.Models.PolyLine polyline, double zStart, double layerHeight)
        {
            const float diameter = 0.1f; // Толщина линии в мм
            float z = (float)(zStart + layerHeight / 2);

            // Создаем массив точек для AddTube
            var path = new Vector3[polyline.Points.Count];
            for (int i = 0; i < polyline.Points.Count; i++)
            {
                var p = polyline.Points[i];
                path[i] = new Vector3(p.X, z, p.Y); // X, Z, Y - потому что Z это высота в HelixToolkit
            }

            // Добавляем трубку по всем точкам полилинии
            if (path.Length >= 2)
            {
                meshBuilder.AddTube(path, diameter, 6, false);
            }
        }

        /// <summary>
        /// Добавляет заполненный полигон из контура (для сплошной модели)
        /// Использует ear clipping алгоритм для триангуляции произвольных полигонов
        /// </summary>
        private void AddFilledPolygon(MeshBuilder meshBuilder, List<ProjectParserTest.Parsers.Shared.Models.Point> points, float z)
        {
            if (points == null || points.Count < 3)
                return;

            // Преобразуем точки в 2D список для триангуляции
            var polygon2D = new List<(float x, float y)>();
            for (int i = 0; i < points.Count; i++)
            {
                polygon2D.Add((points[i].X, points[i].Y));
            }

            // Ear clipping триангуляция
            var indices = TriangulatePolygon(polygon2D);

            // Создаём треугольники из индексов
            for (int i = 0; i < indices.Count; i += 3)
            {
                var p0 = new Vector3(points[indices[i]].X, z, points[indices[i]].Y);
                var p1 = new Vector3(points[indices[i + 1]].X, z, points[indices[i + 1]].Y);
                var p2 = new Vector3(points[indices[i + 2]].X, z, points[indices[i + 2]].Y);

                meshBuilder.AddTriangle(p0, p1, p2);
            }
        }

        /// <summary>
        /// Триангуляция полигона методом ear clipping
        /// Возвращает индексы вершин треугольников
        /// </summary>
        private List<int> TriangulatePolygon(List<(float x, float y)> points)
        {
            var indices = new List<int>();
            if (points.Count < 3) return indices;

            // Создаём список индексов вершин
            var remaining = new List<int>();
            for (int i = 0; i < points.Count; i++)
                remaining.Add(i);

            // Определяем направление обхода (CW или CCW)
            bool isCCW = IsCounterClockwise(points);

            // Отрезаем "уши" пока не останется треугольник
            while (remaining.Count > 3)
            {
                bool earFound = false;

                for (int i = 0; i < remaining.Count; i++)
                {
                    int prev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                    int curr = remaining[i];
                    int next = remaining[(i + 1) % remaining.Count];

                    if (IsEar(points, remaining, prev, curr, next, isCCW))
                    {
                        // Добавляем треугольник
                        indices.Add(prev);
                        indices.Add(curr);
                        indices.Add(next);

                        // Удаляем "ухо"
                        remaining.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }

                if (!earFound)
                {
                    // Если не нашли "ухо", используем простую триангуляцию веером
                    for (int i = 1; i < remaining.Count - 1; i++)
                    {
                        indices.Add(remaining[0]);
                        indices.Add(remaining[i]);
                        indices.Add(remaining[i + 1]);
                    }
                    break;
                }
            }

            // Добавляем последний треугольник
            if (remaining.Count == 3)
            {
                indices.Add(remaining[0]);
                indices.Add(remaining[1]);
                indices.Add(remaining[2]);
            }

            return indices;
        }

        /// <summary>
        /// Проверяет, является ли треугольник "ухом"
        /// </summary>
        private bool IsEar(List<(float x, float y)> points, List<int> remaining, int prev, int curr, int next, bool isCCW)
        {
            var p0 = points[prev];
            var p1 = points[curr];
            var p2 = points[next];

            // Проверяем, что треугольник выпуклый (правильное направление)
            float cross = (p1.x - p0.x) * (p2.y - p0.y) - (p1.y - p0.y) * (p2.x - p0.x);
            if (isCCW && cross < 0) return false;
            if (!isCCW && cross > 0) return false;

            // Проверяем, что внутри треугольника нет других вершин
            for (int i = 0; i < remaining.Count; i++)
            {
                int idx = remaining[i];
                if (idx == prev || idx == curr || idx == next)
                    continue;

                if (PointInTriangle(points[idx], p0, p1, p2))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Проверяет, находится ли точка внутри треугольника
        /// </summary>
        private bool PointInTriangle((float x, float y) p, (float x, float y) a, (float x, float y) b, (float x, float y) c)
        {
            float sign(float x1, float y1, float x2, float y2, float x3, float y3)
            {
                return (x1 - x3) * (y2 - y3) - (x2 - x3) * (y1 - y3);
            }

            float d1 = sign(p.x, p.y, a.x, a.y, b.x, b.y);
            float d2 = sign(p.x, p.y, b.x, b.y, c.x, c.y);
            float d3 = sign(p.x, p.y, c.x, c.y, a.x, a.y);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        /// <summary>
        /// Определяет направление обхода полигона
        /// </summary>
        private bool IsCounterClockwise(List<(float x, float y)> points)
        {
            float area = 0;
            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                area += (points[next].x - points[i].x) * (points[next].y + points[i].y);
            }
            return area < 0;
        }

        /// <summary>
        /// Добавляет вертикальные стенки между двумя контурами
        /// </summary>
        private void AddVerticalWalls(MeshBuilder meshBuilder, List<ProjectParserTest.Parsers.Shared.Models.Point> points, float zBottom, float zTop)
        {
            if (points == null || points.Count < 2)
                return;

            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;

                var p1Bottom = new Vector3(points[i].X, zBottom, points[i].Y);
                var p1Top = new Vector3(points[i].X, zTop, points[i].Y);
                var p2Bottom = new Vector3(points[next].X, zBottom, points[next].Y);
                var p2Top = new Vector3(points[next].X, zTop, points[next].Y);

                // Два треугольника для каждой стенки
                meshBuilder.AddTriangle(p1Bottom, p1Top, p2Top);
                meshBuilder.AddTriangle(p1Bottom, p2Top, p2Bottom);
            }
        }

        #endregion
    }
}
