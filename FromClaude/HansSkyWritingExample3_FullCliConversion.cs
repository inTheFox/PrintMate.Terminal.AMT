using System;
using System.Collections.Generic;
using Hans.NET;

namespace PrintMateMC.Examples
{
    /// <summary>
    /// Полный пример конвертации всех регионов CLI с учетом параметра SkyWriting
    /// </summary>
    public class HansSkyWritingExample3_FullCliConversion
    {
        /// <summary>
        /// Класс для хранения параметров региона из CLI
        /// </summary>
        public class CliRegionParameters
        {
            public string Name { get; set; }
            public double LaserBeamDiameter { get; set; }  // микроны
            public double LaserPower { get; set; }         // Ватты
            public double LaserSpeed { get; set; }         // мм/с
            public int Skywriting { get; set; }            // 0 или 1
            public List<(float x, float y)> Geometry { get; set; } = new List<(float x, float y)>();
        }

        /// <summary>
        /// Полный пример конвертации CLI файла с множественными регионами
        /// </summary>
        public static void Example_CompleteCliConversion()
        {
            // Параметры из CLI JSON (пример из вашего JSON)
            var regions = new List<CliRegionParameters>
            {
                // Edges (контуры)
                new CliRegionParameters
                {
                    Name = "edges",
                    LaserBeamDiameter = 80,
                    LaserPower = 140,
                    LaserSpeed = 550,
                    Skywriting = 1
                },
                // Downskin borders
                new CliRegionParameters
                {
                    Name = "downskin_border",
                    LaserBeamDiameter = 80,
                    LaserPower = 100,
                    LaserSpeed = 800,
                    Skywriting = 1
                },
                // Downskin hatch
                new CliRegionParameters
                {
                    Name = "downskin_hatch",
                    LaserBeamDiameter = 80,
                    LaserPower = 180,
                    LaserSpeed = 1600,
                    Skywriting = 1
                },
                // Infill borders
                new CliRegionParameters
                {
                    Name = "infill_border",
                    LaserBeamDiameter = 80,
                    LaserPower = 140,
                    LaserSpeed = 550,
                    Skywriting = 1
                },
                // Infill hatch
                new CliRegionParameters
                {
                    Name = "infill_hatch",
                    LaserBeamDiameter = 80,
                    LaserPower = 260,
                    LaserSpeed = 900,
                    Skywriting = 1
                },
                // Upskin borders
                new CliRegionParameters
                {
                    Name = "upskin_border",
                    LaserBeamDiameter = 80,
                    LaserPower = 170,
                    LaserSpeed = 500,
                    Skywriting = 1
                },
                // Upskin hatch
                new CliRegionParameters
                {
                    Name = "upskin_hatch",
                    LaserBeamDiameter = 80,
                    LaserPower = 210,
                    LaserSpeed = 800,
                    Skywriting = 1
                },
                // Support borders
                new CliRegionParameters
                {
                    Name = "support_border",
                    LaserBeamDiameter = 80,
                    LaserPower = 100,
                    LaserSpeed = 425,
                    Skywriting = 0  // ВЫКЛЮЧЕНО для supports!
                },
                // Support hatch
                new CliRegionParameters
                {
                    Name = "support_hatch",
                    LaserBeamDiameter = 80,
                    LaserPower = 260,
                    LaserSpeed = 900,
                    Skywriting = 0  // ВЫКЛЮЧЕНО для supports!
                }
            };

            // Обработать каждый регион
            for (int regionIndex = 0; regionIndex < regions.Count; regionIndex++)
            {
                var region = regions[regionIndex];
                ConvertRegionToHans(region, regionIndex, $"layer_{regionIndex}_{region.Name}.bin");
            }

            Console.WriteLine($"Converted {regions.Count} regions from CLI to Hans format");
        }

        /// <summary>
        /// Конвертировать один регион в Hans формат
        /// </summary>
        private static void ConvertRegionToHans(CliRegionParameters region, int layerIndex, string outputFile)
        {
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1); // SPI, 3D mode

            // ВАЖНО: Установить SkyWriting согласно параметру из CLI
            HM_UDM_DLL.UDM_SkyWriting(region.Skywriting);

