using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Buffers;
using SharpDX;
using SharpDX.Direct3D11;
using ProjectParserTest.Parsers.Shared.Models;
using ProjectParserTest.Parsers.Shared.Enums;
using Buffer = SharpDX.Direct3D11.Buffer;
using NetTopologySuite.Geometries;
using NetTopologySuite.Triangulate.Polygon;

namespace PrintMate.Terminal.Rendering
{
    /// <summary>
    /// Билдер для конвертации CLI геометрии в DirectX mesh buffers
    /// С оптимизациями: пул буферов, LOD, ArrayPool
    /// </summary>
    public class CliGeometryBuilder
    {
        private readonly Device _device;

        // ПУЛ GPU БУФЕРОВ для переиспользования
        private readonly ConcurrentBag<CliMesh> _meshPool = new ConcurrentBag<CliMesh>();
        private const int MaxPoolSize = 50;

        // ArrayPool для временных массивов (избегаем GC)
        private static readonly ArrayPool<Vertex> VertexPool = ArrayPool<Vertex>.Shared;
        private static readonly ArrayPool<uint> IndexPool = ArrayPool<uint>.Shared;

        public CliGeometryBuilder(Device device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        #region Пул буферов

        /// <summary>
        /// Возвращает меш в пул для переиспользования
        /// </summary>
        public void ReturnToPool(CliMesh mesh)
        {
            if (mesh == null) return;

            if (_meshPool.Count < MaxPoolSize)
            {
                _meshPool.Add(mesh);
            }
            else
            {
                mesh.Dispose();
            }
        }

        /// <summary>
        /// Очищает пул буферов
        /// </summary>
        public void ClearPool()
        {
            while (_meshPool.TryTake(out var mesh))
            {
                mesh.Dispose();
            }
        }

        #endregion

        #region Построение геометрии для напечатанных слоёв (залитая)

        /// <summary>
        /// Создаёт залитый 3D меш для напечатанных слоёв (от 0 до currentLayer)
        /// PartId кодируется в red channel цвета вершин для shader-based highlighting
        /// </summary>
        public CliMesh BuildPrintedLayersMesh(Project project, int currentLayer, Color4 color)
        {
            if (project == null || project.Layers == null || currentLayer < 0)
            {
                
                return null;
            }

            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            int layerCount = Math.Min(currentLayer, project.Layers.Count);
            

            // DEBUG: Выводим типы регионов в первом слое
            if (layerCount > 0 && project.Layers[0].Regions != null)
            {
                var regionTypes = new System.Collections.Generic.HashSet<GeometryRegion>();
                foreach (var region in project.Layers[0].Regions)
                {
                    regionTypes.Add(region.GeometryRegion);
                }
                
            }

            // Вычисляем центр и радиус для градиента
            var (centerX, centerY, maxRadius) = CalculateProjectCenter(project, layerCount);
            

            // layer.Height содержит абсолютную Z позицию слоя в мм
            // Если layer.Height некорректен (слишком маленький), используем расчёт через индекс
            float layerThickness = project.GetLayerThicknessInMillimeters();

            // Собираем контуры для каждого слоя с информацией о детали
            var layerContours = new List<(float zTop, List<(List<ProjectParserTest.Parsers.Shared.Models.Point> points, int? partId)> contours)>();

            for (int i = 0; i < layerCount; i++)
            {
                var layer = project.Layers[i];

                if (layer.Regions == null)
                {
                    continue;
                }

                // Используем layer.Height как абсолютную Z позицию, если она валидна
                // Иначе вычисляем через индекс * толщину слоя
                float zTop = (float)layer.Height;
                if (zTop < 0.001f) // Если Height слишком маленький - вычисляем сами
                {
                    zTop = (i + 1) * layerThickness;
                }

                // Собираем все контуры текущего слоя с информацией о детали
                var contoursAtThisZ = new List<(List<ProjectParserTest.Parsers.Shared.Models.Point> points, int? partId)>();

                // Логируем типы регионов для первых слоёв
                if (i < 5 || i == 37 || i == 38)
                {
                    var regionTypes = string.Join(", ", layer.Regions.Select(r => r.GeometryRegion.ToString()).Distinct());
                    
                }

                foreach (var region in layer.Regions)
                {
                    if (region.PolyLines == null)
                        continue;

                    // Берём preview регионы для гладкой заливки, либо обычные регионы если preview нет
                    // Также включаем контуры и края для отображения
                    if (region.GeometryRegion == GeometryRegion.InfillRegionPreview ||
                        region.GeometryRegion == GeometryRegion.UpskinRegionPreview ||
                        region.GeometryRegion == GeometryRegion.DownskinRegionPreview ||
                        region.GeometryRegion == GeometryRegion.Infill ||
                        region.GeometryRegion == GeometryRegion.Upskin ||
                        region.GeometryRegion == GeometryRegion.Downskin ||
                        region.GeometryRegion == GeometryRegion.Edges ||
                        region.GeometryRegion == GeometryRegion.Contour ||
                        region.GeometryRegion == GeometryRegion.ContourUpskin ||
                        region.GeometryRegion == GeometryRegion.ContourDownskin)
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

                if (contoursAtThisZ.Count > 0)
                {
                    layerContours.Add((zTop, contoursAtThisZ));
                }
            }

            

            // Теперь создаём геометрию: нижняя поверхность + вертикальные стенки + ТОЛЬКО верхняя поверхность последнего слоя
            if (layerContours.Count > 0)
            {
                // Нижняя поверхность (первый слой) - теперь начинаем с Z=0
                float zBottom = 0.0f;
                foreach (var contour in layerContours[0].contours)
                {
                    // Кодируем partId в альфа-канал (0-255) для нижней поверхности
                    // Зелёный канал: 0.0 = обычный слой, синий канал: не используется
                    float partIdEncoded = contour.partId.HasValue ? (float)contour.partId.Value / 255.0f : 0.0f;
                    Color4 bottomColor = new Color4(0.0f, 0.0f, 0.0f, partIdEncoded); // RGB не используется, partId в alpha
                    AddFilledPolygonWithGradient(vertices, indices, contour.points, zBottom, bottomColor, new Vector3(0, 0, -1), centerX, centerY, maxRadius);
                }

                // Вертикальные стенки между слоями (для всех слоёв)
                for (int i = 0; i < layerContours.Count; i++)
                {
                    float zCurrent = i == 0 ? 0.0f : layerContours[i - 1].zTop;
                    float zNext = layerContours[i].zTop;

                    // Последний слой
                    bool isLastLayer = (i == layerContours.Count - 1);

                    foreach (var contour in layerContours[i].contours)
                    {
                        // Кодируем данные в vertex color:
                        // R channel: 1.0 = последний слой, 0.0 = обычный слой
                        // G, B channels: не используются (0.0)
                        // A channel: partId (0-255 закодировано как 0.0-1.0)

                        float partIdEncoded = contour.partId.HasValue ? (float)contour.partId.Value / 255.0f : 0.0f;
                        float isLastLayerFlag = isLastLayer ? 1.0f : 0.0f;

                        Color4 layerColor = new Color4(isLastLayerFlag, 0.0f, 0.0f, partIdEncoded);

                        // Вертикальные стенки для ВСЕХ слоёв
                        AddVerticalWallsWithGradient(vertices, indices, contour.points, zCurrent, zNext, layerColor, centerX, centerY, maxRadius);
                    }
                }

                // Верхняя поверхность ТОЛЬКО для последнего слоя (оптимизация - модель внутри пустая!)
                int lastLayerIndex = layerContours.Count - 1;
                float zTop = layerContours[lastLayerIndex].zTop;
                foreach (var contour in layerContours[lastLayerIndex].contours)
                {
                    float partIdEncoded = contour.partId.HasValue ? (float)contour.partId.Value / 255.0f : 0.0f;
                    Color4 topColor = new Color4(1.0f, 0.0f, 0.0f, partIdEncoded); // R=1.0 = последний слой (оранжевый)
                    AddFilledPolygonWithGradient(vertices, indices, contour.points, zTop, topColor, new Vector3(0, 0, 1), centerX, centerY, maxRadius);
                }
            }

            if (vertices.Count == 0 || indices.Count == 0)
            {
                
                return null;
            }

            

            // Создаём DirectX buffers
            return CreateMesh(vertices, indices);
        }

        #endregion

        #region Инкрементальное построение слоёв

        // Кеш для Z позиций слоёв (чтобы не пересчитывать каждый раз)
        private static double[] _layerZPositions;
        private static int _cachedProjectLayerCount = -1;

        /// <summary>
        /// Принудительно очищает кеш геометрии (для применения новых алгоритмов рендеринга)
        /// </summary>
        public static void ClearGeometryCache()
        {
            _layerZPositions = null;
            _cachedProjectLayerCount = -1;
            
        }

        /// <summary>
        /// Предварительно вычисляет Z позиции для всех слоёв (один раз для проекта)
        /// </summary>
        private void PrecomputeLayerZPositions(Project project)
        {
            if (project == null || project.Layers == null || project.Layers.Count == 0)
                return;

            // Если кеш уже заполнен для этого проекта, не пересчитываем
            if (_layerZPositions != null && _cachedProjectLayerCount == project.Layers.Count)
                return;

            _cachedProjectLayerCount = project.Layers.Count;
            _layerZPositions = new double[project.Layers.Count + 1]; // +1 для последней границы

            // layer.Height содержит абсолютную Z позицию слоя в мм
            float layerThickness = project.GetLayerThicknessInMillimeters();
            _layerZPositions[0] = 0;

            for (int i = 0; i < project.Layers.Count; i++)
            {
                var layer = project.Layers[i];
                // Если layer.Height валидна - используем её, иначе расчёт по индексу
                double zPos = layer.Height;
                if (zPos < 0.001)
                    zPos = (i + 1) * layerThickness;
                _layerZPositions[i + 1] = zPos;
            }
        }

        /// <summary>
        /// Строит геометрию для одного конкретного слоя (вертикальные стенки + верхняя крышка)
        /// Используется для инкрементального построения при анимации.
        /// Возвращает списки вершин и индексов, которые можно объединить с другими слоями.
        /// ОПТИМИЗИРОВАНО: кеш Z позиций, предаллокация, адаптивное упрощение полигонов
        /// </summary>
        public (List<Vertex> vertices, List<uint> indices) BuildSingleLayerGeometry(
            Project project,
            int layerIndex,
            bool isLastLayer,
            float centerX,
            float centerY,
            float maxRadius,
            float simplificationTolerance = 10.0f)
        {
            if (project == null || project.Layers == null || layerIndex < 0 || layerIndex >= project.Layers.Count)
                return (new List<Vertex>(), new List<uint>());

            var layer = project.Layers[layerIndex];
            if (layer.Regions == null)
                return (new List<Vertex>(), new List<uint>());

            // Предварительно вычисляем Z позиции один раз для всего проекта
            PrecomputeLayerZPositions(project);

            // Получаем Z координаты из кеша (мгновенно вместо O(n))
            float zBottom = (float)_layerZPositions[layerIndex];
            float zTop = (float)_layerZPositions[layerIndex + 1];

            // Собираем контуры текущего слоя
            // Оценка размера: ~50 вершин на контур, ~3 индекса на вершину
            int estimatedContours = layer.Regions.Count * 2;
            var contoursAtThisZ = new List<(List<ProjectParserTest.Parsers.Shared.Models.Point> points, int? partId)>(estimatedContours);

            // НОВАЯ ЛОГИКА: Вместо штриховок ищем Contour для каждого Infill региона
            // Собираем уникальные Contour регионы на основе Infill/Upskin/Downskin
            var infillRegions = new HashSet<GeometryRegion>
            {
                GeometryRegion.Infill,
                GeometryRegion.Upskin,
                GeometryRegion.Downskin,
                GeometryRegion.InfillRegionPreview,
                GeometryRegion.UpskinRegionPreview,
                GeometryRegion.DownskinRegionPreview
            };

            // Сначала находим все Infill регионы с их partId
            var infillParts = new HashSet<int?>();
            foreach (var region in layer.Regions)
            {
                if (infillRegions.Contains(region.GeometryRegion) || region.Type == BlockType.Hatch)
                {
                    infillParts.Add(region.Part?.Id);
                }
            }

            // Теперь ищем соответствующие Contour регионы для этих деталей
            int contourRegionCount = 0;
            int contourPolylineCount = 0;
            foreach (var region in layer.Regions)
            {
                if (region.PolyLines == null)
                    continue;

                // Берём только Contour регионы, для которых есть Infill
                if (region.GeometryRegion == GeometryRegion.Contour && infillParts.Contains(region.Part?.Id))
                {
                    contourRegionCount++;
                    foreach (var polyline in region.PolyLines)
                    {
                        if (polyline.Points != null && polyline.Points.Count >= 3)
                        {
                            contoursAtThisZ.Add((polyline.Points, region.Part?.Id));
                            contourPolylineCount++;
                        }
                    }
                }
            }

            

            if (contoursAtThisZ.Count == 0)
                return (new List<Vertex>(), new List<uint>());

            // НОВАЯ ОПТИМИЗАЦИЯ: Группируем полилинии по partId для создания единого меша
            var partGroups = new Dictionary<int?, List<List<ProjectParserTest.Parsers.Shared.Models.Point>>>();
            foreach (var contour in contoursAtThisZ)
            {
                int? partId = contour.partId ?? 0; // null partId -> 0
                if (!partGroups.ContainsKey(partId))
                    partGroups[partId] = new List<List<ProjectParserTest.Parsers.Shared.Models.Point>>();

                partGroups[partId].Add(contour.points);
            }

            

            // Предаллокация с запасом для лучшей производительности
            int estimatedVertices = contoursAtThisZ.Count * 50; // ~50 вершин на контур
            int estimatedIndices = estimatedVertices * 3; // ~3 индекса на вершину
            var vertices = new List<Vertex>(estimatedVertices);
            var indices = new List<uint>(estimatedIndices);

            // Обрабатываем каждую часть отдельно
            float isLastLayerFlag = isLastLayer ? 1.0f : 0.0f;
            foreach (var partGroup in partGroups)
            {
                int? partId = partGroup.Key;
                var polylines = partGroup.Value;

                float partIdEncoded = partId.HasValue ? (float)partId.Value / 255.0f : 0.0f;

                // Для первого слоя добавляем нижнюю крышку
                if (layerIndex == 0)
                {
                    Color4 bottomColor = new Color4(0.0f, 0.0f, 0.0f, partIdEncoded);
                    AddUnifiedMeshForPart(vertices, indices, polylines, zBottom, bottomColor, new Vector3(0, 0, -1), centerX, centerY, maxRadius, simplificationTolerance);
                }

                // Вертикальные стенки - рисуем для каждого контура отдельно
                Color4 layerColor = new Color4(isLastLayerFlag, 0.0f, 0.0f, partIdEncoded);
                foreach (var polyline in polylines)
                {
                    if (polyline == null || polyline.Count < 3)
                        continue;

                    var simplified = SimplifyPolygon(polyline, simplificationTolerance);
                    if (simplified.Count >= 3)
                    {
                        AddVerticalWallsWithGradient(vertices, indices, simplified, zBottom, zTop, layerColor, centerX, centerY, maxRadius, simplificationTolerance);
                    }
                }

                // НЕ рисуем верхнюю крышку совсем!
                // Визуализация идёт только через вертикальные стенки (голубые контуры)
            }

            return (vertices, indices);
        }

        /// <summary>
        /// Объединяет несколько наборов геометрии в один и создаёт DirectX меш
        /// </summary>
        public CliMesh MergeGeometryToMesh(List<(List<Vertex> vertices, List<uint> indices)> geometries)
        {
            var allVertices = new List<Vertex>();
            var allIndices = new List<uint>();

            foreach (var geometry in geometries)
            {
                uint baseIndex = (uint)allVertices.Count;

                allVertices.AddRange(geometry.vertices);

                // Смещаем индексы на текущее количество вершин
                foreach (var index in geometry.indices)
                {
                    allIndices.Add(baseIndex + index);
                }
            }

            if (allVertices.Count == 0 || allIndices.Count == 0)
                return null;

            return CreateMesh(allVertices, allIndices);
        }

        /// <summary>
        /// Создаёт или обновляет Dynamic mesh для быстрого обновления буферов
        /// Переиспользует существующие буферы если они достаточного размера
        /// </summary>
        public CliMesh CreateOrUpdateDynamicMesh(
            ref CliMesh existingMesh,
            ref int capacityVertices,
            ref int capacityIndices,
            Vertex[] vertices,
            uint[] indices)
        {
            int requiredVertices = vertices.Length;
            int requiredIndices = indices.Length;
            int vertexSizeBytes = SharpDX.Utilities.SizeOf<Vertex>();
            int indexSizeBytes = sizeof(uint);

            // Если буферы достаточного размера - обновляем данные напрямую
            if (existingMesh != null &&
                capacityVertices >= requiredVertices &&
                capacityIndices >= requiredIndices)
            {
                try
                {
                    var context = _device.ImmediateContext;

                    // Обновляем vertex buffer
                    var vertexBox = context.MapSubresource(existingMesh.VertexBuffer, 0,
                        SharpDX.Direct3D11.MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
                    SharpDX.Utilities.Write(vertexBox.DataPointer, vertices, 0, requiredVertices);
                    context.UnmapSubresource(existingMesh.VertexBuffer, 0);

                    // Обновляем index buffer
                    var indexBox = context.MapSubresource(existingMesh.IndexBuffer, 0,
                        SharpDX.Direct3D11.MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
                    SharpDX.Utilities.Write(indexBox.DataPointer, indices, 0, requiredIndices);
                    context.UnmapSubresource(existingMesh.IndexBuffer, 0);

                    existingMesh.VertexCount = requiredVertices;
                    existingMesh.IndexCount = requiredIndices;

                    return existingMesh;
                }
                catch
                {
                    existingMesh?.Dispose();
                    existingMesh = null;
                    capacityVertices = 0;
                    capacityIndices = 0;
                }
            }

            // Создаём новые буферы с запасом (x2 для уменьшения пересозданий)
            existingMesh?.Dispose();

            int newCapacityVertices = Math.Max(requiredVertices * 2, 65536);
            int newCapacityIndices = Math.Max(requiredIndices * 2, 65536 * 3);

            // Создаём Dynamic vertex buffer
            var vertexBufferDesc = new SharpDX.Direct3D11.BufferDescription
            {
                SizeInBytes = vertexSizeBytes * newCapacityVertices,
                Usage = SharpDX.Direct3D11.ResourceUsage.Dynamic,
                BindFlags = SharpDX.Direct3D11.BindFlags.VertexBuffer,
                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Write
            };
            var vertexBuffer = new SharpDX.Direct3D11.Buffer(_device, vertexBufferDesc);

            // Создаём Dynamic index buffer
            var indexBufferDesc = new SharpDX.Direct3D11.BufferDescription
            {
                SizeInBytes = indexSizeBytes * newCapacityIndices,
                Usage = SharpDX.Direct3D11.ResourceUsage.Dynamic,
                BindFlags = SharpDX.Direct3D11.BindFlags.IndexBuffer,
                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Write
            };
            var indexBuffer = new SharpDX.Direct3D11.Buffer(_device, indexBufferDesc);

            // Заполняем данные
            var context2 = _device.ImmediateContext;

            var vertexBox2 = context2.MapSubresource(vertexBuffer, 0,
                SharpDX.Direct3D11.MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
            SharpDX.Utilities.Write(vertexBox2.DataPointer, vertices, 0, requiredVertices);
            context2.UnmapSubresource(vertexBuffer, 0);

            var indexBox2 = context2.MapSubresource(indexBuffer, 0,
                SharpDX.Direct3D11.MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
            SharpDX.Utilities.Write(indexBox2.DataPointer, indices, 0, requiredIndices);
            context2.UnmapSubresource(indexBuffer, 0);

            capacityVertices = newCapacityVertices;
            capacityIndices = newCapacityIndices;

            existingMesh = new CliMesh
            {
                VertexBuffer = vertexBuffer,
                IndexBuffer = indexBuffer,
                VertexCount = requiredVertices,
                IndexCount = requiredIndices
            };

            return existingMesh;
        }

        /// <summary>
        /// Обновляет флаг isLastLayer для вершин предыдущего последнего слоя
        /// (меняет R канал с 1.0 на 0.0)
        /// </summary>
        public void UpdatePreviousLastLayerFlag(List<Vertex> vertices, int layerStartVertexIndex)
        {
            for (int i = layerStartVertexIndex; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                // Меняем R канал с 1.0 на 0.0 (это был последний слой, теперь обычный)
                if (vertex.Color.Red > 0.5f)
                {
                    vertex.Color = new Color4(0.0f, vertex.Color.Green, vertex.Color.Blue, vertex.Color.Alpha);
                    vertices[i] = vertex;
                }
            }
        }

        #endregion

        #region Построение геометрии для текущего слоя (штриховка линиями)

        /// <summary>
        /// Создаёт меш со штриховкой для текущего слоя (тонкие линии как трубки)
        /// </summary>
        public CliMesh BuildCurrentLayerHatchMesh(Layer layer, float zPosition, Color4 hatchColor, Color4 contourColor)
        {
            if (layer?.Regions == null)
                return null;

            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            // DEBUG: Логируем типы регионов
            var regionTypes = string.Join(", ", layer.Regions.Select(r => $"{r.GeometryRegion} (Type={r.Type})").Distinct());
            
            int contourCount = 0;

            foreach (var region in layer.Regions)
            {
                if (region.PolyLines == null)
                    continue;

                // НЕ рисуем заполнение (Infill) для текущего слоя - только контуры!
                // Закомментировано, чтобы не было серой заливки внутри контуров

                // Для контуров - рисуем как толстые линии
                if (region.GeometryRegion == GeometryRegion.Contour ||
                    region.GeometryRegion == GeometryRegion.ContourUpskin ||
                    region.GeometryRegion == GeometryRegion.ContourDownskin ||
                    region.GeometryRegion == GeometryRegion.Edges)
                {
                    // Единый оранжевый цвет для всех фигур
                    // R channel = 1.0 означает "деталь" для шейдера (применяется освещение и оранжевый цвет)
                    // Alpha = partId (1.0/255 минимальный для распознавания как деталь)
                    float partIdEncoded = region.Part != null ? (float)region.Part.Id / 255.0f : 1.0f / 255.0f;
                    Color4 regionColor = new Color4(1.0f, 0.0f, 0.0f, partIdEncoded);

                    foreach (var polyline in region.PolyLines)
                    {
                        if (polyline.Points != null && polyline.Points.Count >= 2)
                        {
                            // Увеличиваем толщину контуров до 0.2 мм
                            AddThinLine(vertices, indices, polyline.Points, zPosition, regionColor, 0.2f);
                            contourCount++;
                        }
                    }
                }
            }

            

            if (vertices.Count == 0 || indices.Count == 0)
                return null;

            return CreateMesh(vertices, indices);
        }

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Добавляет заполненный полигон (триангулированный)
        /// </summary>
        private void AddFilledPolygon(List<Vertex> vertices, List<uint> indices,
            List<ProjectParserTest.Parsers.Shared.Models.Point> points, float z, Color4 color, Vector3 normal)
        {
            if (points == null || points.Count < 3)
                return;

            // Триангуляция полигона (ear clipping)
            var triangleIndices = TriangulatePolygon(points);

            if (triangleIndices.Count == 0)
                return;

            uint baseIndex = (uint)vertices.Count;

            // Добавляем вершины
            foreach (var point in points)
            {
                var position = new Vector3(point.X, point.Y, z);
                vertices.Add(new Vertex(position, normal, color));
            }

            // Добавляем индексы треугольников
            foreach (var index in triangleIndices)
            {
                indices.Add(baseIndex + (uint)index);
            }
        }

        /// <summary>
        /// Добавляет вертикальные стенки между двумя контурами
        /// </summary>
        private void AddVerticalWalls(List<Vertex> vertices, List<uint> indices,
            List<ProjectParserTest.Parsers.Shared.Models.Point> points, float zBottom, float zTop, Color4 color)
        {
            if (points == null || points.Count < 2)
                return;

            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;

                var p1 = points[i];
                var p2 = points[next];

                // Вычисляем нормаль для стенки (наружу)
                Vector2 edge = new Vector2(p2.X - p1.X, p2.Y - p1.Y);
                Vector2 normal2D = new Vector2(-edge.Y, edge.X);
                normal2D.Normalize();
                Vector3 normal = new Vector3(normal2D.X, normal2D.Y, 0);

                // Четыре вершины стенки
                uint baseIndex = (uint)vertices.Count;

                vertices.Add(new Vertex(new Vector3(p1.X, p1.Y, zBottom), normal, color));
                vertices.Add(new Vertex(new Vector3(p1.X, p1.Y, zTop), normal, color));
                vertices.Add(new Vertex(new Vector3(p2.X, p2.Y, zTop), normal, color));
                vertices.Add(new Vertex(new Vector3(p2.X, p2.Y, zBottom), normal, color));

                // Два треугольника для стенки
                indices.Add(baseIndex + 0);
                indices.Add(baseIndex + 1);
                indices.Add(baseIndex + 2);

                indices.Add(baseIndex + 0);
                indices.Add(baseIndex + 2);
                indices.Add(baseIndex + 3);
            }
        }

        /// <summary>
        /// Добавляет тонкую линию как упрощённую трубку (quad strip вокруг линии)
        /// </summary>
        private void AddThinLine(List<Vertex> vertices, List<uint> indices,
            List<ProjectParserTest.Parsers.Shared.Models.Point> points, float z, Color4 color, float thickness)
        {
            if (points == null || points.Count < 2)
                return;

            for (int i = 0; i < points.Count - 1; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];

                // Направление линии
                Vector2 direction = new Vector2(p2.X - p1.X, p2.Y - p1.Y);
                direction.Normalize();

                // Перпендикуляр для ширины
                Vector2 perpendicular = new Vector2(-direction.Y, direction.X) * (thickness / 2f);

                // Четыре угла quad'а
                uint baseIndex = (uint)vertices.Count;

                Vector3 normal = new Vector3(0, 0, 1); // Нормаль вверх для плоских линий

                vertices.Add(new Vertex(new Vector3(p1.X - perpendicular.X, p1.Y - perpendicular.Y, z), normal, color));
                vertices.Add(new Vertex(new Vector3(p1.X + perpendicular.X, p1.Y + perpendicular.Y, z), normal, color));
                vertices.Add(new Vertex(new Vector3(p2.X + perpendicular.X, p2.Y + perpendicular.Y, z), normal, color));
                vertices.Add(new Vertex(new Vector3(p2.X - perpendicular.X, p2.Y - perpendicular.Y, z), normal, color));

                // Два треугольника
                indices.Add(baseIndex + 0);
                indices.Add(baseIndex + 1);
                indices.Add(baseIndex + 2);

                indices.Add(baseIndex + 0);
                indices.Add(baseIndex + 2);
                indices.Add(baseIndex + 3);
            }
        }

        /// <summary>
        /// Триангуляция полигона (ear clipping алгоритм)
        /// </summary>
        private List<int> TriangulatePolygon(List<ProjectParserTest.Parsers.Shared.Models.Point> points)
        {
            var indices = new List<int>();
            if (points.Count < 3) return indices;

            // Создаём список индексов вершин
            var remaining = new List<int>();
            for (int i = 0; i < points.Count; i++)
                remaining.Add(i);

            // Определяем направление обхода
            bool isCCW = IsCounterClockwise(points);

            // Отрезаем "уши" пока не останется треугольник
            int maxIterations = points.Count * 2; // Защита от бесконечного цикла
            int iteration = 0;

            while (remaining.Count > 3 && iteration < maxIterations)
            {
                iteration++;
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

        private bool IsEar(List<ProjectParserTest.Parsers.Shared.Models.Point> points, List<int> remaining, int prev, int curr, int next, bool isCCW)
        {
            var p0 = points[prev];
            var p1 = points[curr];
            var p2 = points[next];

            // Проверяем направление
            float cross = (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);
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

        private bool PointInTriangle(ProjectParserTest.Parsers.Shared.Models.Point p,
            ProjectParserTest.Parsers.Shared.Models.Point a,
            ProjectParserTest.Parsers.Shared.Models.Point b,
            ProjectParserTest.Parsers.Shared.Models.Point c)
        {
            float Sign(float x1, float y1, float x2, float y2, float x3, float y3)
            {
                return (x1 - x3) * (y2 - y3) - (x2 - x3) * (y1 - y3);
            }

            float d1 = Sign(p.X, p.Y, a.X, a.Y, b.X, b.Y);
            float d2 = Sign(p.X, p.Y, b.X, b.Y, c.X, c.Y);
            float d3 = Sign(p.X, p.Y, c.X, c.Y, a.X, a.Y);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        private bool IsCounterClockwise(List<ProjectParserTest.Parsers.Shared.Models.Point> points)
        {
            float area = 0;
            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                area += (points[next].X - points[i].X) * (points[next].Y + points[i].Y);
            }
            return area < 0;
        }

        /// <summary>
        /// Создаёт DirectX меш из вершин и индексов (List версия)
        /// </summary>
        private CliMesh CreateMesh(List<Vertex> vertices, List<uint> indices)
        {
            if (vertices.Count == 0 || indices.Count == 0)
                return null;

            return CreateMesh(vertices.ToArray(), indices.ToArray());
        }

        /// <summary>
        /// Создаёт DirectX меш из вершин и индексов (Array версия)
        /// </summary>
        public CliMesh CreateMesh(Vertex[] vertices, uint[] indices)
        {
            if (vertices.Length == 0 || indices.Length == 0)
                return null;

            // Создаём vertex buffer
            var vertexBufferDesc = new BufferDescription
            {
                SizeInBytes = Utilities.SizeOf<Vertex>() * vertices.Length,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None
            };

            var vertexBuffer = Buffer.Create(_device, vertices, vertexBufferDesc);

            // Создаём index buffer
            var indexBufferDesc = new BufferDescription
            {
                SizeInBytes = sizeof(uint) * indices.Length,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.IndexBuffer,
                CpuAccessFlags = CpuAccessFlags.None
            };

            var indexBuffer = Buffer.Create(_device, indices, indexBufferDesc);

            return new CliMesh
            {
                VertexBuffer = vertexBuffer,
                IndexBuffer = indexBuffer,
                VertexCount = vertices.Length,
                IndexCount = indices.Length
            };
        }

        #endregion

        #region LOD (Level of Detail)

        /// <summary>
        /// Создаёт упрощённую геометрию (LOD) для дальних слоёв
        /// Уменьшает количество треугольников в N раз
        /// </summary>
        public (List<Vertex> vertices, List<uint> indices) SimplifyGeometry(
            List<Vertex> vertices,
            List<uint> indices,
            int simplificationFactor = 2)
        {
            if (simplificationFactor <= 1 || indices.Count < 6)
                return (vertices, indices);

            // Простое прореживание: берём каждый N-й треугольник
            var simplifiedIndices = new List<uint>();
            var usedVertexIndices = new HashSet<uint>();

            for (int i = 0; i < indices.Count - 2; i += 3 * simplificationFactor)
            {
                if (i + 2 < indices.Count)
                {
                    usedVertexIndices.Add(indices[i]);
                    usedVertexIndices.Add(indices[i + 1]);
                    usedVertexIndices.Add(indices[i + 2]);
                }
            }

            // Создаём новый массив вершин только с используемыми
            var oldToNewIndex = new Dictionary<uint, uint>();
            var newVertices = new List<Vertex>();

            foreach (var oldIdx in usedVertexIndices.OrderBy(x => x))
            {
                oldToNewIndex[oldIdx] = (uint)newVertices.Count;
                if (oldIdx < vertices.Count)
                    newVertices.Add(vertices[(int)oldIdx]);
            }

            // Пересоздаём индексы с новыми номерами
            for (int i = 0; i < indices.Count - 2; i += 3 * simplificationFactor)
            {
                if (i + 2 < indices.Count)
                {
                    if (oldToNewIndex.TryGetValue(indices[i], out var idx0) &&
                        oldToNewIndex.TryGetValue(indices[i + 1], out var idx1) &&
                        oldToNewIndex.TryGetValue(indices[i + 2], out var idx2))
                    {
                        simplifiedIndices.Add(idx0);
                        simplifiedIndices.Add(idx1);
                        simplifiedIndices.Add(idx2);
                    }
                }
            }

            return (newVertices, simplifiedIndices);
        }

        /// <summary>
        /// Создаёт меш с LOD на основе расстояния до камеры
        /// lodLevel: 0 = полная детализация, 1 = упрощённая, 2 = очень упрощённая
        /// </summary>
        public CliMesh CreateMeshWithLOD(Vertex[] vertices, uint[] indices, int lodLevel)
        {
            if (lodLevel <= 0)
                return CreateMesh(vertices, indices);

            int simplificationFactor = (int)Math.Pow(2, lodLevel); // 2, 4, 8...
            var (simplifiedVerts, simplifiedInds) = SimplifyGeometry(
                vertices.ToList(),
                indices.ToList(),
                simplificationFactor);

            return CreateMesh(simplifiedVerts.ToArray(), simplifiedInds.ToArray());
        }

        #endregion

        #region Платформа

        /// <summary>
        /// Создаёт геометрию платформы 320x320 мм
        /// </summary>
        public CliMesh BuildPlatformMesh(float platformSize = 320f, float thickness = 2f, Color4 color = default)
        {
            if (color == default)
                color = new Color4(60f/255f, 60f/255f, 60f/255f, 0.0f); // Тёмно-серый RGB(60,60,60), Alpha=0 для partId=0

            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            float halfSize = platformSize / 2f;

            // 8 вершин куба
            // Платформа располагается от Z=-thickness до Z=0
            // Верхняя поверхность платформы на Z=0, где начинается печать модели
            Vector3[] positions = new Vector3[]
            {
                new Vector3(-halfSize, -halfSize, -thickness),  // Нижние вершины
                new Vector3(halfSize, -halfSize, -thickness),
                new Vector3(halfSize, halfSize, -thickness),
                new Vector3(-halfSize, halfSize, -thickness),
                new Vector3(-halfSize, -halfSize, 0),            // Верхние вершины (поверхность платформы)
                new Vector3(halfSize, -halfSize, 0),
                new Vector3(halfSize, halfSize, 0),
                new Vector3(-halfSize, halfSize, 0)
            };

            // 6 граней куба
            AddQuad(vertices, indices, positions, new[] { 0, 1, 2, 3 }, new Vector3(0, 0, -1), color); // Bottom
            AddQuad(vertices, indices, positions, new[] { 4, 7, 6, 5 }, new Vector3(0, 0, 1), color);  // Top
            AddQuad(vertices, indices, positions, new[] { 0, 4, 5, 1 }, new Vector3(0, -1, 0), color); // Front
            AddQuad(vertices, indices, positions, new[] { 2, 6, 7, 3 }, new Vector3(0, 1, 0), color);  // Back
            AddQuad(vertices, indices, positions, new[] { 0, 3, 7, 4 }, new Vector3(-1, 0, 0), color); // Left
            AddQuad(vertices, indices, positions, new[] { 1, 5, 6, 2 }, new Vector3(1, 0, 0), color);  // Right

            return CreateMesh(vertices, indices);
        }

        private void AddQuad(List<Vertex> vertices, List<uint> indices, Vector3[] positions, int[] cornerIndices, Vector3 normal, Color4 color)
        {
            uint baseIndex = (uint)vertices.Count;

            vertices.Add(new Vertex(positions[cornerIndices[0]], normal, color));
            vertices.Add(new Vertex(positions[cornerIndices[1]], normal, color));
            vertices.Add(new Vertex(positions[cornerIndices[2]], normal, color));
            vertices.Add(new Vertex(positions[cornerIndices[3]], normal, color));

            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);

            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }

        /// <summary>
        /// Создаёт плоскость тени под платформой (мягкий градиент от центра к краям)
        /// </summary>
        public CliMesh BuildPlatformShadowMesh(float platformSize = 320f, float shadowSize = 450f, float thickness = 2f)
        {
            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            float halfShadow = shadowSize / 2f;
            float zPos = -thickness - 0.1f; // Чуть ниже нижней грани платформы

            // Цвет тени: чёрный с градиентом прозрачности от центра к краям
            Color4 centerColor = new Color4(0f, 0f, 0f, 0.4f);   // Полупрозрачный чёрный в центре
            Color4 edgeColor = new Color4(0f, 0f, 0f, 0.0f);     // Прозрачный на краях

            // 4 вершины большой плоскости тени
            Vector3[] positions = new Vector3[]
            {
                new Vector3(-halfShadow, -halfShadow, zPos),
                new Vector3(halfShadow, -halfShadow, zPos),
                new Vector3(halfShadow, halfShadow, zPos),
                new Vector3(-halfShadow, halfShadow, zPos)
            };

            Vector3 normal = new Vector3(0, 0, 1); // Нормаль вверх

            // Создаём вершины с градиентом прозрачности
            uint baseIndex = (uint)vertices.Count;
            vertices.Add(new Vertex(positions[0], normal, edgeColor));   // Левый нижний угол - прозрачный
            vertices.Add(new Vertex(positions[1], normal, edgeColor));   // Правый нижний угол - прозрачный
            vertices.Add(new Vertex(positions[2], normal, edgeColor));   // Правый верхний угол - прозрачный
            vertices.Add(new Vertex(positions[3], normal, edgeColor));   // Левый верхний угол - прозрачный

            // Добавляем центральные вершины для более мягкого градиента
            float centerSize = platformSize * 0.6f;
            float halfCenter = centerSize / 2f;
            vertices.Add(new Vertex(new Vector3(-halfCenter, -halfCenter, zPos), normal, centerColor));
            vertices.Add(new Vertex(new Vector3(halfCenter, -halfCenter, zPos), normal, centerColor));
            vertices.Add(new Vertex(new Vector3(halfCenter, halfCenter, zPos), normal, centerColor));
            vertices.Add(new Vertex(new Vector3(-halfCenter, halfCenter, zPos), normal, centerColor));

            // Создаём треугольники (8 штук, формируя градиент от центра к краям)
            // Нижняя часть
            indices.Add(baseIndex + 0); indices.Add(baseIndex + 4); indices.Add(baseIndex + 5);
            indices.Add(baseIndex + 0); indices.Add(baseIndex + 5); indices.Add(baseIndex + 1);
            // Правая часть
            indices.Add(baseIndex + 1); indices.Add(baseIndex + 5); indices.Add(baseIndex + 6);
            indices.Add(baseIndex + 1); indices.Add(baseIndex + 6); indices.Add(baseIndex + 2);
            // Верхняя часть
            indices.Add(baseIndex + 2); indices.Add(baseIndex + 6); indices.Add(baseIndex + 7);
            indices.Add(baseIndex + 2); indices.Add(baseIndex + 7); indices.Add(baseIndex + 3);
            // Левая часть
            indices.Add(baseIndex + 3); indices.Add(baseIndex + 7); indices.Add(baseIndex + 4);
            indices.Add(baseIndex + 3); indices.Add(baseIndex + 4); indices.Add(baseIndex + 0);
            // Центр
            indices.Add(baseIndex + 4); indices.Add(baseIndex + 7); indices.Add(baseIndex + 6);
            indices.Add(baseIndex + 4); indices.Add(baseIndex + 6); indices.Add(baseIndex + 5);

            return CreateMesh(vertices, indices);
        }

        /// <summary>
        /// Создаёт wireframe контур платформы для визуализации её краёв
        /// </summary>
        public CliMesh BuildPlatformEdgesMesh(float platformSize = 320f, float thickness = 20f, Color4 color = default)
        {
            if (color == default)
                color = new Color4(0.8f, 0.8f, 0.85f, 1.0f); // Светло-серый металлический

            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            float halfSize = platformSize / 2f;

            // 8 вершин куба платформы
            Vector3[] positions = new Vector3[]
            {
                new Vector3(-halfSize, -halfSize, -thickness),  // 0: Нижние вершины
                new Vector3(halfSize, -halfSize, -thickness),   // 1
                new Vector3(halfSize, halfSize, -thickness),    // 2
                new Vector3(-halfSize, halfSize, -thickness),   // 3
                new Vector3(-halfSize, -halfSize, 0),           // 4: Верхние вершины (поверхность)
                new Vector3(halfSize, -halfSize, 0),            // 5
                new Vector3(halfSize, halfSize, 0),             // 6
                new Vector3(-halfSize, halfSize, 0)             // 7
            };

            Vector3 normal = new Vector3(0, 0, 1);

            // Добавляем все вершины
            foreach (var pos in positions)
            {
                vertices.Add(new Vertex(pos, normal, color));
            }

            // 12 рёбер куба
            // Нижние рёбра
            AddLine(indices, 0, 1);
            AddLine(indices, 1, 2);
            AddLine(indices, 2, 3);
            AddLine(indices, 3, 0);

            // Верхние рёбра (на поверхности платформы)
            AddLine(indices, 4, 5);
            AddLine(indices, 5, 6);
            AddLine(indices, 6, 7);
            AddLine(indices, 7, 4);

            // Вертикальные рёбра (боковые стороны платформы)
            AddLine(indices, 0, 4);
            AddLine(indices, 1, 5);
            AddLine(indices, 2, 6);
            AddLine(indices, 3, 7);

            return CreateMesh(vertices, indices);
        }

        /// <summary>
        /// Создаёт координатные оси X и Y на платформе (толстые линии 4px через весь экран)
        /// </summary>
        public CliMesh BuildPlatformAxesMesh(float platformSize = 320f)
        {
            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            float halfSize = platformSize / 2f;
            float axisLength = platformSize * 1.5f; // 150% от размера платформы (выходит за пределы)
            float lineWidth = 0.5f; // Ширина линии в мм (примерно 4 пикселя при масштабе 320мм на экран)
            float halfWidth = lineWidth / 2f;
            float zPos = 0.1f; // Чуть выше поверхности платформы

            // Цвет для обеих осей: #595959 (RGB 89,89,89)
            Color4 axisColor = new Color4(89f / 255f, 89f / 255f, 89f / 255f, 1.0f);
            Vector3 normal = new Vector3(0, 0, 1);

            // Ось X - горизонтальный прямоугольник
            uint baseIdx = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(-axisLength, -halfWidth, zPos), normal, axisColor));
            vertices.Add(new Vertex(new Vector3(axisLength, -halfWidth, zPos), normal, axisColor));
            vertices.Add(new Vertex(new Vector3(axisLength, halfWidth, zPos), normal, axisColor));
            vertices.Add(new Vertex(new Vector3(-axisLength, halfWidth, zPos), normal, axisColor));

            // Треугольники для оси X
            indices.Add(baseIdx + 0); indices.Add(baseIdx + 1); indices.Add(baseIdx + 2);
            indices.Add(baseIdx + 0); indices.Add(baseIdx + 2); indices.Add(baseIdx + 3);

            // Ось Y - вертикальный прямоугольник
            baseIdx = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(-halfWidth, -axisLength, zPos), normal, axisColor));
            vertices.Add(new Vertex(new Vector3(halfWidth, -axisLength, zPos), normal, axisColor));
            vertices.Add(new Vertex(new Vector3(halfWidth, axisLength, zPos), normal, axisColor));
            vertices.Add(new Vertex(new Vector3(-halfWidth, axisLength, zPos), normal, axisColor));

            // Треугольники для оси Y
            indices.Add(baseIdx + 0); indices.Add(baseIdx + 1); indices.Add(baseIdx + 2);
            indices.Add(baseIdx + 0); indices.Add(baseIdx + 2); indices.Add(baseIdx + 3);

            // Линии по краям платформы - от центра каждого края к углам
            // Верхний край (Y = halfSize): от центра к левому и правому углам
            AddEdgeLine(vertices, indices, new Vector3(0, halfSize, zPos), new Vector3(-halfSize, halfSize, zPos), lineWidth, axisColor, normal);
            AddEdgeLine(vertices, indices, new Vector3(0, halfSize, zPos), new Vector3(halfSize, halfSize, zPos), lineWidth, axisColor, normal);

            // Нижний край (Y = -halfSize): от центра к левому и правому углам
            AddEdgeLine(vertices, indices, new Vector3(0, -halfSize, zPos), new Vector3(-halfSize, -halfSize, zPos), lineWidth, axisColor, normal);
            AddEdgeLine(vertices, indices, new Vector3(0, -halfSize, zPos), new Vector3(halfSize, -halfSize, zPos), lineWidth, axisColor, normal);

            // Левый край (X = -halfSize): от центра к верхнему и нижнему углам
            AddEdgeLine(vertices, indices, new Vector3(-halfSize, 0, zPos), new Vector3(-halfSize, halfSize, zPos), lineWidth, axisColor, normal);
            AddEdgeLine(vertices, indices, new Vector3(-halfSize, 0, zPos), new Vector3(-halfSize, -halfSize, zPos), lineWidth, axisColor, normal);

            // Правый край (X = halfSize): от центра к верхнему и нижнему углам
            AddEdgeLine(vertices, indices, new Vector3(halfSize, 0, zPos), new Vector3(halfSize, halfSize, zPos), lineWidth, axisColor, normal);
            AddEdgeLine(vertices, indices, new Vector3(halfSize, 0, zPos), new Vector3(halfSize, -halfSize, zPos), lineWidth, axisColor, normal);

            return CreateMesh(vertices, indices);
        }

        /// <summary>
        /// Добавляет толстую линию (прямоугольник) от точки start до точки end
        /// </summary>
        private void AddEdgeLine(List<Vertex> vertices, List<uint> indices, Vector3 start, Vector3 end, float width, Color4 color, Vector3 normal)
        {
            float halfWidth = width / 2f;

            // Вычисляем направление линии и перпендикуляр
            Vector3 direction = Vector3.Normalize(end - start);
            Vector3 perpendicular = new Vector3(-direction.Y, direction.X, 0) * halfWidth;

            uint baseIdx = (uint)vertices.Count;

            // 4 вершины прямоугольника
            vertices.Add(new Vertex(start - perpendicular, normal, color));
            vertices.Add(new Vertex(end - perpendicular, normal, color));
            vertices.Add(new Vertex(end + perpendicular, normal, color));
            vertices.Add(new Vertex(start + perpendicular, normal, color));

            // 2 треугольника
            indices.Add(baseIdx + 0); indices.Add(baseIdx + 1); indices.Add(baseIdx + 2);
            indices.Add(baseIdx + 0); indices.Add(baseIdx + 2); indices.Add(baseIdx + 3);
        }

        /// <summary>
        /// Создаёт рамку границ рабочей области (wireframe cube)
        /// </summary>
        public CliMesh BuildBoundaryWireframeMesh(float width, float depth, float height, Color4 color = default)
        {
            if (color == default)
                color = new Color4(1.0f, 1.0f, 1.0f, 0.3f); // Полупрозрачный белый

            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            float halfWidth = width / 2f;
            float halfDepth = depth / 2f;

            // 8 вершин куба (границы рабочей области)
            // Нижние вершины на Z=0 (на платформе)
            // Верхние вершины на Z=height
            Vector3[] positions = new Vector3[]
            {
                new Vector3(-halfWidth, -halfDepth, 0),      // 0: нижний передний левый
                new Vector3(halfWidth, -halfDepth, 0),       // 1: нижний передний правый
                new Vector3(halfWidth, halfDepth, 0),        // 2: нижний задний правый
                new Vector3(-halfWidth, halfDepth, 0),       // 3: нижний задний левый
                new Vector3(-halfWidth, -halfDepth, height), // 4: верхний передний левый
                new Vector3(halfWidth, -halfDepth, height),  // 5: верхний передний правый
                new Vector3(halfWidth, halfDepth, height),   // 6: верхний задний правый
                new Vector3(-halfWidth, halfDepth, height)   // 7: верхний задний левый
            };

            // Нормаль вверх (не важна для линий, но нужна для вершин)
            Vector3 normal = new Vector3(0, 0, 1);

            // Добавляем вершины
            foreach (var pos in positions)
            {
                vertices.Add(new Vertex(pos, normal, color));
            }

            // 12 рёбер куба (линии)
            // Нижние рёбра (на платформе)
            AddLine(indices, 0, 1);
            AddLine(indices, 1, 2);
            AddLine(indices, 2, 3);
            AddLine(indices, 3, 0);

            // Верхние рёбра
            AddLine(indices, 4, 5);
            AddLine(indices, 5, 6);
            AddLine(indices, 6, 7);
            AddLine(indices, 7, 4);

            // Вертикальные рёбра
            AddLine(indices, 0, 4);
            AddLine(indices, 1, 5);
            AddLine(indices, 2, 6);
            AddLine(indices, 3, 7);

            return CreateMesh(vertices, indices);
        }

        /// <summary>
        /// Создаёт полноэкранный quad с радиальным градиентом (черный в центре -> серый по краям)
        /// </summary>
        public CliMesh BuildGradientBackgroundMesh()
        {
            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            // Центральный цвет - черный
            Color4 centerColor = new Color4(0f, 0f, 0f, 1.0f);

            // Цвет по краям - RGB(50,50,50)
            Color4 edgeColor = new Color4(50f / 255f, 50f / 255f, 50f / 255f, 1.0f);

            Vector3 normal = new Vector3(0, 0, 1);

            // Размер quad - очень большой, чтобы покрыть весь видимый экран
            float size = 10000f;

            // Центральная точка с черным цветом
            vertices.Add(new Vertex(new Vector3(0, 0, -size), normal, centerColor));

            // 4 угла с серым цветом
            vertices.Add(new Vertex(new Vector3(-size, -size, -size), normal, edgeColor));
            vertices.Add(new Vertex(new Vector3(size, -size, -size), normal, edgeColor));
            vertices.Add(new Vertex(new Vector3(size, size, -size), normal, edgeColor));
            vertices.Add(new Vertex(new Vector3(-size, size, -size), normal, edgeColor));

            // Треугольники от центра к каждому углу (4 треугольника образуют полноэкранный quad)
            indices.Add(0); indices.Add(1); indices.Add(2); // Центр -> нижний левый -> нижний правый
            indices.Add(0); indices.Add(2); indices.Add(3); // Центр -> нижний правый -> верхний правый
            indices.Add(0); indices.Add(3); indices.Add(4); // Центр -> верхний правый -> верхний левый
            indices.Add(0); indices.Add(4); indices.Add(1); // Центр -> верхний левый -> нижний левый

            return CreateMesh(vertices, indices);
        }

        private void AddLine(List<uint> indices, uint startVertex, uint endVertex)
        {
            indices.Add(startVertex);
            indices.Add(endVertex);
        }

        /// <summary>
        /// Создаёт сетку на платформе (grid lines)
        /// </summary>
        public CliMesh BuildPlatformGridMesh(float platformSize = 320f, float gridSpacing = 10f, Color4 color = default)
        {
            if (color == default)
                color = new Color4(0.4f, 0.4f, 0.4f, 1.0f); // Серый цвет для линий сетки

            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            float halfSize = platformSize / 2f;
            float z = 0.01f; // Немного выше платформы, чтобы не было z-fighting

            Vector3 normal = new Vector3(0, 0, 1);

            // Горизонтальные линии (вдоль X)
            for (float y = -halfSize; y <= halfSize; y += gridSpacing)
            {
                uint startIdx = (uint)vertices.Count;
                vertices.Add(new Vertex(new Vector3(-halfSize, y, z), normal, color));
                vertices.Add(new Vertex(new Vector3(halfSize, y, z), normal, color));
                AddLine(indices, startIdx, startIdx + 1);
            }

            // Вертикальные линии (вдоль Y)
            for (float x = -halfSize; x <= halfSize; x += gridSpacing)
            {
                uint startIdx = (uint)vertices.Count;
                vertices.Add(new Vertex(new Vector3(x, -halfSize, z), normal, color));
                vertices.Add(new Vertex(new Vector3(x, halfSize, z), normal, color));
                AddLine(indices, startIdx, startIdx + 1);
            }

            return CreateMesh(vertices, indices);
        }

        #endregion

        #region Градиентная заливка

        /// <summary>
        /// Вычисляет центр проекта и максимальный радиус для градиента
        /// </summary>
        private (float centerX, float centerY, float maxRadius) CalculateProjectCenter(Project project, int layerCount)
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            for (int i = 0; i < layerCount; i++)
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
        /// Вычисляет цвет с радиальным градиентом (от светлого в центре к темному по краям)
        /// </summary>
        private Color4 ApplyRadialGradient(float x, float y, Color4 baseColor, float centerX, float centerY, float maxRadius)
        {
            float dx = x - centerX;
            float dy = y - centerY;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            // Нормализуем расстояние от 0 до 1
            float normalizedDistance = Math.Min(distance / maxRadius, 1f);

            // Градиент: от 1.2 (светлее) в центре до 0.6 (темнее) по краям
            float gradient = 1.2f - (normalizedDistance * 0.6f);

            var result = new Color4(
                baseColor.Red * gradient,
                baseColor.Green * gradient,
                baseColor.Blue * gradient,
                baseColor.Alpha
            );

            // Логируем только если это верхний слой (R=1.0 до градиента)
            if (baseColor.Red > 0.9f && baseColor.Alpha < 0.01f)
            {
                
            }

            return result;
        }

        /// <summary>
        /// Упрощает полигон удалением близких точек (decimation)
        /// </summary>
        private List<ProjectParserTest.Parsers.Shared.Models.Point> SimplifyPolygon(
            List<ProjectParserTest.Parsers.Shared.Models.Point> points, float tolerance = 0.5f)
        {
            if (points == null || points.Count <= 3)
                return points; // Минимальный треугольник не упрощаем

            // ФИЛЬТРУЕМ NULL ТОЧКИ
            var filtered = new List<ProjectParserTest.Parsers.Shared.Models.Point>();
            foreach (var pt in points)
            {
                if (pt != null && !double.IsNaN(pt.X) && !double.IsNaN(pt.Y) && !float.IsInfinity(pt.X) && !float.IsInfinity(pt.Y))
                {
                    filtered.Add(pt);
                }
            }

            if (filtered.Count <= 3)
                return filtered;

            var simplified = new List<ProjectParserTest.Parsers.Shared.Models.Point>(filtered.Count);
            simplified.Add(filtered[0]); // Всегда сохраняем первую точку

            float toleranceSq = tolerance * tolerance;

            for (int i = 1; i < filtered.Count - 1; i++)
            {
                var lastAdded = simplified[simplified.Count - 1];
                var current = filtered[i];

                // Вычисляем расстояние между точками
                float dx = current.X - lastAdded.X;
                float dy = current.Y - lastAdded.Y;
                float distSq = dx * dx + dy * dy;

                // Добавляем точку только если она достаточно далеко от предыдущей
                if (distSq > toleranceSq)
                {
                    simplified.Add(current);
                }
            }

            // Всегда добавляем последнюю точку для замыкания контура
            var last = filtered[filtered.Count - 1];
            var firstAdded = simplified[0];
            float dxLast = last.X - firstAdded.X;
            float dyLast = last.Y - firstAdded.Y;
            float distSqLast = dxLast * dxLast + dyLast * dyLast;

            // Добавляем последнюю точку только если она не совпадает с первой
            if (distSqLast > toleranceSq)
            {
                simplified.Add(last);
            }

            // Если упростили до меньше чем 3 точки, возвращаем отфильтрованный
            return simplified.Count >= 3 ? simplified : filtered;
        }

        /// <summary>
        /// Добавляет залитый полигон с градиентной заливкой
        /// ОПТИМИЗИРОВАНО: упрощение полигона для уменьшения количества вершин
        /// </summary>
        private void AddFilledPolygonWithGradient(List<Vertex> vertices, List<uint> indices,
            List<ProjectParserTest.Parsers.Shared.Models.Point> points, float z, Color4 baseColor, Vector3 normal,
            float centerX, float centerY, float maxRadius, float tolerance = 0.5f)
        {
            if (points == null || points.Count < 3)
                return;

            // ОПТИМИЗАЦИЯ: упрощаем полигон (удаляем близкие точки)
            // Tolerance передаётся извне для адаптивного упрощения
            var simplifiedPoints = SimplifyPolygon(points, tolerance);

            // Триангуляция полигона (ear clipping)
            var triangleIndices = TriangulatePolygon(simplifiedPoints);

            if (triangleIndices.Count == 0)
                return;

            uint baseIndex = (uint)vertices.Count;

            // Добавляем вершины с градиентом
            foreach (var point in simplifiedPoints)
            {
                var position = new Vector3(point.X, point.Y, z);
                var gradientColor = ApplyRadialGradient(point.X, point.Y, baseColor, centerX, centerY, maxRadius);
                vertices.Add(new Vertex(position, normal, gradientColor));
            }

            // Добавляем индексы треугольников
            foreach (var index in triangleIndices)
            {
                indices.Add(baseIndex + (uint)index);
            }
        }

        /// <summary>
        /// Добавляет вертикальные стенки с градиентной заливкой
        /// ОПТИМИЗИРОВАНО: упрощение полигона для уменьшения количества вершин
        /// </summary>
        private void AddVerticalWallsWithGradient(List<Vertex> vertices, List<uint> indices,
            List<ProjectParserTest.Parsers.Shared.Models.Point> points, float zBottom, float zTop, Color4 baseColor,
            float centerX, float centerY, float maxRadius, float tolerance = 0.5f)
        {
            if (points == null || points.Count < 2)
                return;

            // ОПТИМИЗАЦИЯ: упрощаем полигон (удаляем близкие точки)
            // Tolerance передаётся извне для адаптивного упрощения
            var simplifiedPoints = SimplifyPolygon(points, tolerance);

            if (simplifiedPoints.Count < 2)
                return;

            uint baseIndex = (uint)vertices.Count;

            for (int i = 0; i < simplifiedPoints.Count; i++)
            {
                var p1 = simplifiedPoints[i];
                var p2 = simplifiedPoints[(i + 1) % simplifiedPoints.Count];

                var v1 = new Vector3(p1.X, p1.Y, zBottom);
                var v2 = new Vector3(p1.X, p1.Y, zTop);
                var v3 = new Vector3(p2.X, p2.Y, zTop);
                var v4 = new Vector3(p2.X, p2.Y, zBottom);

                // Нормаль к стенке
                var edge = new Vector3(p2.X - p1.X, p2.Y - p1.Y, 0);
                var normal = Vector3.Cross(edge, new Vector3(0, 0, 1));
                normal.Normalize();

                // Градиентные цвета для каждой вершины
                var color1 = ApplyRadialGradient(p1.X, p1.Y, baseColor, centerX, centerY, maxRadius);
                var color2 = ApplyRadialGradient(p2.X, p2.Y, baseColor, centerX, centerY, maxRadius);

                vertices.Add(new Vertex(v1, normal, color1));
                vertices.Add(new Vertex(v2, normal, color1));
                vertices.Add(new Vertex(v3, normal, color2));
                vertices.Add(new Vertex(v4, normal, color2));

                // Два треугольника для стенки
                indices.Add(baseIndex + 0);
                indices.Add(baseIndex + 1);
                indices.Add(baseIndex + 2);

                indices.Add(baseIndex + 0);
                indices.Add(baseIndex + 2);
                indices.Add(baseIndex + 3);

                baseIndex += 4;
            }
        }

        /// <summary>
        /// Создаёт единую выпуклую оболочку (Convex Hull) из всех контуров
        /// Объединяет тысячи мелких полилиний в один сплошной полигон
        /// </summary>
        private List<ProjectParserTest.Parsers.Shared.Models.Point> CreateUnifiedConvexHull(
            List<List<ProjectParserTest.Parsers.Shared.Models.Point>> polylines, float tolerance)
        {
            if (polylines == null || polylines.Count == 0)
                return null;

            // Собираем все точки из всех контуров
            var allPoints = new List<ProjectParserTest.Parsers.Shared.Models.Point>();
            foreach (var polyline in polylines)
            {
                if (polyline == null || polyline.Count < 2)
                    continue;

                var simplified = SimplifyPolygon(polyline, tolerance);
                allPoints.AddRange(simplified);
            }

            if (allPoints.Count < 3)
                return null;

            // Строим Convex Hull (Graham Scan алгоритм)
            return ComputeConvexHull(allPoints);
        }

        /// <summary>
        /// Вычисляет выпуклую оболочку (Convex Hull) методом Graham Scan
        /// </summary>
        private List<ProjectParserTest.Parsers.Shared.Models.Point> ComputeConvexHull(
            List<ProjectParserTest.Parsers.Shared.Models.Point> points)
        {
            if (points.Count < 3)
                return points;

            // Находим точку с минимальным Y (при равенстве - минимальный X)
            var pivot = points[0];
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].Y < pivot.Y || (points[i].Y == pivot.Y && points[i].X < pivot.X))
                    pivot = points[i];
            }

