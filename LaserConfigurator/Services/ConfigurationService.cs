using Hans.NET.Models;
using LaserConfigurator.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LaserConfigurator.Services
{
    /// <summary>
    /// Реализация сервиса управления конфигурацией
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private LaserConfiguratorSettings _settings;

        public LaserConfiguratorSettings CurrentSettings => _settings;

        public ConfigurationService()
        {
            CreateDefaultConfiguration();
        }

        public async Task<bool> LoadConfigurationAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                string jsonContent = await File.ReadAllTextAsync(filePath);

                // Try array format first (user's format: [{scanner1}, {scanner2}])
                try
                {
                    var scannersArray = JsonConvert.DeserializeObject<List<ScanatorConfiguration>>(jsonContent);
                    if (scannersArray != null && scannersArray.Count > 0)
                    {
                        _settings.Scanners = scannersArray;
                        _settings.LastConfigDirectory = Path.GetDirectoryName(filePath) ?? "";
                        return true;
                    }
                }
                catch
                {
                    // Try wrapper format ({"Scanners": [{...}, {...}]})
                    var settings = JsonConvert.DeserializeObject<LaserConfiguratorSettings>(jsonContent);
                    if (settings != null)
                    {
                        _settings = settings;
                        _settings.LastConfigDirectory = Path.GetDirectoryName(filePath) ?? "";
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveConfigurationAsync(string filePath)
        {
            try
            {
                var jsonContent = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, jsonContent);
                _settings.LastConfigDirectory = Path.GetDirectoryName(filePath) ?? "";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
                return false;
            }
        }

        public void ApplyConfiguration()
        {
            // Здесь можно добавить логику применения конфигурации
            // Например, отправить события об изменении конфигурации
        }

        public void CreateDefaultConfiguration()
        {
            _settings = new LaserConfiguratorSettings
            {
                AutoConnectOnLoad = false,
                LastConfigDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Scanners = new List<ScanatorConfiguration>
                {
                    CreateDefaultScannerConfigInternal("172.18.34.227", 0),
                    CreateDefaultScannerConfigInternal("172.18.34.228", 1)
                }
            };
        }

        public ScanatorConfiguration CreateDefaultScannerConfig()
        {
            return CreateDefaultScannerConfigInternal("172.18.34.227", 0);
        }

        private ScanatorConfiguration CreateDefaultScannerConfigInternal(string ipAddress, int seqIndex)
        {
            return new ScanatorConfiguration
            {
                CardInfo = new CardInfo
                {
                    IpAddress = ipAddress,
                    SeqIndex = seqIndex
                },
                ScannerConfig = new ScannerConfig
                {
                    FieldSizeX = 100.0f,
                    FieldSizeY = 100.0f,
                    ProtocolCode = 1,
                    OffsetX = 0.0f,
                    OffsetY = 0.0f
                },
                BeamConfig = new BeamConfig
                {
                    MinBeamDiameterMicron = 80.0,
                    RayleighLengthMicron = 5000.0,
                    M2 = 1.2
                },
                LaserPowerConfig = new LaserPowerConfig
                {
                    MaxPower = 500.0f,
                    ActualPowerCorrectionValue = new List<float> { 0, 500 }
                },
                ProcessVariablesMap = new ProcessVariablesMap
                {
                    NonDepends = new List<ProcessVariables>
                    {
                        new ProcessVariables
                        {
                            MarkSpeed = 1000,
                            JumpSpeed = 5000,
                            MarkDelay = 50,
                            JumpDelay = 50,
                            PolygonDelay = 50,
                            LaserOnDelay = 100,
                            LaserOffDelay = 100
                        }
                    },
                    MarkSpeed = new List<ProcessVariables>()
                },
                FunctionSwitcherConfig = new FunctionSwitcherConfig(),
                ThirdAxisConfig = new ThirdAxisConfig()
            };
        }
    }
}
