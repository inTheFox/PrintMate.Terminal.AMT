using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandyControl.Controls;
using HandyControl.Tools.Command;
using Hans.NET.libs;
using LaserConfigurator.Services;
using Prism.Mvvm;

namespace LaserConfigurator.Models
{
    public class HansDeviceState : BindableBase
    {
        private int _index;
        private string _address = string.Empty;
        private DeviceInfo? _deviceInfo;
        private ConnectState _connectState;
        private int _streamProgress;
        private bool _streamEnd;
        private int _markingProgress;
        private bool _markComplete;
        private bool _connectButtonIsEnabled;
        private bool _disconnectButtonIsEnabled;

        public bool ConnectButtonIsEnabled
        {
            get => _connectButtonIsEnabled;
            set => SetProperty(ref _connectButtonIsEnabled, value);
        }

        public bool DisconnectButtonIsEnabled
        {
            get => _disconnectButtonIsEnabled;
            set => SetProperty(ref _disconnectButtonIsEnabled, value);
        }

        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }
        public DeviceInfo? DeviceInfo
        {
            get => _deviceInfo;
            set => SetProperty(ref _deviceInfo, value);
        }
        public ConnectState ConnectState
        {
            get => _connectState;
            set
            {
                SetProperty(ref _connectState, value);
                if (value == ConnectState.Connected)
                {
                    ConnectButtonIsEnabled = false;
                    DisconnectButtonIsEnabled = true;
                }
                else if (value == ConnectState.ReadyToConnect)
                {
                    ConnectButtonIsEnabled = true;
                    DisconnectButtonIsEnabled = false;
                }
                else if (value == ConnectState.Disconnected)
                {
                    ConnectButtonIsEnabled = true;
                    DisconnectButtonIsEnabled = false;
                }
            }
        }

        public int StreamProgress
        {
            get => _streamProgress;
            set => SetProperty(ref _streamProgress, value);
        }
        public bool StreamEnd
        {
            get => _streamEnd;
            set => SetProperty(ref _streamEnd, value);
        }
        public int MarkingProgress
        {
            get => _markingProgress;
            set => SetProperty(ref _markingProgress, value);
        }
        public bool MarkComplete
        {
            get => _markComplete;
            set => SetProperty(ref _markComplete, value);
        }


        public RelayCommand Connect { get; set; }
        public RelayCommand Disconnect { get; set; }


        public HansDeviceState()
        {
            Connect = new RelayCommand(OnConnectCallback);
            Disconnect = new RelayCommand(DisconnectCallback);
        }

        private void DisconnectCallback(object obj)
        {
            HansService.Instance.Disconnect(Address);
        }

        private void OnConnectCallback(object obj)
        {
            HansService.Instance.Connect(Address);
        }
    }
}