            // Сортируем точки по полярному углу относительно pivot
            var sorted = points.OrderBy(p =>
            {
                if (p.X == pivot.X && p.Y == pivot.Y)
                    return -Math.PI; // pivot идёт первым
                return Math.Atan2(p.Y - pivot.Y, p.X - pivot.X);
            }).ToList();

            // Graham Scan
            var hull = new List<ProjectParserTest.Parsers.Shared.Models.Point>();
            foreach (var point in sorted)
            {
                while (hull.Count >= 2 && CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(point);
            }

            return hull;
        }

        /// <summary>
        /// Вычисляет векторное произведение для определения поворота
        /// </summary>
        private float CrossProduct(
            ProjectParserTest.Parsers.Shared.Models.Point a,
            ProjectParserTest.Parsers.Shared.Models.Point b,
            ProjectParserTest.Parsers.Shared.Models.Point c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        /// <summary>
        /// Находит самый большой внешний контур (по площади) среди полилиний
        /// Используется для рисования вертикальных стенок без артефактов от мелких внутренних контуров
        /// </summary>
        private List<ProjectParserTest.Parsers.Shared.Models.Point> FindLargestOuterContour(
            List<List<ProjectParserTest.Parsers.Shared.Models.Point>> polylines, float tolerance)
        {
            if (polylines == null || polylines.Count == 0)
                return null;

            List<ProjectParserTest.Parsers.Shared.Models.Point> largestContour = null;
            double maxArea = 0;

            foreach (var polyline in polylines)
            {
                if (polyline == null || polyline.Count < 3)
                    continue;

                var simplified = SimplifyPolygon(polyline, tolerance);
                if (simplified.Count < 3)
                    continue;

                // Проверяем, что это внешний контур (CCW)
                bool isCCW = IsCounterClockwise(simplified);
                if (!isCCW)
                    continue;

                // Вычисляем площадь контура
                double area = CalculatePolygonArea(simplified);
                if (area > maxArea)
                {
                    maxArea = area;
                    largestContour = simplified;
                }
            }

            return largestContour;
        }

        /// <summary>
        /// Вычисляет площадь полигона по формуле Shoelace
        /// </summary>
        private double CalculatePolygonArea(List<ProjectParserTest.Parsers.Shared.Models.Point> points)
        {
            if (points == null || points.Count < 3)
                return 0;

            double area = 0;
            for (int i = 0; i < points.Count; i++)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % points.Count];
                area += (p1.X * p2.Y) - (p2.X * p1.Y);
            }

