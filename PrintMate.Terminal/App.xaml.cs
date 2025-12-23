using HandyControl.Themes;
using Hans.NET.libs;
using HansScannerHost.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Opc2Lib;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.Database;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views;
using Prism.DryIoc;
using Prism.Ioc;
using ProjectParserTest.Parsers.CliParser;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PrintMate.Terminal
{
    public partial class App : Application
    {

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const uint WM_SETICON = 0x0080;
        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x0010;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        private static Mutex _consoleMutex;
        private const string CONSOLE_MUTEX_NAME = "MyWpfApp_ConsoleMutex";


        protected override async void OnStartup(StartupEventArgs e)
        {
            CloseExistingConsole();
            CreateConsole();

            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                          PrintPRO                          ║");
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

            // Запускаем Observer если еще не запущен
            //StartObserverIfNeeded();

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            var boot = new Bootstrapper();
            boot.Run();
            base.OnStartup(e);
        }


        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Логируем ошибку или показываем пользователю
            Console.WriteLine($"Произошла ошибка: {e.Exception.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

            // Предотвращаем закрытие приложения
            e.Handled = false;
        }

        private void StartObserverIfNeeded()
        {
            const string observerProcessName = "Observer";
            const string observerExeName = "Observer.exe";

            try
            {
                // Проверяем, запущен ли уже Observer
                var existingProcesses = Process.GetProcessesByName(observerProcessName);
                if (existingProcesses.Length > 0)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Observer уже запущен (PID: {existingProcesses[0].Id})");
                    return;
                }

                // Путь к Observer.exe рядом с основным приложением
                var observerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, observerExeName);

                if (!File.Exists(observerPath))
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Observer не найден: {observerPath}");
                    return;
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = observerPath,
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                    }
                };

                process.Start();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Observer запущен (PID: {process.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Ошибка запуска Observer: {ex.Message}");
            }
        }

        private void CreateConsole()
        {
            try
            {
                // Пытаемся завладеть мьютексом
                _consoleMutex = new Mutex(true, CONSOLE_MUTEX_NAME, out bool createdNew);

                if (createdNew)
                {
                    // Мы первые - создаем консоль
                    AllocConsole();
                    Console.Title = "PrintPro";
           
                }
                else
                {
                    // Кто-то уже имеет мьютекс - освобождаем
                    _consoleMutex?.Close();
                    _consoleMutex = null;

                    // Создаем новый мьютекс и консоль
                    _consoleMutex = new Mutex(true, CONSOLE_MUTEX_NAME);
                    AllocConsole();
                    Console.Title = "PrintPro Console";
                 
                }
            }
            catch (Exception ex)
            {
                // Если не удалось создать мьютекс, просто создаем консоль
                AllocConsole();
                Console.WriteLine($"Ошибка управления консолью: {ex.Message}");
            }
        }

        private void CloseExistingConsole()
        {
            try
            {
                // Пытаемся получить существующий мьютекс
                using (var mutex = Mutex.OpenExisting(CONSOLE_MUTEX_NAME))
                {
                    // Если получили - сигнализируем предыдущему процессу закрыть консоль
                    mutex?.Close();

                    // Даем время предыдущему процессу освободить консоль
                    Thread.Sleep(100);
                }
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // Мьютекс не существует - ничего не делаем
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при закрытии предыдущей консоли: {ex.Message}");
            }
        }




        protected override async void OnExit(ExitEventArgs e)
        {
            // Освобождаем консоль при выходе

            FreeConsole();

            base.OnExit(e);
        }
    }


}
