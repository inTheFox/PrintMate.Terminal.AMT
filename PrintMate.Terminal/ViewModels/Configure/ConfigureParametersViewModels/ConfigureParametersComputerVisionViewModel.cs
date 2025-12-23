using System;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using ImTools;

namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels
{
    public class ConfigureParametersComputerVisionViewModel : BindableBase
    {
        #region Приватные свойства
        // настройки
        private string _roiMaskPath;
        private string _calibrationSettingsPath;

        private bool _isRepeatedRecoaterStripe;
        private bool _isPartDelamination;
        private bool _isLackOfPowder;
        private bool _isPlatformAnomaly;

        private byte _observeLayersCount;
        private byte _countLayerWithDefectRakel;
        private ushort _minAreaMm2PartDelamination;
        private ushort _minAreaMm2PlatformAnomaly;
        private byte _percentAreaLackOfPowder;

        private bool _isLayerContoursFolder = false;
        private string _layerContoursFolder;
        private Visibility _layerContoursFolderVisibility = Visibility.Collapsed;
        //
        // 
        private ConfigurationManager _configurationManager;
        private LayerAnalyzerSettings _settings;
        private ModalService _modalService;
        #endregion

        #region Публичные свойства

        public string RoiMaskPath
        {
            get => _roiMaskPath;
            set => SetProperty(ref _roiMaskPath, value);
        }

        public string CalibrationSettingsPath
        {
            get => _calibrationSettingsPath;
            set => SetProperty(ref _calibrationSettingsPath, value);
        }

        public bool IsRepeatedRecoaterStripe
        {
            get => _isRepeatedRecoaterStripe;
            set => SetProperty(ref _isRepeatedRecoaterStripe, value);
        }
        public bool IsPartDelamination
        {
            get => _isPartDelamination;
            set => SetProperty(ref _isPartDelamination, value);
        }
        public bool IsLackOfPowder
        {
            get => _isLackOfPowder;
            set => SetProperty(ref _isLackOfPowder, value);
        }
        public bool IsPlatformAnomaly
        {
            get => _isPlatformAnomaly;
            set => SetProperty(ref _isPlatformAnomaly, value);
        }

        public byte ObserveLayersCount
        {
            get => _observeLayersCount;
            set => SetProperty(ref _observeLayersCount, value);
        }

        public byte CountLayerWithDefectRakel
        {
            get => _countLayerWithDefectRakel;
            set => SetProperty(ref _countLayerWithDefectRakel, value);
        }

        public ushort MinAreaMm2PartDelamination
        {
            get => _minAreaMm2PartDelamination;
            set => SetProperty(ref _minAreaMm2PartDelamination, value);
        }
        public ushort MinAreaMm2PlatformAnomaly
        {
            get => _minAreaMm2PlatformAnomaly;
            set => SetProperty(ref _minAreaMm2PlatformAnomaly, value);
        }
        public byte PercentAreaLackOfPowder
        {
            get => _percentAreaLackOfPowder;
            set => SetProperty(ref _percentAreaLackOfPowder, value);
        }

        public bool IsLayerContoursFolder
        {
            get => _isLayerContoursFolder;
            set
            {
                SetProperty(ref _isLayerContoursFolder, value);
                if (!value)
                {
                    LayerContoursFolderVisibility = Visibility.Collapsed;
                    LayerContoursFolder = "";
                }
                else LayerContoursFolderVisibility = Visibility.Visible;
            }
        }
        public string LayerContoursFolder
        {
            get => _layerContoursFolder;
            set => SetProperty(ref _layerContoursFolder, value);
        }

        public Visibility LayerContoursFolderVisibility
        {
            get => _layerContoursFolderVisibility;
            set => SetProperty(ref _layerContoursFolderVisibility, value);
        }

        #endregion

        #region Команды
        public DelegateCommand PickROIMaskCommand { get; }
        public DelegateCommand PickCalibrationSettingsCommand { get; }
        public DelegateCommand PickLayerContoursCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ResetCommand { get; }

        #endregion

        public ConfigureParametersComputerVisionViewModel(ConfigurationManager configurationManager, ModalService modalService)
        {
            _configurationManager = configurationManager;
            _settings = _configurationManager.Get<LayerAnalyzerSettings>();
            _modalService = modalService;

            Reset();

            PickROIMaskCommand = new DelegateCommand(async ()=> await PickROIMask());
            PickCalibrationSettingsCommand = new DelegateCommand(async () => await PickCalibrationSettings());
            PickLayerContoursCommand = new DelegateCommand(async () => await PickLayerContours());
            SaveCommand = new DelegateCommand(Save);
            ResetCommand = new DelegateCommand(Reset);
        }

        private async Task PickROIMask()
        {
            var modalId = Guid.NewGuid().ToString();
            var options = new Dictionary<string, object>
            {
                { "ShowFiles", true },
                { "AllowedTypes", new List<string> { ".png" } },
                { "ModalId", modalId }
            };
            var result = await _modalService.ShowAsync<DirectoryPickerControl, DirectoryPickerControlViewModel>(modalId, options);
            
            if (!result.IsSuccess) return;

            RoiMaskPath = result.Result.SelectedFilePath;
        }

        private async Task PickCalibrationSettings()
        {
            var modalId = Guid.NewGuid().ToString();
            var options = new Dictionary<string, object>
            {
                { "ShowFiles", true },
                { "AllowedTypes", new List<string> { ".json" } },
                { "ModalId", modalId }
            };
            var result = await _modalService.ShowAsync<DirectoryPickerControl, DirectoryPickerControlViewModel>(modalId, options);

            if (!result.IsSuccess) return;

            CalibrationSettingsPath = result.Result.SelectedFilePath;
        }

        private async Task PickLayerContours()
        {
            var modalId = Guid.NewGuid().ToString();
            var options = new Dictionary<string, object>
            {
                { "ShowFiles", false },
                { "ModalId", modalId }
            };
            var result = await _modalService.ShowAsync<DirectoryPickerControl, DirectoryPickerControlViewModel>(modalId, options);

            if (!result.IsSuccess) return;

            LayerContoursFolder = result.Result.CurrentDirectory;
        }
        private void Save()
        {
            _configurationManager.Update<LayerAnalyzerSettings>(settings =>
            {
                settings.RoiMaskPath = RoiMaskPath;
                settings.CalibrationSettingsPath = CalibrationSettingsPath;
                settings.IsRepeatedRecoaterStripe = IsRepeatedRecoaterStripe;
                settings.IsPartDelamination = IsPartDelamination;
                settings.IsLackOfPowder = IsLackOfPowder;
                settings.IsPlatformAnomaly = IsPlatformAnomaly;
                settings.ObserveLayersCount = ObserveLayersCount;
                settings.CountLayerWithDefectRakel = CountLayerWithDefectRakel;
                settings.MinAreaMm2PartDelamination = MinAreaMm2PartDelamination;
                settings.MinAreaMm2PlatformAnomaly = MinAreaMm2PlatformAnomaly;
                settings.PercentAreaLackOfPowder = PercentAreaLackOfPowder;
                settings.LayerContoursFolder = LayerContoursFolder;
            });

            _configurationManager.SaveNow();
        }


        private void Reset()
        {
            RoiMaskPath = _settings.RoiMaskPath;
            CalibrationSettingsPath = _settings.CalibrationSettingsPath;
            IsRepeatedRecoaterStripe = _settings.IsRepeatedRecoaterStripe;
            IsPartDelamination = _settings.IsPartDelamination;
            IsLackOfPowder = _settings.IsLackOfPowder;
            IsPlatformAnomaly = _settings.IsPlatformAnomaly;
            ObserveLayersCount = _settings.ObserveLayersCount;
            CountLayerWithDefectRakel = _settings.CountLayerWithDefectRakel;
            MinAreaMm2PartDelamination = _settings.MinAreaMm2PartDelamination;
            MinAreaMm2PlatformAnomaly = _settings.MinAreaMm2PlatformAnomaly;
            PercentAreaLackOfPowder = _settings.PercentAreaLackOfPowder;
            if (!string.IsNullOrEmpty(_settings.LayerContoursFolder))
            {
                IsLayerContoursFolder = true;
                LayerContoursFolder = _settings.LayerContoursFolder;
            }
        }
    }
}