            return Math.Abs(area / 2.0);
        }

        /// <summary>
        /// Создаёт единый меш для одной части из множества полилиний
        /// ОПТИМИЗАЦИЯ: Объединяет тысячи маленьких полилиний в один меш с единой триангуляцией
        /// </summary>
        private void AddUnifiedMeshForPart(List<Vertex> vertices, List<uint> indices,
            List<List<ProjectParserTest.Parsers.Shared.Models.Point>> polylines, float z, Color4 baseColor, Vector3 normal,
            float centerX, float centerY, float maxRadius, float tolerance = 0.5f)
        {
            if (polylines == null || polylines.Count == 0)
                return;

            // Разделяем полилинии на внешние контуры (CCW) и отверстия (CW)
            var outerContours = new List<List<ProjectParserTest.Parsers.Shared.Models.Point>>();
            var holes = new List<List<ProjectParserTest.Parsers.Shared.Models.Point>>();

            foreach (var polyline in polylines)
            {
                if (polyline == null || polyline.Count < 3)
                    continue;

                var simplified = SimplifyPolygon(polyline, tolerance);
                if (simplified.Count < 3)
                    continue;

                bool isCCW = IsCounterClockwise(simplified);
                if (isCCW)
                    outerContours.Add(simplified);
                else
                    holes.Add(simplified);
            }

            // Если нет внешних контуров, ничего не рисуем
            if (outerContours.Count == 0)
                return;

            // Триангулируем каждый внешний контур с его отверстиями
            foreach (var outerContour in outerContours)
            {
                // Простая триангуляция без учёта отверстий (для начала)
                // TODO: В будущем можно добавить constrained Delaunay для учёта отверстий
                var triangleIndices = TriangulatePolygon(outerContour);

                if (triangleIndices.Count == 0)
                    continue;

                uint baseIndex = (uint)vertices.Count;

                // Добавляем вершины с градиентом
                foreach (var point in outerContour)
                {
                    var position = new Vector3(point.X, point.Y, z);
                    var gradientColor = ApplyRadialGradient(point.X, point.Y, baseColor, centerX, centerY, maxRadius);
                    vertices.Add(new Vertex(position, normal, gradientColor));
                }

                // Добавляем индексы треугольников
                foreach (var index in triangleIndices)
                {
                    indices.Add(baseIndex + (uint)index);
                }
            }
        }

