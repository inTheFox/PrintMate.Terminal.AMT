//using Opc2Lib;
//using PrintMate.Terminal.Events;
//using PrintMate.Terminal.Opc;
//using PrintMate.Terminal.Services;
//using Prism.Events;
//using Prism.Mvvm;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Documents;
//using System.Windows.Threading;

//namespace PrintMate.Terminal.ViewModels
//{
//    public class IndicatorForMonitoringViewModel : BindableBase
//    {
//        private const string EnablePath = "/images/indicator_green_32.png";
//        private const string DisablePath = "/images/indicator_red_32.png";
//        private const string EnablePathForSaved = "/images/saved.png";
//        private const string DisablePathForSaved = "/images/unsaved.png";


//        private CommandInfo _commandInfo;
//        private string _filePath = DisablePath;
//        private Visibility _boolBlockVisibility = Visibility.Collapsed;
//        private Visibility _decimalBlockVisibility = Visibility.Collapsed;
//        private float _value = 0.00f;
//        private string saveCommandImagePath = DisablePathForSaved;


//        private double _currentValue;
//        public double CurrentValue
//        {
//            get => _currentValue;
//            set
//            {
//                if (SetProperty(ref _currentValue, value))
//                {
//                    //AnimateValueTo(valueCommand);
//                }
//            }
//        }

//        private double _displayedOpacity = 1.0; // Начальное значение
//        public double DisplayedOpacity
//        {
//            get => _displayedOpacity;
//            private set => SetProperty(ref _displayedOpacity, value);
//        }

//        private double _displayedValue;
//        public double DisplayedValue
//        {
//            get => _displayedValue;
//            private set => SetProperty(ref _displayedValue, value);
//        }

//        public CommandInfo CommandInfo
//        {
//            get => _commandInfo;
//            set => SetProperty(ref _commandInfo, value);
//        }
//        public float Value
//        {
//            get => _value;
//            set => SetProperty(ref _value, value);
//        }
//        public string FilePath
//        {
//            get => _filePath;
//            set => SetProperty(ref _filePath, value);
//        }
//        public string SaveCommandImagePath
//        {
//            get => saveCommandImagePath;
//            set => SetProperty(ref saveCommandImagePath, value);
//        }
//        public Visibility BoolBlockVisibility
//        {
//            get => _boolBlockVisibility;
//            set => SetProperty(ref _boolBlockVisibility, value);
//        }
//        public Visibility DecimalBlockVisibility
//        {
//            get => _decimalBlockVisibility;
//            set => SetProperty(ref _decimalBlockVisibility, value);
//        }

//        private CommandId _commandId;
//        private readonly CommandProvider _commandProvider;

//        private readonly ILogicControllerProvider _logicControllerService;
//        private readonly MonitoringManager _monitoringManager;
//        private readonly IEventAggregator _eventAggregator;
//        private readonly ILogicControllerObserver _observer;

//        private readonly DispatcherTimer _animationTimer = new DispatcherTimer(DispatcherPriority.Render);

//        private double _targetValue;
//        private double _startValue;
//        private double _animationProgress; // 0.0 to 1.0
//        private DateTime _animationStartTime;
//        private TimeSpan _animationDuration;

//        public IndicatorForMonitoringViewModel(ILogicControllerProvider logicControllerService, ILogicControllerObserver observer,  MonitoringManager monitoringManager, IEventAggregator eventAggregator)
//        {
//            _observer = observer;
//            _monitoringManager = monitoringManager;
//            _logicControllerService = logicControllerService;
//            _commandProvider = new CommandProvider();
//            _eventAggregator = eventAggregator;


//            _animationTimer.Interval = TimeSpan.FromMilliseconds(8); // ~60 FPS
//            _animationTimer.Tick += OnAnimationTick;
//            // Инициализация начального значения
//            CurrentValue = 0;
//            DisplayedValue = 0;

//            _eventAggregator.GetEvent<OnCommandAddToFavouritesEvent>().Subscribe((command) =>
//            {
//                if (command != CommandInfo) return;
//                SaveCommandImagePath = EnablePathForSaved;
//            });
//            _eventAggregator.GetEvent<OnCommandRemoveFromFavouritesEvent>().Subscribe((command) =>
//            {
//                if (command != CommandInfo) return;
//                SaveCommandImagePath = DisablePathForSaved;
//            });

//#if RELEASE
//            _observer.Subscribe(this, (data) =>
//            {
//                Application.Current.Dispatcher.InvokeAsync(() =>
//                {
//                    switch (CommandInfo.ValueCommandType)
//                    {
//                        case ValueCommandType.Real:
//                            Application.Current.Dispatcher.InvokeAsync(() =>
//                                DisplayedValue = (float)data.Value);
//                            break;
//                        case ValueCommandType.Dint:
//                            Application.Current.Dispatcher.InvokeAsync(() =>
//                                DisplayedValue = (int)data.Value);
//                            break;
//                        case ValueCommandType.Unsigned:
//                            Application.Current.Dispatcher.InvokeAsync(() =>
//                                DisplayedValue = (ushort)data.Value);
//                            break;
//                        case ValueCommandType.Bool:
//                            bool current = (bool)data.Value;

//                            if (current)
//                            {
//                                FilePath = EnablePath;
//                            }
//                            else
//                            {
//                                FilePath = DisablePath;
//                            }

//                            break;
//                    }
//                });
//            }, CommandInfo);
//#endif
//        }

//        public void Start(CommandInfo commandInfo)
//        {
//            if (commandInfo == null) return;
//            CommandInfo = commandInfo;

//            if (_monitoringManager.IsCommandInFavourites(CommandInfo))
//            {
//                SaveCommandImagePath = EnablePathForSaved;
//            }
//            else
//            {
//                SaveCommandImagePath = DisablePathForSaved;
//            }

//            if (CommandInfo.ValueCommandType == ValueCommandType.Bool)
//            {
//                BoolBlockVisibility = Visibility.Visible;
//                DecimalBlockVisibility = Visibility.Collapsed;
//            }
//            else
//            {
//                BoolBlockVisibility = Visibility.Collapsed;
//                DecimalBlockVisibility = Visibility.Visible;
//                InitValue();
//            }
//        }

//        private void InitValue()
//        {
//#if RELEASE

//            switch (CommandInfo.ValueCommandType)
//            {
//                case ValueCommandType.Real:
//                    Application.Current.Dispatcher.InvokeAsync(async () =>
//                        DisplayedValue = await _logicControllerService.GetFloatAsync(CommandInfo));
//                    break;
//                case ValueCommandType.Dint:
//                    Application.Current.Dispatcher.InvokeAsync(async () =>
//                        DisplayedValue = await _logicControllerService.GetInt32Async(CommandInfo));
//                    break;
//                case ValueCommandType.Unsigned:
//                    Application.Current.Dispatcher.InvokeAsync(async () =>
//                        DisplayedValue = await _logicControllerService.GetUInt16Async(CommandInfo));
//                    break;
//            }
//#endif
//        }
//    }
//}
