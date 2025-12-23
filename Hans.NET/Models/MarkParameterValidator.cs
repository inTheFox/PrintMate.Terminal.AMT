using Hans.NET.libs;
using static Hans.NET.libs.HM_UDM_DLL;

namespace Hans.NET.Models
{
    /// <summary>
    /// Валидатор для MarkParameter (структура передаваемая в Hans SDK)
    /// Основан на JNIRet_HM_Mark_Parameter.isValid() из Hans4Java
    /// </summary>
    public static class MarkParameterValidator
    {
        /// <summary>
        /// Проверка, что значение является беззнаковым (>= 0)
        /// </summary>
        private static bool IsUnsigned(uint value) => true; // uint всегда unsigned
        private static bool IsUnsigned(int value) => value >= 0;

        /// <summary>
        /// Валидация параметра MarkParameter перед передачей в SDK
        /// </summary>
        public static ValidationResult Validate(this MarkParameter param)
        {
            // ============= Скорости (mm/s) =============
            if (!IsUnsigned((int)param.MarkSpeed))
                return ValidationResult.Failure(nameof(param.MarkSpeed),
                    $"MarkSpeed must be >= 0, got {param.MarkSpeed}");

            if (!IsUnsigned((int)param.JumpSpeed))
                return ValidationResult.Failure(nameof(param.JumpSpeed),
                    $"JumpSpeed must be >= 0, got {param.JumpSpeed}");

            // ============= Задержки (µs) =============
            if (!IsUnsigned((int)param.MarkDelay))
                return ValidationResult.Failure(nameof(param.MarkDelay),
                    $"MarkDelay must be >= 0, got {param.MarkDelay}");

            if (!IsUnsigned((int)param.JumpDelay))
                return ValidationResult.Failure(nameof(param.JumpDelay),
                    $"JumpDelay must be >= 0, got {param.JumpDelay}");

            if (!IsUnsigned((int)param.PolygonDelay))
                return ValidationResult.Failure(nameof(param.PolygonDelay),
                    $"PolygonDelay must be >= 0, got {param.PolygonDelay}");

            if (!IsUnsigned((int)param.MarkCount))
                return ValidationResult.Failure(nameof(param.MarkCount),
                    $"MarkCount must be >= 0, got {param.MarkCount}");

            // ============= Лазерные задержки (µs) =============
            // Могут быть отрицательными до -320 µs по спецификации
            if (param.LaserOnDelay < -320.0f)
                return ValidationResult.Failure(nameof(param.LaserOnDelay),
                    $"LaserOnDelay must be >= -320.0, got {param.LaserOnDelay}");

            if (param.LaserOffDelay < -320.0f)
                return ValidationResult.Failure(nameof(param.LaserOffDelay),
                    $"LaserOffDelay must be >= -320.0, got {param.LaserOffDelay}");

            // ============= FPK параметры (µs) =============
            if (param.FPKDelay < 0.0f)
                return ValidationResult.Failure(nameof(param.FPKDelay),
                    $"FPKDelay must be >= 0, got {param.FPKDelay}");

            if (param.FPKLength < 0.0f)
                return ValidationResult.Failure(nameof(param.FPKLength),
                    $"FPKLength must be >= 0, got {param.FPKLength}");

            if (param.QDelay < 0.0f)
                return ValidationResult.Failure(nameof(param.QDelay),
                    $"QDelay must be >= 0, got {param.QDelay}");

            // ============= Duty Cycle (0-1) =============
            if (param.DutyCycle < 0.0f || param.DutyCycle > 1.0f)
                return ValidationResult.Failure(nameof(param.DutyCycle),
                    $"DutyCycle must be in range [0, 1], got {param.DutyCycle}");

            // ============= Частоты (kHz) =============
            if (param.Frequency < 0.0f)
                return ValidationResult.Failure(nameof(param.Frequency),
                    $"Frequency must be >= 0, got {param.Frequency}");

            if (param.StandbyFrequency < 0.0f)
                return ValidationResult.Failure(nameof(param.StandbyFrequency),
                    $"StandbyFrequency must be >= 0, got {param.StandbyFrequency}");

            if (param.StandbyDutyCycle < 0.0f || param.StandbyDutyCycle > 1.0f)
                return ValidationResult.Failure(nameof(param.StandbyDutyCycle),
                    $"StandbyDutyCycle must be in range [0, 1], got {param.StandbyDutyCycle}");

            // ============= Мощность (0-100%) =============
            if (param.LaserPower < 0.0f || param.LaserPower > 100.0f)
                return ValidationResult.Failure(nameof(param.LaserPower),
                    $"LaserPower must be in range [0, 100], got {param.LaserPower}");

            // ============= AnalogMode =============
            if (param.AnalogMode != 0 && param.AnalogMode != 1)
                return ValidationResult.Failure(nameof(param.AnalogMode),
                    $"AnalogMode must be 0 or 1, got {param.AnalogMode}");

            // ============= Waveform (SPI laser) =============
            if (param.Waveform < 0 || param.Waveform > 63)
                return ValidationResult.Failure(nameof(param.Waveform),
                    $"Waveform must be in range [0, 63], got {param.Waveform}");

            // ============= Pulse Width Mode =============
            if (param.PulseWidthMode != 0 && param.PulseWidthMode != 1)
                return ValidationResult.Failure(nameof(param.PulseWidthMode),
                    $"PulseWidthMode must be 0 or 1, got {param.PulseWidthMode}");

            if (!IsUnsigned((int)param.PulseWidth))
                return ValidationResult.Failure(nameof(param.PulseWidth),
                    $"PulseWidth must be >= 0, got {param.PulseWidth}");

            // ============= Логическая проверка =============
            // MarkDelay должна быть больше чем LaserOffDelay + 10
            // (закомментировано в Java, но может быть полезно)
            // if (param.MarkDelay < (param.LaserOffDelay + 10))
            //     return ValidationResult.Failure(nameof(param.MarkDelay),
            //         $"MarkDelay ({param.MarkDelay}) must be >= LaserOffDelay + 10 ({param.LaserOffDelay + 10})");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Быстрая проверка валидности без детального сообщения
        /// </summary>
        public static bool IsValid(this MarkParameter param) => param.Validate().IsValid;
    }
}