        #endregion

        #region Global Triangulation (Triangle.NET)

        /// <summary>
        /// Создаёт единый глобальный меш из всех слоёв с использованием NetTopologySuite
        /// НОВАЯ ЛОГИКА: Глобальная триангуляция всех слоёв для создания литой модели
        /// </summary>
        public (List<Vertex> vertices, List<uint> indices) BuildGlobalUnifiedMesh(
            Project project,
            int maxLayerIndex,
            float centerX,
            float centerY,
            float maxRadius)
        {
            if (project == null || project.Layers == null || maxLayerIndex < 0)
                return (new List<Vertex>(), new List<uint>());

            

            var allVertices = new List<Vertex>();
            var allIndices = new List<uint>();

            // Предварительно вычисляем Z позиции
            PrecomputeLayerZPositions(project);

            // Обрабатываем каждый слой с NetTopologySuite
            for (int layerIndex = 0; layerIndex <= maxLayerIndex && layerIndex < project.Layers.Count; layerIndex++)
            {
                var layer = project.Layers[layerIndex];
                if (layer.Regions == null)
                    continue;

                float zBottom = (float)_layerZPositions[layerIndex];
                float zTop = (float)_layerZPositions[layerIndex + 1];
                bool isLastLayer = (layerIndex == maxLayerIndex);

                // Собираем контуры текущего слоя
                var infillRegions = new HashSet<GeometryRegion>
                {
                    GeometryRegion.Infill,
                    GeometryRegion.Upskin,
                    GeometryRegion.Downskin,
                    GeometryRegion.InfillRegionPreview,
                    GeometryRegion.UpskinRegionPreview,
                    GeometryRegion.DownskinRegionPreview
                };

                // Находим части с infill
                var infillParts = new HashSet<int?>();
                foreach (var region in layer.Regions)
                {
                    if (infillRegions.Contains(region.GeometryRegion) || region.Type == BlockType.Hatch)
                    {
                        infillParts.Add(region.Part?.Id);
                    }
                }

                // Группируем контуры по partId
                var partGroups = new Dictionary<int?, List<List<ProjectParserTest.Parsers.Shared.Models.Point>>>();
                foreach (var region in layer.Regions)
                {
                    if (region.PolyLines == null)
                        continue;

                    if (region.GeometryRegion == GeometryRegion.Contour && infillParts.Contains(region.Part?.Id))
                    {
                        int? partId = region.Part?.Id ?? 0;
                        if (!partGroups.ContainsKey(partId))
                            partGroups[partId] = new List<List<ProjectParserTest.Parsers.Shared.Models.Point>>();

                        foreach (var polyline in region.PolyLines)
                        {
                            if (polyline.Points != null && polyline.Points.Count >= 3)
                            {
                                partGroups[partId].Add(polyline.Points);
                            }
                        }
                    }
                }

                // Для каждой части создаём unified mesh с NetTopologySuite
                foreach (var partGroup in partGroups)
                {
                    int? partId = partGroup.Key;
                    var polylines = partGroup.Value;

                    if (polylines.Count == 0)
                        continue;

                    float partIdEncoded = partId.HasValue ? (float)partId.Value / 255.0f : 0.0f;
                    float isLastLayerFlag = isLastLayer ? 1.0f : 0.0f;
                    Color4 layerColor = new Color4(isLastLayerFlag, 0.0f, 0.0f, partIdEncoded);

                    // ДОБАВЛЯЕМ КРАСНУЮ ЗАЛИВКУ между контурами на верхней грани слоя
                    Color4 fillColor = new Color4(0.6f, 0.1f, 0.1f, partIdEncoded); // Красный
                    var (fillVerts, fillInds) = TriangulateWithTriangleNet(
                        polylines,
                        zTop,
                        fillColor,
                        centerX,
                        centerY,
                        maxRadius
                    );

                    if (fillVerts.Count > 0)
                    {
                        uint baseIndex = (uint)allVertices.Count;
                        allVertices.AddRange(fillVerts);
                        allIndices.AddRange(fillInds.Select(i => i + baseIndex));
                    }

                    // Добавляем вертикальные стенки между слоями (для ВСЕХ слоёв)
                    if (layerIndex > 0)
                    {
                        foreach (var polyline in polylines)
                        {
                            var simplified = SimplifyPolygon(polyline, 2.0f);
                            if (simplified.Count >= 3)
                            {
                                AddVerticalWallsWithGradient(allVertices, allIndices, simplified, zBottom, zTop,
                                    layerColor, centerX, centerY, maxRadius, 2.0f);
                            }
                        }
                    }
                }
            }

            

            return (allVertices, allIndices);
        }

