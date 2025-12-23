using System;
using System.Threading.Tasks;
using System.Windows;
using HandyControl.Controls;
using HandyControl.Tools.Command;
using LaserLib.Models;
using Microsoft.Extensions.Logging.Abstractions;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Services;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using MessageBox = System.Windows.MessageBox;

namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels;

public class ConfigureParametersLasersViewModel : BindableBase, IRegionMemberLifetime
{
    public bool KeepAlive => false;

    private const string EnabledImagePath = "/images/indicator_green_32.png";
    private const string DisabledImagePath = "/images/indicator_red_32.png";
    private string _laser1Address;
    private string _laser1ConnectionText;
    private string _laser1IconSrc;

    private string _laser2Address;
    private string _laser2ConnectionText;
    private string _laser2IconSrc;
    private LaserStatus _laser1Status;
    private LaserStatus _laser2Status;

    public LaserStatus Laser1Status
    {
        get => _laser1Status;
        set => SetProperty(ref _laser1Status, value);
    }

    public LaserStatus Laser2Status
    {
        get => _laser2Status;
        set => SetProperty(ref _laser2Status, value);
    }

    public string Laser1Address
    {
        get => _laser1Address;
        set => SetProperty(ref _laser1Address, value);
    }
    public string Laser1ConnectionText
    {
        get => _laser1ConnectionText;
        set => SetProperty(ref _laser1ConnectionText, value);
    }
    public string Laser1IconSrc
    {
        get => _laser1IconSrc;
        set => SetProperty(ref _laser1IconSrc, value);
    }

    public string Laser2Address
    {
        get => _laser2Address;
        set => SetProperty(ref _laser2Address, value);
    }
    public string Laser2ConnectionText
    {
        get => _laser2ConnectionText;
        set => SetProperty(ref _laser2ConnectionText, value);
    }
    public string Laser2IconSrc
    {
        get => _laser2IconSrc;
        set => SetProperty(ref _laser2IconSrc, value);
    }

    public RelayCommand SaveCommand { get; set; }
    public RelayCommand ResetCommand { get; set; }
    private readonly ConfigurationManager _configManager;
    private readonly MultiLaserSystemService _multiLaserSystemService;

//    SwitchAb1
//}">Включить/выключить пилотный лазер</Button>
//    <Button HorizontalAlignment = "Stretch" Background="White" Command="{Binding SwitchEabc1}">Вкл


    public RelayCommand SwitchAb1 { get; set; }
    public RelayCommand SwitchAb2 { get; set; }
    public RelayCommand SwitchEabc1 { get; set; }
    public RelayCommand SwitchEabc2 { get; set; }



    public ConfigureParametersLasersViewModel(
        ConfigurationManager configManager,
        IEventAggregator eventAggregator,
        MultiLaserSystemService multiLaserSystemService)
    {
        _configManager = configManager;
        _multiLaserSystemService = multiLaserSystemService;

        SaveCommand = new RelayCommand(SaveCommandHandler);
        ResetCommand = new RelayCommand(ResetCommandHandler);

        LoadConfiguration();

        eventAggregator.GetEvent<OnPingObserverTaskUpdatedEvent>().Subscribe((data) =>
        {
            if (data.Name == nameof(PingObserver.Laser1ConnectionObserver) ||
                data.Name == nameof(PingObserver.Laser2ConnectionObserver))
            {
                PingStatusHandle();
            }
        });
        PingStatusHandle();

        Task.Run(LasersStatusObserver);

        SwitchAb1 = new RelayCommand(async obj => await SwitchAbHandler(1));
        SwitchAb2 = new RelayCommand(async obj => await SwitchAbHandler(2));
        SwitchEabc1 = new RelayCommand(async obj => await SwitchEabcHandler(1));
        SwitchEabc2 = new RelayCommand(async obj => await SwitchEabcHandler(2));
    }

    private void LoadConfiguration()
    {
        var settings = _configManager.Get<LaserSettings>();
        Laser1Address = settings.Laser1Address;
        Laser2Address = settings.Laser2Address;

        Console.WriteLine($"Laser Configuration loaded: Laser1={Laser1Address}, Laser2={Laser2Address}");
    }

