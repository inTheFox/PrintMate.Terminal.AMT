using System;
using Hans.NET.libs;
using static Hans.NET.libs.HM_UDM_DLL;

namespace Hans.NET.Models
{
    /// <summary>
    /// Примеры использования валидации
    /// </summary>
    public static class ValidationExamples
    {
        /// <summary>
        /// Пример 1: Валидация ProcessVariables
        /// </summary>
        public static void ExampleProcessVariablesValidation()
        {
            var processVars = new ProcessVariables
            {
                MarkSpeed = 1000,
                JumpSpeed = 25000,
                PolygonDelay = 170,
                JumpDelay = 40000,
                MarkDelay = 1200,
                LaserOnDelay = 110.0,
                LaserOffDelay = 120.0,
                CurBeamDiameterMicron = 65.0,
                CurPower = 50.0
            };

            // Способ 1: Быстрая проверка
            if (!processVars.IsValid())
            {
                Console.WriteLine("ProcessVariables invalid!");
            }

            // Способ 2: Детальная проверка
            var result = processVars.Validate();
            if (!result.IsValid)
            {
                Console.WriteLine($"Validation failed: {result}");
            }
            else
            {
                Console.WriteLine("ProcessVariables valid!");
            }
        }

        /// <summary>
        /// Пример 2: Валидация MarkParameter перед отправкой в SDK
        /// </summary>
        public static void ExampleMarkParameterValidation()
        {
            var markParam = new MarkParameter
            {
                MarkSpeed = 1000,
                JumpSpeed = 2000,
                MarkDelay = 1200,
                JumpDelay = 40000,
                PolygonDelay = 170,
                LaserOnDelay = 110.0f,
                LaserOffDelay = 120.0f,
                LaserPower = 60.0f,
                AnalogMode = 1,
                MarkCount = 1
            };

            // Валидация перед передачей в SDK
            var result = markParam.Validate();
            if (!result.IsValid)
            {
                throw new InvalidOperationException(
                    $"Invalid MarkParameter: {result.ErrorMessage}");
            }

            // Теперь можно безопасно передать в UDM_SetLayersPara
            // UDM_SetLayersPara(new[] { markParam }, 1);
        }

        /// <summary>
        /// Пример 3: Валидация BeamConfig
        /// </summary>
        public static void ExampleBeamConfigValidation()
        {
            var beamConfig = new BeamConfig
            {
                MinBeamDiameterMicron = 48.141,
                WavelengthNano = 1070.0,
                RayleighLengthMicron = 1426.715,
                M2 = 1.127,
                FocalLengthMm = 538.46
            };

            var result = beamConfig.Validate();
            if (!result.IsValid)
            {
                Console.WriteLine($"BeamConfig validation failed: {result}");
            }
            else
            {
                Console.WriteLine("BeamConfig valid!");

                // Проверка согласованности Rayleigh length
                double theoretical = beamConfig.CalculateTheoreticalRayleighLength();
                Console.WriteLine($"Configured Rayleigh length: {beamConfig.RayleighLengthMicron} µm");
                Console.WriteLine($"Theoretical Rayleigh length: {theoretical:F2} µm");
            }
        }

        /// <summary>
        /// Пример 4: Полная валидация конфигурации
        /// </summary>
        public static void ExampleFullConfigurationValidation(ScanatorConfiguration config)
        {
            try
            {
                // Бросить исключение если невалидно
                config.ValidateOrThrow();
                Console.WriteLine("✓ Configuration is valid!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Configuration validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Пример 5: Валидация перед генерацией UDM
        /// </summary>
        public static void ExampleValidateBeforeUdmGeneration(ScanatorConfiguration config)
        {
            // Шаг 1: Валидация конфигурации
            var configResult = config.Validate();
            if (!configResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"Cannot generate UDM with invalid configuration: {configResult.ErrorMessage}");
            }

            // Шаг 2: Валидация ProcessVariables
            var processVars = config.ProcessVariablesMap.NonDepends[0];
            var varsResult = processVars.Validate();
            if (!varsResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"Invalid ProcessVariables: {varsResult.ErrorMessage}");
            }

            // Шаг 3: Создание и валидация MarkParameter
            var markParam = new MarkParameter
            {
                MarkSpeed = (uint)processVars.MarkSpeed,
                JumpSpeed = (uint)processVars.JumpSpeed,
                PolygonDelay = (uint)processVars.PolygonDelay,
                JumpDelay = (uint)processVars.JumpDelay,
                MarkDelay = (uint)processVars.MarkDelay,
                LaserOnDelay = (float)processVars.LaserOnDelay,
                LaserOffDelay = (float)processVars.LaserOffDelay,
                LaserPower = 50.0f,
                AnalogMode = 1,
                MarkCount = 1
            };

            var paramResult = markParam.Validate();
            if (!paramResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"Generated MarkParameter is invalid: {paramResult.ErrorMessage}");
            }

            Console.WriteLine("✓ All validations passed, ready to generate UDM");
        }

        /// <summary>
        /// Пример 6: Обработка ошибок валидации
        /// </summary>
        public static bool TryValidateConfiguration(ScanatorConfiguration config, out string errorMessage)
        {
            var result = config.Validate();

            if (result.IsValid)
            {
                errorMessage = null;
                return true;
            }

            errorMessage = $"Configuration validation failed:\n" +
                          $"  Property: {result.PropertyName}\n" +
                          $"  Error: {result.ErrorMessage}";
            return false;
        }
    }
}
