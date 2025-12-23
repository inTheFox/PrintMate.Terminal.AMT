using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hans.NET;

namespace PrintMateMC.Examples
{
    /// <summary>
    /// Примеры использования SkyWriting на основе ВАШЕЙ реальной конфигурации
    /// </summary>
    public class HansSkyWriting_FromYourConfig
    {
        #region JSON Classes для вашей конфигурации

        public class ScannerCardConfig
        {
            [JsonPropertyName("cardInfo")]
            public CardInfo CardInfo { get; set; }

            [JsonPropertyName("processVariablesMap")]
            public ProcessVariablesMap ProcessVariablesMap { get; set; }

            [JsonPropertyName("scannerConfig")]
            public ScannerConfigData ScannerConfig { get; set; }

            [JsonPropertyName("beamConfig")]
            public BeamConfig BeamConfig { get; set; }

            [JsonPropertyName("laserPowerConfig")]
            public LaserPowerConfig LaserPowerConfig { get; set; }

            [JsonPropertyName("functionSwitcherConfig")]
            public FunctionSwitcherConfig FunctionSwitcherConfig { get; set; }

            [JsonPropertyName("thirdAxisConfig")]
            public ThirdAxisConfig ThirdAxisConfig { get; set; }
        }

        public class CardInfo
        {
            [JsonPropertyName("ipAddress")]
            public string IpAddress { get; set; }

            [JsonPropertyName("seqIndex")]
            public int SeqIndex { get; set; }
        }

        public class ProcessVariablesMap
        {
            [JsonPropertyName("nonDepends")]
            public SpeedVariables[] NonDepends { get; set; }

            [JsonPropertyName("markSpeed")]
            public SpeedVariables[] MarkSpeed { get; set; }
        }

        public class SpeedVariables
        {
            [JsonPropertyName("markSpeed")]
            public int MarkSpeed { get; set; }

            [JsonPropertyName("jumpSpeed")]
            public int JumpSpeed { get; set; }

            [JsonPropertyName("polygonDelay")]
            public int PolygonDelay { get; set; }

            [JsonPropertyName("jumpDelay")]
            public int JumpDelay { get; set; }

            [JsonPropertyName("markDelay")]
            public int MarkDelay { get; set; }

            [JsonPropertyName("laserOnDelay")]
            public double LaserOnDelay { get; set; }

            [JsonPropertyName("laserOffDelay")]
            public double LaserOffDelay { get; set; }

            [JsonPropertyName("laserOnDelayForSkyWriting")]
            public double LaserOnDelayForSkyWriting { get; set; }

            [JsonPropertyName("laserOffDelayForSkyWriting")]
            public double LaserOffDelayForSkyWriting { get; set; }

            [JsonPropertyName("curBeamDiameterMicron")]
            public double CurBeamDiameterMicron { get; set; }

            [JsonPropertyName("curPower")]
            public double CurPower { get; set; }

            [JsonPropertyName("jumpMaxLengthLimitMm")]
            public double JumpMaxLengthLimitMm { get; set; }

            [JsonPropertyName("minJumpDelay")]
            public int MinJumpDelay { get; set; }

            [JsonPropertyName("umax")]
            public double Umax { get; set; }

            [JsonPropertyName("swenable")]
            public bool SWEnable { get; set; }
        }

        public class ScannerConfigData
        {
            [JsonPropertyName("fieldSizeX")]
            public double FieldSizeX { get; set; }

            [JsonPropertyName("fieldSizeY")]
            public double FieldSizeY { get; set; }

            [JsonPropertyName("protocolCode")]
            public int ProtocolCode { get; set; }

            [JsonPropertyName("coordinateTypeCode")]
            public int CoordinateTypeCode { get; set; }

            [JsonPropertyName("offsetX")]
            public double OffsetX { get; set; }

            [JsonPropertyName("offsetY")]
            public double OffsetY { get; set; }

            [JsonPropertyName("offsetZ")]
            public double OffsetZ { get; set; }

            [JsonPropertyName("scaleX")]
            public double ScaleX { get; set; }

            [JsonPropertyName("scaleY")]
            public double ScaleY { get; set; }

            [JsonPropertyName("scaleZ")]
            public double ScaleZ { get; set; }

            [JsonPropertyName("rotateAngle")]
            public double RotateAngle { get; set; }
        }

        public class BeamConfig
        {
            [JsonPropertyName("minBeamDiameterMicron")]
            public double MinBeamDiameterMicron { get; set; }

