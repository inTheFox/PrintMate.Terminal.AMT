using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpDX;

namespace PrintMate.Terminal.Rendering
{
    /// <summary>
    /// Высокопроизводительные unsafe операции для построения 3D геометрии.
    /// Используется для максимальной производительности, сопоставимой с C++.
    /// </summary>
    public static unsafe class UnsafeGeometryOps
    {
        // Пул массивов для переиспользования памяти (уменьшает GC pressure)
        private static readonly ArrayPool<Vertex> _vertexPool = ArrayPool<Vertex>.Create(maxArrayLength: 1024 * 1024, maxArraysPerBucket: 16);
        private static readonly ArrayPool<uint> _indexPool = ArrayPool<uint>.Create(maxArrayLength: 4 * 1024 * 1024, maxArraysPerBucket: 16);

        /// <summary>
        /// Контекст для построения геометрии с предаллоцированными буферами
        /// </summary>
        public sealed class GeometryContext : IDisposable
        {
            public Vertex[] Vertices;
            public uint[] Indices;
            public int VertexCount;
            public int IndexCount;

            private readonly int _vertexCapacity;
            private readonly int _indexCapacity;
            private bool _disposed;

            public GeometryContext(int vertexCapacity = 65536, int indexCapacity = 262144)
            {
                _vertexCapacity = vertexCapacity;
                _indexCapacity = indexCapacity;
                Vertices = _vertexPool.Rent(vertexCapacity);
                Indices = _indexPool.Rent(indexCapacity);
                VertexCount = 0;
                IndexCount = 0;
            }

            public void Clear()
            {
                VertexCount = 0;
                IndexCount = 0;
            }

            public void EnsureVertexCapacity(int additionalCount)
            {
                if (VertexCount + additionalCount > Vertices.Length)
                {
                    int newCapacity = Math.Max(Vertices.Length * 2, VertexCount + additionalCount);
                    var newArray = _vertexPool.Rent(newCapacity);
                    Array.Copy(Vertices, newArray, VertexCount);
                    _vertexPool.Return(Vertices);
                    Vertices = newArray;
                }
            }

            public void EnsureIndexCapacity(int additionalCount)
            {
                if (IndexCount + additionalCount > Indices.Length)
                {
                    int newCapacity = Math.Max(Indices.Length * 2, IndexCount + additionalCount);
                    var newArray = _indexPool.Rent(newCapacity);
                    Array.Copy(Indices, newArray, IndexCount);
                    _indexPool.Return(Indices);
                    Indices = newArray;
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    if (Vertices != null)
                    {
                        _vertexPool.Return(Vertices, clearArray: false);
                        Vertices = null;
                    }
                    if (Indices != null)
                    {
                        _indexPool.Return(Indices, clearArray: false);
                        Indices = null;
                    }
                }
            }
        }

        /// <summary>
        /// Быстрое добавление вершины через unsafe pointer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddVertex(GeometryContext ctx, Vector3 position, Vector3 normal, Color4 color)
        {
            ctx.EnsureVertexCapacity(1);
            ctx.Vertices[ctx.VertexCount++] = new Vertex(position, normal, color);
        }

        /// <summary>
        /// Быстрое добавление вершин для quad (4 вершины + 6 индексов) с помощью unsafe кода
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddQuadUnsafe(
            GeometryContext ctx,
            Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
            Vector3 normal,
            Color4 color)
        {
            ctx.EnsureVertexCapacity(4);
            ctx.EnsureIndexCapacity(6);

            uint baseIndex = (uint)ctx.VertexCount;

            // Unsafe прямая запись в массив
            fixed (Vertex* vertexPtr = &ctx.Vertices[ctx.VertexCount])
            {
                vertexPtr[0] = new Vertex(p0, normal, color);
                vertexPtr[1] = new Vertex(p1, normal, color);
                vertexPtr[2] = new Vertex(p2, normal, color);
                vertexPtr[3] = new Vertex(p3, normal, color);
            }
            ctx.VertexCount += 4;

            fixed (uint* indexPtr = &ctx.Indices[ctx.IndexCount])
            {
                // Первый треугольник
                indexPtr[0] = baseIndex;
                indexPtr[1] = baseIndex + 1;
                indexPtr[2] = baseIndex + 2;
                // Второй треугольник
                indexPtr[3] = baseIndex;
                indexPtr[4] = baseIndex + 2;
                indexPtr[5] = baseIndex + 3;
            }
            ctx.IndexCount += 6;
        }

