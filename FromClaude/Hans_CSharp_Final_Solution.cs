using System;
using Hans.NET;

namespace PrintMateMC.HansFinal
{
    /// <summary>
    /// ФИНАЛЬНОЕ РЕШЕНИЕ: Как Hans4Java использует SkyWriting
    /// На основе декомпилированного кода UdmProducer.class
    /// </summary>
    public class HansSkyWritingFinalSolution
    {
        /// <summary>
        /// Применить SkyWriting ТОЧНО КАК Hans4Java
        /// </summary>
        public static void ApplySWEnableOperation_Hans4JavaWay(
            bool enable,
            float laserOnDelayForSkyWriting,
            float laserOffDelayForSkyWriting,
            int markDelayForSkyWriting,
            float laserOnDelayNormal,
            float laserOffDelayNormal,
            int markDelayNormal,
            int jumpDelayNormal,
            int polygonDelayNormal)
        {
            Console.WriteLine($"=== ApplySWEnableOperation({enable}) - Hans4Java Way ===\n");

            // Вызов ПРОСТОЙ версии API (как в Hans4Java)
            HM_UDM_DLL.UDM_SkyWriting(enable ? 1 : 0);

            Console.WriteLine($"Called UDM_SkyWriting({(enable ? 1 : 0)})");

            // Обновить параметры слоя ТОЧНО КАК в updateMarkParam()
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = new MarkParameter();

            if (enable)
            {
                // Когда SkyWriting ВКЛЮЧЕН
                Console.WriteLine("SkyWriting ON - using special delays:");
                layers[0].JumpDelay = 0;        // ← ОБНУЛИТЬ!
                layers[0].PolygonDelay = 0;     // ← ОБНУЛИТЬ!
                layers[0].MarkDelay = (uint)markDelayForSkyWriting;
                layers[0].LaserOnDelay = laserOnDelayForSkyWriting;
                layers[0].LaserOffDelay = laserOffDelayForSkyWriting;

                Console.WriteLine($"  JumpDelay: 0 (forced to 0)");
                Console.WriteLine($"  PolygonDelay: 0 (forced to 0)");
                Console.WriteLine($"  MarkDelay: {markDelayForSkyWriting}");
                Console.WriteLine($"  LaserOnDelay: {laserOnDelayForSkyWriting}");
                Console.WriteLine($"  LaserOffDelay: {laserOffDelayForSkyWriting}");
            }
            else
            {
                // Когда SkyWriting ВЫКЛЮЧЕН
                Console.WriteLine("SkyWriting OFF - using normal delays:");
                layers[0].JumpDelay = (uint)jumpDelayNormal;
                layers[0].PolygonDelay = (uint)polygonDelayNormal;
                layers[0].MarkDelay = (uint)markDelayNormal;
                layers[0].LaserOnDelay = laserOnDelayNormal;
                layers[0].LaserOffDelay = laserOffDelayNormal;

                Console.WriteLine($"  JumpDelay: {jumpDelayNormal}");
                Console.WriteLine($"  PolygonDelay: {polygonDelayNormal}");
                Console.WriteLine($"  MarkDelay: {markDelayNormal}");
                Console.WriteLine($"  LaserOnDelay: {laserOnDelayNormal}");
                Console.WriteLine($"  LaserOffDelay: {laserOffDelayNormal}");
            }

            // Установить параметры слоя
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);
            Console.WriteLine("\n✅ Параметры слоя обновлены\n");
        }

