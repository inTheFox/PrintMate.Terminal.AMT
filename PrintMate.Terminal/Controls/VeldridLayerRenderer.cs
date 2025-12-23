using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.SPIRV;
using ProjectParserTest.Parsers.Shared.Models;
using ProjectParserTest.Parsers.Shared.Enums;
using PrintMate.Terminal.Parsers.Shared.Models;
using CliRegion = ProjectParserTest.Parsers.Shared.Models.Region;

namespace PrintMate.Terminal.Controls
{
    /// <summary>
    /// GPU рендерер на Veldrid с OpenGL бэкендом.
    /// Оптимизирован для Intel UHD графики.
    /// </summary>
    public class VeldridLayerRenderer : IDisposable
    {
        #region Структуры для GPU

        [StructLayout(LayoutKind.Sequential)]
        private struct VertexPositionColor
        {
            public Vector3 Position;
            public RgbaFloat Color;

            public VertexPositionColor(Vector3 position, RgbaFloat color)
            {
                Position = position;
                Color = color;
            }

            public static uint SizeInBytes => (uint)Marshal.SizeOf<VertexPositionColor>();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UniformBuffer
        {
            public Matrix4x4 MVP;
        }

        #endregion

        #region Константы

        private const float FIELD_SIZE = 320f;
        private const float HALF_FIELD = FIELD_SIZE / 2f;
        private const int MAX_VISIBLE_LAYERS = 15;

        // Цвета
        private static readonly RgbaFloat BackgroundColor = new RgbaFloat(0.12f, 0.12f, 0.12f, 1f);
        private static readonly RgbaFloat PlatformColor = new RgbaFloat(0.2f, 0.2f, 0.2f, 1f);
        private static readonly RgbaFloat GridColor = new RgbaFloat(0.25f, 0.25f, 0.25f, 1f);
        private static readonly RgbaFloat AxisColor = new RgbaFloat(0.35f, 0.35f, 0.35f, 1f);
        private static readonly RgbaFloat ContourColor = new RgbaFloat(1f, 0.4f, 0.12f, 1f);
        private static readonly RgbaFloat HatchColor = new RgbaFloat(0.78f, 0.31f, 0.08f, 1f);
        private static readonly RgbaFloat PreviousLayersColor = new RgbaFloat(0.4f, 0.4f, 0.4f, 1f);

        #endregion

        #region Приватные поля

        private GraphicsDevice _graphicsDevice;
        private CommandList _commandList;
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _uniformBuffer;
        private Pipeline _pipeline;
        private ResourceSet _resourceSet;
        private ResourceLayout _resourceLayout;
        private Framebuffer _framebuffer;
        private Texture _offscreenTexture;
        private Texture _depthTexture;

        private Project _currentProject;
        private int _currentLayerIndex;
        private List<float> _layerZPositions;

        // Геометрия
        private List<VertexPositionColor> _vertices = new List<VertexPositionColor>();
        private bool _geometryDirty = true;
        private uint _vertexCount;

        // Камера
        private float _rotationAngle = 45f;
        private float _elevationAngle = 30f;
        private float _zoom = 1.5f;
        private float _panX = 0f;
        private float _panY = 0f;

        private int _width = 800;
        private int _height = 600;
        private bool _isInitialized = false;

        #endregion

        #region Публичные свойства

        public float RotationAngle
        {
            get => _rotationAngle;
            set { _rotationAngle = value % 360f; }
        }

        public float ElevationAngle
        {
            get => _elevationAngle;
            set { _elevationAngle = Math.Clamp(value, 5f, 85f); }
        }

        public float Zoom
        {
            get => _zoom;
            set { _zoom = Math.Clamp(value, 0.1f, 20f); }
        }

        public bool IsInitialized => _isInitialized;

        #endregion

        #region Шейдеры

        private const string VertexShaderCode = @"
#version 450

layout(location = 0) in vec3 Position;
layout(location = 1) in vec4 Color;

layout(location = 0) out vec4 fsin_Color;

layout(set = 0, binding = 0) uniform UniformBuffer
{
    mat4 MVP;
};

void main()
{
    gl_Position = MVP * vec4(Position, 1.0);
    fsin_Color = Color;
}
";

        private const string FragmentShaderCode = @"
#version 450

layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;

void main()
{
    fsout_Color = fsin_Color;
}
";

        #endregion

        #region Инициализация

