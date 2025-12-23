using System;
using System.Collections.Generic;
using HandyControl.Controls;
using HandyControl.Tools.Command;
using HansDebuggerApp.Hans;
using HansDebuggerApp.Services;
using HansDebuggerApp.Views;
using ImTools;
using Microsoft.Win32;
using Opc2Lib;
using Prism.Mvvm;
using System.IO;
using HansDebuggerApp.Opc;

namespace HansDebuggerApp.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string _address;
        private float _x;
        private float _y;
        private float _z;
        private float _power;
        private double _beamDiameter;
        private int _time;
        private bool _connectButtonEnabled;
        private bool _disconnectButtonEnabled;

        private string _configurationFilePath;
        private string _configurationFileName;
        private int _platformStep;
        private int _platformPosition;

        public int PlatformPosition
        {
            get => _platformPosition;
            set => SetProperty(ref _platformPosition, value);
        }

        public int PlatformStep
        {
            get => _platformStep;
            set => SetProperty(ref _platformStep, value, OnPlatformStepValueChanged);
        }

        private async void OnPlatformStepValueChanged()
        {
            await _logicControllerProvider.SetInt32Async(OpcCommands.Set_Axes_PlatformStep, PlatformStep);
        }

        public string ConfigurationFileName
        {
            get => _configurationFileName;
            set => SetProperty(ref _configurationFileName, value);
        }

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }
        public float X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }
        public float Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }
        public float Z
        {
            get => _z;
            set => SetProperty(ref _z, value);
        }
        public float Power
        {
            get => _power;
            set => SetProperty(ref _power, value);
        }
        public double BeamDiameter
        {
            get => _beamDiameter;
            set => SetProperty(ref _beamDiameter, value);
        }
        public int Time
        {
            get => _time;
            set => SetProperty(ref _time, value);
        }

        public bool ConnectButtonEnabled
        {
            get => _connectButtonEnabled;
            set => SetProperty(ref _connectButtonEnabled, value);
        }

        public bool DisconnectButtonEnabled
        {
            get => _disconnectButtonEnabled;
            set => SetProperty(ref _disconnectButtonEnabled, value);
        }

        private double _focalLengthMm;
        public double FocalLengthMm
        {
            get => _focalLengthMm;
            set => SetProperty(ref _focalLengthMm, value);
        }

        private double _focalLengthMicron;
        public double FocalLengthMicron
        {
            get => _focalLengthMicron;
            set => SetProperty(ref _focalLengthMicron, value);
        }

        private float _baseFocal;
        public float BaseFocal
        {
            get => _baseFocal;
            set => SetProperty(ref _baseFocal, value);
        }

        private float _zOffsetMm;
        public float ZOffsetMm
        {
            get => _zOffsetMm;
            set => SetProperty(ref _zOffsetMm, value);
        }

        private float _powerOffsetMicrons;
        public float PowerOffsetMicrons
        {
            get => _powerOffsetMicrons;
            set => SetProperty(ref _powerOffsetMicrons, value);
        }

        private float _zFinal;
        public float ZFinal
        {
            get => _zFinal;
            set => SetProperty(ref _zFinal, value);
        }

        private float _powerWatts;
        public float PowerWatts
        {
            get => _powerWatts;
            set => SetProperty(ref _powerWatts, value);
        }

        private float _correctedPowerWatts;
        public float CorrectedPowerWatts
        {
            get => _correctedPowerWatts;
            set => SetProperty(ref _correctedPowerWatts, value);
        }

        private float _powerPercent;
        public float PowerPercent
        {
            get => _powerPercent;
            set => SetProperty(ref _powerPercent, value);
        }

        public RelayCommand ConnectCommand { get; set; }
        public RelayCommand ImportCommand { get; set; }
        public RelayCommand DisconnectCommand { get; set; }
        public RelayCommand GenerateUdmCommand { get; set; }
        public RelayCommand GenerateUdmCommandWithZ0 { get; set; }
        public RelayCommand StartMarkCommand { get; set; }
        public RelayCommand StopMarkCommand { get; set; }



        private readonly ScannerService _scannerService;
        private readonly ILogicControllerProvider _logicControllerProvider;
        private readonly ILogicControllerObserver _observer;

        public MainWindowViewModel(ScannerService scannerService, ILogicControllerProvider logicControllerProvider, ILogicControllerObserver observer)
        {
            _observer = observer;
            _observer.Subscribe(this, (response) =>
            {
                try
                {
                    PlatformPosition = (int)response.Value;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }, OpcCommands.AM_Axes_PlatformRELPosition);

            _logicControllerProvider = logicControllerProvider;
            _scannerService = scannerService;

            Address = "172.18.34.227";
            X = 0;
            Y = 0;
            Power = 75;
            BeamDiameter = 65;
            ConnectButtonEnabled = true;
            DisconnectButtonEnabled = false;
            ConnectCommand = new RelayCommand(ConnectCommandCallback);
            ImportCommand = new RelayCommand(ImportCommandCallback);
            DisconnectCommand = new RelayCommand(DisconnectCommandCallback);
            GenerateUdmCommand = new RelayCommand(GenerateUdmCallback);
            GenerateUdmCommandWithZ0 = new RelayCommand(GenerateUdmCallbackWithZ0);
            StartMarkCommand = new RelayCommand(StartMarkCommandCallback);
            StopMarkCommand = new RelayCommand(StopMarkCommandCallback);

        }

        private void StopMarkCommandCallback(object obj)
        {
            _scannerService.StopMark();
        }

        private void StartMarkCommandCallback(object obj)
        {
            _scannerService.StartMark();
        }

        private void GenerateUdmCallback(object obj)
        {
            _scannerService.GenerateUdmForAddress(Address, X, Y, BeamDiameter, Power, Time);

            FocalLengthMm = (float)TestUdmBuilder.FocalLengthMm;
            FocalLengthMicron = (float)TestUdmBuilder.FocalLengthMicron;
            // BaseFocal = TestUdmBuilder.BaseFocal;  // Удалено из TestUdmBuilder
            // ZOffsetMm = TestUdmBuilder.ZOffsetMm;  // Удалено из TestUdmBuilder
            PowerOffsetMicrons = (float)TestUdmBuilder.PowerOffsetMicrons;
            ZFinal = (float)TestUdmBuilder.ZFinal;
            // PowerWatts = TestUdmBuilder.PowerWatts;  // Удалено из TestUdmBuilder
            // CorrectedPowerWatts = TestUdmBuilder.CorrectedPowerWatts;  // Удалено из TestUdmBuilder
            // PowerPercent = TestUdmBuilder.PowerPercent;  // Удалено из TestUdmBuilder
        }

        private void GenerateUdmCallbackWithZ0(object obj)
        {
            _scannerService.GenerateUdmForAddress(Address, X, Y, Z, BeamDiameter, Power, Time);

            FocalLengthMm = (float)TestUdmBuilder.FocalLengthMm;
            FocalLengthMicron = (float)TestUdmBuilder.FocalLengthMicron;
            // BaseFocal = TestUdmBuilder.BaseFocal;  // Удалено из TestUdmBuilder
            // ZOffsetMm = TestUdmBuilder.ZOffsetMm;  // Удалено из TestUdmBuilder
            PowerOffsetMicrons = (float)TestUdmBuilder.PowerOffsetMicrons;
            ZFinal = (float)TestUdmBuilder.ZFinal;
            // PowerWatts = TestUdmBuilder.PowerWatts;  // Удалено из TestUdmBuilder
            // CorrectedPowerWatts = TestUdmBuilder.CorrectedPowerWatts;  // Удалено из TestUdmBuilder
            // PowerPercent = TestUdmBuilder.PowerPercent;  // Удалено из TestUdmBuilder
        }

        private void DisconnectCommandCallback(object obj)
        {
            _scannerService.Disconnect();
            ConnectButtonEnabled = true;
            DisconnectButtonEnabled = false;
        }

        private void ImportCommandCallback(object obj)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "JSON files (*.json)|*.json";
            ofd.FilterIndex = 1; // по умолчанию выбирает первый фильтр (JSON)
            ofd.Multiselect = false;
            bool? result = ofd.ShowDialog(MainWindow.Instance);
            if (result == true)
            {
                _configurationFilePath = ofd.FileName;
                ConfigurationFileName = Path.GetFileName(ofd.FileName);

                var configuration = ScanatorConfigurationLoader.LoadFromFile(_configurationFilePath);
                _scannerService.LoadConfiguration(configuration);
            }
        }

        private void ConnectCommandCallback(object obj)
        {
            if (_scannerService.Connect(Address))
            {
                Growl.Success("Вы успешно подключены к сканаторной системе");
                ConnectButtonEnabled = false;
                DisconnectButtonEnabled = true;
            }
            else
            {
                ConnectButtonEnabled = true;
                DisconnectButtonEnabled = false;
                Growl.Success("Ошибка подключения к сканаторной системе");
            }
        }
    }
}
