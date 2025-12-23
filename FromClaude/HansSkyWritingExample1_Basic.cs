using System;
using Hans.NET;

namespace PrintMateMC.Examples
{
    /// <summary>
    /// Базовый пример использования SkyWriting для конвертации CLI параметров
    /// </summary>
    public class HansSkyWritingExample1_Basic
    {
        /// <summary>
        /// Простой пример: включение/выключение SkyWriting на основе CLI параметра
        /// </summary>
        public static void Example_BasicSkyWriting()
        {
            // Параметры из CLI файла (пример)
            // "edge_skywriting": "1"
            // "downskin_hatch_skywriting": "1"
            // "infill_hatch_skywriting": "1"

            int cliSkywritingEnabled = 1; // Значение из CLI файла (0 или 1)

            // Инициализация UDM файла
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1); // Protocol 0 (SPI), Mode 1 (3D)

            // Простое включение/выключение SkyWriting
            if (cliSkywritingEnabled == 1)
            {
                // Включить SkyWriting
                int result = HM_UDM_DLL.UDM_SkyWriting(1);
                Console.WriteLine($"SkyWriting enabled: {result}");
            }
            else
            {
                // Выключить SkyWriting
                int result = HM_UDM_DLL.UDM_SkyWriting(0);
                Console.WriteLine($"SkyWriting disabled: {result}");
            }

            // Далее добавляем геометрию...
            // UDM_AddPolyline3D, UDM_Main, UDM_SaveToFile, UDM_EndMain
        }

        /// <summary>
        /// Пример конвертации одного региона из CLI с учетом SkyWriting
        /// </summary>
        public static void Example_SingleRegionWithSkyWriting()
        {
            // CLI параметры для edges (контуров)
            double edgeLaserBeamDiameter = 80;  // микроны
            double edgeLaserPower = 140;         // Ватты
            double edgeLaserSpeed = 550;         // мм/с
            int edgeSkywriting = 1;              // 0 или 1

            // Инициализация
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Включить SkyWriting если указано в CLI
            HM_UDM_DLL.UDM_SkyWriting(edgeSkywriting);

            // Настроить параметры слоя
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = new MarkParameter
            {
                MarkSpeed = (uint)edgeLaserSpeed,           // 550 мм/с
                LaserPower = (float)(edgeLaserPower / 500.0 * 100.0), // W -> %
                JumpSpeed = 5000,
                LaserOnDelay = 50.0f,
                LaserOffDelay = 50.0f,
                MarkDelay = 100,
                JumpDelay = 100,
                PolygonDelay = 50,
                MarkCount = 1,
                Frequency = 50.0f,
                DutyCycle = 0.5f
            };

            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            // Расчет Z-offset для диаметра луча
            float zOffset = CalculateZOffset(edgeLaserBeamDiameter);

            // Добавить геометрию (пример: квадрат)
            structUdmPos[] points = new structUdmPos[]
            {
                new structUdmPos { x = 0, y = 0, z = zOffset },
                new structUdmPos { x = 10, y = 0, z = zOffset },
                new structUdmPos { x = 10, y = 10, z = zOffset },
                new structUdmPos { x = 0, y = 10, z = zOffset },
                new structUdmPos { x = 0, y = 0, z = zOffset }  // Замкнуть контур
            };

            HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, 0);

            // Генерация и сохранение
            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("output_with_skywriting.bin");
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine($"File saved with SkyWriting={edgeSkywriting}");
        }

        /// <summary>
        /// Расчет Z-offset для диаметра луча
        /// </summary>
        private static float CalculateZOffset(double beamDiameterMicrons)
        {
            // Параметры из калибровки
            double nominalDiameter = 120.0; // микроны (диаметр при Z=0)
            double coefficient = 0.3;       // мм на 10 микрон

            // Формула: Z = (diameter - nominalDiameter) / 10.0 * coefficient
            return (float)((beamDiameterMicrons - nominalDiameter) / 10.0 * coefficient);
        }
    }
}
