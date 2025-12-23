using System.Runtime.InteropServices;

namespace HansHostProvider.Services
{
    /// <summary>
    /// Message-only window без Windows Forms.
    /// Использует чистый Win32 API для получения сообщений от Hans SDK.
    /// </summary>
    public sealed class MessageOnlyWindow : IDisposable
    {
        private const int HWND_MESSAGE = -3;

        private IntPtr _hwnd;
        private readonly WndProcDelegate _wndProcDelegate;
        private bool _disposed;

        public IntPtr Handle => _hwnd;

        public delegate void MessageHandler(int msg, IntPtr wParam, IntPtr lParam);
        public event MessageHandler? MessageReceived;

        #region Win32 API

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int x, int y,
            int nWidth, int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string? lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        #endregion

        public MessageOnlyWindow()
        {
            // Сохраняем делегат, чтобы он не был собран GC
            _wndProcDelegate = WndProc;

            var className = $"HansHostProvider_MessageWindow_{Guid.NewGuid():N}";
            var hInstance = GetModuleHandle(null);

            var wndClass = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                hInstance = hInstance,
                lpszClassName = className
            };

            if (RegisterClassEx(ref wndClass) == 0)
                throw new InvalidOperationException($"Failed to register window class. Error: {Marshal.GetLastWin32Error()}");

            _hwnd = CreateWindowEx(
                0, className, "HansHostProvider Message Window", 0,
                0, 0, 0, 0,
                new IntPtr(HWND_MESSAGE),
                IntPtr.Zero, hInstance, IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to create window. Error: {Marshal.GetLastWin32Error()}");

            Console.WriteLine($"[MessageOnlyWindow] Created: 0x{_hwnd.ToInt64():X}");
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                MessageReceived?.Invoke((int)msg, wParam, lParam);
            }
            catch
            {
                // Игнорируем исключения в обработчике сообщений
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_hwnd != IntPtr.Zero)
            {
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
