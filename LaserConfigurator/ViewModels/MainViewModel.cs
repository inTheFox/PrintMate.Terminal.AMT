using Hans.NET.Models;
using LaserConfigurator.Events;
using LaserConfigurator.Models;
using LaserConfigurator.Services;
using Microsoft.Win32;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HandyControl.Tools.Command;
using DelegateCommand = Prism.Commands.DelegateCommand;

namespace LaserConfigurator.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private readonly IConfigurationService _configService;
        private readonly HansService _hansService;
        private readonly IGeometryService _geometryService;
        private readonly IUdmService _udmService;
        private readonly IEventAggregator _eventAggregator;

        #region Properties

        // Обнаруженные сканаторы
        private ObservableCollection<HansDeviceState> _discoveredScanners = new ObservableCollection<HansDeviceState>();
        public ObservableCollection<HansDeviceState> DiscoveredScanners
        {
            get => _discoveredScanners;
            set => SetProperty(ref _discoveredScanners, value);
        }

        private string _connectionStatusText = "Не подключено";
        public string ConnectionStatusText
        {
            get => _connectionStatusText;
            set => SetProperty(ref _connectionStatusText, value);
        }

        // Shape parameters
        private int _selectedShapeTypeIndex = 0;
        public int SelectedShapeTypeIndex
        {
            get => _selectedShapeTypeIndex;
            set => SetProperty(ref _selectedShapeTypeIndex, value);
        }

        private double _shapeX = 0;
        public double ShapeX
        {
            get => _shapeX;
            set => SetProperty(ref _shapeX, value);
        }

        private double _shapeY = 0;
        public double ShapeY
        {
            get => _shapeY;
            set => SetProperty(ref _shapeY, value);
        }

        private double _shapeSize = 20;
        public double ShapeSize
        {
            get => _shapeSize;
            set => SetProperty(ref _shapeSize, value);
        }

        private double _markSpeed = 1000;
        public double MarkSpeed
        {
            get => _markSpeed;
            set => SetProperty(ref _markSpeed, value);
        }

        private double _laserPower = 100;
        public double LaserPower
        {
            get => _laserPower;
            set => SetProperty(ref _laserPower, value);
        }

        private double _beamDiameter = 80;
        public double BeamDiameter
        {
            get => _beamDiameter;
            set => SetProperty(ref _beamDiameter, value);
        }

        private bool _splitBetweenLasers = false;
        public bool SplitBetweenLasers
        {
            get => _splitBetweenLasers;
            set => SetProperty(ref _splitBetweenLasers, value);
        }

        // Выбор сканатора для одиночного режима
        private int _selectedScannerIndex = 0;
        public int SelectedScannerIndex
        {
            get => _selectedScannerIndex;
            set => SetProperty(ref _selectedScannerIndex, value);
        }

        // Scanner 1 configuration - visual editing
        private ScanatorConfiguration _scanner1Config;
        public ScanatorConfiguration Scanner1Config
        {
            get => _scanner1Config;
            set => SetProperty(ref _scanner1Config, value);
        }

        // Scanner 2 configuration - visual editing
        private ScanatorConfiguration _scanner2Config;
        public ScanatorConfiguration Scanner2Config
        {
            get => _scanner2Config;
            set => SetProperty(ref _scanner2Config, value);
        }

        // Observable collections for MarkSpeed profiles
        private ObservableCollection<ProcessVariables> _scanner1MarkSpeedProfiles;
        public ObservableCollection<ProcessVariables> Scanner1MarkSpeedProfiles
        {
            get => _scanner1MarkSpeedProfiles;
            set => SetProperty(ref _scanner1MarkSpeedProfiles, value);
        }

        private ObservableCollection<ProcessVariables> _scanner2MarkSpeedProfiles;
        public ObservableCollection<ProcessVariables> Scanner2MarkSpeedProfiles
        {
            get => _scanner2MarkSpeedProfiles;
            set => SetProperty(ref _scanner2MarkSpeedProfiles, value);
        }

        #endregion

        #region Commands
        public RelayCommand<HansDeviceState> ConnectButtonCommand { get; set; }
        public RelayCommand<HansDeviceState> DisconnectButtonCommand { get; set; }
        public DelegateCommand LoadConfigCommand { get; }
        public DelegateCommand SaveConfigCommand { get; }
        public DelegateCommand ApplyConfigCommand { get; }
        public DelegateCommand ConnectScanner1Command { get; }
        public DelegateCommand ConnectScanner2Command { get; }
        public DelegateCommand DrawShapeCommand { get; }
        public DelegateCommand ClearPreviewCommand { get; }

        // Commands for MarkSpeed profile management
        public DelegateCommand AddMarkSpeedProfile1Command { get; }
        public Prism.Commands.DelegateCommand<ProcessVariables> EditMarkSpeedProfile1Command { get; }
        public Prism.Commands.DelegateCommand<ProcessVariables> DeleteMarkSpeedProfile1Command { get; }
        public DelegateCommand AddMarkSpeedProfile2Command { get; }
        public Prism.Commands.DelegateCommand<ProcessVariables> EditMarkSpeedProfile2Command { get; }
        public Prism.Commands.DelegateCommand<ProcessVariables> DeleteMarkSpeedProfile2Command { get; }

        #endregion

        public MainViewModel(
            IConfigurationService configService,
            HansService hansService,
            IGeometryService geometryService,
            IUdmService udmService,
            IEventAggregator eventAggregator)
        {
            _configService = configService;
            _hansService = hansService;
            _geometryService = geometryService;
            _udmService = udmService;
            _eventAggregator = eventAggregator;

            // Initialize commands
            LoadConfigCommand = new DelegateCommand(async () => await LoadConfigAsync());
            SaveConfigCommand = new DelegateCommand(async () => await SaveConfigAsync());
            ApplyConfigCommand = new DelegateCommand(ApplyConfig);
            ConnectScanner1Command = new DelegateCommand(ConnectScanner1);
            ConnectScanner2Command = new DelegateCommand(ConnectScanner2);
            DrawShapeCommand = new DelegateCommand(async () => await DrawShapeAsync());
            ClearPreviewCommand = new DelegateCommand(ClearPreview);
            ConnectButtonCommand = new RelayCommand<HansDeviceState>(ConnectButtonCommandCallback);
            DisconnectButtonCommand = new RelayCommand<HansDeviceState>(DisconnectButtonCommandCallback);

            // Initialize MarkSpeed profile commands
            AddMarkSpeedProfile1Command = new DelegateCommand(() => AddMarkSpeedProfile(1));
            EditMarkSpeedProfile1Command = new Prism.Commands.DelegateCommand<ProcessVariables>(profile => EditMarkSpeedProfile(1, profile));
            DeleteMarkSpeedProfile1Command = new Prism.Commands.DelegateCommand<ProcessVariables>(profile => DeleteMarkSpeedProfile(1, profile));
            AddMarkSpeedProfile2Command = new DelegateCommand(() => AddMarkSpeedProfile(2));
            EditMarkSpeedProfile2Command = new Prism.Commands.DelegateCommand<ProcessVariables>(profile => EditMarkSpeedProfile(2, profile));
            DeleteMarkSpeedProfile2Command = new Prism.Commands.DelegateCommand<ProcessVariables>(profile => DeleteMarkSpeedProfile(2, profile));

            // Initialize collections
            Scanner1MarkSpeedProfiles = new ObservableCollection<ProcessVariables>();
            Scanner2MarkSpeedProfiles = new ObservableCollection<ProcessVariables>();

            // Subscribe to scanner status events
            _eventAggregator.GetEvent<OnScanatorStatusChanged>().Subscribe(OnScannerStateChanged, ThreadOption.UIThread);

            // Load default configuration
            LoadConfigurationToUI();
        }

        private void ConnectCommand(object obj)
        {
            throw new NotImplementedException();
        }

        private void DisconnectButtonCommandCallback(HansDeviceState obj)
        {
            MessageBox.Show(obj.GetType().Name);
            if (obj is HansDeviceState state)
            {
                MessageBox.Show($"Click to {state.Address}");
            }
        }

        private void ConnectButtonCommandCallback(HansDeviceState obj)
        {
            if (obj is HansDeviceState state)
            {
                MessageBox.Show($"Click to {state.Address}");
            }
        }

        private void OnScannerStateChanged(HansDeviceState state)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Update or add scanner to discovered list
                var existing = DiscoveredScanners.FirstOrDefault(s => s.Address == state.Address);
                if (existing != null)
                {
                    // Update existing
                    existing.ConnectState = state.ConnectState;
                    existing.StreamProgress = state.StreamProgress;
                    existing.StreamEnd = state.StreamEnd;
                    existing.MarkingProgress = state.MarkingProgress;
                    existing.MarkComplete = state.MarkComplete;
                }
                else
                {
                    // Add new
                    DiscoveredScanners.Add(state);
                }

                // Update connection status text
                UpdateConnectionStatusText();
            });
        }

        private void UpdateConnectionStatusText()
        {
            var connected = DiscoveredScanners.Where(s => s.ConnectState == Hans.NET.libs.ConnectState.Connected).ToList();
            if (connected.Count == 0)
            {
                ConnectionStatusText = "Не подключено";
            }
            else if (connected.Count == 1)
            {
                ConnectionStatusText = $"Подключено: {connected[0].Address}";
            }
            else
            {
                ConnectionStatusText = $"Подключено: {connected.Count} сканаторов";
            }
        }

        private async Task LoadConfigAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Загрузить конфигурацию"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    bool success = await _configService.LoadConfigurationAsync(dialog.FileName);
                    if (success)
                    {
                        LoadConfigurationToUI();
                        MessageBox.Show("Конфигурация успешно загружена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Ошибка загрузки конфигурации", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task SaveConfigAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Сохранить конфигурацию",
                FileName = "scanner_config.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    UpdateConfigFromUI();
                    bool success = await _configService.SaveConfigurationAsync(dialog.FileName);
                    if (success)
                    {
                        MessageBox.Show("Конфигурация успешно сохранена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Ошибка сохранения конфигурации", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ApplyConfig()
        {
            try
            {
                UpdateConfigFromUI();
                _configService.ApplyConfiguration();
                MessageBox.Show("Конфигурация применена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка применения конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConnectScanner1()
        {
            var settings = _configService.CurrentSettings;
            if (settings.Scanners.Count > 0)
            {
                string address = settings.Scanners[0].CardInfo.IpAddress;
                _hansService.Connect(address);
            }
        }

        private void ConnectScanner2()
        {
            var settings = _configService.CurrentSettings;
            if (settings.Scanners.Count > 1)
            {
                string address = settings.Scanners[1].CardInfo.IpAddress;
                _hansService.Connect(address);
            }
        }

        private async Task DrawShapeAsync()
        {
            try
            {
                var connectedScanners = DiscoveredScanners.Where(s => s.ConnectState == Hans.NET.libs.ConnectState.Connected).ToList();
                if (connectedScanners.Count == 0)
                {
                    MessageBox.Show("Подключитесь к сканаторам перед рисованием", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var settings = _configService.CurrentSettings;

                // Если координаты (0, 0) - используем центр поля сканатора
                double drawX = ShapeX;
                double drawY = ShapeY;
                if (Math.Abs(ShapeX) < 0.001 && Math.Abs(ShapeY) < 0.001 && settings.Scanners.Count > 0)
                {
                    // Центр поля = 0, так как поле сканатора обычно от -FieldSize/2 до +FieldSize/2
                    drawX = 0;
                    drawY = 0;
                }

                var parameters = new ShapeParameters
                {
                    Type = (ShapeType)SelectedShapeTypeIndex,
                    X = drawX,
                    Y = drawY,
                    Size = ShapeSize,
                    Speed = MarkSpeed,
                    Power = LaserPower,
                    BeamDiameter = BeamDiameter,
                    SplitBetweenLasers = SplitBetweenLasers
                };

                // Генерация геометрии
                var points = _geometryService.GenerateShape(parameters);

                if (SplitBetweenLasers && connectedScanners.Count >= 2 && settings.Scanners.Count >= 2)
                {
                    // Разделить фигуру между двумя лазерами
                    var (part1, part2) = _geometryService.SplitShape(points);
                    Console.WriteLine($"После сплита. Part1: {part1.Count}, Part2: {part2.Count}");

                    var (udm1, udm2) = await _udmService.GenerateDualUdmDataAsync(
                        settings.Scanners.FirstOrDefault(p=>p.CardInfo.IpAddress.Contains("227")),
                        settings.Scanners.FirstOrDefault(p => p.CardInfo.IpAddress.Contains("228")),
                        part1,
                        part2,
                        parameters);

                    Console.WriteLine($"После сплита. UDM1: {(udm1 != null ? "создан" : "null")}, UDM2: {(udm2 != null ? "создан" : "null")}");

                    string address1 = connectedScanners.FirstOrDefault(p => p.Address.Contains("227")).Address;
                    string address2 = connectedScanners.FirstOrDefault(p => p.Address.Contains("228")).Address;

                    // Загружаем и выполняем первый файл если есть
                    if (udm1 != null)
                    {
                        Console.WriteLine($"UDM1 размер: {new FileInfo(udm1).Length} bytes");
                        _hansService.DownloadUdmFile(address1, udm1);
                        await _hansService.WaitForStreamDownloadComplete(address1);
                        _hansService.StartMarking(address1);
                        await _hansService.WaitForMarkingComplete(address1);
                    }

                    // Загружаем и выполняем второй файл если есть
                    if (udm2 != null)
                    {
                        Console.WriteLine($"UDM2 размер: {new FileInfo(udm2).Length} bytes");
                        _hansService.DownloadUdmFile(address2, udm2);
                        await _hansService.WaitForStreamDownloadComplete(address2);
                        _hansService.StartMarking(address2);
                        await _hansService.WaitForMarkingComplete(address2);
                    }

                    if (udm1 == null && udm2 == null)
                    {
                        MessageBox.Show("Не удалось разделить фигуру между лазерами", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    // Рисуем на выбранном сканаторе
                    int configIndex = SelectedScannerIndex;
                    string address = string.Empty;

                    if (configIndex == 0) address = "172.18.34.227";
                    else if (configIndex == 1) address = "172.18.34.228";

                    ScanatorConfiguration config = Scanner1Config;
                    if (configIndex == 1) config = Scanner2Config;

                    var udmData = await _udmService.GenerateUdmDataAsync(
                        config,
                        points,
                        parameters);

                    if (udmData != null)
                    {
                        _hansService.DownloadUdmFile(address, udmData);
                        await _hansService.WaitForStreamDownloadComplete(address);
                        _hansService.StartMarking(address);
                        await _hansService.WaitForMarkingComplete(address);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при рисовании: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearPreview()
        {
            MessageBox.Show("Функция очистки превью будет реализована в следующей версии", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadConfigurationToUI()
        {
            var settings = _configService.CurrentSettings;

            try
            {
                if (settings.Scanners.Count > 0)
                {
                    Scanner1Config = settings.Scanners[0];

                    // Load MarkSpeed profiles for Scanner 1
                    Scanner1MarkSpeedProfiles.Clear();
                    if (Scanner1Config.ProcessVariablesMap.MarkSpeed != null)
                    {
                        foreach (var profile in Scanner1Config.ProcessVariablesMap.MarkSpeed)
                        {
                            Scanner1MarkSpeedProfiles.Add(profile);
                        }
                    }
                }
                else
                {
                    Scanner1Config = _configService.CreateDefaultScannerConfig();
                    Scanner1MarkSpeedProfiles.Clear();
                }

                if (settings.Scanners.Count > 1)
                {
                    Scanner2Config = settings.Scanners[1];

                    // Load MarkSpeed profiles for Scanner 2
                    Scanner2MarkSpeedProfiles.Clear();
                    if (Scanner2Config.ProcessVariablesMap.MarkSpeed != null)
                    {
                        foreach (var profile in Scanner2Config.ProcessVariablesMap.MarkSpeed)
                        {
                            Scanner2MarkSpeedProfiles.Add(profile);
                        }
                    }
                }
                else
                {
                    Scanner2Config = _configService.CreateDefaultScannerConfig();
                    Scanner2MarkSpeedProfiles.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateConfigFromUI()
        {
            try
            {
                // Sync MarkSpeed profiles back to configurations
                if (Scanner1Config != null)
                {
                    Scanner1Config.ProcessVariablesMap.MarkSpeed = Scanner1MarkSpeedProfiles.ToList();
                }

                if (Scanner2Config != null)
                {
                    Scanner2Config.ProcessVariablesMap.MarkSpeed = Scanner2MarkSpeedProfiles.ToList();
                }

                // Update settings
                _configService.CurrentSettings.Scanners.Clear();
                if (Scanner1Config != null)
                {
                    _configService.CurrentSettings.Scanners.Add(Scanner1Config);
                }
                if (Scanner2Config != null)
                {
                    _configService.CurrentSettings.Scanners.Add(Scanner2Config);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region MarkSpeed Profile Management

        private void AddMarkSpeedProfile(int scannerIndex)
        {
            var newProfile = new ProcessVariables
            {
                MarkSpeed = 1000,
                JumpSpeed = 25000,
                PolygonDelay = 170,
                JumpDelay = 40000,
                MarkDelay = 1200,
                LaserOnDelay = 110.0,
                LaserOffDelay = 120.0,
                LaserOnDelayForSkyWriting = 130.0,
                LaserOffDelayForSkyWriting = 140.0,
                CurBeamDiameterMicron = 65.0,
                CurPower = 50.0,
                JumpMaxLengthLimitMm = 400.0,
                MinJumpDelay = 400,
                Swenable = true,
                Umax = 0.1
            };

            if (ShowEditProfileDialog(newProfile))
            {
                if (scannerIndex == 1)
                {
                    Scanner1MarkSpeedProfiles.Add(newProfile);
                }
                else
                {
                    Scanner2MarkSpeedProfiles.Add(newProfile);
                }
            }
        }

        private void EditMarkSpeedProfile(int scannerIndex, ProcessVariables profile)
        {
            if (profile == null) return;

            // Создаем копию для редактирования
            var editCopy = new ProcessVariables
            {
                MarkSpeed = profile.MarkSpeed,
                JumpSpeed = profile.JumpSpeed,
                PolygonDelay = profile.PolygonDelay,
                JumpDelay = profile.JumpDelay,
                MarkDelay = profile.MarkDelay,
                LaserOnDelay = profile.LaserOnDelay,
                LaserOffDelay = profile.LaserOffDelay,
                LaserOnDelayForSkyWriting = profile.LaserOnDelayForSkyWriting,
                LaserOffDelayForSkyWriting = profile.LaserOffDelayForSkyWriting,
                CurBeamDiameterMicron = profile.CurBeamDiameterMicron,
                CurPower = profile.CurPower,
                JumpMaxLengthLimitMm = profile.JumpMaxLengthLimitMm,
                MinJumpDelay = profile.MinJumpDelay,
                Swenable = profile.Swenable,
                Umax = profile.Umax
            };

            if (ShowEditProfileDialog(editCopy))
            {
                // Копируем изменения обратно
                profile.MarkSpeed = editCopy.MarkSpeed;
                profile.JumpSpeed = editCopy.JumpSpeed;
                profile.PolygonDelay = editCopy.PolygonDelay;
                profile.JumpDelay = editCopy.JumpDelay;
                profile.MarkDelay = editCopy.MarkDelay;
                profile.LaserOnDelay = editCopy.LaserOnDelay;
                profile.LaserOffDelay = editCopy.LaserOffDelay;
                profile.LaserOnDelayForSkyWriting = editCopy.LaserOnDelayForSkyWriting;
                profile.LaserOffDelayForSkyWriting = editCopy.LaserOffDelayForSkyWriting;
                profile.CurBeamDiameterMicron = editCopy.CurBeamDiameterMicron;
                profile.CurPower = editCopy.CurPower;
                profile.JumpMaxLengthLimitMm = editCopy.JumpMaxLengthLimitMm;
                profile.MinJumpDelay = editCopy.MinJumpDelay;
                profile.Swenable = editCopy.Swenable;
                profile.Umax = editCopy.Umax;

                // Обновляем коллекцию для уведомления UI
                if (scannerIndex == 1)
                {
                    var index = Scanner1MarkSpeedProfiles.IndexOf(profile);
                    if (index >= 0)
                    {
                        Scanner1MarkSpeedProfiles.RemoveAt(index);
                        Scanner1MarkSpeedProfiles.Insert(index, profile);
                    }
                }
                else
                {
                    var index = Scanner2MarkSpeedProfiles.IndexOf(profile);
                    if (index >= 0)
                    {
                        Scanner2MarkSpeedProfiles.RemoveAt(index);
                        Scanner2MarkSpeedProfiles.Insert(index, profile);
                    }
                }
            }
        }

        private bool ShowEditProfileDialog(ProcessVariables profile)
        {
            var dialog = new Window
            {
                Title = "Редактирование профиля MarkSpeed",
                Width = 920,
                Height = 870,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var viewModel = new EditMarkSpeedProfileViewModel();
            viewModel.SetProfile(profile);
            viewModel.CloseAction = () => dialog.Close();

            var view = new Views.EditMarkSpeedProfileView
            {
                DataContext = viewModel
            };

            dialog.Content = view;
            dialog.ShowDialog();

            return viewModel.DialogResult;
        }

        private void DeleteMarkSpeedProfile(int scannerIndex, ProcessVariables profile)
        {
            if (profile == null) return;

            var result = MessageBox.Show($"Удалить профиль со скоростью {profile.MarkSpeed}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (scannerIndex == 1)
                {
                    Scanner1MarkSpeedProfiles.Remove(profile);
                }
                else
                {
                    Scanner2MarkSpeedProfiles.Remove(profile);
                }
            }
        }

        #endregion
    }
}
