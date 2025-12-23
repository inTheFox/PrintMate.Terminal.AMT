using System.Collections.Generic;
using System.Linq;

namespace Hans.NET.Models
{
    /// <summary>
    /// Partial class для валидации полной конфигурации ScanatorConfiguration
    /// </summary>
    public partial class ScanatorConfiguration
    {
        /// <summary>
        /// Полная валидация всей конфигурации сканатора
        /// </summary>
        public ValidationResult Validate()
        {
            var errors = new List<string>();

            // ============= CardInfo =============
            if (CardInfo == null)
            {
                return ValidationResult.Failure(nameof(CardInfo), "CardInfo is null");
            }

            if (string.IsNullOrWhiteSpace(CardInfo.IpAddress))
            {
                errors.Add($"{nameof(CardInfo.IpAddress)}: IP address is required");
            }
            else
            {
                // Проверка формата IP
                var parts = CardInfo.IpAddress.Split('.');
                if (parts.Length != 4)
                {
                    errors.Add($"{nameof(CardInfo.IpAddress)}: Invalid IP format '{CardInfo.IpAddress}'");
                }
                else
                {
                    foreach (var part in parts)
                    {
                        if (!int.TryParse(part, out int num) || num < 0 || num > 255)
                        {
                            errors.Add($"{nameof(CardInfo.IpAddress)}: Invalid IP octet '{part}'");
                            break;
                        }
                    }
                }
            }

            if (CardInfo.SeqIndex < 0)
            {
                errors.Add($"{nameof(CardInfo.SeqIndex)}: Must be >= 0, got {CardInfo.SeqIndex}");
            }

            // ============= ProcessVariablesMap =============
            if (ProcessVariablesMap == null)
            {
                return ValidationResult.Failure(nameof(ProcessVariablesMap), "ProcessVariablesMap is null");
            }

            if (ProcessVariablesMap.NonDepends == null || !ProcessVariablesMap.NonDepends.Any())
            {
                errors.Add($"{nameof(ProcessVariablesMap.NonDepends)}: Must have at least one entry");
            }
            else
            {
                for (int i = 0; i < ProcessVariablesMap.NonDepends.Count; i++)
                {
                    var result = ProcessVariablesMap.NonDepends[i].Validate();
                    if (!result.IsValid)
                    {
                        errors.Add($"{nameof(ProcessVariablesMap.NonDepends)}[{i}].{result.PropertyName}: {result.ErrorMessage}");
                    }
                }
            }

            if (ProcessVariablesMap.MarkSpeed != null)
            {
                for (int i = 0; i < ProcessVariablesMap.MarkSpeed.Count; i++)
                {
                    var result = ProcessVariablesMap.MarkSpeed[i].Validate();
                    if (!result.IsValid)
                    {
                        errors.Add($"{nameof(ProcessVariablesMap.MarkSpeed)}[{i}].{result.PropertyName}: {result.ErrorMessage}");
                    }
                }
            }

            // ============= ScannerConfig =============
            if (ScannerConfig == null)
            {
                return ValidationResult.Failure(nameof(ScannerConfig), "ScannerConfig is null");
            }
            else
            {
                var result = ScannerConfig.Validate();
                if (!result.IsValid)
                {
                    errors.Add($"{nameof(ScannerConfig)}.{result.PropertyName}: {result.ErrorMessage}");
                }
            }

            // ============= BeamConfig =============
            if (BeamConfig == null)
            {
                return ValidationResult.Failure(nameof(BeamConfig), "BeamConfig is null");
            }
            else
            {
                var result = BeamConfig.Validate();
                if (!result.IsValid)
                {
                    errors.Add($"{nameof(BeamConfig)}.{result.PropertyName}: {result.ErrorMessage}");
                }
            }

            // ============= LaserPowerConfig =============
            if (LaserPowerConfig == null)
            {
                return ValidationResult.Failure(nameof(LaserPowerConfig), "LaserPowerConfig is null");
            }

            if (LaserPowerConfig.MaxPower <= 0)
            {
                errors.Add($"{nameof(LaserPowerConfig.MaxPower)}: Must be > 0, got {LaserPowerConfig.MaxPower}");
            }

            if (LaserPowerConfig.ActualPowerCorrectionValue != null)
            {
                if (LaserPowerConfig.ActualPowerCorrectionValue.Count < 2)
                {
                    errors.Add($"{nameof(LaserPowerConfig.ActualPowerCorrectionValue)}: Must have at least 2 points for interpolation");
                }

                // Проверка монотонности
                for (int i = 1; i < LaserPowerConfig.ActualPowerCorrectionValue.Count; i++)
                {
                    if (LaserPowerConfig.ActualPowerCorrectionValue[i] < LaserPowerConfig.ActualPowerCorrectionValue[i - 1])
                    {
                        errors.Add($"{nameof(LaserPowerConfig.ActualPowerCorrectionValue)}: Values must be monotonically increasing");
                        break;
                    }
                }
            }

            // ============= FunctionSwitcherConfig =============
            if (FunctionSwitcherConfig == null)
            {
                return ValidationResult.Failure(nameof(FunctionSwitcherConfig), "FunctionSwitcherConfig is null");
            }

            // ============= ThirdAxisConfig =============
            if (ThirdAxisConfig == null)
            {
                return ValidationResult.Failure(nameof(ThirdAxisConfig), "ThirdAxisConfig is null");
            }

            // ============= Сводка ошибок =============
            if (errors.Any())
            {
                return ValidationResult.Failure("Multiple", string.Join("; ", errors));
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Быстрая проверка валидности без детального сообщения
        /// </summary>
        public bool IsValid() => Validate().IsValid;

        /// <summary>
        /// Валидация с выбросом исключения при ошибке
        /// </summary>
        public void ValidateOrThrow()
        {
            var result = Validate();
            if (!result.IsValid)
            {
                throw new System.Exception($"Configuration validation failed: {result.ErrorMessage}");
            }
        }
    }
}
