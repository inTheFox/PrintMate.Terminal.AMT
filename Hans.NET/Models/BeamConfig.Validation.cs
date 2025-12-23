namespace Hans.NET.Models
{
    /// <summary>
    /// Partial class для валидации BeamConfig
    /// </summary>
    public partial class BeamConfig
    {
        /// <summary>
        /// Валидация конфигурации луча
        /// </summary>
        public ValidationResult Validate()
        {
            // ============= Минимальный диаметр =============
            if (MinBeamDiameterMicron <= 0)
                return ValidationResult.Failure(nameof(MinBeamDiameterMicron),
                    $"MinBeamDiameterMicron must be > 0, got {MinBeamDiameterMicron}");

            // Типичный диапазон: 10-500 µm
            if (MinBeamDiameterMicron < 5 || MinBeamDiameterMicron > 1000)
                return ValidationResult.Failure(nameof(MinBeamDiameterMicron),
                    $"MinBeamDiameterMicron suspicious: {MinBeamDiameterMicron} µm (typical: 10-500)");

            // ============= Длина волны =============
            if (WavelengthNano <= 0)
                return ValidationResult.Failure(nameof(WavelengthNano),
                    $"WavelengthNano must be > 0, got {WavelengthNano}");

            // Типичные длины волн лазеров: 532 nm (green), 1064 nm (IR), 1070 nm (fiber)
            if (WavelengthNano < 200 || WavelengthNano > 11000)
                return ValidationResult.Failure(nameof(WavelengthNano),
                    $"WavelengthNano suspicious: {WavelengthNano} nm (typical: 500-11000)");

            // ============= Длина Рэлея =============
            if (RayleighLengthMicron <= 0)
                return ValidationResult.Failure(nameof(RayleighLengthMicron),
                    $"RayleighLengthMicron must be > 0, got {RayleighLengthMicron}");

            // Проверка согласованности с теоретическим значением
            double theoreticalZR = CalculateTheoreticalRayleighLength();
            double deviation = System.Math.Abs(RayleighLengthMicron - theoreticalZR) / theoreticalZR;

            // Предупреждение если отклонение > 50%
            if (deviation > 0.5)
            {
                return ValidationResult.Failure(nameof(RayleighLengthMicron),
                    $"RayleighLengthMicron ({RayleighLengthMicron}) deviates {deviation * 100:F1}% " +
                    $"from theoretical value ({theoreticalZR:F2}). Check M² and wavelength.");
            }

            // ============= M² фактор =============
            if (M2 < 1.0)
                return ValidationResult.Failure(nameof(M2),
                    $"M² must be >= 1.0 (perfect Gaussian), got {M2}");

            // Типичный диапазон: 1.0-20.0 (fiber lasers: 1.1-1.3)
            if (M2 > 100)
                return ValidationResult.Failure(nameof(M2),
                    $"M² too high: {M2} (typical: 1.0-20.0)");

            // ============= Фокусное расстояние =============
            if (FocalLengthMm <= 0)
                return ValidationResult.Failure(nameof(FocalLengthMm),
                    $"FocalLengthMm must be > 0, got {FocalLengthMm}");

            // Типичные F-theta линзы: 100-1000 мм
            if (FocalLengthMm < 50 || FocalLengthMm > 2000)
                return ValidationResult.Failure(nameof(FocalLengthMm),
                    $"FocalLengthMm suspicious: {FocalLengthMm} mm (typical: 100-1000)");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Быстрая проверка валидности без детального сообщения
        /// </summary>
        public bool IsValid() => Validate().IsValid;
    }
}
