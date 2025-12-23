using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hans.NET;

namespace PrintMateMC.Examples
{
    /// <summary>
    /// Примеры использования UDM_SetSkyWritingMode с параметрами из CLI конфигурации
    /// </summary>
    public class HansSkyWritingMode_CliExamples
    {
        /// <summary>
        /// Параметры SkyWriting из CLI JSON
        /// </summary>
        public class CliSkyWritingParameters
        {
            [JsonPropertyName("edge_skywriting")]
            public string EdgeSkywriting { get; set; } = "0";

            [JsonPropertyName("edge_skywriting_uniformLen")]
            public string EdgeSkywritingUniformLen { get; set; } = "0.1";

            [JsonPropertyName("edge_skywriting_accLen")]
            public string EdgeSkywritingAccLen { get; set; } = "0.05";

            [JsonPropertyName("edge_skywriting_angleLimit")]
            public string EdgeSkywritingAngleLimit { get; set; } = "120.0";

            [JsonPropertyName("infill_hatch_skywriting")]
            public string InfillHatchSkywriting { get; set; } = "1";

            [JsonPropertyName("infill_hatch_skywriting_uniformLen")]
            public string InfillHatchSkywritingUniformLen { get; set; } = "0.15";

            [JsonPropertyName("infill_hatch_skywriting_accLen")]
            public string InfillHatchSkywritingAccLen { get; set; } = "0.08";

            [JsonPropertyName("infill_hatch_skywriting_angleLimit")]
            public string InfillHatchSkywritingAngleLimit { get; set; } = "90.0";

            [JsonPropertyName("support_hatch_skywriting")]
            public string SupportHatchSkywriting { get; set; } = "0";
        }

        /// <summary>
        /// Параметры SkyWriting из конфигурации сканера
        /// </summary>
        public class ScannerSkyWritingConfig
        {
            [JsonPropertyName("swenable")]
            public bool SWEnable { get; set; } = false;

            [JsonPropertyName("umax")]
            public double Umax { get; set; } = 0.1;  // Используется как uniformLen

            [JsonPropertyName("accLen")]
            public double AccLen { get; set; } = 0.05;

            [JsonPropertyName("angleLimit")]
            public double AngleLimit { get; set; } = 120.0;
        }

        /// <summary>
        /// Пример 1: Базовое использование UDM_SetSkyWritingMode
        /// Простое включение/выключение с параметрами по умолчанию
        /// </summary>
        public static void Example1_BasicSkyWritingMode()
        {
            Console.WriteLine("=== Example 1: Basic UDM_SetSkyWritingMode ===\n");

            // Параметры из CLI
            int enable = 1;              // 1 = включить, 0 = выключить
            int mode = 0;                // 0 = стандартный режим
            float uniformLen = 0.1f;     // мм - длина равномерного участка (соответствует umax)
            float accLen = 0.05f;        // мм - длина участка ускорения
            float angleLimit = 120.0f;   // градусы - предельный угол

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Установить расширенный режим SkyWriting
            int result = HM_UDM_DLL.UDM_SetSkyWritingMode(
                enable,
                mode,
                uniformLen,
                accLen,
                angleLimit
            );

            Console.WriteLine($"UDM_SetSkyWritingMode result: {result}");
            Console.WriteLine($"  enable: {enable}");
            Console.WriteLine($"  mode: {mode}");
            Console.WriteLine($"  uniformLen: {uniformLen} mm");
            Console.WriteLine($"  accLen: {accLen} mm");
            Console.WriteLine($"  angleLimit: {angleLimit}°\n");

            // Добавить геометрию и сохранить...
        }

        /// <summary>
        /// Пример 2: Использование параметров из CLI JSON
        /// Парсинг JSON и применение параметров для региона edges
        /// </summary>
        public static void Example2_FromCliJson_Edges()
        {
            Console.WriteLine("=== Example 2: SkyWriting для Edges из CLI JSON ===\n");

            string cliJson = @"{
                ""edge_skywriting"": ""1"",
                ""edge_skywriting_uniformLen"": ""0.12"",
                ""edge_skywriting_accLen"": ""0.06"",
                ""edge_skywriting_angleLimit"": ""110.0"",
                ""edge_laser_speed"": ""550"",
                ""edge_laser_power"": ""140"",
                ""edge_laser_beam_diameter"": ""80""
            }";

