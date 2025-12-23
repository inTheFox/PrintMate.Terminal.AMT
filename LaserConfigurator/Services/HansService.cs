using Hans.NET.libs;
using ImTools;
using LaserConfigurator.Events;
using LaserConfigurator.Models;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Hans.NET.libs.HM_HashuScanDLL;
using static System.Windows.Forms.AxHost;

namespace LaserConfigurator.Services;

public class HansService : Form
{
    public static HansService Instance { get; private set; }
    private List<HansDeviceState> _devicesStates = new();

    private readonly IEventAggregator _eventAggregator;

    public HansService(IEventAggregator eventAggregator)
    {
        Instance = this;
        _eventAggregator = eventAggregator;

        int result = HM_InitBoard(Handle);
        if (result == 0)
        {
            Console.WriteLine("HansSDK succesfully initialized !");
        }
        else
        {
            Console.WriteLine($"HansSDK initialized error Code: {result}");
        }

        _ = Task.Run(async () =>
        {
            while (true) 
            {
                foreach (var device in _devicesStates)
                {
                    if (device.ConnectState == ConnectState.Connected)
                    {
                        await InvokeAsync(() =>
                        {
                            Console.WriteLine("Status: " + GetWorkingStatus(device.Address));
                        });
                    }
                }
                await Task.Delay(1000);
            }
        });
    }

    public static DeviceInfo DeviceRefresh(int ipIndex, ulong uIp)
    {
        DeviceInfo device = new DeviceInfo
        {
            IPValue = uIp,
            Index = ipIndex,
            DeviceName = $"{(uIp >> 0) & 0xFF}.{(uIp >> 8) & 0xFF}.{(uIp >> 16) & 0xFF}.{(uIp >> 24) & 0xFF}"
        };
        return device;
    }

    protected override void WndProc(ref Message m)
    {
        try
        {
            Message message = m;
            base.WndProc(ref m);

            // Состояние устройства изменилось 
            if (message.Msg == MessageType.ConnectStateUpdate ||
                // Загрузка очереди
                message.Msg == MessageType.StreamProgress ||
                // Окончание загрузки очереди
                message.Msg == MessageType.StreamEnd ||
                // Окончание маркировки
                message.Msg == MessageType.MarkingComplete ||
                // Прогресс маркировки
                message.Msg == MessageType.MarkingProgress)
            {

                int deviceIndex = (int)message.LParam;
                if (message.Msg == MessageType.ConnectStateUpdate)
                {

                    // Получаем данные об устройстве
                    var device = DeviceRefresh((int)message.LParam, (UInt64)message.WParam);

                    // Добавляем в список устройств, если не добавлено
                    AddToMemoryIfNotExists(device);

                    // Получаем модель устройства
                    var deviceFromMemory = GetState(device.DeviceName);

                    deviceFromMemory!.ConnectState = (ConnectState)HM_GetConnectStatus(deviceFromMemory.Index);

                    switch (deviceFromMemory!.ConnectState)
                    {
                        case ConnectState.ReadyToConnect:
                            HasReadyToConnect(deviceFromMemory);
                            break;
                        case ConnectState.Connected:
                            HasConnected(deviceFromMemory);
                            break;
                        case ConnectState.Disconnected:
                            HasDisconnected(deviceFromMemory);
                            break;
                    }

                    return;
                }
                else if (message.Msg == MessageType.StreamProgress)
                {
                    var state = GetState(deviceIndex);
                    if (state == null) return;

                    state.StreamProgress = (int)message.WParam;
                    Console.WriteLine($"[{state.Address}] Stream progress: {state.StreamProgress}%");
                }
                else if (message.Msg == MessageType.StreamEnd)
                {
                    var state = GetState(deviceIndex);
                    if (state == null) return;

                    state.StreamEnd = true;
                    Console.WriteLine($"[{state.Address}] Stream download completed");
                }
                else if (message.Msg == MessageType.MarkingProgress)
                {
                    var state = GetState(deviceIndex);
                    if (state == null) return;

                    state.MarkingProgress = (int)message.WParam;
                    Console.WriteLine($"[{state.Address}] Marking progress: {state.MarkingProgress}%");
                }
                else if (message.Msg == MessageType.MarkingComplete)
                {
                    var state = GetState(deviceIndex);
                    if (state == null) return;

                    state.MarkComplete = true;
                    Console.WriteLine($"[{state.Address}] Marking completed");
                }
            }
        }
        catch (Exception e) 
        {
            Console.WriteLine(e);
        }
    }