            Console.WriteLine($"Region: {region.Name}");
            Console.WriteLine($"  SkyWriting: {(region.Skywriting == 1 ? "ENABLED" : "DISABLED")}");
            Console.WriteLine($"  Laser Power: {region.LaserPower} W");
            Console.WriteLine($"  Speed: {region.LaserSpeed} mm/s");
            Console.WriteLine($"  Beam Diameter: {region.LaserBeamDiameter} μm");

            // Расчет Z-offset для диаметра луча
            float zOffset = CalculateZOffset(region.LaserBeamDiameter);
            Console.WriteLine($"  Z-offset: {zOffset:F3} mm");

            // Настройка параметров слоя
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = CreateMarkParameter(region);
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            // Добавить геометрию (пример - тестовый квадрат)
            // В реальном приложении здесь должна быть геометрия из CLI файла
            structUdmPos[] points = new structUdmPos[]
            {
                new structUdmPos { x = 0, y = 0, z = zOffset },
                new structUdmPos { x = 5, y = 0, z = zOffset },
                new structUdmPos { x = 5, y = 5, z = zOffset },
                new structUdmPos { x = 0, y = 5, z = zOffset },
                new structUdmPos { x = 0, y = 0, z = zOffset }
            };

            HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, layerIndex);

            // Генерация и сохранение
            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile(outputFile);
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine($"  Saved: {outputFile}\n");
        }

        /// <summary>
        /// Создать MarkParameter из параметров CLI региона
        /// </summary>
        private static MarkParameter CreateMarkParameter(CliRegionParameters region)
        {
            // Конвертация мощности: Watts -> %
            // Предполагаем максимальную мощность 500W
            float powerPercent = (float)(region.LaserPower / 500.0 * 100.0);

            return new MarkParameter
            {
                MarkSpeed = (uint)region.LaserSpeed,
                LaserPower = powerPercent,
                JumpSpeed = 5000,

                // Задержки зависят от скорости (можно взять из processVariablesMap)
                LaserOnDelay = GetLaserOnDelay(region.LaserSpeed),
                LaserOffDelay = GetLaserOffDelay(region.LaserSpeed),
                MarkDelay = GetMarkDelay(region.LaserSpeed),
                JumpDelay = GetJumpDelay(region.LaserSpeed),
                PolygonDelay = GetPolygonDelay(region.LaserSpeed),

                MarkCount = 1,
                Frequency = 50.0f,
                DutyCycle = 0.5f,
                StandbyFrequency = 20.0f,
                StandbyDutyCycle = 0.5f,
                AnalogMode = 0,
                Waveform = 0,
                PulseWidthMode = 0,
                PulseWidth = 0,
                FPKDelay = 0,
                FPKLength = 0,
                QDelay = 0
            };
        }

        /// <summary>
        /// Получить задержки на основе скорости (упрощенная версия)
        /// В реальном приложении эти значения должны браться из processVariablesMap
        /// </summary>
        private static float GetLaserOnDelay(double speed)
        {
            // Примерные значения на основе скорости
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

        private static uint GetMarkDelay(double speed)
        {
            if (speed <= 800) return 100;
            if (speed <= 1250) return 90;
            return 80;
        }

        private static uint GetJumpDelay(double speed)
        {
            if (speed <= 800) return 100;
            if (speed <= 1250) return 90;
            return 80;
        }

        private static uint GetPolygonDelay(double speed)
        {
            if (speed <= 800) return 50;
            if (speed <= 1250) return 45;
            return 40;
        }

        /// <summary>
        /// Расчет Z-offset для диаметра луча
        /// </summary>
        private static float CalculateZOffset(double beamDiameterMicrons)
        {
            // Параметры калибровки (должны браться из конфигурации сканера)
            double nominalDiameter = 120.0; // микроны
            double coefficient = 0.3;       // мм/10μm

            // Формула: Z = (diameter - nominalDiameter) / 10.0 × coefficient
            return (float)((beamDiameterMicrons - nominalDiameter) / 10.0 * coefficient);
        }
    }
}