        /// <summary>
        /// Пример 1: Использование с параметрами из ВАШЕЙ конфигурации
        /// </summary>
        public static void Example1_WithYourConfig()
        {
            Console.WriteLine("=== Example 1: С параметрами из вашей конфигурации ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Параметры из вашей конфигурации для скорости 800 mm/s, Лазер 1
            ApplySWEnableOperation_Hans4JavaWay(
                enable: true,
                // Для SkyWriting (из config)
                laserOnDelayForSkyWriting: 600.0f,
                laserOffDelayForSkyWriting: 730.0f,
                markDelayForSkyWriting: 470,
                // Обычные задержки (из config)
                laserOnDelayNormal: 420.0f,
                laserOffDelayNormal: 490.0f,
                markDelayNormal: 470,
                jumpDelayNormal: 40000,
                polygonDelayNormal: 385
            );

            // Добавить параметры скорости и мощности
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = new MarkParameter
            {
                MarkSpeed = 800,
                JumpSpeed = 25000,
                LaserPower = 28.0f,  // 140W / 500W * 100%
                // Задержки уже установлены выше
                JumpDelay = 0,       // Для SkyWriting
                PolygonDelay = 0,
                MarkDelay = 470,
                LaserOnDelay = 600.0f,
                LaserOffDelay = 730.0f,
                MarkCount = 1
            };
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            // Добавить геометрию...
            structUdmPos[] points = new structUdmPos[]
            {
                new structUdmPos { x = 0, y = 0, z = -1.2f },
                new structUdmPos { x = 10, y = 0, z = -1.2f }
            };
            HM_UDM_DLL.UDM_AddPolyline3D(points, points.Length, 0);

            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("hans4java_way.bin");
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine("✅ Файл создан точно как Hans4Java\n");
        }

        /// <summary>
        /// Пример 2: Переключение между SkyWriting ON и OFF
        /// (В реальности нужны отдельные файлы)
        /// </summary>
        public static void Example2_SwitchingSkyWriting()
        {
            Console.WriteLine("=== Example 2: Переключение SkyWriting ===\n");

            // Файл 1: Edges с SkyWriting ON
            Console.WriteLine("Файл 1: edges_with_skywriting.bin");
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            ApplySWEnableOperation_Hans4JavaWay(
                enable: true,
                laserOnDelayForSkyWriting: 600.0f,
                laserOffDelayForSkyWriting: 730.0f,
                markDelayForSkyWriting: 470,
                laserOnDelayNormal: 420.0f,
                laserOffDelayNormal: 490.0f,
                markDelayNormal: 470,
                jumpDelayNormal: 40000,
                polygonDelayNormal: 385
            );

            // Добавить геометрию edges...
            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("edges_with_skywriting.bin");
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine();

            // Файл 2: Supports БЕЗ SkyWriting
            Console.WriteLine("Файл 2: supports_without_skywriting.bin");
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            ApplySWEnableOperation_Hans4JavaWay(
                enable: false,  // ← OFF для supports
                laserOnDelayForSkyWriting: 600.0f,
                laserOffDelayForSkyWriting: 730.0f,
                markDelayForSkyWriting: 470,
                laserOnDelayNormal: 420.0f,
                laserOffDelayNormal: 490.0f,
                markDelayNormal: 470,
                jumpDelayNormal: 40000,
                polygonDelayNormal: 385
            );

            // Добавить геометрию supports...
            HM_UDM_DLL.UDM_Main();
            HM_UDM_DLL.UDM_SaveToFile("supports_without_skywriting.bin");
            HM_UDM_DLL.UDM_EndMain();

            Console.WriteLine("\n✅ Созданы 2 файла с разным SkyWriting\n");
        }

        /// <summary>
        /// Пример 3: Упрощенная версия для быстрого использования
        /// </summary>
        public static void Example3_SimplifiedVersion()
        {
            Console.WriteLine("=== Example 3: Упрощенная версия ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Просто включить SkyWriting
            HM_UDM_DLL.UDM_SkyWriting(1);

            // Параметры с обнуленными JumpDelay и PolygonDelay
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = new MarkParameter
            {
                MarkSpeed = 800,
                JumpSpeed = 25000,
                LaserPower = 28.0f,
                JumpDelay = 0,           // ← ВАЖНО: 0 для SkyWriting!
                PolygonDelay = 0,        // ← ВАЖНО: 0 для SkyWriting!
                MarkDelay = 470,
                LaserOnDelay = 600.0f,   // Специальные задержки для SkyWriting
                LaserOffDelay = 730.0f,
                MarkCount = 1
            };
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            Console.WriteLine("✅ SkyWriting включен (упрощенный способ)\n");
        }
    }

    /// <summary>
    /// ВАЖНЫЕ ВЫВОДЫ из декомпиляции Hans4Java
    /// </summary>
    public class Hans4JavaFindings
    {
        public static void PrintFindings()
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ВАЖНЕЙШИЕ ВЫВОДЫ из декомпиляции Hans4Java UdmProducer.class  ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("1. Hans4Java использует ПРОСТУЮ версию API:");
            Console.WriteLine("   lib.UDM_SkyWriting(boolean enable)");
            Console.WriteLine("   НЕ UDM_SetSkyWritingMode(enable, mode, uniformLen, accLen, angleLimit)\n");

            Console.WriteLine("2. Параметр uMax НЕ ПЕРЕДАЕТСЯ в UDM API:");
            Console.WriteLine("   - Хранится в DelaysSkyWritingConfig.uMax = 0.1F");
            Console.WriteLine("   - НО не передается в UDM_SkyWriting()!");
            Console.WriteLine("   - Возможно устанавливается через другой API или конфиг файл\n");

            Console.WriteLine("3. Ключевая логика при включении SkyWriting:");
            Console.WriteLine("   if (isSWEnable) {");
            Console.WriteLine("      JumpDelay = 0;        // ← ОБНУЛИТЬ!");
            Console.WriteLine("      PolygonDelay = 0;     // ← ОБНУЛИТЬ!");
            Console.WriteLine("      MarkDelay = delaysSkyWritingConfig.markDelay;");
            Console.WriteLine("      LaserOnDelay = delaysSkyWritingConfig.laserOnDelay;");
            Console.WriteLine("      LaserOffDelay = delaysSkyWritingConfig.laserOffDelay;");
            Console.WriteLine("   }\n");

            Console.WriteLine("4. Два набора задержек:");
            Console.WriteLine("   - delaysSkyWritingConfig (когда SkyWriting ON)");
            Console.WriteLine("   - delaysConfig (когда SkyWriting OFF)\n");

            Console.WriteLine("5. accLen и angleLimit НЕ используются в Java коде:");
            Console.WriteLine("   - Эти параметры либо игнорируются");
            Console.WriteLine("   - Либо установлены в native DLL по умолчанию\n");

            Console.WriteLine("═══════════════════════════════════════════════════════════════════\n");
            Console.WriteLine("РЕКОМЕНДАЦИЯ для C#:");
            Console.WriteLine("  1. Используйте UDM_SkyWriting(int enable) - простую версию");
            Console.WriteLine("  2. ОБНУЛЯЙТЕ JumpDelay и PolygonDelay когда SkyWriting ON");
            Console.WriteLine("  3. Используйте специальные задержки из конфигурации:");
            Console.WriteLine("     - laserOnDelayForSkyWriting");
            Console.WriteLine("     - laserOffDelayForSkyWriting\n");
        }
    }
}
