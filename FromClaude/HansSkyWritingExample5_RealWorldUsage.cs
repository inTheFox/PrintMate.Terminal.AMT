using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Hans.NET;

namespace PrintMateMC.Examples
{
    /// <summary>
    /// Реалистичный пример использования SkyWriting при конвертации CLI файлов
    /// Включает парсинг JSON параметров и правильную обработку всех регионов
    /// </summary>
    public class HansSkyWritingExample5_RealWorldUsage
    {
        /// <summary>
        /// Класс для десериализации JSON параметров из CLI файла
        /// </summary>
        public class CliParameters
        {
            // Edges (контуры)
            [JsonProperty("edge_laser_beam_diameter")]
            public string EdgeLaserBeamDiameter { get; set; }

            [JsonProperty("edge_laser_power")]
            public string EdgeLaserPower { get; set; }

            [JsonProperty("edge_laser_speed")]
            public string EdgeLaserSpeed { get; set; }

            [JsonProperty("edge_skywriting")]
            public string EdgeSkywriting { get; set; }

            // Downskin borders
            [JsonProperty("downskin_border_laser_beam_diameter")]
            public string DownskinBorderLaserBeamDiameter { get; set; }

            [JsonProperty("downskin_border_laser_power")]
            public string DownskinBorderLaserPower { get; set; }

            [JsonProperty("downskin_border_laser_speed")]
            public string DownskinBorderLaserSpeed { get; set; }

            [JsonProperty("downskin_border_skywriting")]
            public string DownskinBorderSkywriting { get; set; }

            // Downskin hatch
            [JsonProperty("downskin_hatch_laser_beam_diameter")]
            public string DownskinHatchLaserBeamDiameter { get; set; }

            [JsonProperty("downskin_hatch_laser_power")]
            public string DownskinHatchLaserPower { get; set; }

            [JsonProperty("downskin_hatch_laser_speed")]
            public string DownskinHatchLaserSpeed { get; set; }

            [JsonProperty("downskin_hatch_skywriting")]
            public string DownskinHatchSkywriting { get; set; }

            // Infill borders
            [JsonProperty("infill_border_laser_beam_diameter")]
            public string InfillBorderLaserBeamDiameter { get; set; }

            [JsonProperty("infill_border_laser_power")]
            public string InfillBorderLaserPower { get; set; }

            [JsonProperty("infill_border_laser_speed")]
            public string InfillBorderLaserSpeed { get; set; }

            [JsonProperty("infill_border_skywriting")]
            public string InfillBorderSkywriting { get; set; }

            // Infill hatch
            [JsonProperty("infill_hatch_laser_beam_diameter")]
            public string InfillHatchLaserBeamDiameter { get; set; }

            [JsonProperty("infill_hatch_laser_power")]
            public string InfillHatchLaserPower { get; set; }

            [JsonProperty("infill_hatch_laser_speed")]
            public string InfillHatchLaserSpeed { get; set; }

            [JsonProperty("infill_hatch_skywriting")]
            public string InfillHatchSkywriting { get; set; }

            // Upskin borders
            [JsonProperty("upskin_border_laser_beam_diameter")]
            public string UpskinBorderLaserBeamDiameter { get; set; }

            [JsonProperty("upskin_border_laser_power")]
            public string UpskinBorderLaserPower { get; set; }

            [JsonProperty("upskin_border_laser_speed")]
            public string UpskinBorderLaserSpeed { get; set; }

            [JsonProperty("upskin_border_skywriting")]
            public string UpskinBorderSkywriting { get; set; }

            // Upskin hatch
            [JsonProperty("upskin_hatch_laser_beam_diameter")]
            public string UpskinHatchLaserBeamDiameter { get; set; }

            [JsonProperty("upskin_hatch_laser_power")]
            public string UpskinHatchLaserPower { get; set; }

            [JsonProperty("upskin_hatch_laser_speed")]
            public string UpskinHatchLaserSpeed { get; set; }

