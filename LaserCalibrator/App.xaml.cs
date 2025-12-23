using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using LaserCalibrator.Views;
using Prism.DryIoc;
using Prism.Ioc;

namespace LaserCalibrator
{
    public partial class App : PrismApplication
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_SHOW = 5;
        private static Mutex? _consoleMutex;
        private const string CONSOLE_MUTEX_NAME = "LaserCalibrator_ConsoleMutex";

        protected override void OnStartup(StartupEventArgs e)
        {
            CreateConsole();
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           LaserCalibrator - Калибровка лазеров             ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Запуск приложения...");

            // Подписка на необработанные исключения
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] ╔════════════════════════════════════════╗");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ║     UNHANDLED EXCEPTION (AppDomain)    ║");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ╚════════════════════════════════════════╝");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Type: {ex?.GetType().FullName}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Message: {ex?.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] StackTrace:\n{ex?.StackTrace}");
                if (ex?.InnerException != null)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] InnerException: {ex.InnerException.Message}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Inner StackTrace:\n{ex.InnerException.StackTrace}");
                }
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] IsTerminating: {args.IsTerminating}");
            };

            DispatcherUnhandledException += (s, args) =>
            {
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] ╔════════════════════════════════════════╗");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ║    UNHANDLED EXCEPTION (Dispatcher)    ║");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ╚════════════════════════════════════════╝");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Type: {args.Exception.GetType().FullName}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Message: {args.Exception.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] StackTrace:\n{args.Exception.StackTrace}");
                if (args.Exception.InnerException != null)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] InnerException: {args.Exception.InnerException.Message}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Inner StackTrace:\n{args.Exception.InnerException.StackTrace}");
                }
                args.Handled = true; // Предотвращаем закрытие приложения
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] ╔════════════════════════════════════════╗");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ║   UNOBSERVED TASK EXCEPTION            ║");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ╚════════════════════════════════════════╝");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Message: {args.Exception.Message}");
                args.SetObserved();
            };

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Global exception handlers registered.");

            base.OnStartup(e);
        }

        protected override Window CreateShell()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Создание главного окна...");
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Регистрация типов...");
        }

        private void CreateConsole()
        {
            try
            {
                _consoleMutex = new Mutex(true, CONSOLE_MUTEX_NAME, out bool createdNew);

                if (createdNew)
                {
                    AllocConsole();
                    Console.Title = "LaserCalibrator Console";
                }
                else
                {
                    _consoleMutex?.Close();
                    _consoleMutex = new Mutex(true, CONSOLE_MUTEX_NAME + "_2");
                    AllocConsole();
                    Console.Title = "LaserCalibrator Console (2)";
                }

                // Показываем консоль
                IntPtr consoleWindow = GetConsoleWindow();
                if (consoleWindow != IntPtr.Zero)
                {
                    ShowWindow(consoleWindow, SW_SHOW);
                }
            }
            catch (Exception ex)
            {
                AllocConsole();
                Console.WriteLine($"Ошибка создания консоли: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Завершение работы...");
            _consoleMutex?.Close();
            FreeConsole();
            base.OnExit(e);
        }
    }
}
