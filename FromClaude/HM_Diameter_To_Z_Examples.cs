using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Hans.NET.DiameterToZ.Examples
{
    #region Теория: Диаметр луча vs Z-координата

    /// <summary>
    /// ВАЖНОЕ ПРИМЕЧАНИЕ: В PrintMateMC используется ДИАМЕТР ЛУЧА, а не Z-координата!
    ///
    /// Физика процесса:
    /// ================
    ///
    /// 1. FOCUS параметр = ДИАМЕТР ЛУЧА в микронах (мкм), НЕ координата Z!
    /// 2. DiameterOperation устанавливает диаметр луча путем расфокусировки
    /// 3. Больший диаметр = больше расфокусировка = фокальная плоскость дальше от линзы
    ///
    /// Формула Гаусса для диаметра луча:
    /// ==================================
    ///
    ///   d(z) = d₀ × √(1 + (z/z_R)²)
    ///
    /// где:
    ///   d(z) - диаметр луча на расстоянии z от фокуса
    ///   d₀   - минимальный диаметр луча в фокусе (beam waist)
    ///   z    - расстояние от фокальной плоскости
    ///   z_R  - длина Рэлея (Rayleigh length) - глубина фокуса
    ///
    /// Параметры из конфигурации (карта 0):
    /// =====================================
    ///   d₀ = 48.141 мкм
    ///   z_R = 1426.715 мкм
    ///   M² = 1.127
    ///   λ = 1070 нм
    ///
    /// Обратная формула (диаметр → Z):
    /// ================================
    ///
    ///   z = ±z_R × √((d/d₀)² - 1)
    ///
    /// Примеры:
    ///   d = 48.141 мкм  → z = 0 мм       (в фокусе)
    ///   d = 65.0 мкм    → z = ±1.06 мм   (расфокусировка)
    ///   d = 67.0 мкм    → z = ±1.16 мм
    ///   d = 100.0 мкм   → z = ±1.96 мм
    ///
    /// </summary>

    #endregion

    #region Вспомогательные классы для расчетов

    /// <summary>
    /// Калькулятор для преобразования диаметр ↔ Z
    /// </summary>
    public class BeamDiameterCalculator
    {
        /// <summary>
        /// Минимальный диаметр луча в фокусе (мкм)
        /// </summary>
        public double MinBeamDiameterMicron { get; set; }

        /// <summary>
        /// Длина Рэлея - глубина фокуса (мкм)
        /// </summary>
        public double RayleighLengthMicron { get; set; }

        /// <summary>
        /// Фактор качества луча M²
        /// </summary>
        public double M2 { get; set; }

        /// <summary>
        /// Длина волны (нм)
        /// </summary>
        public double WavelengthNano { get; set; }

        public BeamDiameterCalculator(double minDiameter, double rayleighLength, double m2, double wavelength)
        {
            MinBeamDiameterMicron = minDiameter;
            RayleighLengthMicron = rayleighLength;
            M2 = m2;
            WavelengthNano = wavelength;
        }

        /// <summary>
        /// Создать калькулятор из конфигурации карты 0
        /// </summary>
        public static BeamDiameterCalculator FromCard0Config()
        {
            return new BeamDiameterCalculator(
                minDiameter: 48.141,
                rayleighLength: 1426.715,
                m2: 1.127,
                wavelength: 1070.0
            );
        }

        /// <summary>
        /// Создать калькулятор из конфигурации карты 1
        /// </summary>
        public static BeamDiameterCalculator FromCard1Config()
        {
            return new BeamDiameterCalculator(
                minDiameter: 53.872,
                rayleighLength: 1616.16,
                m2: 1.175,
                wavelength: 1070.0
            );
        }

        /// <summary>
        /// Рассчитать диаметр луча на расстоянии Z от фокуса
        /// </summary>
        /// <param name="zOffsetMicron">Смещение от фокуса в микронах</param>
        /// <returns>Диаметр луча в микронах</returns>
        public double CalculateDiameter(double zOffsetMicron)
        {
            // d(z) = d₀ × √(1 + (z/z_R)²)
            double ratio = zOffsetMicron / RayleighLengthMicron;
            return MinBeamDiameterMicron * Math.Sqrt(1 + ratio * ratio);
        }

        /// <summary>
        /// Рассчитать смещение Z для заданного диаметра луча
        /// ВНИМАНИЕ: Возвращает абсолютное значение!
        /// Знак зависит от направления (+ = фокус дальше, - = фокус ближе)
        /// </summary>
        /// <param name="diameterMicron">Желаемый диаметр луча в микронах</param>
        /// <returns>Абсолютное смещение от фокуса в микронах</returns>
        public double CalculateZOffset(double diameterMicron)
        {
            if (diameterMicron < MinBeamDiameterMicron)
            {
                throw new ArgumentException($"Диаметр {diameterMicron} мкм меньше минимального {MinBeamDiameterMicron} мкм");
            }

            // z = z_R × √((d/d₀)² - 1)
            double ratio = diameterMicron / MinBeamDiameterMicron;
            return RayleighLengthMicron * Math.Sqrt(ratio * ratio - 1);
        }

        /// <summary>
        /// Вывести информацию о конфигурации
        /// </summary>
        public void PrintInfo()
        {
            Console.WriteLine("Параметры луча:");
            Console.WriteLine($"  Минимальный диаметр (d₀): {MinBeamDiameterMicron:F3} мкм");
            Console.WriteLine($"  Длина Рэлея (z_R): {RayleighLengthMicron:F3} мкм = {RayleighLengthMicron / 1000:F3} мм");
            Console.WriteLine($"  M²: {M2:F3}");
            Console.WriteLine($"  Длина волны: {WavelengthNano:F1} нм");
            Console.WriteLine($"  Глубина фокуса (2×z_R): {2 * RayleighLengthMicron / 1000:F3} мм");
        }

        /// <summary>
        /// Построить таблицу диаметр ↔ Z
        /// </summary>
        public void PrintDiameterTable()
        {
            Console.WriteLine("\nТаблица: Диаметр луча ↔ Смещение Z:");
            Console.WriteLine("┌─────────────────┬─────────────────┬──────────────────┐");
            Console.WriteLine("│ Диаметр (мкм)   │ Z смещение (мм) │ Примечание       │");
            Console.WriteLine("├─────────────────┼─────────────────┼──────────────────┤");

            double[] testDiameters = {
                MinBeamDiameterMicron,  // Минимум (в фокусе)
                50, 55, 60, 65, 70, 75, 80, 90, 100, 120, 150
            };

            foreach (double d in testDiameters)
            {
                if (d < MinBeamDiameterMicron) continue;

                double z = 0;
                string note = "";

                if (Math.Abs(d - MinBeamDiameterMicron) < 0.01)
                {
                    z = 0;
                    note = "В ФОКУСЕ";
                }
                else
                {
                    z = CalculateZOffset(d) / 1000.0; // мкм → мм

                    if (Math.Abs(CalculateZOffset(d)) >= RayleighLengthMicron)
                        note = "За пределами z_R";
                }

                Console.WriteLine($"│ {d,15:F3} │ {(z == 0 ? "0.000" : $"±{z:F3}"),15} │ {note,-16} │");
            }

            Console.WriteLine("└─────────────────┴─────────────────┴──────────────────┘");
        }
    }

    #endregion

    #region Структуры данных UDM

    [StructLayout(LayoutKind.Sequential)]
    public struct structUdmPos
    {
        public float x;
        public float y;
        public float z;
        public float a;

        public structUdmPos(float x, float y, float z = 0, float a = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.a = a;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MarkParameter
    {
        public UInt32 MarkSpeed;
        public UInt32 JumpSpeed;
        public UInt32 MarkDelay;
        public UInt32 JumpDelay;
        public UInt32 PolygonDelay;
        public UInt32 MarkCount;
        public float LaserOnDelay;
        public float LaserOffDelay;
        public float FPKDelay;
        public float FPKLength;
        public float QDelay;
        public float DutyCycle;
        public float Frequency;
        public float StandbyFrequency;
        public float StandbyDutyCycle;
        public float LaserPower;
        public UInt32 AnalogMode;
        public UInt32 Waveform;
        public UInt32 PulseWidthMode;
        public UInt32 PulseWidth;

        public static MarkParameter CreateDefault()
        {
            return new MarkParameter
            {
                MarkSpeed = 800,
                JumpSpeed = 25000,
                MarkDelay = 500,
                JumpDelay = 400,
                PolygonDelay = 200,
                MarkCount = 1,
                LaserOnDelay = 120.0f,
                LaserOffDelay = 120.0f,
                DutyCycle = 0.5f,
                Frequency = 20.0f,
                LaserPower = 50.0f
            };
        }
    }

    public class HM_UDM_DLL
    {
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_NewFile();

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SaveToFile(string strFilePath);

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_Main();

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_EndMain();

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetProtocol(int nProtocol, int nDimensional);

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetLayersPara(MarkParameter[] layersParameter, int count);

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_AddPolyline3D(structUdmPos[] nPos, int nCount, int layerIndex);
    }

    #endregion

    #region Примеры с использованием диаметра вместо Z

    public class DiameterBasedExamples
    {
        /// <summary>
        /// Пример 1: Демонстрация связи диаметр ↔ Z
        /// </summary>
        public static void Example1_DiameterToZ_Demonstration()
        {
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  Пример 1: Связь диаметра луча и Z-координаты");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");

            // Создать калькулятор для карты 0
            var calc = BeamDiameterCalculator.FromCard0Config();
            calc.PrintInfo();
            calc.PrintDiameterTable();

            Console.WriteLine("\n📌 ВАЖНО:");
            Console.WriteLine("  В PrintMateMC параметр FOCUS = ДИАМЕТР луча в мкм");
            Console.WriteLine("  DiameterOperation управляет расфокусировкой");
            Console.WriteLine("  Для 3D маркировки используйте Z-координату в AddPolyline3D");
        }

        /// <summary>
        /// Пример 2: Создание траектории с переменным диаметром
        /// (имитация 3D через изменение диаметра)
        /// </summary>
        public static void Example2_VariableDiameter_AsZ()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════");
            Console.WriteLine("  Пример 2: Траектория с переменным диаметром");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");

            var calc = BeamDiameterCalculator.FromCard0Config();

            Console.WriteLine("Создание линии с изменением диаметра от 50 до 100 мкм:");
            Console.WriteLine("(Имитация подъема по Z путем расфокусировки)\n");

            int points = 10;
            for (int i = 0; i <= points; i++)
            {
                double ratio = i / (double)points;

                // Диаметр от 50 до 100 мкм
                double diameter = 50.0 + ratio * 50.0;

                // Рассчитать соответствующее Z
                double zOffset = 0;
                if (diameter > calc.MinBeamDiameterMicron)
                {
                    zOffset = calc.CalculateZOffset(diameter) / 1000.0; // мкм → мм
                }

                // Координаты
                float x = i * 5.0f; // Движение по X
                float y = 0;

                Console.WriteLine($"  Точка {i,2}: X={x,5:F1} мм, Диаметр={diameter,6:F2} мкм → Z≈±{zOffset:F3} мм");
            }

            Console.WriteLine("\n✓ При маркировке используйте DiameterOperation для установки диаметра");
            Console.WriteLine("  Пример: new DiameterOperation(65.0) // 65 мкм");
        }

        /// <summary>
        /// Пример 3: Правильное использование в ProcessVariables из конфига
        /// </summary>
        public static void Example3_ConfigDiameters()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════");
            Console.WriteLine("  Пример 3: Диаметры из конфигурации");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");

            var calc = BeamDiameterCalculator.FromCard0Config();

            // Диаметры из конфигурации (карта 0)
            double[] configDiameters = { 65.0 }; // curBeamDiameterMicron

            // Диаметры из конфигурации (карта 1)
            double[] configDiameters2 = { 67.0 };

            Console.WriteLine("Карта 0 (IP: 172.18.34.227):");
            calc = BeamDiameterCalculator.FromCard0Config();
            foreach (double d in configDiameters)
            {
                double z = calc.CalculateZOffset(d) / 1000.0;
                Console.WriteLine($"  curBeamDiameterMicron = {d} мкм → Z смещение = ±{z:F3} мм");
            }

            Console.WriteLine("\nКарта 1 (IP: 172.18.34.228):");
            calc = BeamDiameterCalculator.FromCard1Config();
            foreach (double d in configDiameters2)
            {
                double z = calc.CalculateZOffset(d) / 1000.0;
                Console.WriteLine($"  curBeamDiameterMicron = {d} мкм → Z смещение = ±{z:F3} мм");
            }

            Console.WriteLine("\n📊 Анализ:");
            Console.WriteLine("  • Карта 0: d₀=48.141 мкм, при d=65 мкм → Z≈±1.06 мм");
            Console.WriteLine("  • Карта 1: d₀=53.872 мкм, при d=67 мкм → Z≈±1.16 мм");
            Console.WriteLine("  • Расфокусировка увеличивает пятно и снижает плотность энергии");
        }

        /// <summary>
        /// Пример 4: Расчет плотности энергии при разных диаметрах
        /// </summary>
        public static void Example4_EnergyDensity_vs_Diameter()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════");
            Console.WriteLine("  Пример 4: Плотность энергии vs Диаметр");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");

            double power = 50.0;      // Вт
            double speed = 800.0;     // мм/с

            Console.WriteLine($"Параметры:");
            Console.WriteLine($"  Мощность: {power} Вт");
            Console.WriteLine($"  Скорость: {speed} мм/с");
            Console.WriteLine($"  Линейная энергия: {power / speed:F3} Дж/мм\n");

            Console.WriteLine("┌─────────────┬──────────────┬───────────────────┬─────────────────┐");
            Console.WriteLine("│ Диаметр (мкм)│ Площадь (мм²)│ Интенсивность     │ Флюенс (Дж/мм²) │");
            Console.WriteLine("├─────────────┼──────────────┼───────────────────┼─────────────────┤");

            double[] diameters = { 48.141, 50, 60, 65, 70, 80, 90, 100 };

            foreach (double d_micron in diameters)
            {
                double d_mm = d_micron / 1000.0;
                double radius_mm = d_mm / 2.0;
                double area = Math.PI * radius_mm * radius_mm;
                double intensity = power / area;
                double fluence = (power / speed) / d_mm * 1000.0;

                Console.WriteLine($"│ {d_micron,11:F3} │ {area,12:F6} │ {intensity,17:F2} │ {fluence,15:F3} │");
            }

            Console.WriteLine("└─────────────┴──────────────┴───────────────────┴─────────────────┘");

            Console.WriteLine("\n💡 Выводы:");
            Console.WriteLine("  • Меньший диаметр → выше плотность энергии → глубже проплавление");
            Console.WriteLine("  • Больший диаметр → ниже плотность → меньше перегрев");
            Console.WriteLine("  • Выбор диаметра зависит от задачи: контур vs заливка vs гравировка");
        }

        /// <summary>
        /// Пример 5: Когда использовать Z, а когда - DiameterOperation
        /// </summary>
        public static void Example5_When_To_Use_What()
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════");
            Console.WriteLine("  Пример 5: Z vs DiameterOperation - когда что использовать");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");

            Console.WriteLine("📌 Используйте UDM_AddPolyline3D с Z-координатой когда:");
            Console.WriteLine("  ✓ Маркировка на криволинейной поверхности (цилиндр, сфера)");
            Console.WriteLine("  ✓ Следование за рельефом детали");
            Console.WriteLine("  ✓ Компенсация кривизны оптического поля");
            Console.WriteLine("  ✓ Многослойная 3D печать с послойным подъемом");
            Console.WriteLine("");
            Console.WriteLine("  Пример:");
            Console.WriteLine("    structUdmPos point = new structUdmPos(x: 10, y: 20, z: 0.5);");
            Console.WriteLine("    // Z=0.5 мм - фокус на 0.5 мм выше базовой плоскости");
            Console.WriteLine("");

            Console.WriteLine("📌 Используйте DiameterOperation (FOCUS параметр) когда:");
            Console.WriteLine("  ✓ Нужно изменить размер пятна для разных участков");
            Console.WriteLine("  ✓ Контур требует малого диаметра, заливка - большого");
            Console.WriteLine("  ✓ Управление плотностью энергии");
            Console.WriteLine("  ✓ Работа на плоской поверхности с переменным качеством");
            Console.WriteLine("");
            Console.WriteLine("  Пример:");
            Console.WriteLine("    // Параметры для контура");
            Console.WriteLine("    layer.curBeamDiameterMicron = 50.0; // Малый диаметр = высокая точность");
            Console.WriteLine("");
            Console.WriteLine("    // Параметры для заливки");
            Console.WriteLine("    layer.curBeamDiameterMicron = 80.0; // Больший диаметр = меньше перегрев");
            Console.WriteLine("");

            Console.WriteLine("⚠ ВАЖНО:");
            Console.WriteLine("  • Z-координата в AddPolyline3D влияет на положение фокуса в пространстве");
            Console.WriteLine("  • DiameterOperation влияет на размер пятна путем расфокусировки");
            Console.WriteLine("  • Оба параметра можно комбинировать!");
            Console.WriteLine("");
            Console.WriteLine("  Комбинация:");
            Console.WriteLine("    1. Z задает положение детали по высоте");
            Console.WriteLine("    2. Diameter задает размер пятна для конкретного участка");
            Console.WriteLine("    3. Итоговый фокус = базовая_плоскость + Z + смещение_от_диаметра");
        }
    }

    #endregion

    #region Главная программа

    class ProgramDiameterExamples
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   Диаметр луча vs Z-координата в PrintMateMC            ║");
            Console.WriteLine("║        Понимание физики фокусировки                      ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

            try
            {
                // Выполнить все примеры
                DiameterBasedExamples.Example1_DiameterToZ_Demonstration();
                DiameterBasedExamples.Example2_VariableDiameter_AsZ();
                DiameterBasedExamples.Example3_ConfigDiameters();
                DiameterBasedExamples.Example4_EnergyDensity_vs_Diameter();
                DiameterBasedExamples.Example5_When_To_Use_What();

                Console.WriteLine("\n\n╔══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                   РЕЗЮМЕ                                 ║");
                Console.WriteLine("╠══════════════════════════════════════════════════════════╣");
                Console.WriteLine("║                                                          ║");
                Console.WriteLine("║  В PrintMateMC используются ДВА способа управления       ║");
                Console.WriteLine("║  положением фокуса:                                      ║");
                Console.WriteLine("║                                                          ║");
                Console.WriteLine("║  1️⃣  FOCUS параметр (DiameterOperation)                  ║");
                Console.WriteLine("║     • Задает ДИАМЕТР луча в микронах                    ║");
                Console.WriteLine("║     • Работает через расфокусировку                     ║");
                Console.WriteLine("║     • Значение: 48-150 мкм (типично 50-80 мкм)          ║");
                Console.WriteLine("║     • Влияет на плотность энергии                       ║");
                Console.WriteLine("║                                                          ║");
                Console.WriteLine("║  2️⃣  Z-координата (UDM_AddPolyline3D)                    ║");
                Console.WriteLine("║     • Задает положение фокуса в пространстве            ║");
                Console.WriteLine("║     • Используется для 3D траекторий                    ║");
                Console.WriteLine("║     • Значение: ±несколько мм от базовой плоскости      ║");
                Console.WriteLine("║     • Компенсирует рельеф детали                        ║");
                Console.WriteLine("║                                                          ║");
                Console.WriteLine("║  ⚙️  Они КОМБИНИРУЮТСЯ для точного управления фокусом!  ║");
                Console.WriteLine("║                                                          ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ ОШИБКА: {ex.Message}");
            }

            Console.WriteLine("\n\nНажмите Enter для выхода...");
            Console.ReadLine();
        }
    }

    #endregion
}
