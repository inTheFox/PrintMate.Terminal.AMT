using System;

namespace Hans.NET.Models
{
    /// <summary>
    /// Partial class для валидации ProcessVariables
    /// </summary>
    public partial class ProcessVariables
    {
        /// <summary>
        /// Проверка, что значение является беззнаковым (>= 0)
        /// </summary>
        private static bool IsUnsigned(int value) => value >= 0;
        private static bool IsUnsigned(double value) => value >= 0;

        /// <summary>
        /// Валидация всех параметров ProcessVariables
        /// </summary>
        public ValidationResult Validate()
        {
            // ============= Скорости =============
            if (!IsUnsigned(MarkSpeed))
                return ValidationResult.Failure(nameof(MarkSpeed),
                    $"MarkSpeed must be >= 0, got {MarkSpeed}");

            if (!IsUnsigned(JumpSpeed))
                return ValidationResult.Failure(nameof(JumpSpeed),
                    $"JumpSpeed must be >= 0, got {JumpSpeed}");

            // ============= Задержки (целочисленные) =============
            if (!IsUnsigned(PolygonDelay))
                return ValidationResult.Failure(nameof(PolygonDelay),
                    $"PolygonDelay must be >= 0, got {PolygonDelay}");

            if (!IsUnsigned(JumpDelay))
                return ValidationResult.Failure(nameof(JumpDelay),
                    $"JumpDelay must be >= 0, got {JumpDelay}");

            if (!IsUnsigned(MarkDelay))
                return ValidationResult.Failure(nameof(MarkDelay),
                    $"MarkDelay must be >= 0, got {MarkDelay}");

            if (!IsUnsigned(MinJumpDelay))
                return ValidationResult.Failure(nameof(MinJumpDelay),
                    $"MinJumpDelay must be >= 0, got {MinJumpDelay}");

            // ============= Задержки лазера (float) =============
            // LaserOnDelay и LaserOffDelay могут быть отрицательными (до -320 µs по спеке)
            if (LaserOnDelay < -320.0)
                return ValidationResult.Failure(nameof(LaserOnDelay),
                    $"LaserOnDelay must be >= -320.0, got {LaserOnDelay}");

            if (LaserOffDelay < -320.0)
                return ValidationResult.Failure(nameof(LaserOffDelay),
                    $"LaserOffDelay must be >= -320.0, got {LaserOffDelay}");

            if (LaserOnDelayForSkyWriting < -320.0)
                return ValidationResult.Failure(nameof(LaserOnDelayForSkyWriting),
                    $"LaserOnDelayForSkyWriting must be >= -320.0, got {LaserOnDelayForSkyWriting}");

            if (LaserOffDelayForSkyWriting < -320.0)
                return ValidationResult.Failure(nameof(LaserOffDelayForSkyWriting),
                    $"LaserOffDelayForSkyWriting must be >= -320.0, got {LaserOffDelayForSkyWriting}");

            // ============= Параметры луча =============
            if (!IsUnsigned(CurBeamDiameterMicron))
                return ValidationResult.Failure(nameof(CurBeamDiameterMicron),
                    $"CurBeamDiameterMicron must be >= 0, got {CurBeamDiameterMicron}");

            if (!IsUnsigned(CurPower))
                return ValidationResult.Failure(nameof(CurPower),
                    $"CurPower must be >= 0, got {CurPower}");

            if (!IsUnsigned(JumpMaxLengthLimitMm))
                return ValidationResult.Failure(nameof(JumpMaxLengthLimitMm),
                    $"JumpMaxLengthLimitMm must be >= 0, got {JumpMaxLengthLimitMm}");

            // ============= SkyWriting =============
            // Umax обычно 0.0 - 1.0, но может быть больше
            if (!IsUnsigned(Umax))
                return ValidationResult.Failure(nameof(Umax),
                    $"Umax must be >= 0, got {Umax}");

            // ============= Логические проверки =============
            // Проверка разумных диапазонов
            if (MarkSpeed > 50000)
                return ValidationResult.Failure(nameof(MarkSpeed),
                    $"MarkSpeed too high: {MarkSpeed} mm/s (typical max: 50000)");

            if (JumpSpeed > 50000)
                return ValidationResult.Failure(nameof(JumpSpeed),
                    $"JumpSpeed too high: {JumpSpeed} mm/s (typical max: 50000)");

            if (CurBeamDiameterMicron > 1000)
                return ValidationResult.Failure(nameof(CurBeamDiameterMicron),
                    $"CurBeamDiameterMicron too high: {CurBeamDiameterMicron} µm (typical max: 1000)");

            if (CurPower > 10000)
                return ValidationResult.Failure(nameof(CurPower),
                    $"CurPower too high: {CurPower} W (typical max: 10000)");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Быстрая проверка валидности без детального сообщения
        /// </summary>
        public bool IsValid() => Validate().IsValid;
    }
}