        /// <summary>
        /// Выполняет триангуляцию полигонов с использованием NetTopologySuite (constrained Delaunay)
        /// НОВАЯ ВЕРСИЯ: Создаёт полигон с дырками (внешний контур + внутренние holes)
        /// Возвращает вершины и индексы для одного слоя
        /// </summary>
        private (List<Vertex> vertices, List<uint> indices) TriangulateWithTriangleNet(
            List<List<ProjectParserTest.Parsers.Shared.Models.Point>> polylines,
            float z,
            Color4 baseColor,
            float centerX,
            float centerY,
            float maxRadius)
        {
            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            if (polylines == null || polylines.Count == 0)
                return (vertices, indices);

            try
            {
                // НОВАЯ ЛОГИКА: Находим внешний контур и внутренние дырки
                if (polylines.Count == 1)
                {
                    // Простой случай - один контур без дырок
                    var polyline = polylines[0];
                    if (polyline == null || polyline.Count < 3)
                        return (vertices, indices);

                    var simplified = SimplifyPolygon(polyline, 0.5f);
                    if (simplified.Count < 3)
                        return (vertices, indices);

                    // Фильтруем null координаты
                    var coordsList = new List<Coordinate>();
                    for (int i = 0; i < simplified.Count; i++)
                    {
                        var pt = simplified[i];
                        if (pt != null && !double.IsNaN(pt.X) && !double.IsNaN(pt.Y))
                        {
                            coordsList.Add(new Coordinate(pt.X, pt.Y));
                        }
                    }

                    if (coordsList.Count < 3)
                        return (vertices, indices);

                    // Замыкаем кольцо
                    if (!coordsList[0].Equals2D(coordsList[coordsList.Count - 1]))
                    {
                        coordsList.Add(new Coordinate(coordsList[0].X, coordsList[0].Y));
                    }

                    var ring = new LinearRing(coordsList.ToArray());
                    var polygon = new NetTopologySuite.Geometries.Polygon(ring);

                    var triangulator = new ConstrainedDelaunayTriangulator(polygon);
                    var result = triangulator.GetResult();

                    for (int t = 0; t < result.NumGeometries; t++)
                    {
                        var triangle = result.GetGeometryN(t);
                        if (triangle == null)
                            continue;

                        var triangleCoords = triangle.Coordinates;
                        if (triangleCoords.Length < 3)
                            continue;

                        uint baseIdx = (uint)vertices.Count;

                        for (int i = 0; i < 3; i++)
                        {
                            var coord = triangleCoords[i];
                            float x = (float)coord.X;
                            float y = (float)coord.Y;

                            float dx = x - centerX;
                            float dy = y - centerY;
                            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                            float gradientFactor = maxRadius > 0 ? Math.Min(distance / maxRadius, 1.0f) : 0.0f;
                            Color4 vertexColor = new Color4(
                                baseColor.Red + gradientFactor * 0.3f,
                                baseColor.Green + gradientFactor * 0.3f,
                                baseColor.Blue + gradientFactor * 0.3f,
                                baseColor.Alpha
                            );

                            vertices.Add(new Vertex
                            {
                                Position = new Vector3(x, y, z),
                                Normal = new Vector3(0, 0, -1),
                                Color = vertexColor
                            });
                        }

                        indices.Add(baseIdx);
                        indices.Add(baseIdx + 2);
                        indices.Add(baseIdx + 1);
                    }
                }
                else
                {
                    // Сложный случай - несколько контуров (внешний + дырки)
                    // ВАЖНОЕ РЕШЕНИЕ: Если контуров больше одного (есть дырки), НЕ рисуем заливку вообще
                    // Причина: NetTopologySuite.ConstrainedDelaunayTriangulator часто не справляется со сложными
                    // полигонами с дырками и fallback'ит на ear clipping, который НЕ учитывает дырки
                    // Результат: сплошная оранжевая крышка вместо заливки между контурами
                    // РЕШЕНИЕ: Лучше НЕ рисовать заливку, чем рисовать неправильную сплошную крышку

                    
                    return (vertices, indices);
                }

                    /* ЗАКОММЕНТИРОВАНО: Старая логика с полигонами и дырками - не работает надёжно
                    // Находим внешний контур (самый большой по площади)
                    var simplifiedContours = new List<(List<ProjectParserTest.Parsers.Shared.Models.Point> contour, double area)>();

                    foreach (var polyline in polylines)
                    {
                        if (polyline == null || polyline.Count < 3)
                            continue;

                        var simplified = SimplifyPolygon(polyline, 0.5f);
                        if (simplified.Count < 3)
                            continue;

                        // Вычисляем площадь контура
                        double area = 0;
                        for (int i = 0; i < simplified.Count; i++)
                        {
                            int j = (i + 1) % simplified.Count;
                            area += simplified[i].X * simplified[j].Y;
                            area -= simplified[j].X * simplified[i].Y;
                        }
                        area = Math.Abs(area / 2.0);

                        simplifiedContours.Add((simplified, area));
                    }

                    if (simplifiedContours.Count == 0)
                        return (vertices, indices);

                    // Сортируем по площади (самый большой - внешний)
                    simplifiedContours.Sort((a, b) => b.area.CompareTo(a.area));

                    // Создаём внешнее кольцо с проверкой на null
                    var outerContour = simplifiedContours[0].contour;
                    var outerCoordsList = new List<Coordinate>();
                    for (int i = 0; i < outerContour.Count; i++)
                    {
                        var pt = outerContour[i];
                        if (pt != null && !double.IsNaN(pt.X) && !double.IsNaN(pt.Y))
                        {
                            outerCoordsList.Add(new Coordinate(pt.X, pt.Y));
                        }
                    }

                    if (outerCoordsList.Count < 3)
                        return (vertices, indices);

                    // Замыкаем кольцо
                    if (!outerCoordsList[0].Equals2D(outerCoordsList[outerCoordsList.Count - 1]))
                    {
                        outerCoordsList.Add(new Coordinate(outerCoordsList[0].X, outerCoordsList[0].Y));
                    }

                    var outerRing = new LinearRing(outerCoordsList.ToArray());

                    // Создаём внутренние дырки с проверкой на null
                    var holes = new List<LinearRing>();
                    for (int c = 1; c < simplifiedContours.Count; c++)
                    {
                        var holeContour = simplifiedContours[c].contour;
                        var holeCoordsList = new List<Coordinate>();

                        for (int i = 0; i < holeContour.Count; i++)
                        {
                            var pt = holeContour[i];
                            if (pt != null && !double.IsNaN(pt.X) && !double.IsNaN(pt.Y))
                            {
                                holeCoordsList.Add(new Coordinate(pt.X, pt.Y));
                            }
                        }

                        if (holeCoordsList.Count >= 3)
                        {
                            // Замыкаем кольцо
                            if (!holeCoordsList[0].Equals2D(holeCoordsList[holeCoordsList.Count - 1]))
                            {
                                holeCoordsList.Add(new Coordinate(holeCoordsList[0].X, holeCoordsList[0].Y));
                            }

                            holes.Add(new LinearRing(holeCoordsList.ToArray()));
                        }
                    }

                    // Создаём полигон с дырками
                    var polygon = new NetTopologySuite.Geometries.Polygon(outerRing, holes.ToArray());

                    // ВАЖНО: Если полигон с дырками слишком сложный, NetTopologySuite может не справиться
                    // В этом случае просто НЕ рисуем заливку (лучше не рисовать, чем нарисовать сплошную крышку)
                    var triangulator = new ConstrainedDelaunayTriangulator(polygon);
                    NetTopologySuite.Geometries.Geometry result;

                    try
                    {
                        result = triangulator.GetResult();

                        // Проверяем, что результат содержит только треугольники (не ear clipping fallback)
                        // Если NTS не смогла построить constrained Delaunay и использовала ear clipping,
                        // то результат будет содержать сплошную заливку БЕЗ учёта дырок - это НЕ то, что нам нужно
                        bool hasValidTriangulation = true;
                        for (int t = 0; t < result.NumGeometries; t++)
                        {
                            var geom = result.GetGeometryN(t);
                            if (geom == null || geom.Coordinates.Length < 3)
                            {
                                hasValidTriangulation = false;
                                break;
                            }
                        }

                        // Если триангуляция не валидна, не рисуем заливку вообще
                        if (!hasValidTriangulation)
                        {
                            
                            return (vertices, indices);
                        }
                    }
                    catch (Exception ex)
                    {
                        
                        return (vertices, indices);
                    }

                    for (int t = 0; t < result.NumGeometries; t++)
                    {
                        var triangle = result.GetGeometryN(t);
                        if (triangle == null)
                            continue;

                        var triangleCoords = triangle.Coordinates;
                        if (triangleCoords.Length < 3)
                            continue;

                        uint baseIdx = (uint)vertices.Count;

                        for (int i = 0; i < 3; i++)
                        {
                            var coord = triangleCoords[i];
                            float x = (float)coord.X;
                            float y = (float)coord.Y;

                            float dx = x - centerX;
                            float dy = y - centerY;
                            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                            float gradientFactor = maxRadius > 0 ? Math.Min(distance / maxRadius, 1.0f) : 0.0f;
                            Color4 vertexColor = new Color4(
                                baseColor.Red + gradientFactor * 0.3f,
                                baseColor.Green + gradientFactor * 0.3f,
                                baseColor.Blue + gradientFactor * 0.3f,
                                baseColor.Alpha
                            );

                            vertices.Add(new Vertex
                            {
                                Position = new Vector3(x, y, z),
                                Normal = new Vector3(0, 0, -1),
                                Color = vertexColor
                            });
                        }

                        indices.Add(baseIdx);
                        indices.Add(baseIdx + 2);
                        indices.Add(baseIdx + 1);
                    }
                }

                
                */
            }
            catch (Exception ex)
            {
                
                return (vertices, indices);

                /* ЗАКОММЕНТИРОВАНО: Fallback к ear clipping - НЕ работает для полигонов с дырками
                // Fallback к встроенному ear clipping
                vertices.Clear();
                indices.Clear();
                foreach (var polyline in polylines)
                {
                    if (polyline == null || polyline.Count < 3)
                        continue;

                    var simplified = SimplifyPolygon(polyline, 0.5f);
                    if (simplified.Count >= 3)
                    {
                        AddFilledPolygonWithGradient(vertices, indices, simplified, z, baseColor, new Vector3(0, 0, 1), centerX, centerY, maxRadius, 0.5f);
                    }
                }
                */
            }

            return (vertices, indices);
        }

