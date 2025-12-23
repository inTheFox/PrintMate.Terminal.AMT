using System;
using System.Collections.Generic;
using Hans.NET;

namespace PrintMateMC.Examples
{
    /// <summary>
    /// Пример переключения SkyWriting между различными регионами в одном файле
    /// ВАЖНО: Hans API не поддерживает изменение SkyWriting внутри одного UDM файла!
    /// Этот пример показывает ПРАВИЛЬНЫЙ подход - создавать отдельные файлы для каждого региона
    /// </summary>
    public class HansSkyWritingExample4_PerRegionSwitch
    {
        /// <summary>
        /// НЕПРАВИЛЬНЫЙ подход: попытка переключить SkyWriting в одном файле
        /// Hans API не поддерживает изменение SkyWriting после вызова UDM_NewFile
        /// </summary>
        public static void Example_WrongApproach_DoNotUse()
        {
            Console.WriteLine("❌ НЕПРАВИЛЬНЫЙ ПОДХОД - НЕ ИСПОЛЬЗОВАТЬ!");
            Console.WriteLine("Нельзя переключать SkyWriting внутри одного UDM файла\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Включить SkyWriting для edges
            HM_UDM_DLL.UDM_SkyWriting(1);
            // Добавить геометрию edges...

            // ❌ ЭТО НЕ СРАБОТАЕТ! SkyWriting нельзя изменить после первого вызова
            HM_UDM_DLL.UDM_SkyWriting(0);
            // Добавить геометрию supports...

            // Весь файл будет с SkyWriting=1 для ВСЕХ регионов!
        }

        /// <summary>
        /// ПРАВИЛЬНЫЙ подход 1: Отдельные файлы для регионов с разными SkyWriting
        /// </summary>
        public static void Example_CorrectApproach1_SeparateFiles()
        {
            Console.WriteLine("✅ ПРАВИЛЬНЫЙ ПОДХОД 1: Отдельные файлы для каждого региона\n");

            // Список регионов с разными параметрами SkyWriting
            var regions = new[]
            {
                new { Name = "edges", Skywriting = 1, File = "layer_edges.bin" },
                new { Name = "infill", Skywriting = 1, File = "layer_infill.bin" },
                new { Name = "supports", Skywriting = 0, File = "layer_supports.bin" }
            };

            foreach (var region in regions)
            {
                Console.WriteLine($"Creating file for: {region.Name}");
                Console.WriteLine($"  SkyWriting: {region.Skywriting}");

                // Создать НОВЫЙ файл для каждого региона
                HM_UDM_DLL.UDM_NewFile();
                HM_UDM_DLL.UDM_SetProtocol(0, 1);

                // Установить SkyWriting для этого региона
                HM_UDM_DLL.UDM_SkyWriting(region.Skywriting);

                // Настроить параметры
                MarkParameter[] layers = new MarkParameter[1];
                layers[0] = new MarkParameter
                {
                    MarkSpeed = 800,
                    LaserPower = 50.0f,
                    JumpSpeed = 5000
                };
                HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

                // Добавить геометрию региона
                structUdmPos[] points = new structUdmPos[]
                {
                    new structUdmPos { x = 0, y = 0, z = 0 },
                    new structUdmPos { x = 5, y = 0, z = 0 },
                    new structUdmPos { x = 5, y = 5, z = 0 },
                    new structUdmPos { x = 0, y = 0, z = 0 }
                };
                HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, 0);

                // Сохранить отдельный файл
                HM_UDM_DLL.UDM_Main();
                HM_UDM_DLL.UDM_SaveToFile(region.File);
                HM_UDM_DLL.UDM_EndMain();

                Console.WriteLine($"  Saved: {region.File}\n");
            }

            Console.WriteLine("Результат: 3 отдельных файла с разными настройками SkyWriting");
        }

        /// <summary>
        /// ПРАВИЛЬНЫЙ подход 2: Группировка регионов по SkyWriting
        /// Создать 2 файла: один со SkyWriting=1, другой со SkyWriting=0
        /// </summary>
        public static void Example_CorrectApproach2_GroupBySkyWriting()
        {
            Console.WriteLine("✅ ПРАВИЛЬНЫЙ ПОДХОД 2: Группировка по SkyWriting\n");

            // Группа 1: Все регионы со SkyWriting ВКЛЮЧЕНО
            Console.WriteLine("File 1: With SkyWriting (edges, infill, upskin, downskin)");
            CreateGroupedFile(skywritingEnabled: true, "layer_with_skywriting.bin", new[]
            {
                CreateRegion("edges", 80, 140, 550),
                CreateRegion("downskin_border", 80, 100, 800),
                CreateRegion("downskin_hatch", 80, 180, 1600),
                CreateRegion("infill_border", 80, 140, 550),
                CreateRegion("infill_hatch", 80, 260, 900),
                CreateRegion("upskin_border", 80, 170, 500),
                CreateRegion("upskin_hatch", 80, 210, 800)
            });

            // Группа 2: Все регионы со SkyWriting ВЫКЛЮЧЕНО
            Console.WriteLine("\nFile 2: Without SkyWriting (supports)");
            CreateGroupedFile(skywritingEnabled: false, "layer_without_skywriting.bin", new[]
            {
                CreateRegion("support_border", 80, 100, 425),
                CreateRegion("support_hatch", 80, 260, 900)
            });

            Console.WriteLine("\n✅ Результат: 2 файла - один с SkyWriting, другой без");
        }