        public void Initialize(IntPtr hwnd, int width, int height)
        {
            if (_isInitialized) return;

            _width = width;
            _height = height;

            try
            {
                // Создаём GraphicsDevice с D3D11 бэкендом (лучшая совместимость на Windows)
                var options = new GraphicsDeviceOptions
                {
                    PreferStandardClipSpaceYDirection = true,
                    PreferDepthRangeZeroToOne = true,
                    SyncToVerticalBlank = true,
                    SwapchainDepthFormat = PixelFormat.D32_Float_S8_UInt
                };

                var swapchainDesc = new SwapchainDescription(
                    SwapchainSource.CreateWin32(hwnd, IntPtr.Zero),
                    (uint)width,
                    (uint)height,
                    PixelFormat.D32_Float_S8_UInt,
                    true);

                // Используем D3D11 (лучшая совместимость с Intel UHD на Windows)
                _graphicsDevice = GraphicsDevice.CreateD3D11(options, swapchainDesc);

                CreateResources();
                _isInitialized = true;

                Console.WriteLine($"[Veldrid] Initialized with {_graphicsDevice.BackendType} backend");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Veldrid] Initialization failed: {ex.Message}");
                throw;
            }
        }

        private void CreateResources()
        {
            var factory = _graphicsDevice.ResourceFactory;

            // Создаём буфер вершин (начальный размер)
            _vertexBuffer = factory.CreateBuffer(new BufferDescription(
                1024 * VertexPositionColor.SizeInBytes,
                BufferUsage.VertexBuffer | BufferUsage.Dynamic));

            // Uniform буфер для MVP матрицы
            _uniformBuffer = factory.CreateBuffer(new BufferDescription(
                64, // sizeof(Matrix4x4)
                BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            // Создаём шейдеры
            var vertexShaderDesc = new ShaderDescription(ShaderStages.Vertex,
                System.Text.Encoding.UTF8.GetBytes(VertexShaderCode), "main");
            var fragmentShaderDesc = new ShaderDescription(ShaderStages.Fragment,
                System.Text.Encoding.UTF8.GetBytes(FragmentShaderCode), "main");

            var shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

            // Resource layout
            _resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("UniformBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            // Resource set
            _resourceSet = factory.CreateResourceSet(new ResourceSetDescription(_resourceLayout, _uniformBuffer));

            // Pipeline
            var pipelineDesc = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription(
                    cullMode: FaceCullMode.None,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false),
                PrimitiveTopology = PrimitiveTopology.LineList,
                ResourceLayouts = new[] { _resourceLayout },
                ShaderSet = new ShaderSetDescription(
                    new[]
                    {
                        new VertexLayoutDescription(
                            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4))
                    },
                    shaders),
                Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription
            };

            _pipeline = factory.CreateGraphicsPipeline(pipelineDesc);

            // Command list
            _commandList = factory.CreateCommandList();
        }

        #endregion

        #region Публичные методы

        public void LoadProject(Project project)
        {
            _currentProject = project;
            _currentLayerIndex = 0;
            _geometryDirty = true;

            if (project?.Layers != null && project.Layers.Count > 0)
            {
                // Предварительно вычисляем Z-позиции
                _layerZPositions = new List<float>(project.Layers.Count);
                float currentZ = 0;
                foreach (var layer in project.Layers)
                {
                    float height = layer.Height > 0 ? (float)layer.Height : 0.05f;
                    currentZ += height;
                    _layerZPositions.Add(currentZ);
                }

                AutoFitZoom();
            }
        }

        public void SetCurrentLayer(int layerNumber)
        {
            if (_currentProject?.Layers == null) return;

            int newIndex = Math.Clamp(layerNumber - 1, 0, _currentProject.Layers.Count - 1);
            if (newIndex != _currentLayerIndex)
            {
                _currentLayerIndex = newIndex;
                _geometryDirty = true;
            }
        }

        public void Rotate(float deltaAzimuth, float deltaElevation)
        {
            RotationAngle += deltaAzimuth;
            ElevationAngle += deltaElevation;
        }

        public void Pan(float deltaX, float deltaY)
        {
            _panX += deltaX * 0.005f / _zoom;
            _panY -= deltaY * 0.005f / _zoom;
        }

        public void ZoomBy(float delta)
        {
            Zoom *= (1f + delta * 0.001f);
        }

        public void ResetCamera()
        {
            _rotationAngle = 45f;
            _elevationAngle = 30f;
            _panX = 0f;
            _panY = 0f;
            AutoFitZoom();
        }

        public void Resize(int width, int height)
        {
            if (!_isInitialized || width <= 0 || height <= 0) return;

            _width = width;
            _height = height;
            _graphicsDevice.ResizeMainWindow((uint)width, (uint)height);
        }

        public void Render()
        {
            if (!_isInitialized) return;

            try
            {
                // Обновляем геометрию если нужно
                if (_geometryDirty)
                {
                    RebuildGeometry();
                    _geometryDirty = false;
                }

                // Обновляем MVP матрицу
                UpdateMVP();

                // Рендерим
                _commandList.Begin();
                _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
                _commandList.ClearColorTarget(0, BackgroundColor);
                _commandList.ClearDepthStencil(1f);

                if (_vertexCount > 0)
                {
                    _commandList.SetPipeline(_pipeline);
                    _commandList.SetVertexBuffer(0, _vertexBuffer);
                    _commandList.SetGraphicsResourceSet(0, _resourceSet);
                    _commandList.Draw(_vertexCount);
                }

                _commandList.End();
                _graphicsDevice.SubmitCommands(_commandList);
                _graphicsDevice.SwapBuffers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Veldrid] Render error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _pipeline?.Dispose();
            _resourceSet?.Dispose();
            _resourceLayout?.Dispose();
            _vertexBuffer?.Dispose();
            _uniformBuffer?.Dispose();
            _commandList?.Dispose();
            _graphicsDevice?.Dispose();
            _isInitialized = false;
        }

        #endregion

        #region Приватные методы

        private void UpdateMVP()
        {
            float aspect = (float)_width / _height;

            // View матрица (изометрическая камера)
            float radRot = _rotationAngle * MathF.PI / 180f;
            float radElev = _elevationAngle * MathF.PI / 180f;

            float camDist = 500f / _zoom;
            float camX = camDist * MathF.Cos(radElev) * MathF.Sin(radRot);
            float camY = camDist * MathF.Cos(radElev) * MathF.Cos(radRot);
            float camZ = camDist * MathF.Sin(radElev);

            var eye = new Vector3(camX + _panX * 100, camY + _panY * 100, camZ);
            var target = new Vector3(_panX * 100, _panY * 100, 0);
            var up = Vector3.UnitZ;

            var view = Matrix4x4.CreateLookAt(eye, target, up);

            // Projection матрица (ортогональная для изометрии)
            float size = 200f / _zoom;
            var projection = Matrix4x4.CreateOrthographic(size * aspect, size, 0.1f, 2000f);

            var mvp = view * projection;

            _graphicsDevice.UpdateBuffer(_uniformBuffer, 0, mvp);
        }

        private void RebuildGeometry()
        {
            _vertices.Clear();

            // Платформа
            AddPlatform();

            // Сетка
            AddGrid();

            // Слои
            if (_currentProject?.Layers != null && _layerZPositions != null)
            {
                AddLayers();
            }

            // Обновляем буфер вершин
            if (_vertices.Count > 0)
            {
                var vertexArray = _vertices.ToArray();
                uint requiredSize = (uint)vertexArray.Length * VertexPositionColor.SizeInBytes;

                // Пересоздаём буфер если нужно больше места
                if (_vertexBuffer.SizeInBytes < requiredSize)
                {
                    _vertexBuffer.Dispose();
                    _vertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(
                        requiredSize,
                        BufferUsage.VertexBuffer | BufferUsage.Dynamic));
                }

                _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, vertexArray);
                _vertexCount = (uint)vertexArray.Length;
            }
            else
            {
                _vertexCount = 0;
            }
        }

