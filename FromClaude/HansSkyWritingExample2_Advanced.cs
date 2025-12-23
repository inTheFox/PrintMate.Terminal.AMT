using System;
using Hans.NET;

namespace PrintMateMC.Examples
{
    /// <summary>
    /// Продвинутый пример использования SkyWriting с расширенными параметрами
    /// </summary>
    public class HansSkyWritingExample2_Advanced
    {
        /// <summary>
        /// Использование расширенного режима SkyWriting с дополнительными параметрами
        /// UDM_SetSkyWritingMode позволяет точнее контролировать поведение SkyWriting
        /// </summary>
        public static void Example_AdvancedSkyWritingMode()
        {
            // Параметры из CLI
            int skywritingEnabled = 1;

            // Дополнительные параметры SkyWriting (обычно задаются в конфигурации сканера)
            int mode = 0;              // Режим SkyWriting (0 - стандартный)
            float uniformLen = 0.1f;   // Длина равномерного участка (мм)
            float accLen = 0.05f;      // Длина участка ускорения (мм)
            float angleLimit = 120.0f; // Предельный угол для SkyWriting (градусы)

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Использование расширенного режима SkyWriting
            int result = HM_UDM_DLL.UDM_SetSkyWritingMode(
                skywritingEnabled,  // enable: 0 или 1
                mode,               // mode: режим работы
                uniformLen,         // uniformLen: длина равномерного участка
                accLen,             // accLen: длина ускорения
                angleLimit          // angleLimit: предел угла
            );

            Console.WriteLine($"Advanced SkyWriting mode set: {result}");
            Console.WriteLine($"  Mode: {mode}");
            Console.WriteLine($"  Uniform length: {uniformLen} mm");
            Console.WriteLine($"  Acceleration length: {accLen} mm");
            Console.WriteLine($"  Angle limit: {angleLimit}°");

            // Далее добавляем геометрию...
        }

        /// <summary>
        /// Пример использования JumpExtendLen совместно с SkyWriting
        /// JumpExtendLen продлевает прыжки для улучшения качества при SkyWriting
        /// </summary>
        public static void Example_SkyWritingWithJumpExtend()
        {
            // Параметры
            int skywritingEnabled = 1;
            float jumpExtendLen = 0.2f; // Продление прыжка на 0.2 мм

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Включить SkyWriting
            HM_UDM_DLL.UDM_SkyWriting(skywritingEnabled);

            // Установить продление прыжков
            // Это помогает сканеру стабилизироваться перед началом маркировки
            HM_UDM_DLL.UDM_SetJumpExtendLen(jumpExtendLen);

            Console.WriteLine($"SkyWriting enabled with jump extend: {jumpExtendLen} mm");

            // Настройка параметров слоя
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = new MarkParameter
            {
                MarkSpeed = 1600,
                JumpSpeed = 5000,
                LaserPower = 36.0f, // 180W / 500W * 100%
                LaserOnDelay = 30.0f,
                LaserOffDelay = 30.0f,
                MarkDelay = 80,
                JumpDelay = 80,
                PolygonDelay = 40,
                MarkCount = 1
            };

            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            // Добавить геометрию
            structUdmPos[] hatchLine = new structUdmPos[]
            {
                new structUdmPos { x = 0, y = 0, z = -1.2f },
                new structUdmPos { x = 5, y = 0, z = -1.2f }
            };

            HM_UDM_DLL.UDM_AddPolyline3D(hatchLine, hatchLine.Length, 0);

            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("output_skywriting_jumpextend.bin");
            HM_UDM_DLL.UDM_EndMain();
        }

        /// <summary>
        /// Пример без SkyWriting для поддержек (supports)
        /// Обычно для supports SkyWriting отключается
        /// </summary>
        public static void Example_NoSkyWritingForSupports()
        {
            // CLI параметры для supports
            // "support_border_skywriting": "0"
            // "support_hatch_skywriting": "0"

            double supportLaserBeamDiameter = 80;
            double supportLaserPower = 260;
            double supportLaserSpeed = 900;
            int supportSkywriting = 0; // ВЫКЛЮЧЕНО для supports

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Явно отключить SkyWriting для supports
            HM_UDM_DLL.UDM_SkyWriting(supportSkywriting);

            Console.WriteLine("SkyWriting disabled for support structures");

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = new MarkParameter
            {
                MarkSpeed = (uint)supportLaserSpeed,
                LaserPower = (float)(supportLaserPower / 500.0 * 100.0),
                JumpSpeed = 5000,
                LaserOnDelay = 40.0f,
                LaserOffDelay = 40.0f,
                MarkCount = 1
            };

            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            // Добавить геометрию supports
            // ... geometry code ...

            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("supports_no_skywriting.bin");
            HM_UDM_DLL.UDM_EndMain();
        }
    }
}
