using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Hans.NET.Models;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace HansScannerHost.Models
{
    /// <summary>
    /// Загрузчик конфигураций сканаторов из JSON файлов
    /// </summary>
    public static class ScanatorConfigurationLoader
    {
        /// <summary>
        /// Загрузить конфигурацию сканатора из JSON файла
        /// </summary>
        public static List<ScanatorConfiguration>? LoadFromFile(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"Configuration file not found: {jsonFilePath}");
            }

            string jsonContent = File.ReadAllText(jsonFilePath);
            List<ScanatorConfiguration> config = JsonConvert.DeserializeObject<List<ScanatorConfiguration>>(jsonContent);
            return config;
        }
    }
}