        /// <summary>
        /// Быстрое добавление вертикальной стенки между двумя точками
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddWallSegmentUnsafe(
            GeometryContext ctx,
            float x1, float y1, float x2, float y2,
            float zBottom, float zTop,
            Color4 color)
        {
            // Вычисляем нормаль
            float dx = x2 - x1;
            float dy = y2 - y1;
            float len = MathF.Sqrt(dx * dx + dy * dy);

            Vector3 normal;
            if (len > 0.0001f)
            {
                float invLen = 1.0f / len;
                normal = new Vector3(-dy * invLen, dx * invLen, 0);
            }
            else
            {
                normal = new Vector3(1, 0, 0);
            }

            AddQuadUnsafe(ctx,
                new Vector3(x1, y1, zBottom),
                new Vector3(x1, y1, zTop),
                new Vector3(x2, y2, zTop),
                new Vector3(x2, y2, zBottom),
                normal, color);
        }

        /// <summary>
        /// Добавляет вертикальные стенки для контура с помощью unsafe кода
        /// </summary>
        public static void AddVerticalWallsUnsafe(
            GeometryContext ctx,
            ReadOnlySpan<Vector2> points,
            float zBottom, float zTop,
            Color4 color)
        {
            if (points.Length < 2) return;

            int wallCount = points.Length;
            ctx.EnsureVertexCapacity(wallCount * 4);
            ctx.EnsureIndexCapacity(wallCount * 6);

            fixed (Vertex* vertexPtr = ctx.Vertices)
            fixed (uint* indexPtr = ctx.Indices)
            {
                Vertex* vPtr = vertexPtr + ctx.VertexCount;
                uint* iPtr = indexPtr + ctx.IndexCount;
                uint baseIdx = (uint)ctx.VertexCount;

                for (int i = 0; i < points.Length; i++)
                {
                    int next = (i + 1) % points.Length;

                    float x1 = points[i].X;
                    float y1 = points[i].Y;
                    float x2 = points[next].X;
                    float y2 = points[next].Y;

                    // Вычисляем нормаль
                    float dx = x2 - x1;
                    float dy = y2 - y1;
                    float len = MathF.Sqrt(dx * dx + dy * dy);

                    Vector3 normal;
                    if (len > 0.0001f)
                    {
                        float invLen = 1.0f / len;
                        normal = new Vector3(-dy * invLen, dx * invLen, 0);
                    }
                    else
                    {
                        normal = new Vector3(1, 0, 0);
                    }

                    // 4 вершины стенки
                    *vPtr++ = new Vertex(new Vector3(x1, y1, zBottom), normal, color);
                    *vPtr++ = new Vertex(new Vector3(x1, y1, zTop), normal, color);
                    *vPtr++ = new Vertex(new Vector3(x2, y2, zTop), normal, color);
                    *vPtr++ = new Vertex(new Vector3(x2, y2, zBottom), normal, color);

                    // 6 индексов (2 треугольника)
                    uint bi = baseIdx + (uint)(i * 4);
                    *iPtr++ = bi;
                    *iPtr++ = bi + 1;
                    *iPtr++ = bi + 2;
                    *iPtr++ = bi;
                    *iPtr++ = bi + 2;
                    *iPtr++ = bi + 3;
                }
            }

            ctx.VertexCount += wallCount * 4;
            ctx.IndexCount += wallCount * 6;
        }