        /// <summary>
        /// Создать файл с группой регионов с одинаковым параметром SkyWriting
        /// </summary>
        private static void CreateGroupedFile(bool skywritingEnabled, string filename, RegionData[] regions)
        {
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Установить SkyWriting для ВСЕХ регионов в этом файле
            HM_UDM_DLL.UDM_SkyWriting(skywritingEnabled ? 1 : 0);
            Console.WriteLine($"  SkyWriting: {(skywritingEnabled ? "ENABLED" : "DISABLED")}");

            // Создать слой для каждого региона
            MarkParameter[] layers = new MarkParameter[regions.Length];
            for (int i = 0; i < regions.Length; i++)
            {
                var region = regions[i];
                layers[i] = new MarkParameter
                {
                    MarkSpeed = (uint)region.Speed,
                    LaserPower = (float)(region.Power / 500.0 * 100.0),
                    JumpSpeed = 5000,
                    LaserOnDelay = 40.0f,
                    LaserOffDelay = 40.0f,
                    MarkCount = 1
                };

                Console.WriteLine($"  - {region.Name}: {region.Power}W @ {region.Speed}mm/s, diameter={region.Diameter}μm");
            }

            HM_UDM_DLL.UDM_SetLayersPara(layers, layers.Length);

            // Добавить геометрию для каждого региона
            for (int layerIndex = 0; layerIndex < regions.Length; layerIndex++)
            {
                var region = regions[layerIndex];
                float zOffset = CalculateZOffset(region.Diameter);

                // Пример геометрии
                structUdmPos[] points = new structUdmPos[]
                {
                    new structUdmPos { x = layerIndex * 6.0f, y = 0, z = zOffset },
                    new structUdmPos { x = layerIndex * 6.0f + 5.0f, y = 0, z = zOffset },
                    new structUdmPos { x = layerIndex * 6.0f + 5.0f, y = 5, z = zOffset },
                    new structUdmPos { x = layerIndex * 6.0f, y = 0, z = zOffset }
                };

                HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, layerIndex);
            }

            // Сохранить файл
            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile(filename);
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine($"  Saved: {filename}");
        }

        private class RegionData
        {
            public string Name { get; set; }
            public double Diameter { get; set; }
            public double Power { get; set; }
            public double Speed { get; set; }
        }

        private static RegionData CreateRegion(string name, double diameter, double power, double speed)
        {
            return new RegionData
            {
                Name = name,
                Diameter = diameter,
                Power = power,
                Speed = speed
            };
        }

        private static float CalculateZOffset(double beamDiameterMicrons)
        {
            double nominalDiameter = 120.0;
            double coefficient = 0.3;
            return (float)((beamDiameterMicrons - nominalDiameter) / 10.0 * coefficient);
        }

        /// <summary>
        /// ПРАВИЛЬНЫЙ подход 3: Использование слоев (layers) с одинаковым SkyWriting
        /// Все слои в одном файле используют ОДИНАКОВЫЙ параметр SkyWriting
        /// </summary>
        public static void Example_CorrectApproach3_MultipleLayersSameSkyWriting()
        {
            Console.WriteLine("✅ ПРАВИЛЬНЫЙ ПОДХОД 3: Множество слоев с одинаковым SkyWriting\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Включить SkyWriting для ВСЕХ слоев
            HM_UDM_DLL.UDM_SkyWriting(1);

            // Создать несколько слоев (например, edges, downskin, infill)
            // ВСЕ они будут использовать SkyWriting=1
            MarkParameter[] layers = new MarkParameter[3];

            // Layer 0: Edges
            layers[0] = new MarkParameter
            {
                MarkSpeed = 550,
                LaserPower = 28.0f, // 140W / 500W * 100%
                JumpSpeed = 5000
            };

            // Layer 1: Downskin hatch
            layers[1] = new MarkParameter
            {
                MarkSpeed = 1600,
                LaserPower = 36.0f, // 180W / 500W * 100%
                JumpSpeed = 5000
            };

            // Layer 2: Infill hatch
            layers[2] = new MarkParameter
            {
                MarkSpeed = 900,
                LaserPower = 52.0f, // 260W / 500W * 100%
                JumpSpeed = 5000
            };

            HM_UDM_DLL.UDM_SetLayersPara(layers, 3);

            // Добавить геометрию для каждого слоя
            for (int layer = 0; layer < 3; layer++)
            {
                structUdmPos[] points = new structUdmPos[]
                {
                    new structUdmPos { x = 0, y = layer * 6.0f, z = -1.2f },
                    new structUdmPos { x = 5, y = layer * 6.0f, z = -1.2f }
                };
                HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, layer);
            }

            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("multiple_layers_with_skywriting.bin");
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine("✅ Файл создан с 3 слоями, все с SkyWriting=1");
        }
    }
}