        /// <summary>
        /// Создаёт заполненную геометрию для текущего слоя (красная заливка как в 2D превью)
        /// </summary>
        public (List<Vertex> vertices, List<uint> indices) BuildCurrentLayerFilledGeometry(
            Layer layer,
            float z,
            Color4 fillColor,
            float centerX,
            float centerY,
            float maxRadius)
        {
            var allVertices = new List<Vertex>();
            var allIndices = new List<uint>();

            if (layer == null || layer.Regions == null)
                return (allVertices, allIndices);

            // Собираем контуры с infill
            var infillRegions = new HashSet<GeometryRegion>
            {
                GeometryRegion.Infill,
                GeometryRegion.Upskin,
                GeometryRegion.Downskin,
                GeometryRegion.InfillRegionPreview,
                GeometryRegion.UpskinRegionPreview,
                GeometryRegion.DownskinRegionPreview
            };

            // Находим части с infill
            var infillParts = new HashSet<int?>();
            foreach (var region in layer.Regions)
            {
                if (infillRegions.Contains(region.GeometryRegion) || region.Type == BlockType.Hatch)
                {
                    infillParts.Add(region.Part?.Id);
                }
            }

            // Группируем контуры по partId
            var partGroups = new Dictionary<int?, List<List<ProjectParserTest.Parsers.Shared.Models.Point>>>();
            foreach (var region in layer.Regions)
            {
                if (region.PolyLines == null)
                    continue;

                if (region.GeometryRegion == GeometryRegion.Contour && infillParts.Contains(region.Part?.Id))
                {
                    int? partId = region.Part?.Id ?? 0;
                    if (!partGroups.ContainsKey(partId))
                        partGroups[partId] = new List<List<ProjectParserTest.Parsers.Shared.Models.Point>>();

                    foreach (var polyline in region.PolyLines)
                    {
                        if (polyline.Points != null && polyline.Points.Count >= 3)
                        {
                            partGroups[partId].Add(polyline.Points);
                        }
                    }
                }
            }

            // Триангулируем каждую часть и собираем в общий список
            foreach (var partGroup in partGroups)
            {
                var polylines = partGroup.Value;
                if (polylines.Count == 0)
                    continue;

                var (vertices, indices) = TriangulateWithTriangleNet(
                    polylines,
                    z,
                    fillColor,
                    centerX,
                    centerY,
                    maxRadius
                );

                if (vertices.Count > 0)
                {
                    uint baseIndex = (uint)allVertices.Count;
                    allVertices.AddRange(vertices);
                    allIndices.AddRange(indices.Select(i => i + baseIndex));
                }
            }

            return (allVertices, allIndices);
        }

