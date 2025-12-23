using System;
using System.Windows.Forms;
using Hans.NET.libs;
using Hans.NET.Models;
using static Hans.NET.libs.HM_HashuScanDLL;

namespace HansScannerHost
{
    /// <summary>
    /// Скрытая Windows Forms форма для получения HWND и обработки сообщений от Hans SDK
    /// Обрабатывает callback-сообщения от Hans SDK и отправляет события через Named Pipe
    /// </summary>
    public class HiddenMessageForm : Form
    {
        private readonly string _ipAddress;
        private int _boardIndex = -1;
        private bool _isConnected = false;
        private bool _isMarking = false;

        public IntPtr WindowHandle => Handle;
        public int BoardIndex => _boardIndex;
        public bool IsConnected => _isConnected;
        public bool IsMarking => _isMarking;
        public bool IsMarkComplete = false;
        public int MarkProgress = 0;
        public MarkingState MarkingState = MarkingState.Stop;
        public bool IsDownloadMarkFileFinish = false;

        public HiddenMessageForm(string ipAddress)
        {
            _ipAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));

            // Настраиваем форму как невидимую
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Opacity = 0;
            Width = 0;
            Height = 0;

            // Создаём handle сразу
            CreateHandle();

            Console.WriteLine($"Initializing Hans SDK for IP: {_ipAddress}");

