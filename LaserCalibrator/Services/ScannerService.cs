using System;
using System.Threading;
using System.Windows.Forms;
using Hans.NET.libs;
using static Hans.NET.libs.HM_HashuScanDLL;

namespace LaserCalibrator.Services
{
    /// <summary>
    /// Сервис для работы с Hans сканатором.
    /// Использует Windows Forms для обработки callback-сообщений от Hans SDK.
    /// </summary>
    public class ScannerService : Form
    {
        private string _ipAddress = "";
        private int _boardIndex = -1;

        public bool IsConnected { get; private set; }
        public bool IsGuideLaserOn { get; private set; }
        public string IpAddress => _ipAddress;

        // Текущая позиция сканатора
        public float CurrentX { get; private set; }
        public float CurrentY { get; private set; }
        public float CurrentZ { get; private set; }

        // События
        public event Action<string>? OnStatusChanged;
        public event Action<float, float>? OnPositionChanged;

        private static bool _sdkInitialized = false;
        private static readonly object _initLock = new();
        private static ScannerService? _primaryInstance;

        public ScannerService()
        {
            // Настройка невидимой формы для Hans SDK callback
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Opacity = 0;
            Width = 0;
            Height = 0;

            try
            {
                CreateHandle();
                InitializeSdk();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScannerService] Error creating handle: {ex.Message}");
            }
        }