            [JsonPropertyName("wavelengthNano")]
            public double WavelengthNano { get; set; }

            [JsonPropertyName("rayleighLengthMicron")]
            public double RayleighLengthMicron { get; set; }

            [JsonPropertyName("m2")]
            public double M2 { get; set; }

            [JsonPropertyName("focalLengthMm")]
            public double FocalLengthMm { get; set; }
        }

        public class LaserPowerConfig
        {
            [JsonPropertyName("maxPower")]
            public double MaxPower { get; set; }

            [JsonPropertyName("actualPowerCorrectionValue")]
            public double[] ActualPowerCorrectionValue { get; set; }

            [JsonPropertyName("powerOffsetKFactor")]
            public double PowerOffsetKFactor { get; set; }

            [JsonPropertyName("powerOffsetCFactor")]
            public double PowerOffsetCFactor { get; set; }
        }

        public class FunctionSwitcherConfig
        {
            [JsonPropertyName("enablePowerOffset")]
            public bool EnablePowerOffset { get; set; }

            [JsonPropertyName("enablePowerCorrection")]
            public bool EnablePowerCorrection { get; set; }

            [JsonPropertyName("enableZCorrection")]
            public bool EnableZCorrection { get; set; }

            [JsonPropertyName("enableDiameterChange")]
            public bool EnableDiameterChange { get; set; }

            [JsonPropertyName("enableDynamicChangeVariables")]
            public bool EnableDynamicChangeVariables { get; set; }

            [JsonPropertyName("limitVariablesMinPoint")]
            public bool LimitVariablesMinPoint { get; set; }

            [JsonPropertyName("limitVariablesMaxPoint")]
            public bool LimitVariablesMaxPoint { get; set; }

            [JsonPropertyName("enableVariableJumpDelay")]
            public bool EnableVariableJumpDelay { get; set; }
        }

        public class ThirdAxisConfig
        {
            [JsonPropertyName("bfactor")]
            public double BFactor { get; set; }

            [JsonPropertyName("cfactor")]
            public double CFactor { get; set; }

            [JsonPropertyName("afactor")]
            public double AFactor { get; set; }
        }

        #endregion

        /// <summary>
        /// Пример 1: Применение SkyWriting из ВАШЕЙ конфигурации
        /// Использует swenable и umax из JSON
        /// </summary>
        public static void Example1_ApplyFromYourConfig()
        {
            Console.WriteLine("=== Example 1: Применение SkyWriting из вашей конфигурации ===\n");

            // Ваш JSON (первый лазер - 172.18.34.227)
            string json = @"[{
                ""cardInfo"": {
                    ""ipAddress"": ""172.18.34.227"",
                    ""seqIndex"": 0
                },
                ""processVariablesMap"": {
                    ""markSpeed"": [
                        {
                            ""markSpeed"": 800,
                            ""umax"": 0.1,
                            ""swenable"": true
                        }
                    ]
                }
            }]";

            var configs = JsonSerializer.Deserialize<ScannerCardConfig[]>(json);
            var laser1 = configs[0];

            // Параметры для скорости 800 mm/s
            var speedConfig = laser1.ProcessVariablesMap.MarkSpeed
                .FirstOrDefault(s => s.MarkSpeed == 800);

            if (speedConfig == null)
            {
                Console.WriteLine("Speed config not found!");
                return;
            }