            var parameters = JsonSerializer.Deserialize<CliSkyWritingParameters>(cliJson);

            int enable = int.Parse(parameters.EdgeSkywriting);
            float uniformLen = float.Parse(parameters.EdgeSkywritingUniformLen);
            float accLen = float.Parse(parameters.EdgeSkywritingAccLen);
            float angleLimit = float.Parse(parameters.EdgeSkywritingAngleLimit);

            Console.WriteLine("Параметры из CLI для edges:");
            Console.WriteLine($"  SkyWriting: {(enable == 1 ? "ENABLED" : "DISABLED")}");
            Console.WriteLine($"  uniformLen: {uniformLen} mm");
            Console.WriteLine($"  accLen: {accLen} mm");
            Console.WriteLine($"  angleLimit: {angleLimit}°\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Применить параметры
            HM_UDM_DLL.UDM_SetSkyWritingMode(
                enable,
                0,  // mode всегда 0
                uniformLen,
                accLen,
                angleLimit
            );

            Console.WriteLine("✅ Параметры SkyWriting применены для edges\n");
        }

        /// <summary>
        /// Пример 3: Использование параметров из конфигурации сканера
        /// Маппинг swenable и umax на параметры UDM_SetSkyWritingMode
        /// </summary>
        public static void Example3_FromScannerConfig()
        {
            Console.WriteLine("=== Example 3: SkyWriting из конфигурации сканера ===\n");

            string scannerConfigJson = @"{
                ""swenable"": true,
                ""umax"": 0.15,
                ""accLen"": 0.075,
                ""angleLimit"": 100.0
            }";

            var config = JsonSerializer.Deserialize<ScannerSkyWritingConfig>(scannerConfigJson);

            Console.WriteLine("Параметры из scanner config:");
            Console.WriteLine($"  swenable: {config.SWEnable}");
            Console.WriteLine($"  umax: {config.Umax} mm");
            Console.WriteLine($"  accLen: {config.AccLen} mm");
            Console.WriteLine($"  angleLimit: {config.AngleLimit}°\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Маппинг: swenable → enable, umax → uniformLen
            int enable = config.SWEnable ? 1 : 0;
            float uniformLen = (float)config.Umax;      // umax = uniformLen
            float accLen = (float)config.AccLen;
            float angleLimit = (float)config.AngleLimit;

            HM_UDM_DLL.UDM_SetSkyWritingMode(
                enable,
                0,
                uniformLen,
                accLen,
                angleLimit
            );

            Console.WriteLine("✅ Параметры из scanner config применены\n");
        }

        /// <summary>
        /// Пример 4: Различные параметры для разных регионов
        /// Создание отдельных файлов для regions с разными настройками SkyWriting
        /// </summary>
        public static void Example4_DifferentParametersPerRegion()
        {
            Console.WriteLine("=== Example 4: Разные параметры SkyWriting для регионов ===\n");

            // Edges: агрессивное сглаживание
            CreateFileWithSkyWriting(
                regionName: "edges",
                enable: 1,
                uniformLen: 0.15f,   // Больше = более плавно
                accLen: 0.08f,       // Больше = более мягкое ускорение
                angleLimit: 90.0f,   // Меньше = только для плавных углов
                outputFile: "edges_aggressive_smoothing.bin"
            );

            // Infill: умеренное сглаживание
            CreateFileWithSkyWriting(
                regionName: "infill",
                enable: 1,
                uniformLen: 0.1f,
                accLen: 0.05f,
                angleLimit: 120.0f,   // Стандартное значение
                outputFile: "infill_moderate_smoothing.bin"
            );

            // Support: без SkyWriting
            CreateFileWithSkyWriting(
                regionName: "support",
                enable: 0,
                uniformLen: 0.0f,     // Игнорируется при enable=0
                accLen: 0.0f,
                angleLimit: 0.0f,
                outputFile: "support_no_skywriting.bin"
            );

            Console.WriteLine("✅ Созданы 3 файла с разными параметрами SkyWriting\n");
        }