        private void InitializeSdk()
        {
            lock (_initLock)
            {
                if (!_sdkInitialized && _primaryInstance == null)
                {
                    _primaryInstance = this;
                    try
                    {
                        int result = HM_InitBoard(Handle);
                        if (result == 0)
                        {
                            Console.WriteLine("[ScannerService] Hans SDK initialized successfully");
                            _sdkInitialized = true;
                        }
                        else
                        {
                            Console.WriteLine($"[ScannerService] Hans SDK initialization failed: {result}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ScannerService] SDK init exception: {ex.Message}");
                    }
                }
            }
        }

        public bool Connect(string ipAddress)
        {
            try
            {
                _ipAddress = ipAddress;
                Console.WriteLine($"[ScannerService] Connecting to {ipAddress}...");

                // Отправляем запрос на подключение
                int connectResult = HM_ConnectByIpStr(ipAddress);
                Console.WriteLine($"[ScannerService] HM_ConnectByIpStr result: {connectResult}");

                // Ждём и пытаемся получить индекс несколько раз
                const int maxAttempts = 10;
                const int delayMs = 500;

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    _boardIndex = HM_GetIndexByIpAddr(ipAddress);
                    Console.WriteLine($"[ScannerService] Attempt {attempt}/{maxAttempts}: board index = {_boardIndex}");

                    if (_boardIndex >= 0)
                    {
                        var status = (ConnectState)HM_GetConnectStatus(_boardIndex);
                        Console.WriteLine($"[ScannerService] Connection status: {status}");

                        if (status == ConnectState.Connected)
                        {
                            IsConnected = true;

                            // Останавливаем маркировку если она идёт
                            try
                            {
                                if (HM_GetWorkStatus(_boardIndex) == 2)
                                {
                                    HM_StopMark(_boardIndex);
                                }

                                // Устанавливаем 3D координаты
                                //HM_SetMarkRegion(_boardIndex, 350);
                                //HM_SetCoordinate(_boardIndex, 5);

                                Console.WriteLine("REGION AND COORDINATES APPLIED");
                            }
                            catch { }

                            OnStatusChanged?.Invoke($"Подключен к {ipAddress}");
                            Console.WriteLine($"[ScannerService] Connected to {ipAddress}, boardIndex={_boardIndex}");
                            return true;
                        }
                    }

                    if (attempt < maxAttempts)
                    {
                        OnStatusChanged?.Invoke($"Попытка {attempt}/{maxAttempts}...");
                        Thread.Sleep(delayMs);
                    }
                }

                // Все попытки исчерпаны
                IsConnected = false;
                if (_boardIndex < 0)
                {
                    Console.WriteLine($"[ScannerService] Failed to get board index for {ipAddress} after {maxAttempts} attempts");
                    OnStatusChanged?.Invoke($"Ошибка: устройство не найдено");
                }
                else
                {
                    var finalStatus = (ConnectState)HM_GetConnectStatus(_boardIndex);
                    Console.WriteLine($"[ScannerService] Connection failed to {ipAddress}, final status={finalStatus}");
                    OnStatusChanged?.Invoke($"Ошибка подключения: {finalStatus}");
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScannerService] Connect exception: {ex.Message}");
                Console.WriteLine($"[ScannerService] StackTrace: {ex.StackTrace}");
                IsConnected = false;
                OnStatusChanged?.Invoke($"Исключение: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            try
            {
                SetGuideLaser(false);
                HM_DisconnectTo(_boardIndex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScannerService] Disconnect exception: {ex.Message}");
            }

            IsConnected = false;
            _boardIndex = -1;
            OnStatusChanged?.Invoke("Отключен");
            Console.WriteLine($"[ScannerService] Disconnected from {_ipAddress}");
        }

        /// <summary>
        /// Включить/выключить красный (пилотный) лазер
        /// </summary>
        public bool SetGuideLaser(bool enable)
        {
            if (!IsConnected || _boardIndex < 0) return false;

            try
            {
                int result = HM_SetGuidLaser(_boardIndex, enable);
                if (result == 0)
                {
                    IsGuideLaserOn = enable;
                    Console.WriteLine($"[{_ipAddress}] Guide laser: {(enable ? "ON" : "OFF")}");
                    return true;
                }

                Console.WriteLine($"[{_ipAddress}] Failed to set guide laser: {result}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_ipAddress}] SetGuideLaser exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Переместить сканатор в указанную позицию
        /// </summary>
        public bool JumpTo(float x, float y, float z = 0)
        {
            if (!IsConnected || _boardIndex < 0) return false;

            try
            {
                int result = HM_ScannerJump(_boardIndex, x, y, z);
                if (result == 0)
                {
                    CurrentX = x;
                    CurrentY = y;
                    CurrentZ = z;
                    OnPositionChanged?.Invoke(x, y);
                    Console.WriteLine($"[{_ipAddress}] Jump to X={x:F3}, Y={y:F3}, Z={z:F3}");
                    return true;
                }

                Console.WriteLine($"[{_ipAddress}] Jump failed: {result}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_ipAddress}] JumpTo exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Установить смещение (offset) сканатора
        /// </summary>
        public bool SetOffset(float offsetX, float offsetY, float offsetZ = 0)
        {
            if (!IsConnected || _boardIndex < 0) return false;

            try
            {
                int result = HM_SetOffset(_boardIndex, offsetX, offsetY, offsetZ);
                if (result == 0)
                {
                    Console.WriteLine($"[{_ipAddress}] Offset set: X={offsetX:F3}, Y={offsetY:F3}, Z={offsetZ:F3}");
                    return true;
                }

                Console.WriteLine($"[{_ipAddress}] Set offset failed: {result}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{_ipAddress}] SetOffset exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Получить текущую позицию (feedback) от сканатора
        /// </summary>
        public (short x, short y) GetFeedbackPosition()
        {
            if (!IsConnected || _boardIndex < 0) return (0, 0);

            try
            {
                short fbX = 0, fbY = 0;
                HM_GetFeedbackPosXY(_boardIndex, ref fbX, ref fbY);
                return (fbX, fbY);
            }
            catch
            {
                return (0, 0);
            }
        }

        /// <summary>
        /// Обработка Windows сообщений от Hans SDK
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            try
            {
                base.WndProc(ref m);

                if (_boardIndex < 0) return;

                switch (m.Msg)
                {
                    case MessageType.ConnectStateUpdate:
                        var status = (ConnectState)HM_GetConnectStatus(_boardIndex);
                        switch (status)
                        {
                            case ConnectState.Connected:
                                IsConnected = true;
                                OnStatusChanged?.Invoke($"Подключен к {_ipAddress}");
                                break;
                            case ConnectState.Disconnected:
                                IsConnected = false;
                                OnStatusChanged?.Invoke($"Отключен от {_ipAddress}");
                                break;
                        }
                        break;
                }
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    SetGuideLaser(false);
                    if (IsConnected && _boardIndex >= 0)
                    {
                        HM_DisconnectTo(_boardIndex);
                    }
                }
                catch { }
            }
            base.Dispose(disposing);
        }
    }
}
