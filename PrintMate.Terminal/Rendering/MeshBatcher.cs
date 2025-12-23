using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace PrintMate.Terminal.Rendering
{
    /// <summary>
    /// Батчер для объединения множества мелких мешей в один большой
    /// Уменьшает количество Draw calls, что значительно ускоряет рендеринг
    /// </summary>
    public class MeshBatcher
    {
        private readonly Device _device;
        private readonly List<Vertex> _batchedVertices = new List<Vertex>();
        private readonly List<uint> _batchedIndices = new List<uint>();

        // Максимальный размер батча (ограничен размером индексного буфера uint)
        private const int MaxVerticesPerBatch = 1_000_000;
        private const int MaxIndicesPerBatch = 3_000_000;

        public MeshBatcher(Device device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>
        /// Начинает новый батч
        /// </summary>
        public void BeginBatch()
        {
            _batchedVertices.Clear();
            _batchedIndices.Clear();
        }

        /// <summary>
        /// Добавляет геометрию в текущий батч
        /// </summary>
        public bool AddToBatch(Vertex[] vertices, uint[] indices)
        {
            if (vertices == null || indices == null)
                return false;

            // Проверяем, не превысит ли добавление лимиты
            if (_batchedVertices.Count + vertices.Length > MaxVerticesPerBatch ||
                _batchedIndices.Count + indices.Length > MaxIndicesPerBatch)
            {
                return false; // Батч полон, нужно создать новый
            }

            uint baseIndex = (uint)_batchedVertices.Count;

            // Добавляем вершины
            _batchedVertices.AddRange(vertices);

            // Добавляем индексы со смещением
            for (int i = 0; i < indices.Length; i++)
            {
                _batchedIndices.Add(indices[i] + baseIndex);
            }

            return true;
        }

        /// <summary>
        /// Добавляет геометрию из List в текущий батч
        /// </summary>
        public bool AddToBatch(List<Vertex> vertices, List<uint> indices)
        {
            if (vertices == null || indices == null)
                return false;

            if (_batchedVertices.Count + vertices.Count > MaxVerticesPerBatch ||
                _batchedIndices.Count + indices.Count > MaxIndicesPerBatch)
            {
                return false;
            }

            uint baseIndex = (uint)_batchedVertices.Count;

            _batchedVertices.AddRange(vertices);

            for (int i = 0; i < indices.Count; i++)
            {
                _batchedIndices.Add(indices[i] + baseIndex);
            }

            return true;
        }

        /// <summary>
        /// Завершает батч и создаёт итоговый меш
        /// </summary>
        public CliMesh EndBatch()
        {
            if (_batchedVertices.Count == 0 || _batchedIndices.Count == 0)
                return null;

            var vertices = _batchedVertices.ToArray();
            var indices = _batchedIndices.ToArray();

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

            var mesh = new CliMesh
            {
                VertexBuffer = vertexBuffer,
                IndexBuffer = indexBuffer,
                VertexCount = vertices.Length,
                IndexCount = indices.Length
            };

            // Очищаем временные данные
            _batchedVertices.Clear();
            _batchedIndices.Clear();

            return mesh;
        }

        /// <summary>
        /// Возвращает текущий размер батча
        /// </summary>
        public (int vertices, int indices) GetCurrentBatchSize()
        {
            return (_batchedVertices.Count, _batchedIndices.Count);
        }

        /// <summary>
        /// Проверяет, пуст ли текущий батч
        /// </summary>
        public bool IsEmpty => _batchedVertices.Count == 0;

        /// <summary>
        /// Проверяет, заполнен ли батч
        /// </summary>
        public bool IsFull => _batchedVertices.Count >= MaxVerticesPerBatch * 0.9 ||
                             _batchedIndices.Count >= MaxIndicesPerBatch * 0.9;
    }

    /// <summary>
    /// Статистика рендеринга для профилирования
    /// </summary>
    public class RenderStats
    {
        public int DrawCalls { get; set; }
        public int TotalVertices { get; set; }
        public int TotalIndices { get; set; }
        public int CulledObjects { get; set; }
        public double FrameTimeMs { get; set; }
        public int LOD0Objects { get; set; }
        public int LOD1Objects { get; set; }
        public int LOD2Objects { get; set; }

        public void Reset()
        {
            DrawCalls = 0;
            TotalVertices = 0;
            TotalIndices = 0;
            CulledObjects = 0;
            FrameTimeMs = 0;
            LOD0Objects = 0;
            LOD1Objects = 0;
            LOD2Objects = 0;
        }

        public override string ToString()
        {
            return $"Draw: {DrawCalls}, Verts: {TotalVertices:N0}, Culled: {CulledObjects}, " +
                   $"LOD[0:{LOD0Objects} 1:{LOD1Objects} 2:{LOD2Objects}], Time: {FrameTimeMs:F2}ms";
        }
    }
}
