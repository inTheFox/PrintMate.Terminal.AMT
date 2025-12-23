using Hans.NET.Models;
using System.Collections.Generic;

namespace LaserConfigurator.Models
{
    /// <summary>
    /// Конфигурация приложения LaserConfigurator
    /// </summary>
    public class LaserConfiguratorSettings
    {
        /// <summary>
        /// Конфигурации двух сканаторов
        /// </summary>
        public List<ScanatorConfiguration> Scanners { get; set; } = new List<ScanatorConfiguration>();

        /// <summary>
        /// Последняя загруженная директория конфигов
        /// </summary>
        public string LastConfigDirectory { get; set; } = "";

        /// <summary>
        /// Автоматически подключаться к сканаторам при загрузке конфига
        /// </summary>
        public bool AutoConnectOnLoad { get; set; } = false;
    }
}
