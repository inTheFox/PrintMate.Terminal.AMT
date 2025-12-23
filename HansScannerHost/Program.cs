using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace HansScannerHost
{
    /// <summary>
    /// HansScannerHost - отдельный процесс для управления Hans Scanner через SDK
    /// Использует Named Pipes для коммуникации с основным приложением
    /// </summary>
    class Program
    {
        private static HiddenMessageForm? _form;
        public static EventsPipeServer EventsPipeServer;
        public static InvokePipeServer InvokePipeServer;
        public static string IpAddress { get; private set; }

        [STAThread] // Необходим для Windows Forms (Hans SDK требует message loop)
        static int Main(string[] args)
        {
            // Обработка необработанных исключений
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine($"НЕОБРАБОТАННОЕ ИСКЛЮЧЕНИЕ: {e.ExceptionObject}");
                if (e.ExceptionObject is Exception ex)
                {
                    Console.WriteLine($"Сообщение: {ex.Message}");
                    Console.WriteLine($"Трассировка стека: {ex.StackTrace}");
                }
                Console.WriteLine("Нажмите Enter для выхода...");
                Console.ReadLine();
            };

            Application.ThreadException += (s, e) =>
            {
                Console.WriteLine($"ИСКЛЮЧЕНИЕ В ПОТОКЕ ПРИЛОЖЕНИЯ: {e.Exception.Message}");
                Console.WriteLine($"Трассировка стека: {e.Exception.StackTrace}");
                Console.WriteLine("Нажмите Enter для выхода...");
                Console.ReadLine();
            };

            // args = new string[] { "172.18.34.228", "scanner1" };


            // Парсим IP адрес из аргументов командной строки
            if (args.Length < 1)
            {
                Console.WriteLine("Использование: HansScannerHost.exe [ipAddress] [необязательно:pipeId]");
                Console.WriteLine("Пример: HansScannerHost.exe 172.18.34.227");
                Console.WriteLine("Пример: HansScannerHost.exe 172.18.34.227 scanner1");
                //return 1;
            }

            IpAddress = args[0];

            // Опциональный идентификатор для уникальности канала
            string pipeId = args.Length > 1 ? args[1] : IpAddress.Replace(".", "_");

            // Валидация IP адреса
            if (!System.Net.IPAddress.TryParse(IpAddress, out _))
            {
                Console.WriteLine($"ОШИБКА: Некорректный IP-адрес: {IpAddress}");
                Console.WriteLine("Убедитесь, что аргументы указаны в правильном порядке:");
                Console.WriteLine("  HansScannerHost.exe [ipAddress] [pipeId]");
                Console.WriteLine("  Пример: HansScannerHost.exe 172.18.34.227 scanner1");
                Console.ReadLine(); // Ждем, пока пользователь прочтет ошибку
                return 1;
            }

            Console.WriteLine($"===========================================");
            Console.WriteLine($"HansScannerHost запущен");
            Console.WriteLine($"  IP-адрес: {IpAddress}");
            Console.WriteLine($"  Идентификатор канала: {pipeId}");
            Console.WriteLine($"  PID:        {System.Diagnostics.Process.GetCurrentProcess().Id}");
            Console.WriteLine($"===========================================");

            try
            {
                // УСТАНОВКА API HOOK для обхода глобальных мьютексов Hans SDK
                Console.WriteLine("\n[ИНИЦИАЛИЗАЦИЯ] Установка API-хуков для обхода конфликтов мьютексов...");
                ApiHook.InstallHook();
                MutexHook.Initialize();
                Console.WriteLine("[ИНИЦИАЛИЗАЦИЯ] Хуки установлены — мьютексы SDK будут специфичны для процесса\n");

                // Инициализация Windows Forms
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Console.WriteLine("Создание скрытой формы HiddenMessageForm...");
                // Создаем скрытую форму для получения сообщений от Hans SDK
                _form = new HiddenMessageForm(IpAddress);
                Console.WriteLine("Скрытая форма HiddenMessageForm успешно создана");

                Console.WriteLine("Запуск серверов каналов...");
                // Запускаем серверы каналов с уникальными именами
                EventsPipeServer = new EventsPipeServer(pipeId);
                InvokePipeServer = new InvokePipeServer(_form, pipeId);
                Console.WriteLine("Серверы каналов успешно запущены");

                // Настраиваем обработку Ctrl+C для корректного завершения
                Console.CancelKeyPress += (s, e) =>
                {
                    Console.WriteLine("\nПолучен сигнал завершения (Ctrl+C)");
                    e.Cancel = true;
                    _form?.Dispose();
                    Application.Exit();
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ОШИБКА при инициализации: {ex.Message}");
                Console.WriteLine($"Трассировка стека: {ex.StackTrace}");
                Console.WriteLine("Нажмите Enter для выхода...");
                Console.ReadLine();
                return 1;
            }

            // Мониторим основной процесс PrintMate (опционально)
            // ОТКЛЮЧЕНО: может вызывать преждевременное закрытие

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var printMateProcess = Process.GetProcesses()
                        .FirstOrDefault(p => p.ProcessName.ToLower().Contains("printmate"));

                    if (printMateProcess == null)
                    {
                        Console.WriteLine("Процесс PrintMate не найден, завершение работы...");
                        Environment.Exit(0);
                    }

                    await Task.Delay(1000);
                }
            }, TaskCreationOptions.LongRunning);


            // Запускаем цикл обработки сообщений Windows Forms (необходим для Hans SDK)
            Application.Run(_form);
            return 0;
        }
    }
}