        /// <summary>
        /// Создаёт геометрию контуров для текущего слоя (голубые линии)
        /// </summary>
        public (List<Vertex> vertices, List<uint> indices) BuildCurrentLayerContourGeometry(
            Layer layer,
            float zPosition,
            Color4 hatchColor,
            Color4 contourColor)
        {
            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            if (layer?.Regions == null)
                return (vertices, indices);

            foreach (var region in layer.Regions)
            {
                if (region.PolyLines == null)
                    continue;

                // НОВАЯ ЛОГИКА: Рисуем ТОЛЬКО контуры (GeometryRegion.Contour*) И Hatch (Type == BlockType.Hatch) линиями
                bool isContour = region.GeometryRegion == GeometryRegion.Contour ||
                                 region.GeometryRegion == GeometryRegion.ContourUpskin ||
                                 region.GeometryRegion == GeometryRegion.ContourDownskin ||
                                 region.GeometryRegion == GeometryRegion.Edges;

                bool isHatch = region.Type == BlockType.Hatch;

                if (isContour || isHatch)
                {
                    // Единый оранжевый цвет для всех фигур
                    // R channel = 1.0 означает "деталь" для шейдера (применяется освещение и оранжевый цвет)
                    // Alpha = partId (1.0/255 минимальный для распознавания как деталь)
                    float partIdEncoded = region.Part != null ? (float)region.Part.Id / 255.0f : 1.0f / 255.0f;
                    Color4 regionColor = new Color4(1.0f, 0.0f, 0.0f, partIdEncoded);

                    foreach (var polyline in region.PolyLines)
                    {
                        if (polyline.Points != null && polyline.Points.Count >= 2)
                        {
                            AddThinLine(vertices, indices, polyline.Points, zPosition, regionColor, 0.2f);
                        }
                    }
                }
            }

            return (vertices, indices);
        }