    private void AddToMemoryIfNotExists(DeviceInfo deviceInfo)
    {
        var device = _devicesStates.FirstOrDefault(p => p.Address == deviceInfo.DeviceName);
        if (device == null)
        {
            _devicesStates.Add(new HansDeviceState
            {
                Index = deviceInfo.Index,
                Address = deviceInfo.DeviceName,
                DeviceInfo = deviceInfo,
                MarkComplete = false,
                ConnectState = ConnectState.Disconnected,
                MarkingProgress = 0,
                StreamEnd = false,
                StreamProgress = 0
            });
        }
    }

    private HansDeviceState? GetState(string address)
    {
        return _devicesStates.FirstOrDefault(p => p.Address == address);
    }


    private HansDeviceState? GetState(int index)
    {
        return _devicesStates.FirstOrDefault(p => p.Index == index);
    }

    private void HasReadyToConnect(HansDeviceState state)
    {
        state.ConnectState = ConnectState.ReadyToConnect;
        _eventAggregator.GetEvent<OnScanatorStatusChanged>().Publish(state);
        Console.WriteLine($"Device {state.Address} is ready to connect");
    }

    private void HasConnected(HansDeviceState state)
    {
        state.ConnectState = ConnectState.Connected;
        _eventAggregator.GetEvent<OnScanatorStatusChanged>().Publish(state);

        if (state.ConnectState == ConnectState.Connected)
        {
            Console.WriteLine("Apply region and coordinate type");

            Console.WriteLine("Mark region: " + HM_GetMarkRegion(state.Index));
            //{
            //    //HM_SetMarkRegion(state.Index, 350);
            //}
            //HM_SetCoordinate(state.Index, 5);
        }

        Console.WriteLine($"Device {state.Address} is connected");
    }

    private void HasDisconnected(HansDeviceState state)
    {
        state.ConnectState = ConnectState.Disconnected;
        _eventAggregator.GetEvent<OnScanatorStatusChanged>().Publish(state);

        Console.WriteLine($"Device {state.Address} is disconnected");
    }

