using HandyControl.Tools.Command;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HandyControl.Controls;
using Opc2Lib;
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.Services;
using MessageBoxResult = PrintMate.Terminal.Models.MessageBoxResult;
using Visibility = HelixToolkit.SharpDX.Core.Model.Scene2D.Visibility;

namespace PrintMate.Terminal.ViewModels
{
    public class ManualControlSystemsModel : BindableBase
    {
        private const string GasSystemEnabledImagePath = "/images/gas_on.png";
        private const string GasSystemDisabledImagePath = "/images/gas_off.png";
        private const string GasSystemDisabledColor = "#1f1f1f";
        private const string GasSystemEnabledColor = "#279600";

        private const string LaserSystemEnabledImagePath = "/images/laser_on.png";
        private const string LaserSystemDisabledImagePath = "/images/laser_off.png";
        private const string LaserSystemDisabledColor = "#1f1f1f";
        private const string LaserSystemEnabledColor = "#279600";


        private string _gasSystemImagePath = GasSystemDisabledImagePath;
        public string GasSystemImagePath
        {
            get => _gasSystemImagePath;
            set => SetProperty(ref _gasSystemImagePath, value);
        }

        private string _gasSystemColor = GasSystemDisabledColor;
        public string GasSystemColor
        {
            get => _gasSystemColor;
            set => SetProperty(ref _gasSystemColor, value);
        }

        private string _laserSystemImagePath = LaserSystemDisabledImagePath;
        public string LaserSystemImagePath
        {
            get => _laserSystemImagePath;
            set => SetProperty(ref _laserSystemImagePath, value);
        }

        private string _laserSystemColor = LaserSystemDisabledColor;
        public string LaserSystemColor
        {
            get => _laserSystemColor;
            set => SetProperty(ref _laserSystemColor, value);
        }

        private bool _isGasEnabled;
        public bool IsGasEnabled
        {
            get => _isGasEnabled;
            set
            {
                SetProperty(ref _isGasEnabled, value);
                if (value)
                {
                    GasSystemImagePath = GasSystemEnabledImagePath;
                    GasSystemColor = GasSystemEnabledColor;
                }
                else
                {
                    GasSystemImagePath = GasSystemDisabledImagePath;
                    GasSystemColor = GasSystemDisabledColor;
                }
            }
        }

        private bool _isLaserEnabled;
        public bool IsLaserEnabled
        {
            get => _isLaserEnabled;
            set
            {
                SetProperty(ref _isLaserEnabled, value);
                if (value)
                {
                    LaserSystemImagePath = LaserSystemEnabledImagePath;
                    LaserSystemColor = LaserSystemEnabledColor;
                }
                else
                {
                    LaserSystemImagePath = LaserSystemDisabledImagePath;
                    LaserSystemColor = LaserSystemDisabledColor;
                }
            }
        }

        private int _lightCameraImageVisibility = 0;
        public int LightCameraImageVisibility
        {
            get => _lightCameraImageVisibility;
            set => SetProperty(ref _lightCameraImageVisibility, value);
        }

        public ICommand GasButtonCommand { get; private set; }
        public ICommand LaserButtonCommand { get; private set; }

        private readonly ILogicControllerProvider _logicControllerProvider;
        private readonly ILogicControllerObserver _observer;

        private bool _lightValue = false;

        public ManualControlSystemsModel(ILogicControllerProvider logicControllerProvider, ILogicControllerObserver observer)
        {
            _logicControllerProvider = logicControllerProvider;
            _observer = observer;
            //_observer.Subscribe(this, (responce) =>
            //{
            //    if (responce.Value != null && responce.Value is bool value && value != _lightValue)
            //    {
            //        Application.Current.Dispatcher.InvokeAsync(() =>
            //        {
            //            if (value)
            //            {
            //                LightCameraImageVisibility = 1;
            //            }
            //            else
            //            {
            //                LightCameraImageVisibility = 0;
            //            }
            //        });
            //        _lightValue = value;
            //    }
            //}, OpcCommands.Com_PChamber_Light);
            _observer.Subscribe(this, (p) =>
            {
                bool currentState = p.Value != null && p.Value is bool value && value;
                if (IsLaserEnabled != currentState)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        IsLaserEnabled = currentState;
                    });
                }
            }, OpcCommands.Com_LaserSystem);
            _observer.Subscribe(this, (p) =>
            {
                bool currentState = p.Value != null && p.Value is bool value && value;
                if (IsGasEnabled != currentState)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        IsGasEnabled = currentState;
                    });
                }
            }, OpcCommands.Com_GasSystem);

            GasButtonCommand = new RelayCommand(ExecuteGasButton);
            LaserButtonCommand = new RelayCommand(ExecuteLaserButton);
        }

        private async void ExecuteLaserButton(object obj)
        {
            if (!_logicControllerProvider.Connected)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Нет подключения с ПЛК");
                return;
            }

            string question = "Включить лазерную систему ?";
            if (IsLaserEnabled) question = "Отключить лазерную систему ?";

            var result = await CustomMessageBox.ShowConfirmationAsync("Лазерная система", question);
            if (result == MessageBoxResult.Yes)
            {
                bool currentState = await _logicControllerProvider.GetBoolAsync(OpcCommands.Com_LaserSystem);
                await _logicControllerProvider.SetBoolAsync(OpcCommands.Com_LaserSystem, !currentState);
                IsLaserEnabled = !currentState;
            }

        }

        private async void ExecuteGasButton(object obj)
        {
            if (!_logicControllerProvider.Connected)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Нет подключения с ПЛК");
                return;
            }

            string question = "Включить систему подачи газа ?";
            if (IsGasEnabled) question = "Отключить систему подачи газа ?";
            var result = await CustomMessageBox.ShowConfirmationAsync("Система подачи газа", question);
            if (result == MessageBoxResult.Yes)
            {
                bool currentState = await _logicControllerProvider.GetBoolAsync(OpcCommands.Com_GasSystem);
                await _logicControllerProvider.SetBoolAsync(OpcCommands.Com_GasSystem, !currentState);

                // Переключаем состояние
                IsGasEnabled = !currentState;
            }
        }

    }
}
