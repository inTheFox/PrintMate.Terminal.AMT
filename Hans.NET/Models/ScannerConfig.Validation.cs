namespace Hans.NET.Models
{
    /// <summary>
    /// Partial class для валидации ScannerConfig
    /// </summary>
    public partial class ScannerConfig
    {
        /// <summary>
        /// Валидация конфигурации сканера
        /// </summary>
        public ValidationResult Validate()
        {
            // ============= Размер поля =============
            if (FieldSizeX <= 0)
                return ValidationResult.Failure(nameof(FieldSizeX),
                    $"FieldSizeX must be > 0, got {FieldSizeX}");

            if (FieldSizeY <= 0)
                return ValidationResult.Failure(nameof(FieldSizeY),
                    $"FieldSizeY must be > 0, got {FieldSizeY}");

            // Типичные размеры поля: 50-1000 мм
            if (FieldSizeX > 2000)
                return ValidationResult.Failure(nameof(FieldSizeX),
                    $"FieldSizeX too large: {FieldSizeX} mm (typical max: 2000)");

            if (FieldSizeY > 2000)
                return ValidationResult.Failure(nameof(FieldSizeY),
                    $"FieldSizeY too large: {FieldSizeY} mm (typical max: 2000)");

            // ============= Протокол =============
            // 0 = SPI, 1 = XY2-100, 2 = SL2
            if (ProtocolCode < 0 || ProtocolCode > 2)
                return ValidationResult.Failure(nameof(ProtocolCode),
                    $"ProtocolCode must be in range [0, 2], got {ProtocolCode}");

            // ============= Координатная система =============
            // 0-7 (8 вариантов)
            if (CoordinateTypeCode < 0 || CoordinateTypeCode > 7)
                return ValidationResult.Failure(nameof(CoordinateTypeCode),
                    $"CoordinateTypeCode must be in range [0, 7], got {CoordinateTypeCode}");

            // ============= Масштабы =============
            if (ScaleX <= 0)
                return ValidationResult.Failure(nameof(ScaleX),
                    $"ScaleX must be > 0, got {ScaleX}");

            if (ScaleY <= 0)
                return ValidationResult.Failure(nameof(ScaleY),
                    $"ScaleY must be > 0, got {ScaleY}");

            if (ScaleZ <= 0)
                return ValidationResult.Failure(nameof(ScaleZ),
                    $"ScaleZ must be > 0, got {ScaleZ}");

            // Разумные ограничения на масштаб (0.1 - 10.0)
            if (ScaleX < 0.01f || ScaleX > 100.0f)
                return ValidationResult.Failure(nameof(ScaleX),
                    $"ScaleX out of reasonable range [0.01, 100], got {ScaleX}");

            if (ScaleY < 0.01f || ScaleY > 100.0f)
                return ValidationResult.Failure(nameof(ScaleY),
                    $"ScaleY out of reasonable range [0.01, 100], got {ScaleY}");

            // ============= Угол поворота =============
            // Обычно в градусах, любое значение допустимо
            // Но предупреждение если больше 360
            if (RotateAngle < -360 || RotateAngle > 360)
                return ValidationResult.Failure(nameof(RotateAngle),
                    $"RotateAngle suspicious: {RotateAngle}° (consider normalizing to [-360, 360])");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Быстрая проверка валидности без детального сообщения
        /// </summary>
        public bool IsValid() => Validate().IsValid;
    }
}