        /// <summary>
        /// Быстрая триангуляция выпуклого полигона (fan triangulation)
        /// </summary>
        public static void TriangulateConvexPolygonUnsafe(
            GeometryContext ctx,
            ReadOnlySpan<Vector2> points,
            float z,
            Vector3 normal,
            Color4 color)
        {
            if (points.Length < 3) return;

            int vertexCount = points.Length;
            int triangleCount = points.Length - 2;

            ctx.EnsureVertexCapacity(vertexCount);
            ctx.EnsureIndexCapacity(triangleCount * 3);

            uint baseIndex = (uint)ctx.VertexCount;

            // Добавляем вершины
            fixed (Vertex* vertexPtr = &ctx.Vertices[ctx.VertexCount])
            {
                for (int i = 0; i < points.Length; i++)
                {
                    vertexPtr[i] = new Vertex(new Vector3(points[i].X, points[i].Y, z), normal, color);
                }
            }
            ctx.VertexCount += vertexCount;

            // Fan триангуляция (для выпуклых полигонов)
            fixed (uint* indexPtr = &ctx.Indices[ctx.IndexCount])
            {
                for (int i = 0; i < triangleCount; i++)
                {
                    indexPtr[i * 3] = baseIndex;
                    indexPtr[i * 3 + 1] = baseIndex + (uint)(i + 1);
                    indexPtr[i * 3 + 2] = baseIndex + (uint)(i + 2);
                }
            }
            ctx.IndexCount += triangleCount * 3;
        }

        /// <summary>
        /// Вычисление центра и радиуса bounding circle для набора точек
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (float centerX, float centerY, float radius) ComputeBoundingCircle(ReadOnlySpan<Vector2> points)
        {
            if (points.Length == 0)
                return (0, 0, 0);

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                float x = points[i].X;
                float y = points[i].Y;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float radius = MathF.Max(maxX - minX, maxY - minY) * 0.5f;

            return (centerX, centerY, radius);
        }

        /// <summary>
        /// Быстрая проверка точки внутри треугольника
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInTriangle(
            float px, float py,
            float ax, float ay,
            float bx, float by,
            float cx, float cy)
        {
            float Sign(float x1, float y1, float x2, float y2, float x3, float y3)
            {
                return (x1 - x3) * (y2 - y3) - (x2 - x3) * (y1 - y3);
            }

            float d1 = Sign(px, py, ax, ay, bx, by);
            float d2 = Sign(px, py, bx, by, cx, cy);
            float d3 = Sign(px, py, cx, cy, ax, ay);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        /// <summary>
        /// Быстрое копирование массива вершин в List
        /// </summary>
        public static void CopyToList(GeometryContext ctx, List<Vertex> vertices, List<uint> indices)
        {
            // Используем CollectionsMarshal для прямого доступа к внутреннему массиву List
            if (ctx.VertexCount > 0)
            {
                var vertexSpan = new ReadOnlySpan<Vertex>(ctx.Vertices, 0, ctx.VertexCount);
                vertices.AddRange(vertexSpan.ToArray());
            }

            if (ctx.IndexCount > 0)
            {
                // Корректируем индексы с учётом смещения в целевом списке
                uint baseOffset = (uint)(vertices.Count - ctx.VertexCount);
                var indexSpan = new ReadOnlySpan<uint>(ctx.Indices, 0, ctx.IndexCount);

                if (baseOffset == 0)
                {
                    indices.AddRange(indexSpan.ToArray());
                }
                else
                {
                    for (int i = 0; i < ctx.IndexCount; i++)
                    {
                        indices.Add(ctx.Indices[i] + baseOffset);
                    }
                }
            }
        }

        /// <summary>
        /// Получает Span вершин для прямой передачи в GPU буфер
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<Vertex> GetVertexSpan(GeometryContext ctx)
        {
            return new ReadOnlySpan<Vertex>(ctx.Vertices, 0, ctx.VertexCount);
        }

        /// <summary>
        /// Получает Span индексов для прямой передачи в GPU буфер
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<uint> GetIndexSpan(GeometryContext ctx)
        {
            return new ReadOnlySpan<uint>(ctx.Indices, 0, ctx.IndexCount);
        }
    }
}
