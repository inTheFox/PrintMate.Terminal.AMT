using Hans.NET.Models;
using System;
using System.IO;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Простой класс для запуска тестов диаметра пучка
    /// Использование: вызвать статический метод RunAll() или RunQuick()
    /// </summary>
    public static class RunDiameterTests
    {
        /// <summary>
        /// Запускает все тесты с полным набором диаметров и мощностей
        /// </summary>
        public static void RunAll()
        {
            try
            {
                // Загружаем конфигурацию из JSON файла
                var config = LoadConfiguration();

                if (config == null)
                {
                    Console.WriteLine("❌ Не удалось загрузить конфигурацию!");
                    return;
                }

                // Запускаем полный набор тестов
                DiameterVerificationTest.RunDiameterTests(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Анализ реальных измерений
        /// </summary>
        public static void AnalyzeRealMeasurements()
        {
            try
            {
                var config = LoadConfiguration();
                if (config == null)
                {
                    Console.WriteLine("❌ Не удалось загрузить конфигурацию!");
                    return;
                }

                DiagnosticTest.AnalyzeRealMeasurements(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Быстрая калибровка на основе измерений
        /// </summary>
        public static void QuickCalibration()
        {
            CalibrationHelper.TestCurrentMeasurement();
        }

        /// <summary>
        /// Быстрый тест с одним диаметром и одной мощностью
        /// </summary>
        /// <param name="diameter">Диаметр пучка в микронах (по умолчанию 100)</param>
        /// <param name="power">Мощность лазера в ваттах (по умолчанию 200)</param>
        public static void RunQuick(double diameter = 100.0, float power = 200f)
        {
            try
            {
                // Загружаем конфигурацию из JSON файла
                var config = LoadConfiguration();

                if (config == null)
                {
                    Console.WriteLine("❌ Не удалось загрузить конфигурацию!");
                    return;
                }

                // Запускаем быстрый тест
                DiameterVerificationTest.QuickTest(config, diameter, power);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Загружает конфигурацию сканера из JSON файла
        /// </summary>
        public static ScanatorConfiguration LoadConfiguration()
        {
            // Путь к конфигурационному файлу
            string configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "scanator_config_test.json"
            );

            Console.WriteLine($"Загрузка конфигурации из: {configPath}");

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"❌ Файл конфигурации не найден: {configPath}");
                Console.WriteLine("Создайте файл scanator_config_test.json в папке с программой");
                return null;
            }

            try
            {
                // Загружаем JSON
                var configs = ScanatorConfigurationLoader.LoadFromFile(configPath);

                if (configs == null || configs.Count == 0)
                {
                    Console.WriteLine("❌ В файле конфигурации нет данных");
                    return null;
                }

                Console.WriteLine($"✓ Конфигурация загружена успешно");
                Console.WriteLine($"  IP адрес: {configs[0].CardInfo.IpAddress}");
                Console.WriteLine($"  Минимальный диаметр: {configs[0].BeamConfig.MinBeamDiameterMicron:F1} мкм");
                Console.WriteLine($"  Максимальная мощность: {configs[0].LaserPowerConfig.MaxPower:F1} Вт");
                Console.WriteLine();

                return configs[0];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при загрузке конфигурации: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Пример использования с кастомной конфигурацией
        /// </summary>
        public static void RunWithConfig(ScanatorConfiguration config)
        {
            if (config == null)
            {
                Console.WriteLine("❌ Передана пустая конфигурация!");
                return;
            }

            try
            {
                DiameterVerificationTest.RunDiameterTests(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
