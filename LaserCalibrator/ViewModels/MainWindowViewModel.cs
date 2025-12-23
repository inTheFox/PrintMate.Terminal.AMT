using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Hans.NET.Models;
using LaserCalibrator.Services;
using Microsoft.Win32;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;

namespace LaserCalibrator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _configFilePath = "Не загружен";
        private List<ScanatorConfiguration>? _configurations;

        // Сервисы сканаторов
        private ScannerService? _scanner1;
        private ScannerService? _scanner2;

        // IP адреса
        private string _laser1IpAddress = "172.18.34.227";
        private string _laser2IpAddress = "172.18.34.228";

        // Смещения
        private double _laser1OffsetX = 0.0;
        private double _laser1OffsetY = 104.977;
        private double _laser2OffsetX = 0.0;
        private double _laser2OffsetY = -104.977;

        // Размеры полей (для визуализации)
        private double _laser1FieldSizeX = 400.0;
        private double _laser1FieldSizeY = 400.0;
        private double _laser2FieldSizeX = 448.0;
        private double _laser2FieldSizeY = 448.0;

        // Целевая позиция (общая)
        private double _targetX = 0.0;
        private double _targetY = 0.0;

        // Индивидуальные целевые позиции лазеров
        private double _laser1TargetX = 0.0;
        private double _laser1TargetY = 0.0;
        private double _laser1TargetZ = 0.0;
        private double _laser2TargetX = 0.0;
        private double _laser2TargetY = 0.0;
        private double _laser2TargetZ = 0.0;


        // Текущие позиции точек лазеров
        private double _laser1CurrentX = 0.0;
        private double _laser1CurrentY = 0.0;
        private double _laser2CurrentX = 0.0;
        private double _laser2CurrentY = 0.0;

        // Состояние красного лазера
        private bool _laser1GuideLaserOn;
        private bool _laser2GuideLaserOn;

        // Шаг корректировки
        private double _adjustStep = 0.1;

        // Статусы
        private SolidColorBrush _laser1StatusColor = new(Colors.Gray);
        private SolidColorBrush _laser2StatusColor = new(Colors.Gray);
        private string _statusInfo = "Готов к работе";
        private string _coordinatesInfo = "";

        // Таймер опроса позиции
        private DispatcherTimer? _positionPollTimer;

        public MainWindowViewModel()
        {
            // Команды конфигурации
            LoadConfigCommand = new DelegateCommand(LoadConfig);
            SaveConfigCommand = new DelegateCommand(SaveConfig, () => _configurations != null);

            // Команды подключения
            ConnectCommand = new DelegateCommand(Connect);
            DisconnectCommand = new DelegateCommand(Disconnect);

            // Команды красного лазера
            ToggleGuideLasersCommand = new DelegateCommand(ToggleGuideLasers);

            // Команды позиционирования
            MoveToTargetCommand = new DelegateCommand(MoveToTarget);
            MoveToCenterCommand = new DelegateCommand(MoveToCenter);
            ApplyOffsetsCommand = new DelegateCommand(ApplyOffsets);

            // Команды быстрой калибровки
            Laser1AdjustLeftCommand = new DelegateCommand(() => AdjustLaser1Offset(-AdjustStep, 0));
            Laser1AdjustRightCommand = new DelegateCommand(() => AdjustLaser1Offset(AdjustStep, 0));
            Laser1AdjustUpCommand = new DelegateCommand(() => AdjustLaser1Offset(0, AdjustStep));
            Laser1AdjustDownCommand = new DelegateCommand(() => AdjustLaser1Offset(0, -AdjustStep));

            Laser2AdjustLeftCommand = new DelegateCommand(() => AdjustLaser2Offset(-AdjustStep, 0));
            Laser2AdjustRightCommand = new DelegateCommand(() => AdjustLaser2Offset(AdjustStep, 0));
            Laser2AdjustUpCommand = new DelegateCommand(() => AdjustLaser2Offset(0, AdjustStep));
            Laser2AdjustDownCommand = new DelegateCommand(() => AdjustLaser2Offset(0, -AdjustStep));

            // Команды индивидуального перемещения
            MoveLaser1Command = new DelegateCommand(MoveLaser1);
            MoveLaser2Command = new DelegateCommand(MoveLaser2);

            UpdateCoordinatesInfo();
        }

        #region Properties

        public string ConfigFilePath
        {
            get => _configFilePath;
            set => SetProperty(ref _configFilePath, value);
        }

        public string Laser1IpAddress
        {
            get => _laser1IpAddress;
            set => SetProperty(ref _laser1IpAddress, value);
        }

        public string Laser2IpAddress
        {
            get => _laser2IpAddress;
            set => SetProperty(ref _laser2IpAddress, value);
        }

        public double Laser1OffsetX
        {
            get => _laser1OffsetX;
            set
            {
                if (SetProperty(ref _laser1OffsetX, value))
                    UpdateCoordinatesInfo();
            }
        }

        public double Laser1OffsetY
        {
            get => _laser1OffsetY;
            set
            {
                if (SetProperty(ref _laser1OffsetY, value))
                    UpdateCoordinatesInfo();
            }
        }

        public double Laser2OffsetX
        {
            get => _laser2OffsetX;
            set
            {
                if (SetProperty(ref _laser2OffsetX, value))
                    UpdateCoordinatesInfo();
            }
        }

        public double Laser2OffsetY
        {
            get => _laser2OffsetY;
            set
            {
                if (SetProperty(ref _laser2OffsetY, value))
                    UpdateCoordinatesInfo();
            }
        }

        public double Laser1FieldSizeX
        {
            get => _laser1FieldSizeX;
            set => SetProperty(ref _laser1FieldSizeX, value);
        }

        public double Laser1FieldSizeY
        {
            get => _laser1FieldSizeY;
            set => SetProperty(ref _laser1FieldSizeY, value);
        }

        public double Laser2FieldSizeX
        {
            get => _laser2FieldSizeX;
            set => SetProperty(ref _laser2FieldSizeX, value);
        }

        public double Laser2FieldSizeY
        {
            get => _laser2FieldSizeY;
            set => SetProperty(ref _laser2FieldSizeY, value);
        }

        public double TargetX
        {
            get => _targetX;
            set
            {
                if (SetProperty(ref _targetX, value))
                    UpdateCoordinatesInfo();
            }
        }

        public double TargetY
        {
            get => _targetY;
            set
            {
                if (SetProperty(ref _targetY, value))
                    UpdateCoordinatesInfo();
            }
        }

        public double Laser1TargetX
        {
            get => _laser1TargetX;
            set => SetProperty(ref _laser1TargetX, value);
        }

        public double Laser1TargetY
        {
            get => _laser1TargetY;
            set => SetProperty(ref _laser1TargetY, value);
        }

        public double Laser1TargetZ
        {
            get => _laser1TargetZ;
            set => SetProperty(ref _laser1TargetZ, value);
        }

        public double Laser2TargetX
        {
            get => _laser2TargetX;
            set => SetProperty(ref _laser2TargetX, value);
        }

        public double Laser2TargetY
        {
            get => _laser2TargetY;
            set => SetProperty(ref _laser2TargetY, value);
        }

        public double Laser2TargetZ
        {
            get => _laser2TargetZ;
            set => SetProperty(ref _laser2TargetZ, value);
        }

        public double Laser1CurrentX
        {
            get => _laser1CurrentX;
            set => SetProperty(ref _laser1CurrentX, value);
        }

        public double Laser1CurrentY
        {
            get => _laser1CurrentY;
            set => SetProperty(ref _laser1CurrentY, value);
        }

        public double Laser2CurrentX
        {
            get => _laser2CurrentX;
            set => SetProperty(ref _laser2CurrentX, value);
        }

        public double Laser2CurrentY
        {
            get => _laser2CurrentY;
            set => SetProperty(ref _laser2CurrentY, value);
        }

        public bool Laser1GuideLaserOn
        {
            get => _laser1GuideLaserOn;
            set
            {
                if (SetProperty(ref _laser1GuideLaserOn, value))
                {
                    _scanner1?.SetGuideLaser(value);
                    RaisePropertyChanged(nameof(GuideLasersButtonText));
                }
            }
        }

        public bool Laser2GuideLaserOn
        {
            get => _laser2GuideLaserOn;
            set
            {
                if (SetProperty(ref _laser2GuideLaserOn, value))
                {
                    _scanner2?.SetGuideLaser(value);
                    RaisePropertyChanged(nameof(GuideLasersButtonText));
                }
            }
        }

        public string GuideLasersButtonText =>
            (Laser1GuideLaserOn || Laser2GuideLaserOn) ? "Выключить все" : "Включить все";

        public double AdjustStep
        {
            get => _adjustStep;
            set => SetProperty(ref _adjustStep, value);
        }

        public SolidColorBrush Laser1StatusColor
        {
            get => _laser1StatusColor;
            set => SetProperty(ref _laser1StatusColor, value);
        }

        public SolidColorBrush Laser2StatusColor
        {
            get => _laser2StatusColor;
            set => SetProperty(ref _laser2StatusColor, value);
        }

        public string StatusInfo
        {
            get => _statusInfo;
            set => SetProperty(ref _statusInfo, value);
        }

        public string CoordinatesInfo
        {
            get => _coordinatesInfo;
            set => SetProperty(ref _coordinatesInfo, value);
        }

        #endregion

        #region Commands

        public ICommand LoadConfigCommand { get; }
        public ICommand SaveConfigCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand ToggleGuideLasersCommand { get; }
        public ICommand MoveToTargetCommand { get; }
        public ICommand MoveToCenterCommand { get; }
        public ICommand ApplyOffsetsCommand { get; }

        public ICommand Laser1AdjustLeftCommand { get; }
        public ICommand Laser1AdjustRightCommand { get; }
        public ICommand Laser1AdjustUpCommand { get; }
        public ICommand Laser1AdjustDownCommand { get; }

        public ICommand Laser2AdjustLeftCommand { get; }
        public ICommand Laser2AdjustRightCommand { get; }
        public ICommand Laser2AdjustUpCommand { get; }
        public ICommand Laser2AdjustDownCommand { get; }

        public ICommand MoveLaser1Command { get; }
        public ICommand MoveLaser2Command { get; }

        #endregion

        #region Methods

        private void LoadConfig()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                Title = "Выберите файл конфигурации сканаторов"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dialog.FileName);
                    _configurations = JsonConvert.DeserializeObject<List<ScanatorConfiguration>>(json);

                    if (_configurations != null && _configurations.Count >= 2)
                    {
                        ConfigFilePath = dialog.FileName;
                        LoadFromConfigurations();
                        ((DelegateCommand)SaveConfigCommand).RaiseCanExecuteChanged();
                        StatusInfo = "Конфигурация загружена";
                    }
                    else
                    {
                        HandyControl.Controls.MessageBox.Error(
                            "Файл должен содержать конфигурации для двух сканаторов.",
                            "Ошибка загрузки");
                    }
                }
                catch (Exception ex)
                {
                    HandyControl.Controls.MessageBox.Error(
                        $"Ошибка при загрузке файла:\n{ex.Message}",
                        "Ошибка загрузки");
                }
            }
        }

        private void LoadFromConfigurations()
        {
            if (_configurations == null || _configurations.Count < 2) return;

            var config1 = _configurations[0];
            var config2 = _configurations[1];

            Laser1IpAddress = config1.CardInfo.IpAddress;
            Laser1OffsetX = config1.ScannerConfig.OffsetX;
            Laser1OffsetY = config1.ScannerConfig.OffsetY;
            Laser1FieldSizeX = config1.ScannerConfig.FieldSizeX;
            Laser1FieldSizeY = config1.ScannerConfig.FieldSizeY;

            Laser2IpAddress = config2.CardInfo.IpAddress;
            Laser2OffsetX = config2.ScannerConfig.OffsetX;
            Laser2OffsetY = config2.ScannerConfig.OffsetY;
            Laser2FieldSizeX = config2.ScannerConfig.FieldSizeX;
            Laser2FieldSizeY = config2.ScannerConfig.FieldSizeY;
        }

        private void SaveConfig()
        {
            if (_configurations == null || _configurations.Count < 2) return;

            var dialog = new SaveFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                Title = "Сохранить конфигурацию",
                FileName = Path.GetFileName(ConfigFilePath)
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Обновляем конфигурации
                    _configurations[0].CardInfo.IpAddress = Laser1IpAddress;
                    _configurations[0].ScannerConfig.OffsetX = (float)Laser1OffsetX;
                    _configurations[0].ScannerConfig.OffsetY = (float)Laser1OffsetY;

                    _configurations[1].CardInfo.IpAddress = Laser2IpAddress;
                    _configurations[1].ScannerConfig.OffsetX = (float)Laser2OffsetX;
                    _configurations[1].ScannerConfig.OffsetY = (float)Laser2OffsetY;

                    string json = JsonConvert.SerializeObject(_configurations, Formatting.Indented);
                    File.WriteAllText(dialog.FileName, json);

                    ConfigFilePath = dialog.FileName;
                    StatusInfo = "Конфигурация сохранена";

                    HandyControl.Controls.MessageBox.Success(
                        "Конфигурация успешно сохранена!",
                        "Сохранение");
                }
                catch (Exception ex)
                {
                    HandyControl.Controls.MessageBox.Error(
                        $"Ошибка при сохранении:\n{ex.Message}",
                        "Ошибка");
                }
            }
        }

        private void Connect()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ========== CONNECT STARTED ==========");

            try
            {
                StatusInfo = "Подключение...";
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Creating ScannerService instances...");

                // Создаём сервисы если нужно
                if (_scanner1 == null)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Creating Scanner1 service...");
                    _scanner1 = new ScannerService();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Scanner1 service created");
                }

                if (_scanner2 == null)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Creating Scanner2 service...");
                    _scanner2 = new ScannerService();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Scanner2 service created");
                }

                // Подписываемся на события
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Subscribing to events...");
                _scanner1.OnStatusChanged += s =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Scanner1 status: {s}");
                    StatusInfo = $"Лазер 1: {s}";
                };
                _scanner2.OnStatusChanged += s =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Scanner2 status: {s}");
                    StatusInfo = $"Лазер 2: {s}";
                };

                // Подключаемся
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connecting to Scanner1 at {Laser1IpAddress}...");
                bool connected1 = _scanner1.Connect(Laser1IpAddress);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Scanner1 connection result: {connected1}");

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connecting to Scanner2 at {Laser2IpAddress}...");
                bool connected2 = _scanner2.Connect(Laser2IpAddress);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Scanner2 connection result: {connected2}");

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Updating status colors...");
                Laser1StatusColor = new SolidColorBrush(connected1 ? Colors.LimeGreen : Colors.Red);
                Laser2StatusColor = new SolidColorBrush(connected2 ? Colors.LimeGreen : Colors.Red);

                if (connected1 && connected2)
                {
                    StatusInfo = "Оба сканатора подключены";
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Both scanners connected, moving to target...");
                    // Перемещаем в начальную позицию
                    MoveToTarget();
                }
                else
                {
                    StatusInfo = $"Подключено: Л1={connected1}, Л2={connected2}";
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Partial connection: L1={connected1}, L2={connected2}");
                }

                // Запускаем таймер опроса позиции
                if (connected1 || connected2)
                {
                    StartPositionPolling();
                }

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ========== CONNECT FINISHED ==========");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] !!! CONNECT EXCEPTION !!!");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Exception type: {ex.GetType().Name}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Message: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] InnerException: {ex.InnerException.Message}");
                }
                StatusInfo = $"Ошибка: {ex.Message}";
            }
        }

        private void Disconnect()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ========== DISCONNECT STARTED ==========");

            try
            {
                // Останавливаем опрос позиции
                StopPositionPolling();

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Turning off guide lasers...");
                _scanner1?.SetGuideLaser(false);
                _scanner2?.SetGuideLaser(false);

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Disconnecting scanners...");
                _scanner1?.Disconnect();
                _scanner2?.Disconnect();

                Laser1GuideLaserOn = false;
                Laser2GuideLaserOn = false;

                Laser1StatusColor = new SolidColorBrush(Colors.Gray);
                Laser2StatusColor = new SolidColorBrush(Colors.Gray);

                StatusInfo = "Отключено";
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ========== DISCONNECT FINISHED ==========");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] !!! DISCONNECT EXCEPTION: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] StackTrace: {ex.StackTrace}");
            }
        }

        private void ToggleGuideLasers()
        {
            bool newState = !(Laser1GuideLaserOn || Laser2GuideLaserOn);
            Laser1GuideLaserOn = newState;
            Laser2GuideLaserOn = newState;
        }

        private void MoveToTarget()
        {
            if (_scanner1?.IsConnected == true)
            {
                _scanner1.JumpTo((float)TargetX, (float)TargetY);
                Laser1CurrentX = TargetX;
                Laser1CurrentY = TargetY;
            }

            if (_scanner2?.IsConnected == true)
            {
                _scanner2.JumpTo((float)TargetX, (float)TargetY);
                Laser2CurrentX = TargetX;
                Laser2CurrentY = TargetY;
            }

            UpdateCoordinatesInfo();
            StatusInfo = $"Перемещено в ({TargetX:F3}, {TargetY:F3})";
        }

        private void MoveToCenter()
        {
            TargetX = 0;
            TargetY = 0;
            MoveToTarget();
        }

        private void MoveLaser1()
        {
            if (_scanner1?.IsConnected == true)
            {
                _scanner1.JumpTo((float)Laser1TargetX, (float)Laser1TargetY, (float)Laser1TargetZ);
                Laser1CurrentX = Laser1TargetX;
                Laser1CurrentY = Laser1TargetY;
                UpdateCoordinatesInfo();
                StatusInfo = $"Лазер 1 перемещён в ({Laser1TargetX:F3}, {Laser1TargetY:F3})";
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Laser1 moved to ({Laser1TargetX:F3}, {Laser1TargetY:F3})");
            }
            else
            {
                StatusInfo = "Лазер 1 не подключен";
            }
        }

        private void MoveLaser2()
        {
            if (_scanner2?.IsConnected == true)
            {
                _scanner2.JumpTo((float)Laser2TargetX, (float)Laser2TargetY, (float)Laser2TargetZ);
                Laser2CurrentX = Laser2TargetX;
                Laser2CurrentY = Laser2TargetY;
                UpdateCoordinatesInfo();
                StatusInfo = $"Лазер 2 перемещён в ({Laser2TargetX:F3}, {Laser2TargetY:F3})";
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Laser2 moved to ({Laser2TargetX:F3}, {Laser2TargetY:F3})");
            }
            else
            {
                StatusInfo = "Лазер 2 не подключен";
            }
        }

        private void ApplyOffsets()
        {
            if (_scanner1?.IsConnected == true)
            {
                _scanner1.SetOffset((float)Laser1OffsetX, (float)Laser1OffsetY);
            }

            if (_scanner2?.IsConnected == true)
            {
                _scanner2.SetOffset((float)Laser2OffsetX, (float)Laser2OffsetY);
            }

            StatusInfo = "Смещения применены";

            // Перемещаем в текущую целевую позицию для проверки
            MoveToTarget();
        }

        private void AdjustLaser1Offset(double dx, double dy)
        {
            Laser1OffsetX += dx;
            Laser1OffsetY += dy;

            if (_scanner1?.IsConnected == true)
            {
                _scanner1.SetOffset((float)Laser1OffsetX, (float)Laser1OffsetY);
                _scanner1.JumpTo((float)TargetX, (float)TargetY);
                Laser1CurrentX = TargetX;
                Laser1CurrentY = TargetY;
            }

            UpdateCoordinatesInfo();
            StatusInfo = $"Л1 Offset: ({Laser1OffsetX:F3}, {Laser1OffsetY:F3})";
        }

        private void AdjustLaser2Offset(double dx, double dy)
        {
            Laser2OffsetX += dx;
            Laser2OffsetY += dy;

            if (_scanner2?.IsConnected == true)
            {
                _scanner2.SetOffset((float)Laser2OffsetX, (float)Laser2OffsetY);
                _scanner2.JumpTo((float)TargetX, (float)TargetY);
                Laser2CurrentX = TargetX;
                Laser2CurrentY = TargetY;
            }

            UpdateCoordinatesInfo();
            StatusInfo = $"Л2 Offset: ({Laser2OffsetX:F3}, {Laser2OffsetY:F3})";
        }

        private void UpdateCoordinatesInfo()
        {
            var info = $"Цель: ({TargetX:F3}, {TargetY:F3}) мм\n";
            info += $"Л1: позиция ({Laser1CurrentX:F3}, {Laser1CurrentY:F3}), offset ({Laser1OffsetX:F3}, {Laser1OffsetY:F3})\n";
            info += $"Л2: позиция ({Laser2CurrentX:F3}, {Laser2CurrentY:F3}), offset ({Laser2OffsetX:F3}, {Laser2OffsetY:F3})\n";

            // Глобальные позиции лазеров (позиция + offset)
            double laser1GlobalX = Laser1CurrentX + Laser1OffsetX;
            double laser1GlobalY = Laser1CurrentY + Laser1OffsetY;
            double laser2GlobalX = Laser2CurrentX + Laser2OffsetX;
            double laser2GlobalY = Laser2CurrentY + Laser2OffsetY;

            // Расстояние между точками лазеров
            double pointDistance = Math.Sqrt(
                Math.Pow(laser1GlobalX - laser2GlobalX, 2) +
                Math.Pow(laser1GlobalY - laser2GlobalY, 2));

            info += $"Расстояние между точками: {pointDistance:F3} мм";

            CoordinatesInfo = info;
        }

        #region Position Polling

        private void StartPositionPolling()
        {
            if (_positionPollTimer != null) return;

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Starting position polling timer (100ms interval)");

            _positionPollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _positionPollTimer.Tick += PositionPollTimer_Tick;
            _positionPollTimer.Start();
        }

        private void StopPositionPolling()
        {
            if (_positionPollTimer == null) return;

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Stopping position polling timer");

            _positionPollTimer.Stop();
            _positionPollTimer.Tick -= PositionPollTimer_Tick;
            _positionPollTimer = null;
        }

        private void PositionPollTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                bool updated = false;

                // Опрашиваем позицию сканатора 1
                if (_scanner1?.IsConnected == true)
                {
                    var (fbX1, fbY1) = _scanner1.GetFeedbackPosition();
                    // Конвертируем из единиц сканатора в мм (feedback возвращает short)
                    // Предполагаем, что feedback уже в нужных единицах или требует масштабирования
                    double newX = _scanner1.CurrentX;
                    double newY = _scanner1.CurrentY;

                    if (Math.Abs(Laser1CurrentX - newX) > 0.001 || Math.Abs(Laser1CurrentY - newY) > 0.001)
                    {
                        Laser1CurrentX = newX;
                        Laser1CurrentY = newY;
                        updated = true;
                    }
                }

                // Опрашиваем позицию сканатора 2
                if (_scanner2?.IsConnected == true)
                {
                    var (fbX2, fbY2) = _scanner2.GetFeedbackPosition();
                    double newX = _scanner2.CurrentX;
                    double newY = _scanner2.CurrentY;

                    if (Math.Abs(Laser2CurrentX - newX) > 0.001 || Math.Abs(Laser2CurrentY - newY) > 0.001)
                    {
                        Laser2CurrentX = newX;
                        Laser2CurrentY = newY;
                        updated = true;
                    }
                }

                if (updated)
                {
                    UpdateCoordinatesInfo();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Position poll error: {ex.Message}");
            }
        }

        #endregion

        #endregion
    }
}
