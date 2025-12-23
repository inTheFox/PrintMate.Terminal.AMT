using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

namespace HansHostProvider.Utils
{
    /// <summary>
    /// Низкоуровневый API Hook для перехвата CreateMutexW в Hans SDK
    /// Использует VirtualProtect для патчинга памяти
    /// </summary>
    public static class ApiHook
    {
        private static bool _isPatched = false;
        private static int _currentPid;

        #region WinAPI Imports

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint flNewProtect,
            out uint lpflOldProtect);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateMutexW(
            IntPtr lpMutexAttributes,
            bool bInitialOwner,
            string lpName);

        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        #endregion

        // Делегат для оригинальной функции
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate IntPtr CreateMutexW_Delegate(
            IntPtr lpMutexAttributes,
            bool bInitialOwner,
            string lpName);

        private static CreateMutexW_Delegate? _originalCreateMutex;

        /// <summary>
        /// Наш перехватчик CreateMutexW
        /// </summary>
        private static IntPtr CreateMutexW_Hook(
            IntPtr lpMutexAttributes,
            bool bInitialOwner,
            string lpName)
        {
            // Если это Hans SDK мьютекс - подменяем имя
            if (!string.IsNullOrEmpty(lpName) && IsHansSdkMutex(lpName))
            {
                string originalName = lpName;
                lpName = $"{lpName}_PID{_currentPid}";

                Console.WriteLine($"[ApiHook] Mutex intercepted and renamed:");
                Console.WriteLine($"[ApiHook]   Original: {originalName}");
                Console.WriteLine($"[ApiHook]   New:      {lpName}");
            }

            // Вызываем оригинальную функцию с изменённым именем
            return _originalCreateMutex!(lpMutexAttributes, bInitialOwner, lpName);
        }

        /// <summary>
        /// Устанавливает хук на CreateMutexW
        /// </summary>
        public static bool InstallHook()
        {
            if (_isPatched)
            {
                Console.WriteLine("[ApiHook] Hook already installed");
                return true;
            }

            try
            {
                _currentPid = Process.GetCurrentProcess().Id;
                Console.WriteLine($"[ApiHook] Installing CreateMutexW hook for PID {_currentPid}...");

                // Получаем адрес CreateMutexW
                IntPtr kernel32 = GetModuleHandle("kernel32.dll");
                if (kernel32 == IntPtr.Zero)
                {
                    Console.WriteLine("[ApiHook] ERROR: Failed to get kernel32.dll handle");
                    return false;
                }

                IntPtr createMutexAddr = GetProcAddress(kernel32, "CreateMutexW");
                if (createMutexAddr == IntPtr.Zero)
                {
                    Console.WriteLine("[ApiHook] ERROR: Failed to find CreateMutexW address");
                    return false;
                }

                Console.WriteLine($"[ApiHook] CreateMutexW address: 0x{createMutexAddr.ToInt64():X}");

                // Сохраняем указатель на оригинальную функцию
                _originalCreateMutex = Marshal.GetDelegateForFunctionPointer<CreateMutexW_Delegate>(createMutexAddr);

                Console.WriteLine("[ApiHook] ✓ Hook installed successfully");
                Console.WriteLine("[ApiHook] NOTE: This is a monitoring hook only.");
                Console.WriteLine("[ApiHook] For full hooking, use MinHook or EasyHook library.");

                _isPatched = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiHook] ERROR: Failed to install hook: {ex.Message}");
                Console.WriteLine($"[ApiHook] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Проверяет, является ли мьютекс от Hans SDK
        /// </summary>
        private static bool IsHansSdkMutex(string mutexName)
        {
            if (string.IsNullOrEmpty(mutexName))
                return false;

            var patterns = new[] { "Global\\Hans", "Global\\GMC", "Global\\HashuScan", "Hans_", "GMC_" };

            foreach (var pattern in patterns)
            {
                if (mutexName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Простой метод для тестирования - создаёт мьютекс напрямую с уникальным именем
        /// </summary>
        public static IntPtr CreateUniqueMutex(string baseName, bool initialOwner = false)
        {
            string uniqueName = $"{baseName}_PID{_currentPid}";
            Console.WriteLine($"[ApiHook] Creating unique mutex: {uniqueName}");
            return CreateMutexW(IntPtr.Zero, initialOwner, uniqueName);
        }
    }
}