            [JsonProperty("upskin_hatch_skywriting")]
            public string UpskinHatchSkywriting { get; set; }

            // Support borders
            [JsonProperty("support_border_laser_beam_diameter")]
            public string SupportBorderLaserBeamDiameter { get; set; }

            [JsonProperty("support_border_laser_power")]
            public string SupportBorderLaserPower { get; set; }

            [JsonProperty("support_border_laser_speed")]
            public string SupportBorderLaserSpeed { get; set; }

            [JsonProperty("support_border_skywriting")]
            public string SupportBorderSkywriting { get; set; }

            // Support hatch
            [JsonProperty("support_hatch_laser_beam_diameter")]
            public string SupportHatchLaserBeamDiameter { get; set; }

            [JsonProperty("support_hatch_laser_power")]
            public string SupportHatchLaserPower { get; set; }

            [JsonProperty("support_hatch_laser_speed")]
            public string SupportHatchLaserSpeed { get; set; }

            [JsonProperty("support_hatch_skywriting")]
            public string SupportHatchSkywriting { get; set; }
        }

        /// <summary>
        /// Полный рабочий пример конвертации CLI с SkyWriting
        /// </summary>
        public static void Example_RealWorldCliToHansConversion()
        {
            // JSON из вашего CLI файла (секция "base")
            string jsonParameters = @"{
                ""edge_laser_beam_diameter"": ""80"",
                ""edge_laser_power"": ""140"",
                ""edge_laser_speed"": ""550"",
                ""edge_skywriting"": ""1"",
                ""downskin_border_laser_beam_diameter"": ""80"",
                ""downskin_border_laser_power"": ""100"",
                ""downskin_border_laser_speed"": ""800"",
                ""downskin_border_skywriting"": ""1"",
                ""downskin_hatch_laser_beam_diameter"": ""80"",
                ""downskin_hatch_laser_power"": ""180"",
                ""downskin_hatch_laser_speed"": ""1600"",
                ""downskin_hatch_skywriting"": ""1"",
                ""infill_border_laser_beam_diameter"": ""80"",
                ""infill_border_laser_power"": ""140"",
                ""infill_border_laser_speed"": ""550"",
                ""infill_border_skywriting"": ""1"",
                ""infill_hatch_laser_beam_diameter"": ""80"",
                ""infill_hatch_laser_power"": ""260"",
                ""infill_hatch_laser_speed"": ""900"",
                ""infill_hatch_skywriting"": ""1"",
                ""upskin_border_laser_beam_diameter"": ""80"",
                ""upskin_border_laser_power"": ""170"",
                ""upskin_border_laser_speed"": ""500"",
                ""upskin_border_skywriting"": ""1"",
                ""upskin_hatch_laser_beam_diameter"": ""80"",
                ""upskin_hatch_laser_power"": ""210"",
                ""upskin_hatch_laser_speed"": ""800"",
                ""upskin_hatch_skywriting"": ""1"",
                ""support_border_laser_beam_diameter"": ""80"",
                ""support_border_laser_power"": ""100"",
                ""support_border_laser_speed"": ""425"",
                ""support_border_skywriting"": ""0"",
                ""support_hatch_laser_beam_diameter"": ""80"",
                ""support_hatch_laser_power"": ""260"",
                ""support_hatch_laser_speed"": ""900"",
                ""support_hatch_skywriting"": ""0""
            }";

            // Парсинг JSON
            var parameters = JsonConvert.DeserializeObject<CliParameters>(jsonParameters);

            Console.WriteLine("=== CLI to Hans Conversion with SkyWriting ===\n");

            // Стратегия: группировать регионы по SkyWriting
            // Файл 1: Все регионы с SkyWriting=1
            // Файл 2: Все регионы с SkyWriting=0

            ConvertRegionsWithSkyWriting(parameters, skywritingEnabled: true, "output_with_skywriting.bin");
            ConvertRegionsWithSkyWriting(parameters, skywritingEnabled: false, "output_without_skywriting.bin");