    public async Task SwitchAbHandler(int laserNum)
    {
        var laserService = _multiLaserSystemService.GetService(laserNum);
        var status = await laserService.GetStatus();

        //// 🔴 Если в QCW-режиме или External Shutdown — пилотом нельзя управлять напрямую!
        //if (IsHCR(state))
        //{
        //    Growl.Warning("Невозможно управлять пилотным лазером: устройство в QCW-режиме, External Shutdown или Compatibility Mode.");
        //    return;
        //}


        // 2. Выйти из Compatibility Mode, если нужно
        if (status.STAStates[13])
        {
            await laserService.SendCommandAsync("edc");
            Growl.Info("Выход из Compatibility Mode выполнен");
        }
        // 3. Отключить HW-управление пилотом, если включено
        if (status.STAStates[27])
        {
            await laserService.SendCommandAsync("deabc");
            Growl.Info("Отключено внешнее управление пилотным лазером");
        }

        if (status.STAStates[8])
        {
            await _multiLaserSystemService.GetService(laserNum).SendCommandAsync("abf");
            
            status = await laserService.GetStatus();
            if (!status.STAStates[8])
            {
                Growl.Info("Пилотный лазер отключен");
            }
            else
            {
                Growl.Info("Не удалось отключить пилотный лазер");
            }
        }
        else
        {
            await _multiLaserSystemService.GetService(laserNum).SendCommandAsync("abn");

            status = await laserService.GetStatus();
            if (status.STAStates[8])
            {
                Growl.Info("Пилотный лазер включен");
            }
            else
            {
                Growl.Info("Не удалось включить пилотный лазер");
            }
        }
    }

    private bool IsHCR(LaserStatus status)
    {
        int sta = status.STA; // Исправлено: STA уже int, не нужно использовать int.TryParse
        int rcfg = status.RCFG;
            
        bool compatibilityMode = (sta & (1 << 13)) != 0;
        bool externalShutdown = (sta & (1 << 15)) != 0;
        bool isQCW = (rcfg & ((1 << 3) | (1 << 4))) != 0;

        return compatibilityMode || externalShutdown || isQCW;
    }

    public async Task SwitchEabcHandler(int laserNum)
    {
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var status = await _multiLaserSystemService.GetService(laserNum).GetStatus();
            if (status.STAStates[27])
            {
                if (await _multiLaserSystemService.GetService(laserNum).SendCommandAsync("deabc"))
                    Growl.Success("Внеш. управление пилотным лазером отключено");
            }
            else
            {
                if (await _multiLaserSystemService.GetService(laserNum).SendCommandAsync("eeabc"))
                    Growl.Success("Внеш. управление пилотным лазером включено");
            }
        });
    }

    private void SaveCommandHandler(object obj)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _configManager.Update<LaserSettings>(settings =>
            {
                settings.Laser1Address = Laser1Address;
                settings.Laser2Address = Laser2Address;
            });

            _configManager.SaveNow();

            PingObserver.Laser1ConnectionObserver.SetAddress(Laser1Address);
            PingObserver.Laser2ConnectionObserver.SetAddress(Laser2Address);

            Console.WriteLine($"Laser settings saved: Laser1={Laser1Address}, Laser2={Laser2Address}");
        });
    }

    private void ResetCommandHandler(object obj)
    {
        _configManager.Reset<LaserSettings>();
        _configManager.SaveNow();
        LoadConfiguration();

        PingObserver.Laser1ConnectionObserver.SetAddress(Laser1Address);
        PingObserver.Laser2ConnectionObserver.SetAddress(Laser2Address);

        Console.WriteLine("Laser settings reset to default");
    }

    private void PingStatusHandle()
    {
        if (PingObserver.Laser1ConnectionObserver != null && PingObserver.Laser1ConnectionObserver.Result != null && PingObserver.Laser1ConnectionObserver.Result.Success)
        {
            Laser1ConnectionText = "В сети";
            Laser1IconSrc = EnabledImagePath;
        }
        else
        {
            Laser1ConnectionText = "Не в сети";
            Laser1IconSrc = DisabledImagePath;
        }
        if (PingObserver.Laser2ConnectionObserver != null && PingObserver.Laser2ConnectionObserver.Result != null && PingObserver.Laser2ConnectionObserver.Result.Success)
        {
            Laser2ConnectionText = "В сети";
            Laser2IconSrc = EnabledImagePath;
        }
        else
        {
            Laser2ConnectionText = "Не в сети";
            Laser2IconSrc = DisabledImagePath;
        }
    }

    private async Task LasersStatusObserver()
    {
        while (true)
        {
            try
            {
                if (PingObserver.Laser1ConnectionObserver != null &&
                    PingObserver.Laser1ConnectionObserver.Result != null &&
                    PingObserver.Laser1ConnectionObserver.Result.Success)
                {
                    //var state = await _multiLaserSystemService.GetService(1).GetStatus();
                    Application.Current.Dispatcher.InvokeAsync(async () => Laser1Status = await _multiLaserSystemService.GetService(1).GetStatus());
                }

                if (PingObserver.Laser2ConnectionObserver != null &&
                    PingObserver.Laser2ConnectionObserver.Result != null &&
                    PingObserver.Laser2ConnectionObserver.Result.Success)
                {
                    //var state = await _multiLaserSystemService.GetService(2).GetStatus();
                    Application.Current.Dispatcher.InvokeAsync(async () => Laser2Status = await _multiLaserSystemService.GetService(2).GetStatus());
                }
                await Task.Delay(2000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Task.Run(LasersStatusObserver);
            }
        }
    }
}