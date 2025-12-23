using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Hans.NET.UDM.Examples
{
    #region Определения структур UDM

    /// <summary>
    /// Позиция точки в 3D пространстве (структура UDM)
    /// </summary>
    public struct structUdmPos
    {
        /// <summary>
        /// Координата X в миллиметрах
        /// </summary>
        public float x;

        /// <summary>
        /// Координата Y в миллиметрах
        /// </summary>
        public float y;

        /// <summary>
        /// Координата Z в миллиметрах (ось фокуса)
        /// </summary>
        public float z;

        /// <summary>
        /// Координата A (угол поворота или дополнительная ось)
        /// </summary>
        public float a;

        public structUdmPos(float x, float y, float z = 0, float a = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.a = a;
        }

        public override string ToString()
        {
            return $"({x:F3}, {y:F3}, {z:F3})";
        }
    }

    /// <summary>
    /// Параметры маркировки для слоя (layer)
    /// Каждый слой может иметь свои параметры скорости, мощности и т.д.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MarkParameter
    {
        /// <summary>
        /// Скорость маркировки в мм/с
        /// Пример: 1000 = 1 м/с
        /// Определяет скорость движения луча при включенном лазере
        /// </summary>
        public UInt32 MarkSpeed;

        /// <summary>
        /// Скорость прыжка в мм/с (без лазера)
        /// Пример: 25000 = 25 м/с
        /// Максимальная скорость ограничена инерцией зеркал
        /// </summary>
        public UInt32 JumpSpeed;

        /// <summary>
        /// Задержка маркировки в микросекундах
        /// Пауза перед началом маркировки
        /// </summary>
        public UInt32 MarkDelay;

        /// <summary>
        /// Задержка прыжка в микросекундах
        /// Время стабилизации зеркал после прыжка
        /// </summary>
        public UInt32 JumpDelay;

        /// <summary>
        /// Задержка на углах (полигонах) в микросекундах
        /// Уменьшает скругление острых углов
        /// </summary>
        public UInt32 PolygonDelay;

        /// <summary>
        /// Количество проходов маркировки
        /// 1 = один проход, 2+ = несколько проходов
        /// </summary>
        public UInt32 MarkCount;

        /// <summary>
        /// Задержка включения лазера в микросекундах
        /// Компенсирует время выхода лазера на рабочую мощность
        /// </summary>
        public float LaserOnDelay;

        /// <summary>
        /// Задержка выключения лазера в микросекундах
        /// Компенсирует инерцию выключения лазера
        /// </summary>
        public float LaserOffDelay;

        /// <summary>
        /// Задержка подавления первого импульса в микросекундах
        /// FPK = First Pulse Killer
        /// Используется для подавления нестабильного первого импульса
        /// </summary>
        public float FPKDelay;

        /// <summary>
        /// Длительность подавления первого импульса в микросекундах
        /// </summary>
        public float FPKLength;

        /// <summary>
        /// Задержка Q-модуляции в микросекундах
        /// Используется для импульсных лазеров
        /// </summary>
        public float QDelay;

        /// <summary>
        /// Скважность (duty cycle) при маркировке (0.0 - 1.0)
        /// 0.5 = 50% (импульс / период)
        /// Определяет соотношение времени включения к периоду
        /// </summary>
        public float DutyCycle;

        /// <summary>
        /// Частота импульсов при маркировке в кГц
        /// Пример: 20.0 = 20 кГц
        /// Определяет частоту модуляции лазера
        /// </summary>
        public float Frequency;

        /// <summary>
        /// Частота в режиме ожидания (без маркировки) в кГц
        /// Поддерживает лазер в рабочем состоянии
        /// </summary>
        public float StandbyFrequency;

        /// <summary>
        /// Скважность в режиме ожидания (0.0 - 1.0)
        /// </summary>
        public float StandbyDutyCycle;

        /// <summary>
        /// Мощность лазера в процентах (0.0 - 100.0)
        /// 50.0 = 50% от максимальной мощности
        /// </summary>
        public float LaserPower;

        /// <summary>
        /// Режим аналогового управления мощностью
        /// 1 = использовать аналоговый выход 0-10V для управления мощностью
        /// 0 = цифровое управление
        /// </summary>
        public UInt32 AnalogMode;

        /// <summary>
        /// Номер формы волны для SPI лазера (0-63)
        /// Предопределенные формы импульсов
        /// </summary>
        public UInt32 Waveform;

        /// <summary>
        /// Режим управления длительностью импульса для MOPA лазеров
        /// 0 = выключен
        /// 1 = включен (используется PulseWidth)
        /// </summary>
        public UInt32 PulseWidthMode;

        /// <summary>
        /// Длительность импульса для MOPA лазера в наносекундах
        /// Типичные значения: 20-500 нс
        /// Влияет на глубину проплавления и качество маркировки
        /// </summary>
        public UInt32 PulseWidth;

        /// <summary>
        /// Создает параметры маркировки с типичными значениями
        /// </summary>
        public static MarkParameter CreateDefault()
        {
            return new MarkParameter
            {
                MarkSpeed = 1000,           // 1 м/с
                JumpSpeed = 25000,          // 25 м/с
                MarkDelay = 500,            // 500 мкс
                JumpDelay = 400,            // 400 мкс
                PolygonDelay = 200,         // 200 мкс
                MarkCount = 1,              // 1 проход
                LaserOnDelay = 100.0f,      // 100 мкс
                LaserOffDelay = 100.0f,     // 100 мкс
                FPKDelay = 0.0f,            // отключено
                FPKLength = 0.0f,           // отключено
                QDelay = 0.0f,
                DutyCycle = 0.5f,           // 50%
                Frequency = 20.0f,          // 20 кГц
                StandbyFrequency = 20.0f,   // 20 кГц
                StandbyDutyCycle = 0.1f,    // 10%
                LaserPower = 50.0f,         // 50%
                AnalogMode = 0,             // цифровое управление
                Waveform = 0,
                PulseWidthMode = 0,         // выключен
                PulseWidth = 100            // 100 нс
            };
        }
    }

    #endregion

    #region UDM DLL Импорт

    /// <summary>
    /// API для создания файлов маркировки UDM (Universal Data Model)
    /// Позволяет программно создавать траектории маркировки
    /// </summary>
    public class HM_UDM_DLL
    {
        /// <summary>
        /// Создать новый файл маркировки
        /// Очищает предыдущие данные и инициализирует новый проект
        /// </summary>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_NewFile();

        /// <summary>
        /// Сохранить созданный файл маркировки на диск
        /// </summary>
        /// <param name="strFilePath">Путь для сохранения .bin файла</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SaveToFile(string strFilePath);

        /// <summary>
        /// Получить буфер UDM в памяти (для загрузки без файла)
        /// </summary>
        /// <param name="pUdmBuffer">Указатель на буфер</param>
        /// <param name="nBytesCount">Размер буфера в байтах</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_GetUDMBuffer(ref IntPtr pUdmBuffer, ref int nBytesCount);

        /// <summary>
        /// Начать основной блок команд маркировки
        /// Все команды между Main() и EndMain() будут выполнены
        /// </summary>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_Main();

        /// <summary>
        /// Завершить основной блок команд маркировки
        /// </summary>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_EndMain();

        /// <summary>
        /// Установить протокол связи со сканером
        /// </summary>
        /// <param name="nProtocol">
        ///   0 = SPI протокол
        ///   1 = XY2-100 протокол
        ///   2 = SL2 протокол
        /// </param>
        /// <param name="nDimensional">
        ///   0 = 2D маркировка
        ///   1 = 3D маркировка
        /// </param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetProtocol(int nProtocol, int nDimensional);

        /// <summary>
        /// Начать блок повторения
        /// </summary>
        /// <param name="repeatCount">Количество повторений</param>
        /// <returns>Адрес начала блока (для UDM_RepeatEnd)</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_RepeatStart(int repeatCount);

        /// <summary>
        /// Завершить блок повторения
        /// </summary>
        /// <param name="startAddress">Адрес начала блока (из UDM_RepeatStart)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_RepeatEnd(int startAddress);

        /// <summary>
        /// Прыжок в точку без включения лазера
        /// </summary>
        /// <param name="x">Координата X в мм</param>
        /// <param name="y">Координата Y в мм</param>
        /// <param name="z">Координата Z в мм (фокус)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_Jump(float x, float y, float z);

        /// <summary>
        /// Пауза (задержка)
        /// </summary>
        /// <param name="msTime">Время задержки в миллисекундах</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_Wait(float msTime);

        /// <summary>
        /// Включить/выключить направляющий лазер (красный указатель)
        /// </summary>
        /// <param name="enable">true - включить, false - выключить</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetGuidLaser(bool enable);

        /// <summary>
        /// Ожидание входного сигнала
        /// Выполнение приостанавливается до получения сигнала
        /// </summary>
        /// <param name="uInIndex">Индекс входа (0-7)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetInput(UInt32 uInIndex);

        /// <summary>
        /// Установить все выходы одновременно
        /// </summary>
        /// <param name="uData">
        ///   Битовая маска выходов
        ///   Пример: 0b1111 = все 4 выхода включены
        ///           0b0011 = OUT0 и OUT1 включены, OUT2 и OUT3 выключены
        /// </param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOutPutAll(UInt32 uData);

        /// <summary>
        /// Включить один выход
        /// </summary>
        /// <param name="nOutIndex">Индекс выхода (0-7)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOutPutOn(UInt32 nOutIndex);

        /// <summary>
        /// Выключить один выход
        /// </summary>
        /// <param name="nOutIndex">Индекс выхода (0-7)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOutPutOff(UInt32 nOutIndex);

        /// <summary>
        /// Включить выход на карте GMC4
        /// </summary>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOutputOn_GMC4(UInt32 nOutIndex);

        /// <summary>
        /// Выключить выход на карте GMC4
        /// </summary>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOutputOff_GMC4(UInt32 nOutIndex);

        /// <summary>
        /// Установить значения аналоговых выходов
        /// </summary>
        /// <param name="VoutA">VOUTA: 0.0=0V, 0.5=5V, 1.0=10V</param>
        /// <param name="VoutB">VOUTB: 0.0=0V, 0.5=5V, 1.0=10V</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetAnalogValue(float VoutA, float VoutB);

        /// <summary>
        /// Установить смещение всех координат
        /// Применяется ко всем последующим точкам
        /// </summary>
        /// <param name="offsetX">Смещение по X в мм</param>
        /// <param name="offsetY">Смещение по Y в мм</param>
        /// <param name="offsetZ">Смещение по Z в мм</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOffset(float offsetX, float offsetY, float offsetZ);

        /// <summary>
        /// Установить поворот координат
        /// </summary>
        /// <param name="angle">Угол поворота в градусах</param>
        /// <param name="centryX">Центр поворота по X</param>
        /// <param name="centryY">Центр поворота по Y</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetRotate(float angle, float centryX, float centryY);

        /// <summary>
        /// Включить триггер от педали (ножной педали)
        /// </summary>
        /// <param name="nDelayTime">Задержка после триггера в мс</param>
        /// <param name="nTriggerType">
        ///   0 = триггер по переднему фронту
        ///   1 = триггер по уровню
        /// </param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_FootTrigger(uint nDelayTime, int nTriggerType);

        /// <summary>
        /// Настроить режим SkyWriting (непрерывная маркировка без выключения лазера)
        /// </summary>
        /// <param name="enable">0 = выключен, 1 = включен</param>
        /// <param name="mode">Режим SkyWriting</param>
        /// <param name="uniformLen">Длина участка равномерной скорости</param>
        /// <param name="accLen">Длина участка ускорения</param>
        /// <param name="angleLimit">Предельный угол для включения SkyWriting</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetSkyWritingMode(int enable, int mode, float uniformLen, float accLen, float angleLimit);

        /// <summary>
        /// Установить длину продления прыжка
        /// Удлиняет траекторию прыжка для плавности
        /// </summary>
        /// <param name="jumpExtendLen">Длина продления в мм</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetJumpExtendLen(float jumpExtendLen);

        /// <summary>
        /// Установить параметры для нескольких слоев маркировки
        /// </summary>
        /// <param name="layersParameter">Массив параметров слоев</param>
        /// <param name="count">Количество слоев</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetLayersPara(MarkParameter[] layersParameter, int count);

        /// <summary>
        /// Добавить 2D полилинию (ломаную линию)
        /// Маркируется с включенным лазером
        /// </summary>
        /// <param name="nPos">Массив точек</param>
        /// <param name="nCount">Количество точек</param>
        /// <param name="layerIndex">Индекс слоя (параметры из SetLayersPara)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_AddPolyline2D(structUdmPos[] nPos, int nCount, int layerIndex);

        /// <summary>
        /// Добавить 3D полилинию (с изменением фокуса)
        /// </summary>
        /// <param name="nPos">Массив точек с координатами Z</param>
        /// <param name="nCount">Количество точек</param>
        /// <param name="layerIndex">Индекс слоя</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_AddPolyline3D(structUdmPos[] nPos, int nCount, int layerIndex);

        /// <summary>
        /// Добавить точечную маркировку (точка)
        /// </summary>
        /// <param name="pos">Координаты точки</param>
        /// <param name="time">Время маркировки точки в миллисекундах</param>
        /// <param name="layerIndex">Индекс слоя</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_AddPoint2D(structUdmPos pos, float time, int layerIndex);

        /// <summary>
        /// Настроить замкнутый контур управления (closed-loop control)
        /// </summary>
        /// <param name="enable">Включить/выключить</param>
        /// <param name="galvoType">Тип гальванометра</param>
        /// <param name="followErrorMax">Максимальная ошибка следования</param>
        /// <param name="followErrorCount">Счетчик ошибок</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetCloseLoop(bool enable, int galvoType, int followErrorMax, int followErrorCount);

        /// <summary>
        /// Установить параметры 3D коррекции (кривизна поля)
        /// </summary>
        /// <param name="baseFocal">Базовое фокусное расстояние</param>
        /// <param name="paraK">Массив коэффициентов коррекции</param>
        /// <param name="nCount">Количество коэффициентов</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_Set3dCorrectionPara(float baseFocal, double[] paraK, int nCount);

        /// <summary>
        /// Получить значение Z для 3D коррекции
        /// </summary>
        /// <param name="x">Координата X</param>
        /// <param name="y">Координата Y</param>
        /// <param name="height">Высота</param>
        /// <returns>Скорректированное значение Z</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_GetZvalue(float x, float y, float height);

        /// <summary>
        /// Добавить 3D полилинию с разрывами и коррекцией
        /// </summary>
        /// <param name="nPos">Массив точек</param>
        /// <param name="nCount">Количество точек</param>
        /// <param name="p2pGap">Расстояние между точками для разрыва</param>
        /// <param name="layerIndex">Индекс слоя</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_AddBreakAndCorPolyline3D(structUdmPos[] nPos, int nCount, float p2pGap, int layerIndex);
    }

    #endregion

    #region Примеры использования UDM

    /// <summary>
    /// Класс с примерами использования UDM API для создания файлов маркировки
    /// </summary>
    public class UDM_Examples
    {
        /// <summary>
        /// Пример 1: Создание простого файла маркировки - квадрат
        /// </summary>
        public static void Example1_CreateSimpleSquare(string outputPath)
        {
            Console.WriteLine("=== Пример 1: Создание квадрата ===\n");

            // Шаг 1: Создать новый файл
            int result = HM_UDM_DLL.UDM_NewFile();
            if (result != 0)
            {
                Console.WriteLine($"✗ Ошибка создания файла: {result}");
                return;
            }
            Console.WriteLine("✓ Новый файл создан");

            // Шаг 2: Установить протокол (SPI, 2D)
            result = HM_UDM_DLL.UDM_SetProtocol(0, 0);
            Console.WriteLine("✓ Протокол установлен: SPI, 2D");

            // Шаг 3: Создать параметры слоя
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 800;      // 0.8 м/с
            layers[0].LaserPower = 30.0f;   // 30% мощности

            result = HM_UDM_DLL.UDM_SetLayersPara(layers, 1);
            Console.WriteLine($"✓ Параметры слоя установлены: скорость={layers[0].MarkSpeed} мм/с, мощность={layers[0].LaserPower}%");

            // Шаг 4: Начать основной блок
            result = HM_UDM_DLL.UDM_Main();

            // Шаг 5: Создать квадрат 20x20 мм с центром в (0, 0)
            structUdmPos[] squarePoints = new structUdmPos[]
            {
                new structUdmPos(-10, -10, 0),  // Левый нижний угол
                new structUdmPos( 10, -10, 0),  // Правый нижний угол
                new structUdmPos( 10,  10, 0),  // Правый верхний угол
                new structUdmPos(-10,  10, 0),  // Левый верхний угол
                new structUdmPos(-10, -10, 0)   // Замкнуть контур
            };

            Console.WriteLine("\nДобавление точек квадрата:");
            foreach (var point in squarePoints)
            {
                Console.WriteLine($"  {point}");
            }

            result = HM_UDM_DLL.UDM_AddPolyline2D(squarePoints, squarePoints.Length, 0);
            Console.WriteLine("✓ Квадрат добавлен");

            // Шаг 6: Завершить основной блок
            result = HM_UDM_DLL.UDM_EndMain();

            // Шаг 7: Сохранить файл
            result = HM_UDM_DLL.UDM_SaveToFile(outputPath);
            if (result == 0)
            {
                Console.WriteLine($"\n✓ Файл сохранен: {outputPath}");
            }
            else
            {
                Console.WriteLine($"\n✗ Ошибка сохранения: {result}");
            }
        }

        /// <summary>
        /// Пример 2: Создание окружности методом полилинии
        /// </summary>
        public static void Example2_CreateCircle(string outputPath, float radius, int segments)
        {
            Console.WriteLine($"\n=== Пример 2: Создание окружности ===");
            Console.WriteLine($"Радиус: {radius} мм, Сегментов: {segments}\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 0);

            // Настройка параметров для окружности
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 1250;     // Чуть быстрее
            layers[0].LaserPower = 40.0f;
            layers[0].PolygonDelay = 100;   // Меньше задержка на углах (окружность гладкая)

            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);
            HM_UDM_DLL.UDM_Main();

            // Генерация точек окружности
            structUdmPos[] circlePoints = new structUdmPos[segments + 1];
            double angleStep = 2 * Math.PI / segments;

            Console.WriteLine("Генерация точек окружности:");
            for (int i = 0; i <= segments; i++)
            {
                double angle = i * angleStep;
                float x = radius * (float)Math.Cos(angle);
                float y = radius * (float)Math.Sin(angle);
                circlePoints[i] = new structUdmPos(x, y, 0);

                if (i < 5 || i > segments - 2)
                {
                    Console.WriteLine($"  Точка {i}: {circlePoints[i]}");
                }
                else if (i == 5)
                {
                    Console.WriteLine($"  ... ({segments - 9} точек пропущено) ...");
                }
            }

            HM_UDM_DLL.UDM_AddPolyline2D(circlePoints, circlePoints.Length, 0);
            Console.WriteLine($"\n✓ Окружность добавлена ({circlePoints.Length} точек)");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 3: Создание текста по точкам (упрощенно - буква "H")
        /// </summary>
        public static void Example3_CreateLetterH(string outputPath)
        {
            Console.WriteLine("\n=== Пример 3: Создание буквы 'H' ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 0);

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 600;      // Медленнее для точности
            layers[0].LaserPower = 50.0f;

            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);
            HM_UDM_DLL.UDM_Main();

            // Буква H состоит из трех линий: левая вертикаль, правая вертикаль, горизонталь посередине
            float height = 15.0f;   // мм
            float width = 10.0f;    // мм

            // Левая вертикальная линия
            structUdmPos[] leftLine = new structUdmPos[]
            {
                new structUdmPos(0, 0, 0),
                new structUdmPos(0, height, 0)
            };

            // Горизонтальная линия (перемычка)
            structUdmPos[] middleLine = new structUdmPos[]
            {
                new structUdmPos(0, height / 2, 0),
                new structUdmPos(width, height / 2, 0)
            };

            // Правая вертикальная линия
            structUdmPos[] rightLine = new structUdmPos[]
            {
                new structUdmPos(width, 0, 0),
                new structUdmPos(width, height, 0)
            };

            Console.WriteLine("Добавление линий буквы 'H':");
            Console.WriteLine($"  Левая вертикаль: (0, 0) -> (0, {height})");
            HM_UDM_DLL.UDM_AddPolyline2D(leftLine, leftLine.Length, 0);

            Console.WriteLine($"  Перемычка: (0, {height/2}) -> ({width}, {height/2})");
            HM_UDM_DLL.UDM_AddPolyline2D(middleLine, middleLine.Length, 0);

            Console.WriteLine($"  Правая вертикаль: ({width}, 0) -> ({width}, {height})");
            HM_UDM_DLL.UDM_AddPolyline2D(rightLine, rightLine.Length, 0);

            Console.WriteLine("\n✓ Буква 'H' добавлена");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 4: Использование нескольких слоев с разными параметрами
        /// </summary>
        public static void Example4_MultiLayerMarking(string outputPath)
        {
            Console.WriteLine("\n=== Пример 4: Многослойная маркировка ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 0);

            // Создаем 3 слоя с разными параметрами
            MarkParameter[] layers = new MarkParameter[3];

            // Слой 0: Контур (быстро, малая мощность)
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 2000;
            layers[0].LaserPower = 20.0f;

            // Слой 1: Заливка (средняя скорость, средняя мощность)
            layers[1] = MarkParameter.CreateDefault();
            layers[1].MarkSpeed = 1000;
            layers[1].LaserPower = 40.0f;

            // Слой 2: Глубокая гравировка (медленно, высокая мощность)
            layers[2] = MarkParameter.CreateDefault();
            layers[2].MarkSpeed = 500;
            layers[2].LaserPower = 70.0f;
            layers[2].MarkCount = 3;  // 3 прохода

            HM_UDM_DLL.UDM_SetLayersPara(layers, 3);
            Console.WriteLine("Параметры слоев:");
            Console.WriteLine($"  Слой 0 (контур):     скорость={layers[0].MarkSpeed} мм/с, мощность={layers[0].LaserPower}%");
            Console.WriteLine($"  Слой 1 (заливка):    скорость={layers[1].MarkSpeed} мм/с, мощность={layers[1].LaserPower}%");
            Console.WriteLine($"  Слой 2 (гравировка): скорость={layers[2].MarkSpeed} мм/с, мощность={layers[2].LaserPower}%, проходов={layers[2].MarkCount}");

            HM_UDM_DLL.UDM_Main();

            // Квадрат с контуром (слой 0)
            structUdmPos[] outline = new structUdmPos[]
            {
                new structUdmPos(-15, -15, 0),
                new structUdmPos( 15, -15, 0),
                new structUdmPos( 15,  15, 0),
                new structUdmPos(-15,  15, 0),
                new structUdmPos(-15, -15, 0)
            };
            HM_UDM_DLL.UDM_AddPolyline2D(outline, outline.Length, 0);
            Console.WriteLine("\n✓ Контур добавлен (слой 0)");

            // Заливка штриховкой (слой 1)
            for (float y = -14; y <= 14; y += 2.0f)
            {
                structUdmPos[] hatchLine = new structUdmPos[]
                {
                    new structUdmPos(-14, y, 0),
                    new structUdmPos( 14, y, 0)
                };
                HM_UDM_DLL.UDM_AddPolyline2D(hatchLine, hatchLine.Length, 1);
            }
            Console.WriteLine("✓ Штриховка добавлена (слой 1, 15 линий)");

            // Центральная метка глубокой гравировкой (слой 2)
            structUdmPos[] crosshair = new structUdmPos[]
            {
                new structUdmPos(-3, 0, 0),
                new structUdmPos( 3, 0, 0)
            };
            HM_UDM_DLL.UDM_AddPolyline2D(crosshair, crosshair.Length, 2);

            structUdmPos[] crosshairV = new structUdmPos[]
            {
                new structUdmPos(0, -3, 0),
                new structUdmPos(0,  3, 0)
            };
            HM_UDM_DLL.UDM_AddPolyline2D(crosshairV, crosshairV.Length, 2);
            Console.WriteLine("✓ Центральная метка добавлена (слой 2, глубокая гравировка)");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"\n✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 5: Использование управления I/O и задержек
        /// </summary>
        public static void Example5_IOControl(string outputPath)
        {
            Console.WriteLine("\n=== Пример 5: Управление I/O и задержки ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 0);

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Сценарий: Маркировка с управлением затвором и ожиданием сигналов

            Console.WriteLine("Сценарий маркировки:");

            // 1. Включить выход OUT0 (например, открыть затвор)
            HM_UDM_DLL.UDM_SetOutPutOn(0);
            Console.WriteLine("  1. Включен выход OUT0 (открытие затвора)");

            // 2. Подождать 100 мс (время открытия затвора)
            HM_UDM_DLL.UDM_Wait(100);
            Console.WriteLine("  2. Задержка 100 мс");

            // 3. Включить направляющий лазер (красный)
            HM_UDM_DLL.UDM_SetGuidLaser(true);
            Console.WriteLine("  3. Включен направляющий лазер");

            // 4. Подождать 500 мс (для визуальной проверки позиции)
            HM_UDM_DLL.UDM_Wait(500);
            Console.WriteLine("  4. Задержка 500 мс (проверка позиции)");

            // 5. Выключить направляющий лазер
            HM_UDM_DLL.UDM_SetGuidLaser(false);
            Console.WriteLine("  5. Выключен направляющий лазер");

            // 6. Ожидание сигнала на входе IN0 (например, кнопка "Старт")
            Console.WriteLine("  6. Ожидание сигнала на входе IN0 (кнопка старт)");
            HM_UDM_DLL.UDM_SetInput(0);

            // 7. Выполнить маркировку
            structUdmPos[] line = new structUdmPos[]
            {
                new structUdmPos(-10, 0, 0),
                new structUdmPos( 10, 0, 0)
            };
            HM_UDM_DLL.UDM_AddPolyline2D(line, line.Length, 0);
            Console.WriteLine("  7. Выполнение маркировки");

            // 8. Выключить выход OUT0 (закрыть затвор)
            HM_UDM_DLL.UDM_SetOutPutOff(0);
            Console.WriteLine("  8. Выключен выход OUT0 (закрытие затвора)");

            // 9. Включить сигнализацию (OUT1 - зеленая лампа)
            HM_UDM_DLL.UDM_SetOutPutOn(1);
            Console.WriteLine("  9. Включен выход OUT1 (сигнализация завершения)");

            // 10. Задержка 2 секунды
            HM_UDM_DLL.UDM_Wait(2000);
            Console.WriteLine("  10. Задержка 2000 мс");

            // 11. Выключить сигнализацию
            HM_UDM_DLL.UDM_SetOutPutOff(1);
            Console.WriteLine("  11. Выключен выход OUT1");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"\n✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 6: Использование повторений (циклов)
        /// </summary>
        public static void Example6_RepeatPatterns(string outputPath)
        {
            Console.WriteLine("\n=== Пример 6: Повторения (массив фигур) ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 0);

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 1500;
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Создать массив 5x5 маленьких кругов
            int gridSize = 5;
            float spacing = 10.0f;  // Расстояние между центрами
            float radius = 2.0f;    // Радиус каждого круга
            int segments = 16;      // Точек на окружность

            Console.WriteLine($"Создание массива {gridSize}x{gridSize} окружностей:");
            Console.WriteLine($"  Радиус: {radius} мм");
            Console.WriteLine($"  Расстояние: {spacing} мм");

            float startX = -(gridSize - 1) * spacing / 2;
            float startY = -(gridSize - 1) * spacing / 2;

            int totalCircles = 0;

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    float centerX = startX + col * spacing;
                    float centerY = startY + row * spacing;

                    // Генерация окружности вокруг центра (centerX, centerY)
                    structUdmPos[] circle = new structUdmPos[segments + 1];
                    double angleStep = 2 * Math.PI / segments;

                    for (int i = 0; i <= segments; i++)
                    {
                        double angle = i * angleStep;
                        float x = centerX + radius * (float)Math.Cos(angle);
                        float y = centerY + radius * (float)Math.Sin(angle);
                        circle[i] = new structUdmPos(x, y, 0);
                    }

                    HM_UDM_DLL.UDM_AddPolyline2D(circle, circle.Length, 0);
                    totalCircles++;
                }
            }

            Console.WriteLine($"\n✓ Добавлено {totalCircles} окружностей");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 7: 3D маркировка с изменением фокуса
        /// </summary>
        public static void Example7_3DMarking(string outputPath)
        {
            Console.WriteLine("\n=== Пример 7: 3D маркировка (спираль) ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);  // 3D режим!

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 800;
            layers[0].LaserPower = 60.0f;
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Создать спираль с изменением высоты
            int turns = 5;          // Количество витков
            int pointsPerTurn = 20; // Точек на виток
            float radius = 15.0f;   // Радиус спирали
            float heightStep = 2.0f; // Подъем по Z на каждый виток

            int totalPoints = turns * pointsPerTurn + 1;
            structUdmPos[] spiral = new structUdmPos[totalPoints];

            Console.WriteLine($"Создание 3D спирали:");
            Console.WriteLine($"  Витков: {turns}");
            Console.WriteLine($"  Радиус: {radius} мм");
            Console.WriteLine($"  Подъем: {heightStep} мм/виток");

            double angleStep = 2 * Math.PI / pointsPerTurn;

            for (int i = 0; i < totalPoints; i++)
            {
                double angle = i * angleStep;
                float x = radius * (float)Math.Cos(angle);
                float y = radius * (float)Math.Sin(angle);
                float z = (i / (float)pointsPerTurn) * heightStep;  // Линейное увеличение Z

                spiral[i] = new structUdmPos(x, y, z);

                if (i < 3 || i > totalPoints - 3)
                {
                    Console.WriteLine($"  Точка {i}: ({x:F2}, {y:F2}, {z:F2})");
                }
                else if (i == 3)
                {
                    Console.WriteLine($"  ... ({totalPoints - 6} точек пропущено) ...");
                }
            }

            HM_UDM_DLL.UDM_AddPolyline3D(spiral, spiral.Length, 0);
            Console.WriteLine($"\n✓ 3D спираль добавлена ({totalPoints} точек)");
            Console.WriteLine($"  Диапазон Z: 0.00 - {(turns * heightStep):F2} мм");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 8: Точечная маркировка (растр)
        /// </summary>
        public static void Example8_DotMatrix(string outputPath)
        {
            Console.WriteLine("\n=== Пример 8: Точечная маркировка (растр) ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 0);

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].LaserPower = 80.0f;  // Высокая мощность для точек
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Создать растр 10x10 точек
            int dotsX = 10;
            int dotsY = 10;
            float spacingX = 2.0f;  // мм
            float spacingY = 2.0f;  // мм
            float dotTime = 5.0f;   // мс на точку

            Console.WriteLine($"Создание растра {dotsX}x{dotsY} точек:");
            Console.WriteLine($"  Расстояние: {spacingX} x {spacingY} мм");
            Console.WriteLine($"  Время на точку: {dotTime} мс");

            float startX = -(dotsX - 1) * spacingX / 2;
            float startY = -(dotsY - 1) * spacingY / 2;

            int totalDots = 0;

            for (int row = 0; row < dotsY; row++)
            {
                for (int col = 0; col < dotsX; col++)
                {
                    float x = startX + col * spacingX;
                    float y = startY + row * spacingY;

                    structUdmPos dotPos = new structUdmPos(x, y, 0);
                    HM_UDM_DLL.UDM_AddPoint2D(dotPos, dotTime, 0);
                    totalDots++;
                }
            }

            Console.WriteLine($"\n✓ Добавлено {totalDots} точек");
            Console.WriteLine($"  Общее время маркировки: {totalDots * dotTime / 1000:F2} сек");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 9: Использование смещения и поворота
        /// </summary>
        public static void Example9_OffsetAndRotation(string outputPath)
        {
            Console.WriteLine("\n=== Пример 9: Смещение и поворот ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 0);

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            // Установить смещение (переместить все на 20 мм вправо, 30 мм вверх)
            HM_UDM_DLL.UDM_SetOffset(20.0f, 30.0f, 0.0f);
            Console.WriteLine("✓ Смещение установлено: X=+20 мм, Y=+30 мм");

            // Установить поворот (45 градусов вокруг точки (20, 30))
            HM_UDM_DLL.UDM_SetRotate(45.0f, 20.0f, 30.0f);
            Console.WriteLine("✓ Поворот установлен: 45° вокруг точки (20, 30)");

            HM_UDM_DLL.UDM_Main();

            // Добавить квадрат с центром в (0, 0)
            // Он будет автоматически смещен и повернут
            structUdmPos[] square = new structUdmPos[]
            {
                new structUdmPos(-10, -10, 0),
                new structUdmPos( 10, -10, 0),
                new structUdmPos( 10,  10, 0),
                new structUdmPos(-10,  10, 0),
                new structUdmPos(-10, -10, 0)
            };

            HM_UDM_DLL.UDM_AddPolyline2D(square, square.Length, 0);
            Console.WriteLine("\n✓ Квадрат добавлен (будет смещен и повернут автоматически)");
            Console.WriteLine("  Исходный центр: (0, 0)");
            Console.WriteLine("  После смещения: (20, 30)");
            Console.WriteLine("  После поворота: повернут на 45° вокруг (20, 30)");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"\n✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 10: Использование SkyWriting
        /// </summary>
        public static void Example10_SkyWriting(string outputPath)
        {
            Console.WriteLine("\n=== Пример 10: Режим SkyWriting ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 0);

            // Включить SkyWriting
            // Параметры: enable, mode, uniformLen, accLen, angleLimit
            HM_UDM_DLL.UDM_SetSkyWritingMode(
                enable: 1,              // Включен
                mode: 0,                // Режим 0 (базовый)
                uniformLen: 0.5f,       // Длина равномерного участка 0.5 мм
                accLen: 0.2f,           // Длина участка ускорения 0.2 мм
                angleLimit: 120.0f      // Предельный угол 120°
            );
            Console.WriteLine("✓ SkyWriting включен");
            Console.WriteLine("  Режим: базовый");
            Console.WriteLine("  Длина равномерного участка: 0.5 мм");
            Console.WriteLine("  Длина ускорения: 0.2 мм");
            Console.WriteLine("  Предельный угол: 120°");

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 2000;  // Высокая скорость (SkyWriting позволяет)
            layers[0].LaserPower = 45.0f;
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Создать сложную траекторию с множеством углов
            // SkyWriting улучшит качество на углах
            structUdmPos[] zigzag = new structUdmPos[]
            {
                new structUdmPos(-15,  0, 0),
                new structUdmPos(-10, 10, 0),
                new structUdmPos( -5,  0, 0),
                new structUdmPos(  0, 10, 0),
                new structUdmPos(  5,  0, 0),
                new structUdmPos( 10, 10, 0),
                new structUdmPos( 15,  0, 0)
            };

            HM_UDM_DLL.UDM_AddPolyline2D(zigzag, zigzag.Length, 0);
            Console.WriteLine("\n✓ Зигзагообразная траектория добавлена");
            Console.WriteLine("  SkyWriting сгладит углы без остановок лазера");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"\n✓ Файл сохранен: {outputPath}");
        }
    }

    #endregion

    #region Главная программа

    class ProgramUDMExamples
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         Примеры работы с UDM API (HM_HashuScan)         ║");
            Console.WriteLine("║      Программное создание файлов маркировки              ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

            try
            {
                string outputDir = @"C:\MarkFiles\UDM_Examples";

                // Создать директорию если не существует
                if (!System.IO.Directory.Exists(outputDir))
                {
                    System.IO.Directory.CreateDirectory(outputDir);
                    Console.WriteLine($"📁 Создана директория: {outputDir}\n");
                }

                // Выполнить все примеры
                UDM_Examples.Example1_CreateSimpleSquare($@"{outputDir}\01_square.bin");
                UDM_Examples.Example2_CreateCircle($@"{outputDir}\02_circle.bin", radius: 15.0f, segments: 64);
                UDM_Examples.Example3_CreateLetterH($@"{outputDir}\03_letter_H.bin");
                UDM_Examples.Example4_MultiLayerMarking($@"{outputDir}\04_multilayer.bin");
                UDM_Examples.Example5_IOControl($@"{outputDir}\05_io_control.bin");
                UDM_Examples.Example6_RepeatPatterns($@"{outputDir}\06_grid_5x5.bin");
                UDM_Examples.Example7_3DMarking($@"{outputDir}\07_spiral_3d.bin");
                UDM_Examples.Example8_DotMatrix($@"{outputDir}\08_dot_matrix.bin");
                UDM_Examples.Example9_OffsetAndRotation($@"{outputDir}\09_offset_rotation.bin");
                UDM_Examples.Example10_SkyWriting($@"{outputDir}\10_skywriting.bin");

                Console.WriteLine("\n\n╔══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║            Все примеры успешно выполнены!                ║");
                Console.WriteLine($"║  Файлы сохранены в: {outputDir.PadRight(30)}");
                Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            }
            catch (DllNotFoundException ex)
            {
                Console.WriteLine($"\n✗ ОШИБКА: Не найдена библиотека HM_HashuScan.dll");
                Console.WriteLine($"  Детали: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ ОШИБКА: {ex.Message}");
                Console.WriteLine($"  Стек вызовов: {ex.StackTrace}");
            }

            Console.WriteLine("\n\nНажмите Enter для выхода...");
            Console.ReadLine();
        }
    }

    #endregion
}