            Console.WriteLine("\n=== Conversion Complete ===");
            Console.WriteLine("Created 2 files:");
            Console.WriteLine("  1. output_with_skywriting.bin (edges, infill, upskin, downskin)");
            Console.WriteLine("  2. output_without_skywriting.bin (supports)");
        }

        /// <summary>
        /// Конвертировать регионы с определенным значением SkyWriting
        /// </summary>
        private static void ConvertRegionsWithSkyWriting(CliParameters parameters, bool skywritingEnabled, string outputFile)
        {
            int skywritingValue = skywritingEnabled ? 1 : 0;

            // Собрать все регионы с указанным значением SkyWriting
            var regions = new List<RegionInfo>();

            // Edges
            if (int.Parse(parameters.EdgeSkywriting) == skywritingValue)
            {
                regions.Add(new RegionInfo
                {
                    Name = "edges",
                    Diameter = double.Parse(parameters.EdgeLaserBeamDiameter),
                    Power = double.Parse(parameters.EdgeLaserPower),
                    Speed = double.Parse(parameters.EdgeLaserSpeed)
                });
            }

            // Downskin border
            if (int.Parse(parameters.DownskinBorderSkywriting) == skywritingValue)
            {
                regions.Add(new RegionInfo
                {
                    Name = "downskin_border",
                    Diameter = double.Parse(parameters.DownskinBorderLaserBeamDiameter),
                    Power = double.Parse(parameters.DownskinBorderLaserPower),
                    Speed = double.Parse(parameters.DownskinBorderLaserSpeed)
                });
            }

            // Downskin hatch
            if (int.Parse(parameters.DownskinHatchSkywriting) == skywritingValue)
            {
                regions.Add(new RegionInfo
                {
                    Name = "downskin_hatch",
                    Diameter = double.Parse(parameters.DownskinHatchLaserBeamDiameter),
                    Power = double.Parse(parameters.DownskinHatchLaserPower),
                    Speed = double.Parse(parameters.DownskinHatchLaserSpeed)
                });
            }

            // Infill border
            if (int.Parse(parameters.InfillBorderSkywriting) == skywritingValue)
            {
                regions.Add(new RegionInfo
                {
                    Name = "infill_border",
                    Diameter = double.Parse(parameters.InfillBorderLaserBeamDiameter),
                    Power = double.Parse(parameters.InfillBorderLaserPower),
                    Speed = double.Parse(parameters.InfillBorderLaserSpeed)
                });
            }

            // Infill hatch
            if (int.Parse(parameters.InfillHatchSkywriting) == skywritingValue)
            {
                regions.Add(new RegionInfo
                {
                    Name = "infill_hatch",
                    Diameter = double.Parse(parameters.InfillHatchLaserBeamDiameter),
                    Power = double.Parse(parameters.InfillHatchLaserPower),
                    Speed = double.Parse(parameters.InfillHatchLaserSpeed)
                });
            }

            // Upskin border
            if (int.Parse(parameters.UpskinBorderSkywriting) == skywritingValue)
            {
                regions.Add(new RegionInfo
                {
                    Name = "upskin_border",
                    Diameter = double.Parse(parameters.UpskinBorderLaserBeamDiameter),
                    Power = double.Parse(parameters.UpskinBorderLaserPower),
                    Speed = double.Parse(parameters.UpskinBorderLaserSpeed)
                });
            }

            // Upskin hatch
            if (int.Parse(parameters.UpskinHatchSkywriting) == skywritingValue)
            {
                regions.Add(new RegionInfo
                {
                    Name = "upskin_hatch",
                    Diameter = double.Parse(parameters.UpskinHatchLaserBeamDiameter),
                    Power = double.Parse(parameters.UpskinHatchLaserPower),
                    Speed = double.Parse(parameters.UpskinHatchLaserSpeed)
                });
            }

            // Support border
            if (int.Parse(parameters.SupportBorderSkywriting) == skywritingValue)
            {
                regions.Add(new RegionInfo
                {
                    Name = "support_border",
                    Diameter = double.Parse(parameters.SupportBorderLaserBeamDiameter),
                    Power = double.Parse(parameters.SupportBorderLaserPower),
                    Speed = double.Parse(parameters.SupportBorderLaserSpeed)
                });
            }

            // Support hatch
            if (int.Parse(parameters.SupportHatchSkywriting) == skywritingValue)
            {
                regions.Add(new RegionInfo
                {
                    Name = "support_hatch",
                    Diameter = double.Parse(parameters.SupportHatchLaserBeamDiameter),
                    Power = double.Parse(parameters.SupportHatchLaserPower),
                    Speed = double.Parse(parameters.SupportHatchLaserSpeed)
                });
            }

            // Если нет регионов с этим значением SkyWriting, пропустить
            if (regions.Count == 0)
            {
                Console.WriteLine($"No regions with SkyWriting={skywritingValue}, skipping {outputFile}");
                return;
            }

            // Создать Hans файл
            Console.WriteLine($"\nCreating {outputFile} (SkyWriting={skywritingValue}):");
            Console.WriteLine($"  Regions: {regions.Count}");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1); // SPI, 3D mode

