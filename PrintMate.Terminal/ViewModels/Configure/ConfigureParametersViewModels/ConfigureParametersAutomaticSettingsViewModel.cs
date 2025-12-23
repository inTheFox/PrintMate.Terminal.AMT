using System;
using System.Threading.Tasks;
using System.Windows;
using HandyControl.Controls;
using HandyControl.Tools.Command;
using Opc2Lib;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels
{
    public class ConfigureParametersAutomaticSettingsViewModel : BindableBase
    {
        private readonly ILogicControllerObserver _logicControllerObserver;
        private readonly ILogicControllerProvider _logicControllerProvider;
        private readonly KeyboardService _keyboardService;

        #region Настройки автоматического процесса (Local Settings)

        // 1. Готовность газовой системы
        private bool _readyGasSystemCheck;
        public bool ReadyGasSystemCheck
        {
            get => _readyGasSystemCheck;
            set
            {
                SetProperty(ref _readyGasSystemCheck, value);
                Bootstrapper.Configuration.Get<AutomaticProcessSettings>().ReadyGasSystemCheck = value;
                Bootstrapper.Configuration.SaveNow();
            }
        }

        // 2. Готовность лазерной системы
        private bool _readyLaserSystemCheck;
        public bool ReadyLaserSystemCheck
        {
            get => _readyLaserSystemCheck;
            set
            {
                SetProperty(ref _readyLaserSystemCheck, value);
                Bootstrapper.Configuration.Get<AutomaticProcessSettings>().ReadyLaserSystemCheck = value;
                Bootstrapper.Configuration.SaveNow();
            }
        }

        // 3. Готовность нагревателя
        private bool _readyHeatingTableCheck;
        public bool ReadyHeatingTableCheck
        {
            get => _readyHeatingTableCheck;
            set
            {
                SetProperty(ref _readyHeatingTableCheck, value);
                Bootstrapper.Configuration.Get<AutomaticProcessSettings>().ReadyHeatingTableCheck = value;
                Bootstrapper.Configuration.SaveNow();
            }
        }

        // 4. Отсутствие ошибок блокировки
        private bool _notErrorsBlockingPrintingCheck;
        public bool NotErrorsBlockingPrintingCheck
        {
            get => _notErrorsBlockingPrintingCheck;
            set
            {
                SetProperty(ref _notErrorsBlockingPrintingCheck, value);
                Bootstrapper.Configuration.Get<AutomaticProcessSettings>().NotErrorsBlockingPrintingCheck = value;
                Bootstrapper.Configuration.SaveNow();
            }
        }

        // 5. Ошибка реферирования рекоутера
        private bool _recoaterRefErrorCheck;
        public bool RecoaterRefErrorCheck
        {
            get => _recoaterRefErrorCheck;
            set
            {
                SetProperty(ref _recoaterRefErrorCheck, value);
                Bootstrapper.Configuration.Get<AutomaticProcessSettings>().RecoaterRefErrorCheck = value;
                Bootstrapper.Configuration.SaveNow();
            }
        }

        // 6. Ошибка привода рекоутера
        private bool _recoaterEngineErrorCheck;
        public bool RecoaterEngineErrorCheck
        {
            get => _recoaterEngineErrorCheck;
            set
            {
                SetProperty(ref _recoaterEngineErrorCheck, value);
                Bootstrapper.Configuration.Get<AutomaticProcessSettings>().RecoaterEngineErrorCheck = value;
                Bootstrapper.Configuration.SaveNow();
            }
        }

        // 7. Ошибка привода дозатора
        private bool _doserAxesErrorCheck;
        public bool DoserAxesErrorCheck
        {
            get => _doserAxesErrorCheck;
            set
            {
                SetProperty(ref _doserAxesErrorCheck, value);
                Bootstrapper.Configuration.Get<AutomaticProcessSettings>().DoserAxesErrorCheck = value;
                Bootstrapper.Configuration.SaveNow();
            }
        }

        // 8. Ошибка привода платформы
        private bool _platformAxesErrorCheck;
        public bool PlatformAxesErrorCheck
        {
            get => _platformAxesErrorCheck;
            set
            {
                SetProperty(ref _platformAxesErrorCheck, value);
                Bootstrapper.Configuration.Get<AutomaticProcessSettings>().PlatformAxesErrorCheck = value;
                Bootstrapper.Configuration.SaveNow();
            }
        }

        #endregion

        #region Привода (Axes) Section

        // Рекоутер спереди при экспонировании
        private bool _recoaterFrontOnExpose;
        public bool RecoaterFrontOnExpose
        {
            get => _recoaterFrontOnExpose;
            set
            {
                if (SetProperty(ref _recoaterFrontOnExpose, value))
                {
                    _ = SetBoolValueAsync(OpcCommands.SCP_Axes_RecoaterFrontOnExpose, value);
                }
            }
        }

        // Рекоутер правое конечное положение, мм
        private uint _recoaterDistRight;
        public uint RecoaterDistRight
        {
            get => _recoaterDistRight;
            set => SetProperty(ref _recoaterDistRight, value);
        }

        // Рекоутер скорость JOG, мм/с
        private uint _jogSpeedRecoater;
        public uint JogSpeedRecoater
        {
            get => _jogSpeedRecoater;
            set => SetProperty(ref _jogSpeedRecoater, value);
        }

        // Компенсация зазора ШВП вниз, мкм
        private uint _platformBacklashDown;
        public uint PlatformBacklashDown
        {
            get => _platformBacklashDown;
            set => SetProperty(ref _platformBacklashDown, value);
        }

        // Компенсация погрешности ШВП вниз, мкм
        private uint _platformCorrectionDown;
        public uint PlatformCorrectionDown
        {
            get => _platformCorrectionDown;
            set => SetProperty(ref _platformCorrectionDown, value);
        }

        #endregion

        #region Газ и фильтры (GasFilter) Section

        // Предупреждение выскоий кислород, %
        private float _alarmO2;
        public float AlarmO2
        {
            get => _alarmO2;
            set => SetProperty(ref _alarmO2, value);
        }

        // Предупреждение низкое давление инертного газа, bar
        private float _alarmInertPressure;
        public float AlarmInertPressure
        {
            get => _alarmInertPressure;
            set => SetProperty(ref _alarmInertPressure, value);
        }

        // Предупреждение заполнен фильтр, м/с
        private uint _alarmGasFlow;
        public uint AlarmGasFlow
        {
            get => _alarmGasFlow;
            set => SetProperty(ref _alarmGasFlow, value);
        }

        // Минимльное давление теста герметичности, кПа
        private float _errPresNotSealed;
        public float ErrPresNotSealed
        {
            get => _errPresNotSealed;
            set => SetProperty(ref _errPresNotSealed, value);
        }

        // ПИД воздуходувки Ki
        private float _blowerPidKi;
        public float BlowerPidKi
        {
            get => _blowerPidKi;
            set => SetProperty(ref _blowerPidKi, value);
        }

        // ПИД воздуходувки Kd
        private float _blowerPidKd;
        public float BlowerPidKd
        {
            get => _blowerPidKd;
            set => SetProperty(ref _blowerPidKd, value);
        }

        // Ошибка температуры воздуходувки, °C
        private uint _errTempBlower;
        public uint ErrTempBlower
        {
            get => _errTempBlower;
            set => SetProperty(ref _errTempBlower, value);
        }

        // Ошибка выскоий кислород, %
        private float _errO2;
        public float ErrO2
        {
            get => _errO2;
            set => SetProperty(ref _errO2, value);
        }

        // Предупреждение температуры воздуходувки, °C
        private uint _alarmTempBlower;
        public uint AlarmTempBlower
        {
            get => _alarmTempBlower;
            set => SetProperty(ref _alarmTempBlower, value);
        }

        // ПИД воздуходувки Kp
        private float _blowerPidKp;
        public float BlowerPidKp
        {
            get => _blowerPidKp;
            set => SetProperty(ref _blowerPidKp, value);
        }

        // Стартовый уровень кислорода, %
        private float _startPointO2;
        public float StartPointO2
        {
            get => _startPointO2;
            set => SetProperty(ref _startPointO2, value);
        }

        // Ошибка заполнен фильтр, м/с
        private uint _errGasFlow;
        public uint ErrGasFlow
        {
            get => _errGasFlow;
            set => SetProperty(ref _errGasFlow, value);
        }

        // Ошибка низкое давление инертного газа, bar
        private float _errInertPressure;
        public float ErrInertPressure
        {
            get => _errInertPressure;
            set => SetProperty(ref _errInertPressure, value);
        }

        // Максимальная скорость потока воздуходувки, м/с
        private uint _blowerMaxFlow;
        public uint BlowerMaxFlow
        {
            get => _blowerMaxFlow;
            set => SetProperty(ref _blowerMaxFlow, value);
        }

        // Целевой уровень кислорода, %
        private float _setPointO2;
        public float SetPointO2
        {
            get => _setPointO2;
            set => SetProperty(ref _setPointO2, value);
        }

        // Ошибка фильтр закрыт, кПа
        private float _errFilterClosed;
        public float ErrFilterClosed
        {
            get => _errFilterClosed;
            set => SetProperty(ref _errFilterClosed, value);
        }

        // Значение кислорода при продувке камеры, %
        private float _inertFillChamber;
        public float InertFillChamber
        {
            get => _inertFillChamber;
            set => SetProperty(ref _inertFillChamber, value);
        }

        #endregion

        #region Рабочая камера (PChamber) Section

        // Предупреждение высокое давление в камере, кПа
        private float _alarmPressure;
        public float AlarmPressure
        {
            get => _alarmPressure;
            set => SetProperty(ref _alarmPressure, value);
        }

        // Ошибка высокое давление в камере, кПа
        private float _errPressure;
        public float ErrPressure
        {
            get => _errPressure;
            set => SetProperty(ref _errPressure, value);
        }

        // Открытие клапана сброса для компенсации избыточного давления, кПа
        private float _setExhaustPressure;
        public float SetExhaustPressure
        {
            get => _setExhaustPressure;
            set => SetProperty(ref _setExhaustPressure, value);
        }

        // Подача аргона для компенсации низкого давления, кПа
        private float _setInertFillPressure;
        public float SetInertFillPressure
        {
            get => _setInertFillPressure;
            set => SetProperty(ref _setInertFillPressure, value);
        }

        #endregion

        #region Commands

        public RelayCommand SelectRecoaterDistRightCommand { get; }
        public RelayCommand SelectJogSpeedRecoaterCommand { get; }
        public RelayCommand SelectPlatformBacklashDownCommand { get; }
        public RelayCommand SelectPlatformCorrectionDownCommand { get; }

        public RelayCommand SelectAlarmO2Command { get; }
        public RelayCommand SelectAlarmInertPressureCommand { get; }
        public RelayCommand SelectAlarmGasFlowCommand { get; }
        public RelayCommand SelectErrPresNotSealedCommand { get; }
        public RelayCommand SelectBlowerPidKiCommand { get; }
        public RelayCommand SelectBlowerPidKdCommand { get; }
        public RelayCommand SelectErrTempBlowerCommand { get; }
        public RelayCommand SelectErrO2Command { get; }
        public RelayCommand SelectAlarmTempBlowerCommand { get; }
        public RelayCommand SelectBlowerPidKpCommand { get; }
        public RelayCommand SelectStartPointO2Command { get; }
        public RelayCommand SelectErrGasFlowCommand { get; }
        public RelayCommand SelectErrInertPressureCommand { get; }
        public RelayCommand SelectBlowerMaxFlowCommand { get; }
        public RelayCommand SelectSetPointO2Command { get; }
        public RelayCommand SelectErrFilterClosedCommand { get; }
        public RelayCommand SelectInertFillChamberCommand { get; }

        public RelayCommand SelectAlarmPressureCommand { get; }
        public RelayCommand SelectErrPressureCommand { get; }
        public RelayCommand SelectSetExhaustPressureCommand { get; }
        public RelayCommand SelectSetInertFillPressureCommand { get; }

        #endregion

        public ConfigureParametersAutomaticSettingsViewModel(
            ILogicControllerObserver logicControllerObserver,
            ILogicControllerProvider logicControllerProvider,
            KeyboardService keyboardService)
        {
            _logicControllerObserver = logicControllerObserver;
            _logicControllerProvider = logicControllerProvider;
            _keyboardService = keyboardService;

            // Load local settings from configuration
            LoadLocalSettings();

            // Subscribe to OPC updates
            SubscribeToOpcUpdates();

            // Initialize commands for Axes section
            SelectRecoaterDistRightCommand = new RelayCommand(_ => SelectUIntValue(
                "Рекоутер правое конечное положение, мм",
                RecoaterDistRight,
                OpcCommands.SCP_Axes_SetRecoaterDistRight,
                v => RecoaterDistRight = v));

            SelectJogSpeedRecoaterCommand = new RelayCommand(_ => SelectUIntValue(
                "Рекоутер скорость JOG, мм/с",
                JogSpeedRecoater,
                OpcCommands.SCP_Axes_JogSpeedRecoater,
                v => JogSpeedRecoater = v));

            SelectPlatformBacklashDownCommand = new RelayCommand(_ => SelectUIntValue(
                "Компенсация зазора ШВП вниз, мкм",
                PlatformBacklashDown,
                OpcCommands.SCP_Axes_PlatformBacklashDOWN,
                v => PlatformBacklashDown = v));

            SelectPlatformCorrectionDownCommand = new RelayCommand(_ => SelectUIntValue(
                "Компенсация погрешности ШВП вниз, мкм",
                PlatformCorrectionDown,
                OpcCommands.SCP_Axes_PlatformCorrectionDOWN,
                v => PlatformCorrectionDown = v));

            // Initialize commands for GasFilter section
            SelectAlarmO2Command = new RelayCommand(_ => SelectFloatValue(
                "Предупреждение высокий кислород, %",
                AlarmO2,
                OpcCommands.SCP_GasFilter_AlarmO2,
                v => AlarmO2 = v));

            SelectAlarmInertPressureCommand = new RelayCommand(_ => SelectFloatValue(
                "Предупреждение низкое давление инертного газа, bar",
                AlarmInertPressure,
                OpcCommands.SCP_GasFilter_AlarmInertPressure,
                v => AlarmInertPressure = v));

            SelectAlarmGasFlowCommand = new RelayCommand(_ => SelectUIntValue(
                "Предупреждение заполнен фильтр, м/с",
                AlarmGasFlow,
                OpcCommands.SCP_GasFilter_AlarmGasFlow,
                v => AlarmGasFlow = v));

            SelectErrPresNotSealedCommand = new RelayCommand(_ => SelectFloatValue(
                "Минимальное давление теста герметичности, кПа",
                ErrPresNotSealed,
                OpcCommands.SCP_GasFilter_ErrPresNotSealed,
                v => ErrPresNotSealed = v));

            SelectBlowerPidKiCommand = new RelayCommand(_ => SelectFloatValue(
                "ПИД воздуходувки Ki",
                BlowerPidKi,
                OpcCommands.SCP_GasFilter_BlowerPID_Ki,
                v => BlowerPidKi = v));

            SelectBlowerPidKdCommand = new RelayCommand(_ => SelectFloatValue(
                "ПИД воздуходувки Kd",
                BlowerPidKd,
                OpcCommands.SCP_GasFilter_BlowerPID_Kd,
                v => BlowerPidKd = v));

            SelectErrTempBlowerCommand = new RelayCommand(_ => SelectUIntValue(
                "Ошибка температуры воздуходувки, °C",
                ErrTempBlower,
                OpcCommands.SCP_GasFilter_ErrTempBlower,
                v => ErrTempBlower = v));

            SelectErrO2Command = new RelayCommand(_ => SelectFloatValue(
                "Ошибка высокий кислород, %",
                ErrO2,
                OpcCommands.SCP_GasFilter_ErrO2,
                v => ErrO2 = v));

            SelectAlarmTempBlowerCommand = new RelayCommand(_ => SelectUIntValue(
                "Предупреждение температуры воздуходувки, °C",
                AlarmTempBlower,
                OpcCommands.SCP_GasFilter_AlarmTempBlower,
                v => AlarmTempBlower = v));

            SelectBlowerPidKpCommand = new RelayCommand(_ => SelectFloatValue(
                "ПИД воздуходувки Kp",
                BlowerPidKp,
                OpcCommands.SCP_GasFilter_BlowerPID_Kp,
                v => BlowerPidKp = v));

            SelectStartPointO2Command = new RelayCommand(_ => SelectFloatValue(
                "Стартовый уровень кислорода, %",
                StartPointO2,
                OpcCommands.SCP_GasFilter_StartPointO2,
                v => StartPointO2 = v));

            SelectErrGasFlowCommand = new RelayCommand(_ => SelectUIntValue(
                "Ошибка заполнен фильтр, м/с",
                ErrGasFlow,
                OpcCommands.SCP_GasFilter_ErrGasFlow,
                v => ErrGasFlow = v));

            SelectErrInertPressureCommand = new RelayCommand(_ => SelectFloatValue(
                "Ошибка низкое давление инертного газа, bar",
                ErrInertPressure,
                OpcCommands.SCP_GasFilter_ErrInertPressure,
                v => ErrInertPressure = v));

            SelectBlowerMaxFlowCommand = new RelayCommand(_ => SelectUIntValue(
                "Максимальная скорость потока воздуходувки, м/с",
                BlowerMaxFlow,
                OpcCommands.SCP_GasFilter_BlowerMaxFlow,
                v => BlowerMaxFlow = v));

            SelectSetPointO2Command = new RelayCommand(_ => SelectFloatValue(
                "Целевой уровень кислорода, %",
                SetPointO2,
                OpcCommands.SCP_GasFilter_SetPointO2,
                v => SetPointO2 = v));

            SelectErrFilterClosedCommand = new RelayCommand(_ => SelectFloatValue(
                "Ошибка фильтр закрыт, кПа",
                ErrFilterClosed,
                OpcCommands.SCP_GasFilter_ErrFilterClosed,
                v => ErrFilterClosed = v));

            SelectInertFillChamberCommand = new RelayCommand(_ => SelectFloatValue(
                "Значение кислорода при продувке камеры, %",
                InertFillChamber,
                OpcCommands.SCP_GasFilter_InertFillChamber,
                v => InertFillChamber = v));

            // Initialize commands for PChamber section
            SelectAlarmPressureCommand = new RelayCommand(_ => SelectFloatValue(
                "Предупреждение высокое давление в камере, кПа",
                AlarmPressure,
                OpcCommands.SCP_PChamber_AlarmPressure,
                v => AlarmPressure = v));

            SelectErrPressureCommand = new RelayCommand(_ => SelectFloatValue(
                "Ошибка высокое давление в камере, кПа",
                ErrPressure,
                OpcCommands.SCP_PChamber_ErrPressure,
                v => ErrPressure = v));

            SelectSetExhaustPressureCommand = new RelayCommand(_ => SelectFloatValue(
                "Открытие клапана сброса, кПа",
                SetExhaustPressure,
                OpcCommands.SCP_PChamber_SetExhaustPressure,
                v => SetExhaustPressure = v));

            SelectSetInertFillPressureCommand = new RelayCommand(_ => SelectFloatValue(
                "Подача аргона для компенсации низкого давления, кПа",
                SetInertFillPressure,
                OpcCommands.SCP_PChamber_SetInertFillPressure,
                v => SetInertFillPressure = v));
        }

        private void SubscribeToOpcUpdates()
        {
            // Subscribe to Axes SCP parameters
            _logicControllerObserver.Subscribe(this, HandleAxesUpdates,
                OpcCommands.SCP_Axes_RecoaterFrontOnExpose,
                OpcCommands.SCP_Axes_SetRecoaterDistRight,
                OpcCommands.SCP_Axes_JogSpeedRecoater,
                OpcCommands.SCP_Axes_PlatformBacklashDOWN,
                OpcCommands.SCP_Axes_PlatformCorrectionDOWN);

            // Subscribe to GasFilter SCP parameters
            _logicControllerObserver.Subscribe(this, HandleGasFilterUpdates,
                OpcCommands.SCP_GasFilter_AlarmO2,
                OpcCommands.SCP_GasFilter_AlarmInertPressure,
                OpcCommands.SCP_GasFilter_AlarmGasFlow,
                OpcCommands.SCP_GasFilter_ErrPresNotSealed,
                OpcCommands.SCP_GasFilter_BlowerPID_Ki,
                OpcCommands.SCP_GasFilter_BlowerPID_Kd,
                OpcCommands.SCP_GasFilter_ErrTempBlower,
                OpcCommands.SCP_GasFilter_ErrO2,
                OpcCommands.SCP_GasFilter_AlarmTempBlower,
                OpcCommands.SCP_GasFilter_BlowerPID_Kp,
                OpcCommands.SCP_GasFilter_StartPointO2,
                OpcCommands.SCP_GasFilter_ErrGasFlow,
                OpcCommands.SCP_GasFilter_ErrInertPressure,
                OpcCommands.SCP_GasFilter_BlowerMaxFlow,
                OpcCommands.SCP_GasFilter_SetPointO2,
                OpcCommands.SCP_GasFilter_ErrFilterClosed,
                OpcCommands.SCP_GasFilter_InertFillChamber);

            // Subscribe to PChamber SCP parameters
            _logicControllerObserver.Subscribe(this, HandlePChamberUpdates,
                OpcCommands.SCP_PChamber_AlarmPressure,
                OpcCommands.SCP_PChamber_ErrPressure,
                OpcCommands.SCP_PChamber_SetExhaustPressure,
                OpcCommands.SCP_PChamber_SetInertFillPressure);
        }

        private void HandleAxesUpdates(CommandResponse response)
        {
            if (response?.Value == null) return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (response.CommandInfo == OpcCommands.SCP_Axes_RecoaterFrontOnExpose)
                {
                    _recoaterFrontOnExpose = Convert.ToBoolean(response.Value);
                    RaisePropertyChanged(nameof(RecoaterFrontOnExpose));
                }
                else if (response.CommandInfo == OpcCommands.SCP_Axes_SetRecoaterDistRight)
                {
                    _recoaterDistRight = Convert.ToUInt32(response.Value);
                    RaisePropertyChanged(nameof(RecoaterDistRight));
                }
                else if (response.CommandInfo == OpcCommands.SCP_Axes_JogSpeedRecoater)
                {
                    _jogSpeedRecoater = Convert.ToUInt32(response.Value);
                    RaisePropertyChanged(nameof(JogSpeedRecoater));
                }
                else if (response.CommandInfo == OpcCommands.SCP_Axes_PlatformBacklashDOWN)
                {
                    _platformBacklashDown = Convert.ToUInt32(response.Value);
                    RaisePropertyChanged(nameof(PlatformBacklashDown));
                }
                else if (response.CommandInfo == OpcCommands.SCP_Axes_PlatformCorrectionDOWN)
                {
                    _platformCorrectionDown = Convert.ToUInt32(response.Value);
                    RaisePropertyChanged(nameof(PlatformCorrectionDown));
                }
            });
        }

        private void HandleGasFilterUpdates(CommandResponse response)
        {
            if (response?.Value == null) return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (response.CommandInfo == OpcCommands.SCP_GasFilter_AlarmO2)
                {
                    _alarmO2 = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(AlarmO2));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_AlarmInertPressure)
                {
                    _alarmInertPressure = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(AlarmInertPressure));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_AlarmGasFlow)
                {
                    _alarmGasFlow = Convert.ToUInt32(response.Value);
                    RaisePropertyChanged(nameof(AlarmGasFlow));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_ErrPresNotSealed)
                {
                    _errPresNotSealed = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(ErrPresNotSealed));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_BlowerPID_Ki)
                {
                    _blowerPidKi = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(BlowerPidKi));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_BlowerPID_Kd)
                {
                    _blowerPidKd = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(BlowerPidKd));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_ErrTempBlower)
                {
                    _errTempBlower = Convert.ToUInt32(response.Value);
                    RaisePropertyChanged(nameof(ErrTempBlower));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_ErrO2)
                {
                    _errO2 = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(ErrO2));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_AlarmTempBlower)
                {
                    _alarmTempBlower = Convert.ToUInt32(response.Value);
                    RaisePropertyChanged(nameof(AlarmTempBlower));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_BlowerPID_Kp)
                {
                    _blowerPidKp = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(BlowerPidKp));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_StartPointO2)
                {
                    _startPointO2 = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(StartPointO2));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_ErrGasFlow)
                {
                    _errGasFlow = Convert.ToUInt32(response.Value);
                    RaisePropertyChanged(nameof(ErrGasFlow));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_ErrInertPressure)
                {
                    _errInertPressure = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(ErrInertPressure));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_BlowerMaxFlow)
                {
                    _blowerMaxFlow = Convert.ToUInt32(response.Value);
                    RaisePropertyChanged(nameof(BlowerMaxFlow));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_SetPointO2)
                {
                    _setPointO2 = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(SetPointO2));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_ErrFilterClosed)
                {
                    _errFilterClosed = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(ErrFilterClosed));
                }
                else if (response.CommandInfo == OpcCommands.SCP_GasFilter_InertFillChamber)
                {
                    _inertFillChamber = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(InertFillChamber));
                }
            });
        }

        private void HandlePChamberUpdates(CommandResponse response)
        {
            if (response?.Value == null) return;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (response.CommandInfo == OpcCommands.SCP_PChamber_AlarmPressure)
                {
                    _alarmPressure = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(AlarmPressure));
                }
                else if (response.CommandInfo == OpcCommands.SCP_PChamber_ErrPressure)
                {
                    _errPressure = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(ErrPressure));
                }
                else if (response.CommandInfo == OpcCommands.SCP_PChamber_SetExhaustPressure)
                {
                    _setExhaustPressure = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(SetExhaustPressure));
                }
                else if (response.CommandInfo == OpcCommands.SCP_PChamber_SetInertFillPressure)
                {
                    _setInertFillPressure = Convert.ToSingle(response.Value);
                    RaisePropertyChanged(nameof(SetInertFillPressure));
                }
            });
        }

        private async void SelectUIntValue(string title, uint currentValue, CommandInfo command, Action<uint> setter)
        {
            string result = _keyboardService.Show(KeyboardType.Numpad, title, currentValue.ToString());
            if (string.IsNullOrEmpty(result)) return;

            if (result.Contains(".") || result.Contains(","))
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Введите целое число");
                return;
            }

            if (!_logicControllerProvider.Connected)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Нет подключения к ПЛК");
                return;
            }

            if (uint.TryParse(result, out uint intResult))
            {
                await _logicControllerProvider.SetUInt32Async(command, intResult);
                setter(intResult);
                await CustomMessageBox.ShowSuccessAsync("Успешно", "Значение установлено");
            }
            else
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Вы ввели некорректное значение");
            }
        }

        private async void SelectFloatValue(string title, float currentValue, CommandInfo command, Action<float> setter)
        {
            string result = _keyboardService.Show(KeyboardType.Numpad, title, currentValue.ToString("F2"));
            if (string.IsNullOrEmpty(result)) return;

            // Replace comma with dot for parsing
            result = result.Replace(",", ".");

            if (!_logicControllerProvider.Connected)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Нет подключения к ПЛК");
                return;
            }

            if (float.TryParse(result, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatResult))
            {
                await _logicControllerProvider.SetFloatAsync(command, floatResult);
                setter(floatResult);
            }
            else
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Вы ввели некорректное значение");
            }
        }

        private async Task SetBoolValueAsync(CommandInfo command, bool value)
        {
            if (!_logicControllerProvider.Connected)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Нет подключения к ПЛК");
                return;
            }

            await _logicControllerProvider.SetBoolAsync(command, value);
        }

        private void LoadLocalSettings()
        {
            var settings = Bootstrapper.Configuration.Get<AutomaticProcessSettings>();
            _readyGasSystemCheck = settings.ReadyGasSystemCheck;
            _readyLaserSystemCheck = settings.ReadyLaserSystemCheck;
            _readyHeatingTableCheck = settings.ReadyHeatingTableCheck;
            _notErrorsBlockingPrintingCheck = settings.NotErrorsBlockingPrintingCheck;
            _recoaterRefErrorCheck = settings.RecoaterRefErrorCheck;
            _recoaterEngineErrorCheck = settings.RecoaterEngineErrorCheck;
            _doserAxesErrorCheck = settings.DoserAxesErrorCheck;
            _platformAxesErrorCheck = settings.PlatformAxesErrorCheck;
        }
    }
}