        private static void CreateFileWithSkyWriting(
            string regionName,
            int enable,
            float uniformLen,
            float accLen,
            float angleLimit,
            string outputFile)
        {
            Console.WriteLine($"Создание файла для {regionName}:");
            Console.WriteLine($"  SkyWriting: {(enable == 1 ? "ON" : "OFF")}");
            if (enable == 1)
            {
                Console.WriteLine($"  uniformLen: {uniformLen} mm");
                Console.WriteLine($"  accLen: {accLen} mm");
                Console.WriteLine($"  angleLimit: {angleLimit}°");
            }

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            HM_UDM_DLL.UDM_SetSkyWritingMode(
                enable,
                0,
                uniformLen,
                accLen,
                angleLimit
            );

            // Настроить параметры слоя
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = new MarkParameter
            {
                MarkSpeed = 800,
                LaserPower = 50.0f,
                JumpSpeed = 5000
            };
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            // Добавить тестовую геометрию
            structUdmPos[] points = new structUdmPos[]
            {
                new structUdmPos { x = 0, y = 0, z = 0 },
                new structUdmPos { x = 5, y = 0, z = 0 },
                new structUdmPos { x = 5, y = 5, z = 0 },
                new structUdmPos { x = 0, y = 0, z = 0 }
            };
            HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, 0);

            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile(outputFile);
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine($"  Saved: {outputFile}\n");
        }

        /// <summary>
        /// Пример 5: Адаптивные параметры на основе скорости
        /// Чем выше скорость, тем более агрессивное сглаживание
        /// </summary>
        public static void Example5_AdaptiveParametersBySpeed()
        {
            Console.WriteLine("=== Example 5: Адаптивные параметры SkyWriting по скорости ===\n");

            var speedConfigs = new[]
            {
                new { Speed = 500, UniformLen = 0.08f, AccLen = 0.04f, AngleLimit = 130.0f },
                new { Speed = 1000, UniformLen = 0.12f, AccLen = 0.06f, AngleLimit = 110.0f },
                new { Speed = 2000, UniformLen = 0.18f, AccLen = 0.09f, AngleLimit = 90.0f }
            };

            foreach (var config in speedConfigs)
            {
                Console.WriteLine($"Скорость: {config.Speed} mm/s");
                Console.WriteLine($"  uniformLen: {config.UniformLen} mm (больше для высоких скоростей)");
                Console.WriteLine($"  accLen: {config.AccLen} mm");
                Console.WriteLine($"  angleLimit: {config.AngleLimit}° (меньше для высоких скоростей)\n");

                HM_UDM_DLL.UDM_NewFile();
                HM_UDM_DLL.UDM_SetProtocol(0, 1);

                HM_UDM_DLL.UDM_SetSkyWritingMode(
                    enable: 1,
                    mode: 0,
                    uniformLen: config.UniformLen,
                    accLen: config.AccLen,
                    angleLimit: config.AngleLimit
                );

                MarkParameter[] layers = new MarkParameter[1];
                layers[0] = new MarkParameter
                {
                    MarkSpeed = (uint)config.Speed,
                    LaserPower = 50.0f,
                    JumpSpeed = 5000
                };
                HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

                // Добавить геометрию...
                structUdmPos[] points = new structUdmPos[]
                {
                    new structUdmPos { x = 0, y = 0, z = 0 },
                    new structUdmPos { x = 10, y = 0, z = 0 }
                };
                HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, 0);

                HM_UDM_DLL.UDM_Main();
                HM_UDM_DLL.UDM_SaveToFile($"speed_{config.Speed}_adaptive.bin");
                HM_UDM_DLL.UDM_EndMain();
            }

