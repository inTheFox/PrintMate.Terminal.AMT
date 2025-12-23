using System;
using SharpDX;

namespace PrintMate.Terminal.Rendering
{
    /// <summary>
    /// Класс для Frustum Culling - отсечения объектов вне поля зрения камеры
    /// Оптимизация: не рендерим геометрию, которую не видно
    /// </summary>
    public class FrustumCuller
    {
        private Plane[] _frustumPlanes = new Plane[6];

        /// <summary>
        /// Обновляет плоскости фрустума на основе матрицы ViewProjection
        /// </summary>
        public void Update(Matrix viewProjection)
        {
            // Извлекаем 6 плоскостей фрустума из матрицы ViewProjection
            // Left plane
            _frustumPlanes[0] = new Plane(
                viewProjection.M14 + viewProjection.M11,
                viewProjection.M24 + viewProjection.M21,
                viewProjection.M34 + viewProjection.M31,
                viewProjection.M44 + viewProjection.M41);

            // Right plane
            _frustumPlanes[1] = new Plane(
                viewProjection.M14 - viewProjection.M11,
                viewProjection.M24 - viewProjection.M21,
                viewProjection.M34 - viewProjection.M31,
                viewProjection.M44 - viewProjection.M41);

            // Bottom plane
            _frustumPlanes[2] = new Plane(
                viewProjection.M14 + viewProjection.M12,
                viewProjection.M24 + viewProjection.M22,
                viewProjection.M34 + viewProjection.M32,
                viewProjection.M44 + viewProjection.M42);

            // Top plane
            _frustumPlanes[3] = new Plane(
                viewProjection.M14 - viewProjection.M12,
                viewProjection.M24 - viewProjection.M22,
                viewProjection.M34 - viewProjection.M32,
                viewProjection.M44 - viewProjection.M42);

            // Near plane
            _frustumPlanes[4] = new Plane(
                viewProjection.M13,
                viewProjection.M23,
                viewProjection.M33,
                viewProjection.M43);

            // Far plane
            _frustumPlanes[5] = new Plane(
                viewProjection.M14 - viewProjection.M13,
                viewProjection.M24 - viewProjection.M23,
                viewProjection.M34 - viewProjection.M33,
                viewProjection.M44 - viewProjection.M43);

            // Нормализуем плоскости
            for (int i = 0; i < 6; i++)
            {
                _frustumPlanes[i].Normalize();
            }
        }

        /// <summary>
        /// Проверяет, находится ли AABB (axis-aligned bounding box) в фрустуме
        /// </summary>
        public bool IsBoxInFrustum(BoundingBox box)
        {
            for (int i = 0; i < 6; i++)
            {
                var plane = _frustumPlanes[i];

                // Находим вершину box, которая наиболее "в направлении" плоскости
                Vector3 positiveVertex = new Vector3(
                    plane.Normal.X >= 0 ? box.Maximum.X : box.Minimum.X,
                    plane.Normal.Y >= 0 ? box.Maximum.Y : box.Minimum.Y,
                    plane.Normal.Z >= 0 ? box.Maximum.Z : box.Minimum.Z);

                // Если эта вершина за плоскостью - box полностью снаружи
                float distance = Vector3.Dot(plane.Normal, positiveVertex) + plane.D;
                if (distance < 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Проверяет, находится ли сфера в фрустуме
        /// </summary>
        public bool IsSphereInFrustum(Vector3 center, float radius)
        {
            for (int i = 0; i < 6; i++)
            {
                var plane = _frustumPlanes[i];
                float distance = Vector3.Dot(plane.Normal, center) + plane.D;

                if (distance < -radius)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Проверяет, находится ли точка в фрустуме
        /// </summary>
        public bool IsPointInFrustum(Vector3 point)
        {
            for (int i = 0; i < 6; i++)
            {
                var plane = _frustumPlanes[i];
                float distance = Vector3.Dot(plane.Normal, point) + plane.D;

                if (distance < 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Вычисляет расстояние до центра box от камеры (для LOD)
        /// </summary>
        public float GetDistanceToBox(Vector3 cameraPosition, BoundingBox box)
        {
            Vector3 center = (box.Minimum + box.Maximum) * 0.5f;
            return Vector3.Distance(cameraPosition, center);
        }

        /// <summary>
        /// Определяет уровень LOD на основе расстояния до камеры
        /// </summary>
        public int GetLODLevel(float distance, float lodDistance1 = 500f, float lodDistance2 = 1000f)
        {
            if (distance < lodDistance1)
                return 0; // Полная детализация
            else if (distance < lodDistance2)
                return 1; // Средняя детализация
            else
                return 2; // Низкая детализация
        }
    }

    /// <summary>
    /// Bounding Box для группы слоёв или объектов
    /// </summary>
    public class LayerBounds
    {
        public int LayerIndex { get; set; }
        public BoundingBox Bounds { get; set; }
        public int VertexCount { get; set; }
        public int IndexCount { get; set; }

        public LayerBounds(int layerIndex, Vector3 min, Vector3 max)
        {
            LayerIndex = layerIndex;
            Bounds = new BoundingBox(min, max);
        }
    }
}
