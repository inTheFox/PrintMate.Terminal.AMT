using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace HansHostProvider.Services
{
    /// <summary>
    /// Message pump для Hans SDK без зависимости от Windows Forms.
    /// Создаёт STA-поток с message loop и позволяет выполнять действия в этом потоке.
    /// </summary>
    public sealed class SdkMessagePump : IDisposable
    {
        private const int WindowCreationTimeoutMs = 5000;
        private const uint WM_QUIT = 0x0012;
        private const uint PM_REMOVE = 0x0001;

        private Thread? _messageThread;
        private MessageOnlyWindow? _window;
        private readonly ManualResetEventSlim _windowCreated = new(false);
        private readonly CancellationTokenSource _cts = new();
        private readonly BlockingCollection<Action> _workQueue = new();
        private bool _disposed;

        public IntPtr WindowHandle { get; private set; }
        public int ThreadId { get; private set; }
        public bool IsInitialized => _windowCreated.IsSet;
        public bool InvokeRequired => Environment.CurrentManagedThreadId != ThreadId;

        public event Action<int, IntPtr, IntPtr>? MessageReceived;

        #region Win32 API

        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern bool PostThreadMessage(int idThread, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        #endregion

        public void Start()
        {
            if (_messageThread != null)
                throw new InvalidOperationException("Message pump already started");

            _messageThread = new Thread(MessageThreadProc)
            {
                Name = "HansSDK_MessagePump",
                IsBackground = false
            };
            _messageThread.SetApartmentState(ApartmentState.STA);
            _messageThread.Start();

            if (!_windowCreated.Wait(TimeSpan.FromMilliseconds(WindowCreationTimeoutMs)))
                throw new TimeoutException("Failed to create message window");

            Console.WriteLine($"[SdkMessagePump] Started on thread {ThreadId}");
        }

        private void MessageThreadProc()
        {
            try
            {
                ThreadId = Environment.CurrentManagedThreadId;

                _window = new MessageOnlyWindow();
                _window.MessageReceived += (msg, wParam, lParam) => MessageReceived?.Invoke(msg, wParam, lParam);
                WindowHandle = _window.Handle;

                Console.WriteLine($"[SdkMessagePump] Window created: 0x{WindowHandle.ToInt64():X}");
                _windowCreated.Set();

                RunMessageLoop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SdkMessagePump] Error in message thread: {ex}");
            }
            finally
            {
                _window?.Dispose();
                Console.WriteLine("[SdkMessagePump] Message thread exited");
            }
        }

        private void RunMessageLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                // Обрабатываем все ожидающие действия
                while (_workQueue.TryTake(out var action, 0))
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SdkMessagePump] Error executing action: {ex.Message}");
                    }
                }

                // Обрабатываем Windows-сообщения
                if (PeekMessage(out var msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                {
                    if (msg.message == WM_QUIT)
                        break;

                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        /// <summary>
        /// Выполняет действие в потоке message pump синхронно
        /// </summary>
        public void Invoke(Action action)
        {
            if (Environment.CurrentManagedThreadId == ThreadId)
            {
                action();
                return;
            }

            using var completionEvent = new ManualResetEventSlim(false);
            Exception? exception = null;

            _workQueue.Add(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    completionEvent.Set();
                }
            });

            completionEvent.Wait();

            if (exception != null)
                throw new AggregateException("Error in message pump thread", exception);
        }

        /// <summary>
        /// Выполняет функцию в потоке message pump синхронно с возвратом значения
        /// </summary>
        public T Invoke<T>(Func<T> func)
        {
            if (Environment.CurrentManagedThreadId == ThreadId)
                return func();

            using var completionEvent = new ManualResetEventSlim(false);
            T result = default!;
            Exception? exception = null;

            _workQueue.Add(() =>
            {
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    completionEvent.Set();
                }
            });

            completionEvent.Wait();

            if (exception != null)
                throw new AggregateException("Error in message pump thread", exception);

            return result;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _cts.Cancel();
            _workQueue.CompleteAdding();

            if (ThreadId != 0)
                PostThreadMessage(ThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);

            _messageThread?.Join(TimeSpan.FromSeconds(5));

            _cts.Dispose();
            _windowCreated.Dispose();
            _workQueue.Dispose();

            _disposed = true;
        }
    }
}