            // Применить SkyWriting
            ApplySkyWritingFromConfig(speedConfig);
        }

        /// <summary>
        /// Пример 2: Автоматический выбор параметров по скорости
        /// </summary>
        public static void Example2_AutoSelectBySpeed(string configFilePath, int desiredSpeed)
        {
            Console.WriteLine($"=== Example 2: Автовыбор параметров для скорости {desiredSpeed} mm/s ===\n");

            // Загрузить конфигурацию
            string json = File.ReadAllText(configFilePath);
            var configs = JsonSerializer.Deserialize<ScannerCardConfig[]>(json);

            // Первый лазер
            var laser1 = configs[0];

            // Найти ближайшую скорость
            var speedConfig = laser1.ProcessVariablesMap.MarkSpeed
                .OrderBy(s => Math.Abs(s.MarkSpeed - desiredSpeed))
                .FirstOrDefault();

            if (speedConfig != null)
            {
                Console.WriteLine($"Найдена конфигурация для скорости: {speedConfig.MarkSpeed} mm/s");
                ApplySkyWritingFromConfig(speedConfig);
            }
        }

        /// <summary>
        /// Пример 3: Упрощенная версия для ВАШЕЙ конфигурации
        /// Все скорости имеют одинаковые параметры: umax=0.1, swenable=true
        /// </summary>
        public static void Example3_SimplifiedForYourConfig()
        {
            Console.WriteLine("=== Example 3: Упрощенная версия (фиксированные параметры) ===\n");

            // В вашей конфигурации ВСЕ скорости имеют:
            // - umax = 0.1
            // - swenable = true

            Console.WriteLine("Наблюдение: все скорости в конфигурации используют одинаковые параметры");
            Console.WriteLine("  umax = 0.1 mm");
            Console.WriteLine("  swenable = true\n");

            Console.WriteLine("Поэтому можно использовать фиксированные значения:\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            HM_UDM_DLL.UDM_SetSkyWritingMode(
                enable: 1,              // Всегда ON (swenable: true)
                mode: 0,
                uniformLen: 0.1f,       // Всегда 0.1 (umax: 0.1)
                accLen: 0.05f,          // = uniformLen / 2
                angleLimit: 120.0f      // Default
            );

            Console.WriteLine("✅ SkyWriting применен с фиксированными параметрами");
            Console.WriteLine("   enable: 1");
            Console.WriteLine("   uniformLen: 0.1 mm");
            Console.WriteLine("   accLen: 0.05 mm");
            Console.WriteLine("   angleLimit: 120.0°\n");
        }

        /// <summary>
        /// Пример 4: Два лазера с разными параметрами
        /// </summary>
        public static void Example4_TwoLasersFromConfig(string configFilePath)
        {
            Console.WriteLine("=== Example 4: Применение параметров для двух лазеров ===\n");

            string json = File.ReadAllText(configFilePath);
            var configs = JsonSerializer.Deserialize<ScannerCardConfig[]>(json);

            // Лазер 1: 172.18.34.227
            var laser1 = configs[0];
            Console.WriteLine($"Лазер 1 (IP: {laser1.CardInfo.IpAddress})");
            var speed1 = laser1.ProcessVariablesMap.MarkSpeed[0];
            ApplySkyWritingFromConfig(speed1);

            Console.WriteLine();

            // Лазер 2: 172.18.34.228
            var laser2 = configs[1];
            Console.WriteLine($"Лазер 2 (IP: {laser2.CardInfo.IpAddress})");
            var speed2 = laser2.ProcessVariablesMap.MarkSpeed[0];
            ApplySkyWritingFromConfig(speed2);
        }

        /// <summary>
        /// Пример 5: Использование laserOnDelayForSkyWriting и laserOffDelayForSkyWriting
        /// Эти параметры ОТЛИЧАЮТСЯ от обычных задержек когда SkyWriting включен
        /// </summary>
        public static void Example5_SkyWritingDelays()
        {
            Console.WriteLine("=== Example 5: Учет специальных задержек для SkyWriting ===\n");

            string json = @"{
                ""markSpeed"": 800,
                ""laserOnDelay"": 420.0,
                ""laserOffDelay"": 490.0,
                ""laserOnDelayForSkyWriting"": 600.0,
                ""laserOffDelayForSkyWriting"": 730.0,
                ""umax"": 0.1,
                ""swenable"": true
            }";

            var speedConfig = JsonSerializer.Deserialize<SpeedVariables>(json);

            Console.WriteLine("Обычные задержки (без SkyWriting):");
            Console.WriteLine($"  laserOnDelay: {speedConfig.LaserOnDelay} ns");
            Console.WriteLine($"  laserOffDelay: {speedConfig.LaserOffDelay} ns\n");

            Console.WriteLine("Специальные задержки (с SkyWriting):");
            Console.WriteLine($"  laserOnDelayForSkyWriting: {speedConfig.LaserOnDelayForSkyWriting} ns");
            Console.WriteLine($"  laserOffDelayForSkyWriting: {speedConfig.LaserOffDelayForSkyWriting} ns\n");

            // Применить параметры
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // SkyWriting
            ApplySkyWritingFromConfig(speedConfig);

            // Параметры слоя с задержками ДЛЯ SKYWRITING
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = new MarkParameter
            {
                MarkSpeed = (uint)speedConfig.MarkSpeed,
                LaserPower = 50.0f,
                JumpSpeed = (uint)speedConfig.JumpSpeed,
                LaserOnDelay = (float)speedConfig.LaserOnDelayForSkyWriting,   // ← Специальные задержки!
                LaserOffDelay = (float)speedConfig.LaserOffDelayForSkyWriting, // ← Специальные задержки!
                MarkDelay = (uint)speedConfig.MarkDelay,
                JumpDelay = (uint)speedConfig.JumpDelay,
                PolygonDelay = (uint)speedConfig.PolygonDelay,
                MarkCount = 1
            };

            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            Console.WriteLine("✅ Применены специальные задержки для SkyWriting режима\n");
        }

        /// <summary>
        /// Главная функция применения SkyWriting из конфигурации
        /// Аналог того что делает Hans4Java внутри
        /// </summary>
        private static void ApplySkyWritingFromConfig(SpeedVariables config)
        {
            // Параметры ИЗ конфигурации
            int enable = config.SWEnable ? 1 : 0;
            float uniformLen = (float)config.Umax;

            // Параметры по умолчанию (как в Hans4Java)
            int mode = 0;
            float accLen = uniformLen * 0.5f;    // Эвристика: половина от uniformLen
            float angleLimit = 120.0f;           // Стандартное значение

            Console.WriteLine($"Параметры SkyWriting:");
            Console.WriteLine($"  enable: {enable} (из swenable: {config.SWEnable})");
            Console.WriteLine($"  uniformLen: {uniformLen} mm (из umax: {config.Umax})");
            Console.WriteLine($"  accLen: {accLen} mm (calculated = uniformLen / 2)");
            Console.WriteLine($"  angleLimit: {angleLimit}° (default value)");

            HM_UDM_DLL.UDM_SetSkyWritingMode(
                enable,
                mode,
                uniformLen,
                accLen,
                angleLimit
            );

            Console.WriteLine("✅ UDM_SetSkyWritingMode вызван успешно\n");
        }

        /// <summary>
        /// Пример 6: Отличия между двумя лазерами
        /// </summary>
        public static void Example6_CompareTwoLasers(string configFilePath)
        {
            Console.WriteLine("=== Example 6: Сравнение параметров двух лазеров ===\n");

            string json = File.ReadAllText(configFilePath);
            var configs = JsonSerializer.Deserialize<ScannerCardConfig[]>(json);

            var laser1 = configs[0];
            var laser2 = configs[1];

            Console.WriteLine("┌─────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│                   Сравнение лазеров                             │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"│ Параметр              │ Лазер 1 (227)   │ Лазер 2 (228)   │");
            Console.WriteLine("├─────────────────────────────────────────────────────────────────┤");

            var speed1 = laser1.ProcessVariablesMap.MarkSpeed[0];
            var speed2 = laser2.ProcessVariablesMap.MarkSpeed[0];

            Console.WriteLine($"│ IP Address            │ {laser1.CardInfo.IpAddress,15} │ {laser2.CardInfo.IpAddress,15} │");
            Console.WriteLine($"│ seqIndex              │ {laser1.CardInfo.SeqIndex,15} │ {laser2.CardInfo.SeqIndex,15} │");
            Console.WriteLine($"│ swenable              │ {speed1.SWEnable,15} │ {speed2.SWEnable,15} │");
            Console.WriteLine($"│ umax (uniformLen)     │ {speed1.Umax,15} │ {speed2.Umax,15} │");
            Console.WriteLine($"│ jumpDelay             │ {speed1.JumpDelay,15} │ {speed2.JumpDelay,15} │");
            Console.WriteLine($"│ minBeamDiameterMicron │ {laser1.BeamConfig.MinBeamDiameterMicron,15} │ {laser2.BeamConfig.MinBeamDiameterMicron,15} │");
            Console.WriteLine($"│ offsetZ               │ {laser1.ScannerConfig.OffsetZ,15} │ {laser2.ScannerConfig.OffsetZ,15} │");
            Console.WriteLine("└─────────────────────────────────────────────────────────────────┘\n");

            Console.WriteLine("Важные отличия:");
            Console.WriteLine("  1. jumpDelay: Лазер 1 = 40000 ns, Лазер 2 = 35000 ns");
            Console.WriteLine("  2. minBeamDiameterMicron: Лазер 1 = 58.91 μm, Лазер 2 = 66.8 μm");
            Console.WriteLine("  3. offsetZ: Лазер 1 = -0.08 mm, Лазер 2 = -0.067 mm\n");

            Console.WriteLine("Параметры SkyWriting ОДИНАКОВЫЕ для обоих лазеров:");
            Console.WriteLine("  - swenable: true");
            Console.WriteLine("  - umax: 0.1\n");
        }
    }
}