    /// <summary>
    /// Подключиться к устройству
    /// </summary>
    public bool Connect(string address)
    {
        try
        {
            int result = HM_ConnectByIpStr(address);
            if (result >= 0)
            {
                Console.WriteLine($"Connection initiated to {address}, code: {result}");
                var state = GetState(address);
                state.ConnectState = (ConnectState)HM_GetConnectStatus(state.Index);
                _eventAggregator.GetEvent<OnScanatorStatusChanged>().Publish(state);


                return true;
            }
            else
            {
                Console.WriteLine($"Failed to connect to {address}, code: {result}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception connecting to {address}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Отключиться от устройства
    /// </summary>
    public bool Disconnect(string address)
    {
        try
        {
            var device = GetState(address);
            if (device == null) return false;

            int result = HM_DisconnectTo(device.Index);
            if (result == 0)
            {
                Console.WriteLine($"Disconnected from {address}");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to disconnect from {address}, error: {result}");
                return false;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception disconnecting from {address}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Загрузить UDM файл для маркировки
    /// </summary>
    public bool DownloadUdmFile(string address, string udmFilePath)
    {
        try
        {
            var state = GetState(address);
            if (state == null)
            {
                Console.WriteLine("Device not initialized");
                return false;
            }
            if (!System.IO.File.Exists(udmFilePath))
            {
                Console.WriteLine($"UDM file not found: {udmFilePath}");
                return false;
            }

            // Обнуляем состояние
            state.StreamEnd = false;
            state.StreamProgress = 0;
            state.MarkComplete = false;
            state.MarkingProgress = 0;

            HM_BurnMarkFile(state.Index, true);
            int result = HM_DownloadMarkFile(state.Index, udmFilePath, Handle);

            if (result == 0)
            {
                Console.WriteLine($"UDM file download initiated to {address}");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to download UDM file to {address}, error: {result}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception downloading UDM file to {address}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ожидание завершения загрузки очереди
    /// </summary>
    public async Task WaitForStreamDownloadComplete(string address, int delay = 1000)
    {
        var state = GetState(address);
        if (state == null)
        {
            Console.WriteLine($"[WaitForStreamDownloadComplete] Ошибка при ожидании завершения загрузки очереди для устройства ({address})");
            Console.WriteLine($"[WaitForStreamDownloadComplete] Устройство не инициализировано ({address})");
            return;
        }

        var cancelationTokenSource = new CancellationTokenSource(60000);

        while (true)
        {
            if (cancelationTokenSource.Token.IsCancellationRequested) break;
            if (state.StreamEnd) break;
            await Task.Delay(1000, cancelationTokenSource.Token);
        }
    }

    public async Task WaitForMarkingComplete(string address, int delay = 1000)
    {
        var state = GetState(address);
        if (state == null)
        {
            Console.WriteLine($"[WaitForMarkingComplete] Ошибка при ожидании завершения загрузки очереди для устройства ({address})");
            Console.WriteLine($"[WaitForMarkingComplete] Устройство не инициализировано ({address})");
            return;
        }

        var cancelationTokenSource = new CancellationTokenSource(60000);

        while (true)
        {
            if (cancelationTokenSource.Token.IsCancellationRequested) break;
            if (state.MarkComplete) break;
            await Task.Delay(1000, cancelationTokenSource.Token);
        }
    }

    /// <summary>
    /// Запустить маркировку
    /// </summary>
    public bool StartMarking(string address)
    {
        try
        {
            var state = GetState(address);
            if (state == null)
            {
                Console.WriteLine($"[StartMarking] Ошибка при запуске сканирования ({address})");
                Console.WriteLine($"[StartMarking] Устройство не инициализировано ({address})");
                return false;
            }
            int result = HM_StartMark(state.Index);
            if (result == 0)
            {
                Console.WriteLine($"Marking started on {state.Address}");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to start marking on {state.Address}, error: {result}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception starting marking on {ex.Message}");
            return false;
        }
    }

    public bool StopMarking(string address)
    {
        var state = GetState(address);
        if (state == null)
        {
            Console.WriteLine($"[StopMarking] Ошибка при запуске сканирования ({address})");
            Console.WriteLine($"[StopMarking] Устройство не инициализировано ({address})");
            return false;
        }
        try
        {
            int result = HM_StopMark(state.Index);
            if (result == 0)
            {
                Console.WriteLine($"Marking stopped on {state.Address}");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to stop marking on {state.Address}, error: {result}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception stopping marking on {address}: {ex.Message}");
            return false;
        }
    }

    public WorkingStatus GetWorkingStatus(string address)
    {
        var state = GetState(address);
        if (state == null)
        {
            Console.WriteLine($"[GetWorkingStatus] Ошибка при получении статуса устройства ({address})");
            Console.WriteLine($"[GetWorkingStatus] Устройство не инициализировано ({address})");
            return WorkingStatus.Unknown;
        }
        try
        {
            return (WorkingStatus)HM_GetWorkStatus(state.Index);
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"[GetWorkingStatus] Ошибка при получении статуса устройства ({address}), Ошибка: {ex.Message}");
            return WorkingStatus.Unknown;
        }
    }
}