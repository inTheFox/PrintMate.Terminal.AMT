using LaserConfigurator.Models;
using System.Threading.Tasks;

namespace LaserConfigurator.Services
{
    /// <summary>
    /// Сервис управления конфигурацией
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Текущая конфигурация
        /// </summary>
        LaserConfiguratorSettings CurrentSettings { get; }

        /// <summary>
        /// Загрузить конфигурацию из файла
        /// </summary>
        Task<bool> LoadConfigurationAsync(string filePath);

        /// <summary>
        /// Сохранить текущую конфигурацию в файл
        /// </summary>
        Task<bool> SaveConfigurationAsync(string filePath);

        /// <summary>
        /// Применить изменения конфигурации
        /// </summary>
        void ApplyConfiguration();

        /// <summary>
        /// Создать конфигурацию по умолчанию
        /// </summary>
        void CreateDefaultConfiguration();

        /// <summary>
        /// Создать конфигурацию сканатора по умолчанию
        /// </summary>
        Hans.NET.Models.ScanatorConfiguration CreateDefaultScannerConfig();
    }
}