            Console.WriteLine("✅ Созданы файлы с адаптивными параметрами для разных скоростей\n");
        }

        /// <summary>
        /// Пример 6: Полная конвертация CLI с параметрами SkyWriting
        /// Реальный сценарий использования
        /// </summary>
        public static void Example6_CompleteCliConversionWithSkyWritingMode()
        {
            Console.WriteLine("=== Example 6: Полная конвертация CLI с UDM_SetSkyWritingMode ===\n");

            string cliJson = @"{
                ""edge_skywriting"": ""1"",
                ""edge_skywriting_uniformLen"": ""0.1"",
                ""edge_skywriting_accLen"": ""0.05"",
                ""edge_skywriting_angleLimit"": ""120.0"",
                ""edge_laser_speed"": ""550"",
                ""edge_laser_power"": ""140"",
                ""edge_laser_beam_diameter"": ""80"",

                ""infill_hatch_skywriting"": ""1"",
                ""infill_hatch_skywriting_uniformLen"": ""0.15"",
                ""infill_hatch_skywriting_accLen"": ""0.08"",
                ""infill_hatch_skywriting_angleLimit"": ""100.0"",
                ""infill_hatch_laser_speed"": ""900"",
                ""infill_hatch_laser_power"": ""260"",
                ""infill_hatch_laser_beam_diameter"": ""80"",

                ""support_hatch_skywriting"": ""0"",
                ""support_hatch_laser_speed"": ""900"",
                ""support_hatch_laser_power"": ""260"",
                ""support_hatch_laser_beam_diameter"": ""80""
            }";

            var parameters = JsonSerializer.Deserialize<CliSkyWritingParameters>(cliJson);

            // Файл 1: Edges с SkyWriting
            Console.WriteLine("Файл 1: edges_with_skywriting.bin");
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            HM_UDM_DLL.UDM_SetSkyWritingMode(
                enable: int.Parse(parameters.EdgeSkywriting),
                mode: 0,
                uniformLen: float.Parse(parameters.EdgeSkywritingUniformLen),
                accLen: float.Parse(parameters.EdgeSkywritingAccLen),
                angleLimit: float.Parse(parameters.EdgeSkywritingAngleLimit)
            );

            Console.WriteLine($"  SkyWriting: ON");
            Console.WriteLine($"  uniformLen: {parameters.EdgeSkywritingUniformLen} mm");
            Console.WriteLine($"  accLen: {parameters.EdgeSkywritingAccLen} mm");
            Console.WriteLine($"  angleLimit: {parameters.EdgeSkywritingAngleLimit}°\n");

            // Настроить параметры и добавить геометрию...
            MarkParameter[] edgeLayers = new MarkParameter[1];
            edgeLayers[0] = new MarkParameter { MarkSpeed = 550, LaserPower = 28.0f, JumpSpeed = 5000 };
            HM_UDM_DLL.UDM_SetLayersPara(edgeLayers, 1);

            // Добавить геометрию edges...
            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("edges_with_skywriting.bin");
            HM_UDM_DLL.UDM_EndMain();

            // Файл 2: Infill с другими параметрами SkyWriting
            Console.WriteLine("Файл 2: infill_with_skywriting.bin");
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            HM_UDM_DLL.UDM_SetSkyWritingMode(
                enable: int.Parse(parameters.InfillHatchSkywriting),
                mode: 0,
                uniformLen: float.Parse(parameters.InfillHatchSkywritingUniformLen),
                accLen: float.Parse(parameters.InfillHatchSkywritingAccLen),
                angleLimit: float.Parse(parameters.InfillHatchSkywritingAngleLimit)
            );

            Console.WriteLine($"  SkyWriting: ON");
            Console.WriteLine($"  uniformLen: {parameters.InfillHatchSkywritingUniformLen} mm");
            Console.WriteLine($"  accLen: {parameters.InfillHatchSkywritingAccLen} mm");
            Console.WriteLine($"  angleLimit: {parameters.InfillHatchSkywritingAngleLimit}°\n");

            // Настроить параметры и добавить геометрию...
            MarkParameter[] infillLayers = new MarkParameter[1];
            infillLayers[0] = new MarkParameter { MarkSpeed = 900, LaserPower = 52.0f, JumpSpeed = 5000 };
            HM_UDM_DLL.UDM_SetLayersPara(infillLayers, 1);

            // Добавить геометрию infill...
            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("infill_with_skywriting.bin");
            HM_UDM_DLL.UDM_EndMain();

            // Файл 3: Support без SkyWriting
            Console.WriteLine("Файл 3: support_no_skywriting.bin");
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            HM_UDM_DLL.UDM_SetSkyWritingMode(
                enable: int.Parse(parameters.SupportHatchSkywriting),
                mode: 0,
                uniformLen: 0.0f,
                accLen: 0.0f,
                angleLimit: 0.0f
            );

            Console.WriteLine($"  SkyWriting: OFF\n");

            // Настроить параметры и добавить геометрию...
            MarkParameter[] supportLayers = new MarkParameter[1];
            supportLayers[0] = new MarkParameter { MarkSpeed = 900, LaserPower = 52.0f, JumpSpeed = 5000 };
            HM_UDM_DLL.UDM_SetLayersPara(supportLayers, 1);

            // Добавить геометрию support...
            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("support_no_skywriting.bin");
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine("✅ Созданы 3 файла с различными параметрами SkyWriting\n");
        }
    }
}
