using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Hans.NET.Examples
{
    #region Определения из Hans.NET библиотеки

    /// <summary>
    /// Информация об устройстве сканера
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// IP адрес карты в числовом формате
        /// </summary>
        public UInt64 IPValue { get; set; }

        /// <summary>
        /// Индекс карты в системе (0, 1, 2...)
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// IP адрес в строковом формате (например, "172.18.34.227")
        /// </summary>
        public string DeviceName { get; set; }

        public override bool Equals(object obj)
        {
            DeviceInfo tmp = obj as DeviceInfo;
            return tmp != null && this.IPValue == tmp.IPValue;
        }

        public override int GetHashCode()
        {
            return (int)IPValue;
        }
    }

    /// <summary>
    /// Результат выполнения операции
    /// </summary>
    public enum enumResult
    {
        HM_OK = 0,           // Успешно
        HM_FAILED = 1,       // Ошибка
        HM_UNKNOW = 2        // Неизвестный статус
    }

    /// <summary>
    /// Статус подключения устройства
    /// </summary>
    public enum enumConnectStatus
    {
        HM_DEV_Connect = 0,        // Подключено
        HM_DEV_Ready = 1,          // Готово к работе
        HM_DEV_NotAvailable = 2    // Недоступно
    }

    /// <summary>
    /// Статус работы устройства
    /// </summary>
    public enum enumWorkStatus
    {
        Ready = 1,    // Готов к работе
        Running = 2,  // Выполняется маркировка
        Alarm = 3     // Авария/ошибка
    }

    /// <summary>
    /// Коды сообщений от библиотеки
    /// </summary>
    public class clsEnumMessageDef
    {
        public const Int32 HM_MSG_DeviceStatusUpdate = 5991;  // Обновление статуса подключения
        public const Int32 HM_MSG_StreamProgress = 6011;      // Прогресс загрузки файла
        public const Int32 HM_MSG_StreamEnd = 6012;           // Загрузка файла завершена
        public const Int32 HM_MSG_MarkOver = 6035;            // Маркировка завершена
        public const Int32 HM_MSG_QueryExecProcess = 6037;    // Прогресс выполнения маркировки
    }

    /// <summary>
    /// Обертка для HM_HashuScan.dll - библиотека управления сканером Hans
    /// </summary>
    public class HM_HashuScanDLL
    {
        /// <summary>
        /// Инициализация платы сканера
        /// </summary>
        /// <param name="hWnd">Дескриптор окна для получения сообщений</param>
        /// <returns>0 - успешно, иначе код ошибки</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_InitBoard(IntPtr hWnd);

        /// <summary>
        /// Подключиться к карте по индексу
        /// </summary>
        /// <param name="nIndex">Индекс карты (0, 1, 2...)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_ConnectTo(int nIndex);

        /// <summary>
        /// Подключиться к карте по IP адресу
        /// </summary>
        /// <param name="pIp">IP адрес в формате "172.18.34.227"</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_ConnectByIpStr(string pIp);

        /// <summary>
        /// Отключиться от карты
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_DisconnectTo(int ipIndex);

        /// <summary>
        /// Получить индекс карты по IP адресу
        /// </summary>
        /// <param name="strIP">IP адрес</param>
        /// <returns>Индекс карты или -1 если не найдена</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_GetIndexByIpAddr(string strIP);

        /// <summary>
        /// Получить статус подключения карты
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>0 - подключена, 1 - готова, 2 - недоступна</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_GetConnectStatus(int ipIndex);

        /// <summary>
        /// Загрузить файл маркировки из файла
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="filePath">Путь к .bin файлу</param>
        /// <param name="hWnd">Дескриптор окна для сообщений о прогрессе</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_DownloadMarkFile(int ipIndex, string filePath, IntPtr hWnd);

        /// <summary>
        /// Загрузить файл маркировки из буфера памяти
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="pUDMBuff">Указатель на буфер с данными</param>
        /// <param name="nBytesCount">Размер буфера в байтах</param>
        /// <param name="hWnd">Дескриптор окна</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_DownloadMarkFileBuff(int ipIndex, IntPtr pUDMBuff, int nBytesCount, IntPtr hWnd);

        /// <summary>
        /// Записать файл маркировки в Flash (для автономной работы)
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="enable">true - записать, false - стереть</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_BurnMarkFile(int ipIndex, bool enable);

        /// <summary>
        /// Получить прогресс выполнения маркировки
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>Процент выполнения 0-100</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_ExecuteProgress(int ipIndex);

        /// <summary>
        /// Запустить маркировку
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_StartMark(int ipIndex);

        /// <summary>
        /// Остановить маркировку
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_StopMark(int ipIndex);

        /// <summary>
        /// Приостановить маркировку
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_PauseMark(int ipIndex);

        /// <summary>
        /// Продолжить маркировку после паузы
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_ContinueMark(int ipIndex);

        /// <summary>
        /// Установить смещение поля сканирования
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="offsetX">Смещение по X в мм</param>
        /// <param name="offsetY">Смещение по Y в мм</param>
        /// <param name="offsetZ">Смещение по Z в мм (фокус)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_SetOffset(int ipIndex, float offsetX, float offsetY, float offsetZ);

        /// <summary>
        /// Установить поворот поля сканирования
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="angle">Угол поворота в градусах</param>
        /// <param name="centryX">Центр поворота по X</param>
        /// <param name="centryY">Центр поворота по Y</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_SetRotates(int ipIndex, float angle, float centryX, float centryY);

        /// <summary>
        /// Управление направляющим лазером (красный указатель)
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="enable">true - включить, false - выключить</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_SetGuidLaser(int ipIndex, bool enable);

        /// <summary>
        /// Переместить зеркала сканера в указанную позицию (без лазера)
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="X">Координата X в мм</param>
        /// <param name="Y">Координата Y в мм</param>
        /// <param name="Z">Координата Z в мм (фокус)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_ScannerJump(int ipIndex, float X, float Y, float Z);

        /// <summary>
        /// Получить статус работы
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>1 - готов, 2 - работает, 3 - авария</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_GetWorkStatus(int ipIndex);

        /// <summary>
        /// Установить систему координат
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="coordinate">Тип координат 0-7</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_SetCoordinate(int ipIndex, int coordinate);

        /// <summary>
        /// Установить область маркировки
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="region">Номер области</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_SetMarkRegion(int ipIndex, int region);

        /// <summary>
        /// Получить текущую область маркировки
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>Номер области</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_GetMarkRegion(int ipIndex);

        /// <summary>
        /// Загрузить файл коррекции (калибровочную таблицу) в DDR
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="filePath">Путь к .crt файлу</param>
        /// <param name="hWnd">Дескриптор окна</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_DownloadCorrection(int ipIndex, string filePath, IntPtr hWnd);

        /// <summary>
        /// Записать файл коррекции во Flash (постоянное хранение)
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="filePath">Путь к .crt файлу</param>
        /// <param name="hWnd">Дескриптор окна</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_BurnCorrection(int ipIndex, string filePath, IntPtr hWnd);

        /// <summary>
        /// Выбрать таблицу коррекции (для систем с несколькими головками)
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="crtIndex">Индекс таблицы коррекции (0 или 1)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_SelectCorrection(int ipIndex, int crtIndex);

        /// <summary>
        /// Получить состояние входов GMC2 карты
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>Битовая маска входов (младший бит = IN0)</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_GetInput_GMC2(int ipIndex);

        /// <summary>
        /// Получить состояние входов GMC4 карты
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>Битовая маска входов</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_GetInput_GMC4(int ipIndex);

        /// <summary>
        /// Получить состояние входов от лазера (аварийные сигналы)
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>Битовая маска: Alarm1, Alarm2, Alarm3, Alarm4</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_GetLaserInput(int ipIndex);

        /// <summary>
        /// Установить выход в высокий уровень (GMC2)
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="nOutIndex">Номер выхода</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_SetOutputOn_GMC2(int ipIndex, int nOutIndex);

        /// <summary>
        /// Установить выход в низкий уровень (GMC2)
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="nOutIndex">Номер выхода</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_SetOutputOff_GMC2(int ipIndex, int nOutIndex);

        /// <summary>
        /// Установить выход в высокий уровень (GMC4)
        /// </summary>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_SetOutputOn_GMC4(int ipIndex, int nOutIndex);

        /// <summary>
        /// Установить выход в низкий уровень (GMC4)
        /// </summary>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_SetOutputOff_GMC4(int ipIndex, int nOutIndex);

        /// <summary>
        /// Установить значения аналоговых выходов
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="VoutA">Значение для VOUTA (0.0 = 0V, 0.5 = 5V, 1.0 = 10V)</param>
        /// <param name="VoutB">Значение для VOUTB (0.0 = 0V, 0.5 = 5V, 1.0 = 10V)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_SetAnalog(int ipIndex, float VoutA, float VoutB);

        /// <summary>
        /// Получить фактическую позицию зеркал XY
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="fbX">Позиция X (выход)</param>
        /// <param name="fbY">Позиция Y (выход)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_GetFeedbackPosXY(int ipIndex, ref short fbX, ref short fbY);

        /// <summary>
        /// Получить командную позицию зеркал XY
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="cmdX">Команда X (выход)</param>
        /// <param name="cmdY">Команда Y (выход)</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_GetCmdPosXY(int ipIndex, ref short cmdX, ref short cmdY);

        /// <summary>
        /// Получить статус моторов XY
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="xStatus">Статус мотора X</param>
        /// <param name="yStatus">Статус мотора Y</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_GetXYGalvoStatus(int ipIndex, ref short xStatus, ref short yStatus);

        /// <summary>
        /// Получить статус мотора Z
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="zStatus">Статус мотора Z</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_GetZGalvoStatus(int ipIndex, ref short zStatus);

        /// <summary>
        /// Сбросить ошибку замкнутого контура
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <returns>0 - успешно</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_ClearCloseLoopAlarm(int ipIndex);

        /// <summary>
        /// Получить информацию о статусе гальванометра
        /// </summary>
        /// <param name="ipIndex">Индекс карты</param>
        /// <param name="galvoType">Тип гальванометра (0=X, 1=Y, 2=Z)</param>
        /// <returns>Код статуса</returns>
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int HM_GetGalvoStatusInfo(int ipIndex, int galvoType);
    }

    #endregion

    #region Примеры использования HM_HashuScan

    /// <summary>
    /// Класс с примерами использования библиотеки HM_HashuScan
    /// </summary>
    public class HM_HashuScanExamples
    {
        /// <summary>
        /// Пример 1: Инициализация и подключение к сканеру
        /// </summary>
        public static void Example1_InitializeAndConnect()
        {
            Console.WriteLine("=== Пример 1: Инициализация и подключение ===\n");

            // Шаг 1: Инициализация платы сканера
            // hWnd - дескриптор окна для получения сообщений (можно передать IntPtr.Zero)
            Console.WriteLine("Инициализация платы сканера...");
            int result = HM_HashuScanDLL.HM_InitBoard(IntPtr.Zero);
            if (result == 0)
            {
                Console.WriteLine("✓ Плата успешно инициализирована");
            }
            else
            {
                Console.WriteLine($"✗ Ошибка инициализации: код {result}");
                return;
            }

            // Шаг 2: Подключение к карте по IP адресу
            string scannerIP = "172.18.34.227"; // IP первой карты
            Console.WriteLine($"\nПодключение к сканеру {scannerIP}...");
            result = HM_HashuScanDLL.HM_ConnectByIpStr(scannerIP);
            if (result == 0)
            {
                Console.WriteLine("✓ Подключение установлено");
            }
            else
            {
                Console.WriteLine($"✗ Ошибка подключения: код {result}");
                return;
            }

            // Шаг 3: Получение индекса карты по IP
            int cardIndex = HM_HashuScanDLL.HM_GetIndexByIpAddr(scannerIP);
            Console.WriteLine($"Индекс карты: {cardIndex}");

            // Шаг 4: Проверка статуса подключения
            int status = HM_HashuScanDLL.HM_GetConnectStatus(cardIndex);
            Console.WriteLine($"Статус подключения: {(enumConnectStatus)status}");

            // Шаг 5: Проверка рабочего статуса
            int workStatus = HM_HashuScanDLL.HM_GetWorkStatus(cardIndex);
            Console.WriteLine($"Рабочий статус: {(enumWorkStatus)workStatus}");

            Console.WriteLine("\n--- Подключение завершено ---");
        }

        /// <summary>
        /// Пример 2: Загрузка и выполнение файла маркировки
        /// </summary>
        public static void Example2_LoadAndExecuteMarkFile(int cardIndex, string markFilePath)
        {
            Console.WriteLine("\n=== Пример 2: Загрузка и выполнение файла маркировки ===\n");

            // Шаг 1: Загрузка файла маркировки в DDR память карты
            Console.WriteLine($"Загрузка файла: {markFilePath}");
            int result = HM_HashuScanDLL.HM_DownloadMarkFile(cardIndex, markFilePath, IntPtr.Zero);

            if (result != 0)
            {
                Console.WriteLine($"✗ Ошибка загрузки файла: код {result}");
                return;
            }
            Console.WriteLine("✓ Файл успешно загружен");

            // Шаг 2: Проверка готовности к работе
            int workStatus = HM_HashuScanDLL.HM_GetWorkStatus(cardIndex);
            if (workStatus != (int)enumWorkStatus.Ready)
            {
                Console.WriteLine($"⚠ Сканер не готов к работе. Статус: {workStatus}");
                return;
            }

            // Шаг 3: Запуск маркировки
            Console.WriteLine("\nЗапуск маркировки...");
            result = HM_HashuScanDLL.HM_StartMark(cardIndex);
            if (result != 0)
            {
                Console.WriteLine($"✗ Ошибка запуска: код {result}");
                return;
            }
            Console.WriteLine("✓ Маркировка запущена");

            // Шаг 4: Мониторинг прогресса выполнения
            int progress = 0;
            while (progress < 100)
            {
                progress = HM_HashuScanDLL.HM_ExecuteProgress(cardIndex);
                Console.Write($"\rПрогресс: {progress}%");
                Thread.Sleep(100); // Проверка каждые 100 мс

                // Проверка статуса
                workStatus = HM_HashuScanDLL.HM_GetWorkStatus(cardIndex);
                if (workStatus == (int)enumWorkStatus.Alarm)
                {
                    Console.WriteLine("\n✗ Авария во время выполнения!");
                    HM_HashuScanDLL.HM_StopMark(cardIndex);
                    return;
                }
                else if (workStatus == (int)enumWorkStatus.Ready)
                {
                    // Маркировка завершена
                    break;
                }
            }

            Console.WriteLine("\n✓ Маркировка завершена успешно");
        }

        /// <summary>
        /// Пример 3: Установка смещений и поворота поля сканирования
        /// </summary>
        public static void Example3_SetOffsetAndRotation(int cardIndex)
        {
            Console.WriteLine("\n=== Пример 3: Смещения и поворот ===\n");

            // Пример: Сместить поле на 10 мм вправо, 20 мм вверх, поднять фокус на 0.5 мм
            float offsetX = 10.0f;   // мм
            float offsetY = 20.0f;   // мм
            float offsetZ = 0.5f;    // мм (положительное = фокус выше)

            Console.WriteLine($"Установка смещения: X={offsetX}, Y={offsetY}, Z={offsetZ}");
            int result = HM_HashuScanDLL.HM_SetOffset(cardIndex, offsetX, offsetY, offsetZ);

            if (result == 0)
            {
                Console.WriteLine("✓ Смещение установлено");
            }
            else
            {
                Console.WriteLine($"✗ Ошибка установки смещения: код {result}");
            }

            // Пример: Повернуть поле на 15 градусов вокруг центра (0, 0)
            float rotationAngle = 15.0f;  // градусы
            float centerX = 0.0f;         // центр поворота по X
            float centerY = 0.0f;         // центр поворота по Y

            Console.WriteLine($"\nУстановка поворота: угол={rotationAngle}°, центр=({centerX}, {centerY})");
            result = HM_HashuScanDLL.HM_SetRotates(cardIndex, rotationAngle, centerX, centerY);

            if (result == 0)
            {
                Console.WriteLine("✓ Поворот установлен");
            }
            else
            {
                Console.WriteLine($"✗ Ошибка установки поворота: код {result}");
            }

            // Пример использования: это полезно для:
            // - Компенсации механического смещения сканера
            // - Выравнивания нескольких лазеров
            // - Корректировки положения детали
        }

        /// <summary>
        /// Пример 4: Управление направляющим лазером (красный указатель)
        /// </summary>
        public static void Example4_GuideLaser(int cardIndex)
        {
            Console.WriteLine("\n=== Пример 4: Направляющий лазер ===\n");

            // Включить красный лазер для визуального позиционирования
            Console.WriteLine("Включение направляющего лазера...");
            int result = HM_HashuScanDLL.HM_SetGuidLaser(cardIndex, true);

            if (result == 0)
            {
                Console.WriteLine("✓ Красный лазер включен");
            }

            // Переместить луч в разные точки для проверки поля
            float[,] testPoints = new float[,]
            {
                { 0, 0, 0 },        // Центр
                { 100, 100, 0 },    // Правый верхний угол
                { -100, 100, 0 },   // Левый верхний угол
                { -100, -100, 0 },  // Левый нижний угол
                { 100, -100, 0 }    // Правый нижний угол
            };

            Console.WriteLine("\nПеремещение луча по контрольным точкам:");
            for (int i = 0; i < testPoints.GetLength(0); i++)
            {
                float x = testPoints[i, 0];
                float y = testPoints[i, 1];
                float z = testPoints[i, 2];

                Console.WriteLine($"  Точка {i + 1}: ({x:F1}, {y:F1}, {z:F1}) мм");
                result = HM_HashuScanDLL.HM_ScannerJump(cardIndex, x, y, z);

                if (result == 0)
                {
                    Thread.Sleep(500); // Пауза для визуальной проверки
                }
                else
                {
                    Console.WriteLine($"  ✗ Ошибка перемещения: код {result}");
                }
            }

            // Вернуться в центр
            HM_HashuScanDLL.HM_ScannerJump(cardIndex, 0, 0, 0);

            // Выключить красный лазер
            Console.WriteLine("\nВыключение направляющего лазера...");
            HM_HashuScanDLL.HM_SetGuidLaser(cardIndex, false);
            Console.WriteLine("✓ Красный лазер выключен");
        }

        /// <summary>
        /// Пример 5: Работа с калибровочными таблицами (коррекция)
        /// </summary>
        public static void Example5_CorrectionManagement(int cardIndex, string correctionFilePath)
        {
            Console.WriteLine("\n=== Пример 5: Калибровочные таблицы ===\n");

            // Шаг 1: Загрузка таблицы коррекции в DDR (временно)
            Console.WriteLine($"Загрузка таблицы коррекции: {correctionFilePath}");
            int result = HM_HashuScanDLL.HM_DownloadCorrection(cardIndex, correctionFilePath, IntPtr.Zero);

            if (result == 0)
            {
                Console.WriteLine("✓ Таблица коррекции загружена в DDR");
            }
            else
            {
                Console.WriteLine($"✗ Ошибка загрузки: код {result}");
                return;
            }

            // Шаг 2: Запись таблицы во Flash (постоянное хранение)
            // ВНИМАНИЕ: Эта операция записывает данные во Flash память!
            Console.WriteLine("\nЗапись таблицы коррекции во Flash...");
            Console.WriteLine("⚠ Это необратимая операция! В реальном коде добавьте подтверждение.");

            // Раскомментируйте для реальной записи:
            // result = HM_HashuScanDLL.HM_BurnCorrection(cardIndex, correctionFilePath, IntPtr.Zero);
            // if (result == 0)
            // {
            //     Console.WriteLine("✓ Таблица записана во Flash");
            // }

            // Шаг 3: Переключение между таблицами коррекции
            // (для систем с несколькими сканирующими головками)
            Console.WriteLine("\nВыбор таблицы коррекции #0 (основная головка)...");
            result = HM_HashuScanDLL.HM_SelectCorrection(cardIndex, 0);
            if (result == 0)
            {
                Console.WriteLine("✓ Таблица #0 активна");
            }

            // Если есть вторая головка:
            // Console.WriteLine("Выбор таблицы коррекции #1 (вторая головка)...");
            // HM_HashuScanDLL.HM_SelectCorrection(cardIndex, 1);
        }

        /// <summary>
        /// Пример 6: Работа с цифровыми входами/выходами
        /// </summary>
        public static void Example6_DigitalIO(int cardIndex)
        {
            Console.WriteLine("\n=== Пример 6: Цифровые входы/выходы ===\n");

            // Получение состояния входов
            int inputs = HM_HashuScanDLL.HM_GetInput_GMC2(cardIndex);
            Console.WriteLine($"Состояние входов (битовая маска): 0b{Convert.ToString(inputs, 2).PadLeft(8, '0')}");

            // Расшифровка каждого входа
            for (int i = 0; i < 8; i++)
            {
                bool isHigh = (inputs & (1 << i)) != 0;
                Console.WriteLine($"  IN{i}: {(isHigh ? "HIGH (1)" : "LOW (0)")}");
            }

            // Получение состояния аварийных сигналов от лазера
            int laserAlarms = HM_HashuScanDLL.HM_GetLaserInput(cardIndex);
            Console.WriteLine($"\nСостояние входов лазера: 0b{Convert.ToString(laserAlarms, 2).PadLeft(4, '0')}");

            string[] alarmNames = { "Alarm1", "Alarm2", "Alarm3", "Alarm4" };
            for (int i = 0; i < 4; i++)
            {
                bool isActive = (laserAlarms & (1 << i)) != 0;
                Console.WriteLine($"  {alarmNames[i]}: {(isActive ? "АКТИВНА" : "норма")}");
            }

            // Управление выходами - пример мигания
            Console.WriteLine("\nУправление выходом OUT0 (мигание 3 раза):");
            for (int i = 0; i < 3; i++)
            {
                // Включить
                HM_HashuScanDLL.HM_SetOutputOn_GMC2(cardIndex, 0);
                Console.WriteLine("  OUT0: ON");
                Thread.Sleep(500);

                // Выключить
                HM_HashuScanDLL.HM_SetOutputOff_GMC2(cardIndex, 0);
                Console.WriteLine("  OUT0: OFF");
                Thread.Sleep(500);
            }

            // Примеры использования выходов:
            // - Управление затвором лазера
            // - Управление пневматикой
            // - Сигнализация (лампа, зуммер)
            // - Блокировка безопасности
        }

        /// <summary>
        /// Пример 7: Работа с аналоговыми выходами
        /// </summary>
        public static void Example7_AnalogOutput(int cardIndex)
        {
            Console.WriteLine("\n=== Пример 7: Аналоговые выходы ===\n");

            // Аналоговые выходы используются для:
            // - Управления мощностью лазера
            // - Управления скоростью вентилятора
            // - Управления пневматическим давлением

            Console.WriteLine("Настройка аналоговых выходов:");
            Console.WriteLine("  VOUTA и VOUTB: 0.0 = 0V, 0.5 = 5V, 1.0 = 10V\n");

            // Пример 1: Установить VOUTA на 5V (50%), VOUTB на 7.5V (75%)
            float voutA = 0.5f;  // 5V
            float voutB = 0.75f; // 7.5V

            Console.WriteLine($"Установка: VOUTA = {voutA * 10}V, VOUTB = {voutB * 10}V");
            int result = HM_HashuScanDLL.HM_SetAnalog(cardIndex, voutA, voutB);

            if (result == 0)
            {
                Console.WriteLine("✓ Аналоговые выходы установлены");
            }

            // Пример 2: Плавное изменение напряжения (0V -> 10V -> 0V)
            Console.WriteLine("\nПлавное изменение VOUTA от 0V до 10V:");
            for (float v = 0.0f; v <= 1.0f; v += 0.1f)
            {
                HM_HashuScanDLL.HM_SetAnalog(cardIndex, v, voutB);
                Console.WriteLine($"  VOUTA: {v * 10:F1}V");
                Thread.Sleep(200);
            }

            Console.WriteLine("\nПлавное уменьшение VOUTA от 10V до 0V:");
            for (float v = 1.0f; v >= 0.0f; v -= 0.1f)
            {
                HM_HashuScanDLL.HM_SetAnalog(cardIndex, v, voutB);
                Console.WriteLine($"  VOUTA: {v * 10:F1}V");
                Thread.Sleep(200);
            }

            // Сброс выходов в 0V
            HM_HashuScanDLL.HM_SetAnalog(cardIndex, 0.0f, 0.0f);
            Console.WriteLine("\n✓ Выходы сброшены в 0V");
        }

        /// <summary>
        /// Пример 8: Мониторинг позиции и статуса гальванометров
        /// </summary>
        public static void Example8_GalvoMonitoring(int cardIndex)
        {
            Console.WriteLine("\n=== Пример 8: Мониторинг гальванометров ===\n");

            // Переменные для хранения позиций
            short fbX = 0, fbY = 0;      // Фактическая позиция
            short cmdX = 0, cmdY = 0;    // Командная позиция
            short xStatus = 0, yStatus = 0, zStatus = 0;  // Статусы

            // Получение фактической позиции зеркал
            int result = HM_HashuScanDLL.HM_GetFeedbackPosXY(cardIndex, ref fbX, ref fbY);
            if (result == 0)
            {
                Console.WriteLine($"Фактическая позиция: X={fbX}, Y={fbY}");
            }

            // Получение командной позиции
            result = HM_HashuScanDLL.HM_GetCmdPosXY(cardIndex, ref cmdX, ref cmdY);
            if (result == 0)
            {
                Console.WriteLine($"Командная позиция:   X={cmdX}, Y={cmdY}");
            }

            // Расчет ошибки позиционирования
            int errorX = Math.Abs(fbX - cmdX);
            int errorY = Math.Abs(fbY - cmdY);
            Console.WriteLine($"Ошибка позиции:      X={errorX}, Y={errorY}");

            if (errorX > 100 || errorY > 100)
            {
                Console.WriteLine("⚠ ВНИМАНИЕ: Большая ошибка позиционирования!");
            }

            // Получение статуса моторов
            result = HM_HashuScanDLL.HM_GetXYGalvoStatus(cardIndex, ref xStatus, ref yStatus);
            if (result == 0)
            {
                Console.WriteLine($"\nСтатус мотора X: {xStatus} ({InterpretGalvoStatus(xStatus)})");
                Console.WriteLine($"Статус мотора Y: {yStatus} ({InterpretGalvoStatus(yStatus)})");
            }

            result = HM_HashuScanDLL.HM_GetZGalvoStatus(cardIndex, ref zStatus);
            if (result == 0)
            {
                Console.WriteLine($"Статус мотора Z: {zStatus} ({InterpretGalvoStatus(zStatus)})");
            }

            // Проверка на ошибки замкнутого контура
            if (xStatus != 0 || yStatus != 0 || zStatus != 0)
            {
                Console.WriteLine("\n⚠ Обнаружена ошибка в системе управления!");
                Console.WriteLine("Попытка сброса ошибки...");

                result = HM_HashuScanDLL.HM_ClearCloseLoopAlarm(cardIndex);
                if (result == 0)
                {
                    Console.WriteLine("✓ Ошибка сброшена");
                }
            }
            else
            {
                Console.WriteLine("\n✓ Все моторы в нормальном состоянии");
            }
        }

        /// <summary>
        /// Вспомогательная функция для интерпретации статуса гальванометра
        /// </summary>
        private static string InterpretGalvoStatus(short status)
        {
            if (status == 0) return "OK - Норма";
            if ((status & 0x01) != 0) return "Ошибка позиции";
            if ((status & 0x02) != 0) return "Ошибка скорости";
            if ((status & 0x04) != 0) return "Перегрузка по току";
            if ((status & 0x08) != 0) return "Ошибка датчика";
            return $"Неизвестная ошибка: {status}";
        }

        /// <summary>
        /// Пример 9: Пауза и продолжение маркировки
        /// </summary>
        public static void Example9_PauseResume(int cardIndex)
        {
            Console.WriteLine("\n=== Пример 9: Пауза и продолжение маркировки ===\n");

            // Эта функция полезна для:
            // - Инспекции во время процесса
            // - Корректировки параметров
            // - Аварийной остановки без потери позиции

            Console.WriteLine("Запуск маркировки...");
            int result = HM_HashuScanDLL.HM_StartMark(cardIndex);
            if (result != 0)
            {
                Console.WriteLine($"✗ Ошибка запуска: {result}");
                return;
            }

            // Имитация работы
            Thread.Sleep(2000);
            int progress = HM_HashuScanDLL.HM_ExecuteProgress(cardIndex);
            Console.WriteLine($"Прогресс: {progress}%");

            // Пауза
            Console.WriteLine("\nПриостановка маркировки...");
            result = HM_HashuScanDLL.HM_PauseMark(cardIndex);
            if (result == 0)
            {
                Console.WriteLine("✓ Маркировка на паузе");
                Console.WriteLine("Можно выполнить проверку, корректировки и т.д.");
            }

            // Пауза для демонстрации
            Thread.Sleep(3000);

            // Продолжение
            Console.WriteLine("\nПродолжение маркировки...");
            result = HM_HashuScanDLL.HM_ContinueMark(cardIndex);
            if (result == 0)
            {
                Console.WriteLine("✓ Маркировка продолжена");
            }

            // Дожидаемся завершения
            while (true)
            {
                progress = HM_HashuScanDLL.HM_ExecuteProgress(cardIndex);
                Console.Write($"\rПрогресс: {progress}%");

                int workStatus = HM_HashuScanDLL.HM_GetWorkStatus(cardIndex);
                if (workStatus == (int)enumWorkStatus.Ready)
                {
                    break;
                }
                Thread.Sleep(100);
            }

            Console.WriteLine("\n✓ Маркировка завершена");
        }

        /// <summary>
        /// Пример 10: Полный цикл работы с несколькими картами
        /// </summary>
        public static void Example10_MultiCardOperation()
        {
            Console.WriteLine("\n=== Пример 10: Работа с несколькими картами ===\n");

            // IP адреса двух карт из конфигурации
            string[] scannerIPs = { "172.18.34.227", "172.18.34.228" };
            int[] cardIndices = new int[2];

            // Инициализация системы
            Console.WriteLine("Инициализация системы...");
            HM_HashuScanDLL.HM_InitBoard(IntPtr.Zero);

            // Подключение к обеим картам
            for (int i = 0; i < scannerIPs.Length; i++)
            {
                Console.WriteLine($"\nПодключение к карте {i}: {scannerIPs[i]}");
                int result = HM_HashuScanDLL.HM_ConnectByIpStr(scannerIPs[i]);

                if (result == 0)
                {
                    cardIndices[i] = HM_HashuScanDLL.HM_GetIndexByIpAddr(scannerIPs[i]);
                    Console.WriteLine($"  ✓ Карта {i} подключена, индекс: {cardIndices[i]}");

                    // Применение смещений из конфигурации
                    if (i == 0)
                    {
                        // Карта 0: offsetY = 105.03 мм
                        HM_HashuScanDLL.HM_SetOffset(cardIndices[i], 0.0f, 105.03f, -0.001f);
                        Console.WriteLine($"  Смещение установлено: Y=+105.03 мм");
                    }
                    else
                    {
                        // Карта 1: offsetY = -105.03 мм, offsetX = -2.636 мм
                        HM_HashuScanDLL.HM_SetOffset(cardIndices[i], -2.636f, -105.03f, 0.102f);
                        Console.WriteLine($"  Смещение установлено: X=-2.636 мм, Y=-105.03 мм");
                    }
                }
                else
                {
                    Console.WriteLine($"  ✗ Ошибка подключения: {result}");
                }
            }

            // Синхронная работа двух лазеров
            Console.WriteLine("\n--- Обе карты готовы к работе ---");
            Console.WriteLine("Лазеры расположены на расстоянии 210 мм друг от друга по оси Y");
            Console.WriteLine("Это позволяет обрабатывать большую область одновременно");

            // Пример: Одновременный запуск маркировки на обеих картах
            // (в реальном коде здесь должна быть загрузка файлов маркировки)

            Console.WriteLine("\nОтключение от карт...");
            for (int i = 0; i < cardIndices.Length; i++)
            {
                HM_HashuScanDLL.HM_DisconnectTo(cardIndices[i]);
                Console.WriteLine($"  ✓ Карта {i} отключена");
            }
        }
    }

    #endregion

    #region Главная программа

    class ProgramHansExamples
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║    Примеры работы с библиотекой HM_HashuScan.dll        ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

            try
            {
                // ВАЖНО: Для работы примеров необходимо:
                // 1. Наличие HM_HashuScan.dll и зависимых библиотек в папке приложения
                // 2. Реальное подключение к сканерам Hans по сети
                // 3. Правильные IP адреса сканеров

                Console.WriteLine("⚠ ВНИМАНИЕ: Эти примеры требуют подключения к реальному оборудованию!");
                Console.WriteLine("Для демонстрации без оборудования закомментируйте вызовы функций DLL.\n");

                // Раскомментируйте примеры для выполнения:

                // Пример 1: Базовая инициализация
                // HM_HashuScanExamples.Example1_InitializeAndConnect();

                // Пример 2: Загрузка и выполнение файла маркировки
                // int cardIndex = 0;
                // string markFile = @"C:\MarkFiles\test.bin";
                // HM_HashuScanExamples.Example2_LoadAndExecuteMarkFile(cardIndex, markFile);

                // Пример 3: Смещения и поворот
                // HM_HashuScanExamples.Example3_SetOffsetAndRotation(cardIndex);

                // Пример 4: Направляющий лазер
                // HM_HashuScanExamples.Example4_GuideLaser(cardIndex);

                // Пример 5: Калибровочные таблицы
                // string correctionFile = @"C:\Corrections\scanner_cal.crt";
                // HM_HashuScanExamples.Example5_CorrectionManagement(cardIndex, correctionFile);

                // Пример 6: Цифровые входы/выходы
                // HM_HashuScanExamples.Example6_DigitalIO(cardIndex);

                // Пример 7: Аналоговые выходы
                // HM_HashuScanExamples.Example7_AnalogOutput(cardIndex);

                // Пример 8: Мониторинг гальванометров
                // HM_HashuScanExamples.Example8_GalvoMonitoring(cardIndex);

                // Пример 9: Пауза и продолжение
                // HM_HashuScanExamples.Example9_PauseResume(cardIndex);

                // Пример 10: Работа с несколькими картами
                // HM_HashuScanExamples.Example10_MultiCardOperation();

                Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║             Все примеры описаны в коде                   ║");
                Console.WriteLine("║   Раскомментируйте нужные примеры для выполнения         ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

            }
            catch (DllNotFoundException ex)
            {
                Console.WriteLine($"\n✗ ОШИБКА: Не найдена библиотека HM_HashuScan.dll");
                Console.WriteLine($"  Убедитесь, что DLL находится в папке приложения:");
                Console.WriteLine($"  - HM_HashuScan.dll");
                Console.WriteLine($"  - HM_Comm.dll");
                Console.WriteLine($"  - Hashu4Java_64.dll");
                Console.WriteLine($"  - Udm4Java_64.dll");
                Console.WriteLine($"  - libwinpthread-1.dll");
                Console.WriteLine($"\n  Детали: {ex.Message}");
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