        /// <summary>
        /// Строит контуры и Hatch для ОДНОГО слоя (для кеширования)
        /// Цвета определяются автоматически: все детали оранжевые
        /// </summary>
        public (List<Vertex> vertices, List<uint> indices) BuildSingleLayerContoursAndHatch(
            Project project,
            int layerIndex,
            float zPosition)
        {
            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            if (project?.Layers == null || layerIndex >= project.Layers.Count)
                return (vertices, indices);

            var layer = project.Layers[layerIndex];
            if (layer?.Regions == null)
                return (vertices, indices);

            uint baseIndex = 0;

            foreach (var region in layer.Regions)
            {
                if (region.PolyLines == null)
                    continue;

                // Рисуем ТОЛЬКО контуры И Hatch
                bool isContour = region.GeometryRegion == GeometryRegion.Contour ||
                                 region.GeometryRegion == GeometryRegion.ContourUpskin ||
                                 region.GeometryRegion == GeometryRegion.ContourDownskin ||
                                 region.GeometryRegion == GeometryRegion.Edges;

                bool isHatch = region.Type == BlockType.Hatch;

                if (isContour || isHatch)
                {
                    // Единый оранжевый цвет для всех фигур
                    // R channel = 1.0 означает "деталь" для шейдера (применяется освещение и оранжевый цвет)
                    // Alpha = partId (1.0/255 минимальный для распознавания как деталь)
                    float partIdEncoded = region.Part != null ? (float)region.Part.Id / 255.0f : 1.0f / 255.0f;
                    Color4 regionColor = new Color4(1.0f, 0.0f, 0.0f, partIdEncoded);

                    foreach (var polyline in region.PolyLines)
                    {
                        if (polyline.Points != null && polyline.Points.Count >= 2)
                        {
                            // Создаём временные списки для этой линии
                            var tempVertices = new List<Vertex>();
                            var tempIndices = new List<uint>();

                            AddThinLine(tempVertices, tempIndices, polyline.Points, zPosition, regionColor, 0.2f);

                            // Добавляем к общим спискам со смещением индексов
                            vertices.AddRange(tempVertices);
                            indices.AddRange(tempIndices.Select(idx => idx + baseIndex));

                            baseIndex = (uint)vertices.Count;
                        }
                    }
                }
            }

            return (vertices, indices);
        }

        /// <summary>
        /// ОПТИМИЗИРОВАННЫЙ метод: строит контуры и Hatch для ВСЕХ слоёв от 0 до maxLayerIndex
        /// Цвета определяются автоматически: контуры - синие, Hatch - серые
        /// </summary>
        public (List<Vertex> vertices, List<uint> indices) BuildAllLayersContoursAndHatch(
            Project project,
            int maxLayerIndex,
            float centerX,
            float centerY,
            float maxRadius)
        {
            var allVertices = new List<Vertex>();
            var allIndices = new List<uint>();

            if (project?.Layers == null)
                return (allVertices, allIndices);

            // Проходим по всем слоям от 0 до maxLayerIndex
            // layer.Height содержит абсолютную Z позицию в мм
            float layerThickness = project.GetLayerThicknessInMillimeters();
            if (layerThickness <= 0) layerThickness = 0.03f; // default 30 microns

            for (int i = 0; i <= maxLayerIndex && i < project.Layers.Count; i++)
            {
                var layer = project.Layers[i];
                if (layer?.Regions == null)
                {
                    continue;
                }

                // Используем layer.Height как абсолютную Z позицию, если валидна
                float zPosition = (float)layer.Height;
                if (zPosition < 0.001f)
                    zPosition = i * layerThickness;

                uint baseIndex = (uint)allVertices.Count;

                foreach (var region in layer.Regions)
                {
                    if (region.PolyLines == null)
                        continue;

                    // Рисуем ТОЛЬКО контуры И Hatch
                    bool isContour = region.GeometryRegion == GeometryRegion.Contour ||
                                     region.GeometryRegion == GeometryRegion.ContourUpskin ||
                                     region.GeometryRegion == GeometryRegion.ContourDownskin ||
                                     region.GeometryRegion == GeometryRegion.Edges;

                    bool isHatch = region.Type == BlockType.Hatch;

                    if (isContour || isHatch)
                    {
                        // Единый оранжевый цвет для всех фигур
                        // R channel = 1.0 означает "деталь" для шейдера (применяется освещение и оранжевый цвет)
                        // Alpha = partId (1.0/255 минимальный для распознавания как деталь)
                        float partIdEncoded = region.Part != null ? (float)region.Part.Id / 255.0f : 1.0f / 255.0f;
                        Color4 regionColor = new Color4(1.0f, 0.0f, 0.0f, partIdEncoded);

                        foreach (var polyline in region.PolyLines)
                        {
                            if (polyline.Points != null && polyline.Points.Count >= 2)
                            {
                                // Создаём временные списки для этой линии
                                var tempVertices = new List<Vertex>();
                                var tempIndices = new List<uint>();

                                AddThinLine(tempVertices, tempIndices, polyline.Points, zPosition, regionColor, 0.2f);

                                // Добавляем к общим спискам со смещением индексов
                                allVertices.AddRange(tempVertices);
                                allIndices.AddRange(tempIndices.Select(idx => idx + baseIndex));

                                baseIndex = (uint)allVertices.Count;
                            }
                        }
                    }
                }
            }

            return (allVertices, allIndices);
        }

        #endregion
    }
}