        private void AddPlatform()
        {
            // Границы платформы
            AddLine(-HALF_FIELD, -HALF_FIELD, 0, HALF_FIELD, -HALF_FIELD, 0, PlatformColor);
            AddLine(HALF_FIELD, -HALF_FIELD, 0, HALF_FIELD, HALF_FIELD, 0, PlatformColor);
            AddLine(HALF_FIELD, HALF_FIELD, 0, -HALF_FIELD, HALF_FIELD, 0, PlatformColor);
            AddLine(-HALF_FIELD, HALF_FIELD, 0, -HALF_FIELD, -HALF_FIELD, 0, PlatformColor);
        }

        private void AddGrid()
        {
            // Центральные линии
            AddLine(-HALF_FIELD, 0, 0, HALF_FIELD, 0, 0, GridColor);
            AddLine(0, -HALF_FIELD, 0, 0, HALF_FIELD, 0, GridColor);

            // Оси
            AddLine(0, 0, 0, HALF_FIELD * 0.8f, 0, 0, AxisColor);
            AddLine(0, 0, 0, 0, HALF_FIELD * 0.8f, 0, AxisColor);
        }

        private void AddLayers()
        {
            // Определяем какие слои рисовать
            int historyCount = Math.Min(MAX_VISIBLE_LAYERS - 1, _currentLayerIndex);
            int step = historyCount > 0 ? Math.Max(1, _currentLayerIndex / historyCount) : 1;

            // Исторические слои (упрощённо)
            for (int i = 0; i < _currentLayerIndex; i += step)
            {
                float zPos = _layerZPositions[i];
                AddLayerContoursOnly(_currentProject.Layers[i], zPos, PreviousLayersColor);
            }

            // Текущий слой полностью
            if (_currentLayerIndex < _currentProject.Layers.Count)
            {
                float zPos = _layerZPositions[_currentLayerIndex];
                AddLayerFull(_currentProject.Layers[_currentLayerIndex], zPos);
            }
        }

