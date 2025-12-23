using System;
using SharpDX;

namespace PrintMate.Terminal.Rendering
{
    /// <summary>
    /// Orbit камера для 3D просмотра с управлением мышью
    /// </summary>
    public class OrbitCamera
    {
        #region Приватные поля

        private Vector3 _target;
        private float _distance;
        private float _azimuth;   // Горизонтальный угол (вокруг оси Y)
        private float _elevation; // Вертикальный угол
        private float _fov;
        private float _aspectRatio;
        private float _nearPlane;
        private float _farPlane;

        // Ограничения
        private float _minDistance = 50f;
        private float _maxDistance = 2000f;
        private float _minElevation = 5f;  // Минимум 5° над землёй
        private float _maxElevation = 89f; // Максимум 89° (не переворачиваем)

        #endregion

        #region Публичные свойства

        public Vector3 Target
        {
            get => _target;
            set => _target = value;
        }

        public float Distance
        {
            get => _distance;
            set => _distance = MathUtil.Clamp(value, _minDistance, _maxDistance);
        }

        public float Azimuth
        {
            get => _azimuth;
            set => _azimuth = value % 360f;
        }

        public float Elevation
        {
            get => _elevation;
            set => _elevation = MathUtil.Clamp(value, _minElevation, _maxElevation);
        }

        public float FieldOfView
        {
            get => _fov;
            set => _fov = MathUtil.Clamp(value, 10f, 120f);
        }

        public float AspectRatio
        {
            get => _aspectRatio;
            set => _aspectRatio = value;
        }

        public Vector3 Position { get; private set; }
        public Vector3 Forward { get; private set; }
        public Vector3 Right { get; private set; }
        public Vector3 Up { get; private set; }

        #endregion

        #region Конструктор

        public OrbitCamera(float distance = 500f, float azimuth = 45f, float elevation = 30f)
        {
            _target = Vector3.Zero;
            _distance = distance;
            _azimuth = azimuth;
            _elevation = elevation;
            _fov = 60f;
            _aspectRatio = 16f / 9f;
            _nearPlane = 1f;
            _farPlane = 10000f;

            UpdateCameraVectors();
        }

        #endregion

        #region Обновление

        /// <summary>
        /// Обновляет векторы камеры на основе углов
        /// </summary>
        private void UpdateCameraVectors()
        {
            // Конвертируем углы в радианы
            float azimuthRad = MathUtil.DegreesToRadians(_azimuth);
            float elevationRad = MathUtil.DegreesToRadians(_elevation);

            // Вычисляем позицию камеры в сферических координатах
            float x = _distance * MathF.Cos(elevationRad) * MathF.Cos(azimuthRad);
            float y = _distance * MathF.Cos(elevationRad) * MathF.Sin(azimuthRad);
            float z = _distance * MathF.Sin(elevationRad);

            Position = _target + new Vector3(x, y, z);

            // Вычисляем направление взгляда
            Forward = Vector3.Normalize(_target - Position);

            // Вычисляем правый вектор (cross product с мировым Up)
            Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitZ));

            // Вычисляем локальный Up (cross product)
            Up = Vector3.Cross(Right, Forward);
        }

        #endregion

        #region Матрицы

        /// <summary>
        /// Возвращает матрицу View
        /// </summary>
        public Matrix GetViewMatrix()
        {
            UpdateCameraVectors();
            return Matrix.LookAtLH(Position, _target, Up);
        }

        /// <summary>
        /// Возвращает матрицу Projection
        /// </summary>
        public Matrix GetProjectionMatrix()
        {
            return Matrix.PerspectiveFovLH(MathUtil.DegreesToRadians(_fov), _aspectRatio, _nearPlane, _farPlane);
        }

        #endregion

        #region Управление

        /// <summary>
        /// Вращение камеры (мышь)
        /// </summary>
        public void Rotate(float deltaAzimuth, float deltaElevation)
        {
            Azimuth += deltaAzimuth;
            Elevation += deltaElevation;
        }

        /// <summary>
        /// Зум камеры (колесо мыши)
        /// </summary>
        public void Zoom(float delta)
        {
            Distance -= delta;
        }

        /// <summary>
        /// Панорамирование камеры (средняя кнопка мыши)
        /// </summary>
        public void Pan(float deltaX, float deltaY)
        {
            UpdateCameraVectors();

            // Перемещаем target в плоскости камеры
            Vector3 offset = Right * deltaX - Up * deltaY;
            _target += offset;
        }

        /// <summary>
        /// Сброс камеры в позицию по умолчанию
        /// </summary>
        public void Reset()
        {
            _target = Vector3.Zero;
            _distance = 500f;
            _azimuth = 45f;
            _elevation = 30f;
            UpdateCameraVectors();
        }

        /// <summary>
        /// Устанавливает target чтобы смотреть на bounding box
        /// </summary>
        public void FocusOn(BoundingBox boundingBox)
        {
            // Центр bounding box
            _target = (boundingBox.Minimum + boundingBox.Maximum) / 2f;

            // Вычисляем размер bounding box
            Vector3 size = boundingBox.Maximum - boundingBox.Minimum;
            float maxSize = MathF.Max(MathF.Max(size.X, size.Y), size.Z);

            // Устанавливаем расстояние чтобы весь объект был виден
            Distance = maxSize * 1.5f;

            UpdateCameraVectors();
        }

        /// <summary>
        /// Создаёт луч из экранных координат для ray picking
        /// </summary>
        /// <param name="screenX">X координата в экране (0..screenWidth)</param>
        /// <param name="screenY">Y координата в экране (0..screenHeight)</param>
        /// <param name="screenWidth">Ширина экрана</param>
        /// <param name="screenHeight">Высота экрана</param>
        /// <returns>Луч в мировых координатах (origin, direction)</returns>
        public Ray GetPickingRay(float screenX, float screenY, float screenWidth, float screenHeight)
        {
            // Преобразуем экранные координаты в нормализованные координаты устройства [-1, 1]
            float ndcX = (2.0f * screenX / screenWidth) - 1.0f;
            float ndcY = 1.0f - (2.0f * screenY / screenHeight); // Инвертируем Y

            // Получаем матрицы
            Matrix view = GetViewMatrix();
            Matrix projection = GetProjectionMatrix();

            // Инвертируем view * projection матрицу
            Matrix viewProj = view * projection;
            Matrix.Invert(ref viewProj, out Matrix invViewProj);

            // Точки луча в clip space (near и far plane)
            Vector3 nearPoint = new Vector3(ndcX, ndcY, 0.0f); // Near plane (z = 0)
            Vector3 farPoint = new Vector3(ndcX, ndcY, 1.0f);  // Far plane (z = 1)

            // Преобразуем в world space
            Vector3 nearWorld = Vector3.TransformCoordinate(nearPoint, invViewProj);
            Vector3 farWorld = Vector3.TransformCoordinate(farPoint, invViewProj);

            // Создаём луч
            Vector3 rayOrigin = nearWorld;
            Vector3 rayDirection = Vector3.Normalize(farWorld - nearWorld);

            return new Ray(rayOrigin, rayDirection);
        }

        #endregion
    }
}
