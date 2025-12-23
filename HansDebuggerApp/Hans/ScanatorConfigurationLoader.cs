using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HandyControl.Controls;
using Hans.NET.Models;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Загрузчик конфигураций сканаторов из JSON файлов
    /// </summary>
    public static class ScanatorConfigurationLoader
    {
        /// <summary>
        /// Загрузить конфигурацию сканатора из JSON файла
        /// </summary>
        public static List<ScanatorConfiguration> LoadFromFile(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                Growl.Error("File not found");
            }

            string jsonContent = File.ReadAllText(jsonFilePath);
            List<ScanatorConfiguration> config = JsonConvert.DeserializeObject<List<ScanatorConfiguration>>(jsonContent);

            if (config == null)
            {
                Growl.Error($"Failed to deserialize configuration from {jsonFilePath}");
            }

            return config;
        }
    }
}
