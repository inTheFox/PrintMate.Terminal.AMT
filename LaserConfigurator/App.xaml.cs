using HandyControl.Themes;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace LaserConfigurator
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


        protected override void OnStartup(StartupEventArgs e)
        {
            CloseExistingConsole();
            CreateConsole();

            base.OnStartup(e);
            var boot = new Bootstrapper();
            boot.Run();
        }

        internal void UpdateTheme(ApplicationTheme theme)
        {
            if (ThemeManager.Current.ApplicationTheme != theme)
            {
                ThemeManager.Current.ApplicationTheme = theme;
            }
        }

        internal void UpdateAccent(Brush accent)
        {
            if (ThemeManager.Current.AccentColor != accent)
            {
                ThemeManager.Current.AccentColor = accent;
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
#if DEBUG
           // FreeConsole();
#endif
            base.OnExit(e);
        }
    }
}
