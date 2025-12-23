using Hans.NET.libs;
using Hans.NET.Models;
using HansDebuggerApp.Hans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;
using static Hans.NET.libs.HM_HashuScanDLL;

namespace HansDebuggerApp.Services
{
    /// <summary>
    /// Скрытая Windows Forms форма для получения HWND и обработки сообщений от Hans SDK
    /// Обрабатывает callback-сообщения от Hans SDK и отправляет события через Named Pipe
    /// </summary>
    public class ScannerService : Form
    {
        private List<ScanatorConfiguration> _scanatorConfigurations = null;
        private Dictionary<string, TestUdmBuilder> _udmBuilders = new();
        private string _address;

        public bool Connected = false;

        public ScannerService()
        {
            // Настраиваем форму как невидимую
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Opacity = 0;
            Width = 0;
            Height = 0;

            // Создаём handle сразу
            CreateHandle();


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

        public bool Connect(string address)
        {
            _address = address;
            HM_ConnectByIpStr(address);
            if ((ConnectState)HM_GetConnectStatus(GetBoardIndex()) == ConnectState.Connected)
            {
                Console.WriteLine($"CurStatus: {HM_GetWorkStatus(GetBoardIndex())}");
                if (HM_GetWorkStatus(GetBoardIndex()) == 2) HM_StopMark(GetBoardIndex());
                Connected = true;
                //HM_SetCoordinate(GetBoardIndex(), 5);
            }
            else
            {
                Connected = false;
            }
            return Connected;
        }

        public bool Disconnect()
        {
            if (!Connected) return false;
            if (HM_DisconnectTo(GetBoardIndex()) == 0) return true;
            return false;
        }

        public bool DownloadMarkFile(string udmFilePath)
        {
            if (!System.IO.File.Exists(udmFilePath))
            {
                Console.WriteLine($"[{_address}] ERROR: File not found: {udmFilePath}");
                return false;
            }

            var fileInfo = new System.IO.FileInfo(udmFilePath);
            Console.WriteLine($"[{_address}] ╔════════════════════════════════════════");
            Console.WriteLine($"[{_address}] ║ Download Request");
            Console.WriteLine($"[{_address}] ║ File: {System.IO.Path.GetFileName(udmFilePath)}");
            Console.WriteLine($"[{_address}] ║ Size: {fileInfo.Length:N0} bytes");
            Console.WriteLine($"[{_address}] ║ HWND: 0x{Handle.ToInt64():X}");
            Console.WriteLine($"[{_address}] ║ PID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
            Console.WriteLine($"[{_address}] ╚════════════════════════════════════════");

            int result = HM_DownloadMarkFile(GetBoardIndex(), udmFilePath, Handle);
            HM_BurnMarkFile(GetBoardIndex(), false);
            return true;
        }

        public ConnectState GetConnectStatus()
        {
            return (ConnectState)HM_GetConnectStatus(GetBoardIndex());
        }

        public bool StartMark()
        {
            int result = HM_StartMark(GetBoardIndex());
            if (result == 0) return true;
            return false;
        }

        public bool StopMark()
        {
            int result = HM_StopMark(GetBoardIndex());

            if (result == 0)
            {
                Console.WriteLine("Mark stopped successfully");
                return true;
            }
            else
            {
                Console.WriteLine($"Stop mark failed with code: {result}");
                return false;
            }
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


            switch (m.Msg)
            {
                case MessageType.ConnectStateUpdate:
                    int currentIpIndex = (int)msg.LParam;
                    DeviceInfo deviceInfo = DeviceRefresh(currentIpIndex, (ulong)msg.WParam);

                    var status = (ConnectState)HM_GetConnectStatus(currentIpIndex);

                    switch (status)
                    {
                        case ConnectState.ReadyToConnect:
                            Console.WriteLine($"[{deviceInfo.DeviceName}] has ready");
                            break;

                        case ConnectState.Connected:
                            Console.WriteLine($"[{deviceInfo.DeviceName}] has connected");
                            //HM_SetCoordinate(deviceInfo.Index, 5);
                            break;

                        case ConnectState.Disconnected:
                            Console.WriteLine($"[{deviceInfo.DeviceName}] has disconnected");
                            break;
                    }
                    
                    break;

                case MessageType.StreamProgress:
                    int downloadProgress = (int)msg.WParam;
                    Console.WriteLine($"[{msg.LParam}] Download progress: {downloadProgress}%");
                    break;

                case MessageType.StreamEnd:
                    Console.WriteLine($"[{msg.LParam}] Download completed");
                    break;

                case MessageType.MarkingProgress:
                    int markProgress = (int)msg.WParam;
                    Console.WriteLine($"[{msg.LParam}] Mark progress: {markProgress}%");
                    break;

                case MessageType.MarkingComplete:
                    HM_StopMark(GetBoardIndex());
                    Console.WriteLine($"[{msg.LParam}] Mark completed");
                    break;
            }
        }

        public int GetBoardIndex()
        {
            return HM_GetIndexByIpAddr(_address);
        }

        public void LoadConfiguration(List<ScanatorConfiguration> configList)
        {
            _scanatorConfigurations = configList;
            _udmBuilders.Clear();
            foreach (var config in _scanatorConfigurations)
            {
                _udmBuilders.Add(config.CardInfo.IpAddress, new TestUdmBuilder(config));
            }
        }

        public void GenerateUdmForAddress(string address, float x, float y, double beamDiameterMicron, float powerWatts, int delay)
        {
            if (!_udmBuilders.ContainsKey(address)) return;
            var builder = _udmBuilders[address];
            string path = builder.BuildSinglePoint(x, y, beamDiameterMicron, powerWatts, delay); 
            //DownloadMarkFile(path);
        }
        public void GenerateUdmForAddress(string address, float x, float y, float z, double beamDiameterMicron, float powerWatts, int delay)
        {
            if (!_udmBuilders.ContainsKey(address)) return;
            var builder = _udmBuilders[address];
            string path = builder.BuildSinglePoint(x, y, z, beamDiameterMicron, powerWatts, delay);
            DownloadMarkFile(path);
        }

        protected override void Dispose(bool disposing)
        {
            HM_DisconnectTo(GetBoardIndex());
            base.Dispose(disposing);
        }
    }
}