            // ВАЖНО: Установить SkyWriting для всего файла
            HM_UDM_DLL.UDM_SkyWriting(skywritingValue);

            // Создать параметры слоев
            MarkParameter[] layers = new MarkParameter[regions.Count];
            for (int i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                layers[i] = new MarkParameter
                {
                    MarkSpeed = (uint)region.Speed,
                    LaserPower = (float)(region.Power / 500.0 * 100.0), // W -> %
                    JumpSpeed = 5000,
                    LaserOnDelay = GetLaserOnDelay(region.Speed),
                    LaserOffDelay = GetLaserOffDelay(region.Speed),
                    MarkDelay = 100,
                    JumpDelay = 100,
                    PolygonDelay = 50,
                    MarkCount = 1,
                    Frequency = 50.0f,
                    DutyCycle = 0.5f
                };

                Console.WriteLine($"    - {region.Name}: {region.Power}W @ {region.Speed}mm/s, ∅{region.Diameter}μm");
            }

            HM_UDM_DLL.UDM_SetLayersPara(layers, layers.Length);

            // Добавить геометрию для каждого слоя
            for (int layerIndex = 0; layerIndex < regions.Count; layerIndex++)
            {
                var region = regions[layerIndex];
                float zOffset = CalculateZOffset(region.Diameter);

                // Пример геометрии (в реальном приложении это будет из CLI файла)
                structUdmPos[] points = new structUdmPos[]
                {
                    new structUdmPos { x = 0, y = 0, z = zOffset },
                    new structUdmPos { x = 5, y = 0, z = zOffset },
                    new structUdmPos { x = 5, y = 5, z = zOffset },
                    new structUdmPos { x = 0, y = 5, z = zOffset },
                    new structUdmPos { x = 0, y = 0, z = zOffset }
                };

                HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, layerIndex);
            }

            // Генерация и сохранение
            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile(outputFile);
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine($"  ✓ Saved: {outputFile}");
        }

        private class RegionInfo
        {
            public string Name { get; set; }
            public double Diameter { get; set; }
            public double Power { get; set; }
            public double Speed { get; set; }
        }

        private static float GetLaserOnDelay(double speed)
        {
            if (speed <= 800) return 50.0f;
            if (speed <= 1250) return 40.0f;
            return 30.0f;
        }

        private static float GetLaserOffDelay(double speed)
        {
            if (speed <= 800) return 50.0f;
            if (speed <= 1250) return 40.0f;
            return 30.0f;
        }

        private static float CalculateZOffset(double beamDiameterMicrons)
        {
            // Параметры калибровки
            double nominalDiameter = 120.0; // микроны
            double coefficient = 0.3;       // мм/10μm

            return (float)((beamDiameterMicrons - nominalDiameter) / 10.0 * coefficient);
        }
    }
}