        private void AddLayerContoursOnly(Layer layer, float z, RgbaFloat color)
        {
            if (layer.Regions == null) return;

            foreach (var region in layer.Regions)
            {
                if (region.Type == BlockType.Hatch) continue;
                if (region.PolyLines == null) continue;

                foreach (var polyLine in region.PolyLines)
                {
                    AddPolyLineSimple(polyLine, z, color);
                }
            }
        }

        private void AddLayerFull(Layer layer, float z)
        {
            if (layer.Regions == null) return;

            foreach (var region in layer.Regions)
            {
                if (region.PolyLines == null) continue;

                var color = GetRegionColor(region);
                bool isHatch = region.Type == BlockType.Hatch;
                int step = isHatch ? 3 : 1;

                foreach (var polyLine in region.PolyLines)
                {
                    AddPolyLine(polyLine, z, color, step);
                }
            }
        }

        private void AddPolyLineSimple(PolyLine polyLine, float z, RgbaFloat color)
        {
            var points = polyLine.Points;
            if (points == null || points.Count < 2) return;

            int step = Math.Max(1, points.Count / 20);

            for (int i = step; i < points.Count; i += step)
            {
                var p1 = points[i - step];
                var p2 = points[i];
                AddLine(p1.X, p1.Y, z, p2.X, p2.Y, z, color);
            }

            // Замыкаем
            if (points.Count > 2)
            {
                var last = points[points.Count - 1];
                var first = points[0];
                AddLine(last.X, last.Y, z, first.X, first.Y, z, color);
            }
        }

        private void AddPolyLine(PolyLine polyLine, float z, RgbaFloat color, int step)
        {
            var points = polyLine.Points;
            if (points == null || points.Count < 2) return;

            for (int i = step; i < points.Count; i += step)
            {
                var p1 = points[i - step];
                var p2 = points[i];
                AddLine(p1.X, p1.Y, z, p2.X, p2.Y, z, color);
            }

            // Добавляем последний сегмент если пропустили
            int lastDrawn = ((points.Count - 1) / step) * step;
            if (lastDrawn < points.Count - 1)
            {
                var p1 = points[lastDrawn];
                var p2 = points[points.Count - 1];
                AddLine(p1.X, p1.Y, z, p2.X, p2.Y, z, color);
            }

            // Замыкаем контур (для не-штриховки)
            if (step == 1 && points.Count > 2)
            {
                var last = points[points.Count - 1];
                var first = points[0];
                AddLine(last.X, last.Y, z, first.X, first.Y, z, color);
            }
        }

        private void AddLine(float x1, float y1, float z1, float x2, float y2, float z2, RgbaFloat color)
        {
            _vertices.Add(new VertexPositionColor(new Vector3(x1, y1, z1), color));
            _vertices.Add(new VertexPositionColor(new Vector3(x2, y2, z2), color));
        }

        private RgbaFloat GetRegionColor(CliRegion region)
        {
            return region.GeometryRegion switch
            {
                GeometryRegion.Contour => ContourColor,
                GeometryRegion.ContourUpskin => new RgbaFloat(1f, 0.59f, 0.2f, 1f),
                GeometryRegion.ContourDownskin => new RgbaFloat(1f, 0.47f, 0.16f, 1f),
                GeometryRegion.Infill => HatchColor,
                GeometryRegion.Upskin => new RgbaFloat(0.86f, 0.39f, 0.12f, 1f),
                GeometryRegion.Downskin => new RgbaFloat(0.71f, 0.31f, 0.08f, 1f),
                _ => new RgbaFloat(0.59f, 0.59f, 0.59f, 1f)
            };
        }

        private void AutoFitZoom()
        {
            if (_currentProject?.Layers == null) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

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
                _zoom = 200f / projectSize;
            }
        }

        #endregion
    }
}