            // Инициализируем Hans SDK с HWND этой формы
            int result = HM_InitBoard(Handle);
            if (result == 0)
            {
                Console.WriteLine("Hans SDK initialized successfully");
            }
            else
            {
                Console.WriteLine($"Hans SDK initialization failed with code: {result}");
            }
        }

        /// <summary>
        /// Загрузить UDM файл в контроллер
        /// </summary>
        public bool DownloadMarkFile(string udmFilePath)
        {
            //if (!_isConnected)
            //{
            //    Console.WriteLine("Cannot download: not connected");
            //    return false;
            //}

            if (!System.IO.File.Exists(udmFilePath))
            {
                Console.WriteLine($"[{_ipAddress}] ERROR: File not found: {udmFilePath}");
                return false;
            }

            var fileInfo = new System.IO.FileInfo(udmFilePath);
            Console.WriteLine($"[{_ipAddress}] ╔════════════════════════════════════════");
            Console.WriteLine($"[{_ipAddress}] ║ Download Request");
            Console.WriteLine($"[{_ipAddress}] ║ Board: {_boardIndex}");
            Console.WriteLine($"[{_ipAddress}] ║ File: {System.IO.Path.GetFileName(udmFilePath)}");
            Console.WriteLine($"[{_ipAddress}] ║ Size: {fileInfo.Length:N0} bytes");
            Console.WriteLine($"[{_ipAddress}] ║ HWND: 0x{Handle.ToInt64():X}");
            Console.WriteLine($"[{_ipAddress}] ║ PID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
            Console.WriteLine($"[{_ipAddress}] ╚════════════════════════════════════════");

            IsDownloadMarkFileFinish = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int result = HM_DownloadMarkFile(_boardIndex, udmFilePath, Handle);
            HM_BurnMarkFile(_boardIndex, false);
            sw.Stop();

            if (result == 0)
            {
                Console.WriteLine($"[{_ipAddress}] ✓ Download initiated (call took {sw.ElapsedMilliseconds}ms)");
                return true;
            }
            else
            {
                Console.WriteLine($"[{_ipAddress}] ✗ Download FAILED");
                Console.WriteLine($"[{_ipAddress}]   Error code: {result}");
                Console.WriteLine($"[{_ipAddress}]   Call duration: {sw.ElapsedMilliseconds}ms");
                Console.WriteLine($"[{_ipAddress}]   IsConnected: {_isConnected}");
                Console.WriteLine($"[{_ipAddress}]   ConnectStatus: {GetConnectStatus()}");
                return false;
            }
        }


        public ConnectState GetConnectStatus()
        {
            return (ConnectState)HM_GetConnectStatus(_boardIndex);
        }

        /// <summary>
        /// Начать маркировку
        /// </summary>
        public bool StartMark()
        {
            if (!_isConnected)
            {
                Console.WriteLine("Cannot start mark: not connected");
                return false;
            }

            Console.WriteLine($"Starting mark on board {_boardIndex}");

            IsMarkComplete = false;
            int result = HM_StartMark(_boardIndex);
            MarkingState = MarkingState.Marking;

            if (result == 0)
            {
                Console.WriteLine("Mark started successfully");
                _isMarking = true;
                return true;
            }
            else
            {
                Console.WriteLine($"Start mark failed with code: {result}");
                return false;
            }
        }

        /// <summary>
        /// Остановить маркировку
        /// </summary>
        public bool StopMark()
        {
            if (!_isMarking)
            {
                Console.WriteLine("Cannot stop mark: not marking");
                return false;
            }

            Console.WriteLine($"Stopping mark on board {_boardIndex}");

            int result = HM_StopMark(_boardIndex);

            if (result == 0)
            {
                Console.WriteLine("Mark stopped successfully");
                _isMarking = false;
                return true;
            }
            else
            {
                Console.WriteLine($"Stop mark failed with code: {result}");
                return false;
            }
        }

        /// <summary>
        /// Получить статус работы контроллера (1=ready, 2=run, 3=alarm)
        /// </summary>
        public int GetWorkStatus()
        {
            if (!_isConnected) return 0;
            return HM_GetWorkStatus(_boardIndex);
        }

        public string GetWorkStatusStr()
        {
            if (!_isConnected) return "FAILED CONNECTION";
            int status = GetWorkStatus();

            if (status == 1) return "Ready";
            if (status == 2) return "Run";
            if (status == 3) return "Alarm";
            return "NotFoundStatus";
        }

        /// <summary>
        /// Преобразует числовой IP в строковый формат
        /// </summary>
        private DeviceInfo DeviceRefresh(int ipIndex, ulong uIp)
        {
            DeviceInfo device = new DeviceInfo
            {
                IPValue = uIp,
                Index = ipIndex,
                DeviceName = $"{(uIp >> 0) & 0xFF}.{(uIp >> 8) & 0xFF}.{(uIp >> 16) & 0xFF}.{(uIp >> 24) & 0xFF}"
            };
            return device;
        }

        /// <summary>
        /// Обработка сообщений от Hans SDK
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            Message msg = m;
            base.WndProc(ref m);

            if (_boardIndex != -1 && _boardIndex != msg.LParam) return;

            switch (m.Msg)
            {
                case MessageType.ConnectStateUpdate:
                    int currentIpIndex = (int)msg.LParam;
                    DeviceInfo deviceInfo = DeviceRefresh(currentIpIndex, (ulong)msg.WParam);

                    if (deviceInfo.DeviceName == _ipAddress)
                    {
                        _boardIndex = currentIpIndex;

                        var status = (ConnectState)HM_GetConnectStatus(currentIpIndex);
                        DeviceStatusUpdated(status);

                        switch (status)
                        {
                            case ConnectState.ReadyToConnect:
                                HM_ConnectTo(_boardIndex);
                                if ((ConnectState)HM_GetConnectStatus(currentIpIndex) == ConnectState.Connected)
                                {
                                    _isConnected = true;
                                    DeviceStatusUpdated(ConnectState.Connected);
                                    Console.WriteLine($"Device {_ipAddress} connected (index={currentIpIndex})");


                                    HM_StopMark(_boardIndex);
                                    //HM_SetMarkRegion(_boardIndex, 350);
                                    //HM_SetCoordinate(_boardIndex, 5);

                                    Task.Factory.StartNew(async () =>
                                    {
                                        while (true)
                                        {
                                            if ((ConnectState)HM_GetConnectStatus(currentIpIndex) ==
                                                ConnectState.Disconnected) break;
                                            HM_ExecuteProgress(_boardIndex);
                                            Console.WriteLine($"Current status: {GetWorkStatusStr()})");
                                            await Task.Delay(2000);
                                        }
                                    });
                                }
                                break;

                            case ConnectState.Connected:
                                Console.WriteLine($"Device {_ipAddress} connected (index={currentIpIndex})");
                                _isConnected = true;
                                break;

                            case ConnectState.Disconnected:
                                Console.WriteLine($"Device {_ipAddress} not available");
                                _isConnected = false;
                                _isMarking = false;
                                break;
                        }
                    }
                    break;

                case MessageType.StreamProgress:
                    int downloadProgress = (int)msg.WParam;
                    Console.WriteLine($"Download progress: {downloadProgress}%");
                    break;

                case MessageType.StreamEnd:
                    IsDownloadMarkFileFinish = true;
                    Program.EventsPipeServer?.SendEvent(HansEventType.DownloadCompleted);

                    Console.WriteLine("Download completed");
                    break;

                case MessageType.MarkingProgress:
                    int markProgress = (int)msg.WParam;
                    MarkProgress = markProgress;

                    Console.WriteLine($"Mark progress: {markProgress}%");
                    if (markProgress > 0)
                    {
                        Program.EventsPipeServer?.SendEvent(HansEventType.MarkingProgress, MarkProgress.ToString());
                    }
                    break;

                case MessageType.MarkingComplete:
                    _isMarking = false;
                    IsMarkComplete = true;
                    MarkingState = MarkingState.Finish;
                    HM_StopMark(_boardIndex);
                    Program.EventsPipeServer?.SendEvent(HansEventType.MarkingCompleted);

                    Console.WriteLine("Mark completed");
                    break;
            }
        }

        private void DeviceStatusUpdated(ConnectState state)
        {
            switch (state)
            {
                case ConnectState.Connected:

                    if (GetWorkStatus() == 2) StopMark();
                    Program.EventsPipeServer?.SendEvent(HansEventType.BoardConnected);
                    break;
                case ConnectState.ReadyToConnect:
                    Program.EventsPipeServer?.SendEvent(HansEventType.BoardReady);

                    break;
                case ConnectState.Disconnected:
                    Program.EventsPipeServer?.SendEvent(HansEventType.BoardDisconnected);
                    break;
            }
        }

        public int GetMarkProgress() => MarkProgress;
        public MarkingState GetMarkingState() => MarkingState;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Console.WriteLine("Disposing HiddenMessageForm...");

                // Останавливаем маркировку если активна
                if (_isMarking)
                {
                    StopMark();
                }

                // Отключаемся от контроллера
                if (_isConnected && _boardIndex >= 0)
                {
                    Console.WriteLine($"Disconnecting from board {_boardIndex}");
                    HM_DisconnectTo(_boardIndex);
                    _isConnected = false;
                }
            }

            base.Dispose(disposing);
        }
    }
}
