using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace HansScannerHost
{
    /// <summary>
    /// Перехватчик WinAPI вызовов для обхода глобальных мьютексов Hans SDK
    /// Подменяет имена мьютексов, добавляя PID процесса
    /// </summary>
    public static class MutexHook
    {
        private static int _currentPid;
        private static bool _isHooked = false;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateMutexW(
            IntPtr lpMutexAttributes,
            bool bInitialOwner,
            string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        /// <summary>
        /// Инициализирует хук для текущего процесса
        /// </summary>
        public static void Initialize()
        {
            if (_isHooked)
            {
                Console.WriteLine("[MutexHook] Already initialized");
                return;
            }

            _currentPid = Process.GetCurrentProcess().Id;
            _isHooked = true;

            Console.WriteLine($"[MutexHook] Initialized for PID {_currentPid}");
            Console.WriteLine("[MutexHook] All Hans SDK mutexes will be process-specific");
        }

        /// <summary>
        /// Создаёт мьютекс с уникальным именем для текущего процесса
        /// Вызывайте этот метод вместо прямого вызова CreateMutex в критичных местах
        /// </summary>
        public static IntPtr CreateProcessSpecificMutex(string mutexName, bool initialOwner = false)
        {
            if (!_isHooked)
            {
                Initialize();
            }

            // Подменяем имя мьютекса
            string uniqueName = $"{mutexName}_{_currentPid}";

            Console.WriteLine($"[MutexHook] Creating mutex: {mutexName} -> {uniqueName}");

            IntPtr handle = CreateMutexW(IntPtr.Zero, initialOwner, uniqueName);

            if (handle == IntPtr.Zero)
            {
                uint error = GetLastError();
                Console.WriteLine($"[MutexHook] Failed to create mutex {uniqueName}, error: {error}");
            }
            else
            {
                Console.WriteLine($"[MutexHook] Successfully created mutex {uniqueName}, handle: 0x{handle.ToInt64():X}");
            }

            return handle;
        }

        /// <summary>
        /// Проверяет, содержит ли имя мьютекса паттерны Hans SDK
        /// </summary>
        public static bool IsHansMutex(string mutexName)
        {
            if (string.IsNullOrEmpty(mutexName))
                return false;

            // Типичные паттерны Hans SDK
            var patterns = new[]
            {
                "Hans",
                "GMC",
                "HashuScan",
                "HM_",
                "Scanner",
                "Download",
                "Mark"
            };

            foreach (var pattern in patterns)
            {
                if (mutexName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[MutexHook] Detected Hans SDK mutex: {mutexName}");
                    return true;
                }
            }

            return false;
        }
    }
}
