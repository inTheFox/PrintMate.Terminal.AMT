using HandyControl.Tools.Command;
using Hans.NET.Models;
using HansScannerHost.Models;
using ImTools;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Windows;

namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels
{
    public class ConfigureParametersScanatorViewModel : BindableBase
    {
        private readonly ConfigurationManager _configManager;

        // Scanner 1
        private ScanatorConfiguration _scanner1;
        public ScanatorConfiguration Scanner1
        {
            get => _scanner1;
            set => SetProperty(ref _scanner1, value);
        }

        // Scanner 2
        private ScanatorConfiguration _scanner2;
        public ScanatorConfiguration Scanner2
        {
            get => _scanner2;
            set => SetProperty(ref _scanner2, value);
        }

        public RelayCommand ImportCommand { get; set; }
        public RelayCommand ResetCommand { get; set; }

        private readonly ModalService _modalService;
        private readonly IEventAggregator _eventAggregator;
        public ConfigureParametersScanatorViewModel(ConfigurationManager configManager, ModalService modalService, IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _modalService = modalService;
            _configManager = configManager;
            LoadConfiguration();

            ImportCommand = new RelayCommand(ImportCommandCallback);
            ResetCommand = new RelayCommand(ResetToDefaultCallback);
        }

        private void ResetToDefaultCallback(object obj)
        {
            _configManager.Reset<ScannerSettings>();
            _configManager.SaveNow();
            _eventAggregator.GetEvent<OnScanatorsConfigurationChangedEvent>().Publish();
            LoadConfiguration();
        }

        private async void ImportCommandCallback(object obj)
        {
            string modalId = Guid.NewGuid().ToString();
            var options = new Dictionary<string, object>();
            options.Add("ShowFiles", true);
            options.Add("AllowedTypes", new List<string> {".json"});
            options.Add("ModalId", modalId);
            var result = await _modalService.ShowAsync<DirectoryPickerControl, DirectoryPickerControlViewModel>(modalId, options);
            if (result.IsSuccess)
            {
                var settings = ScanatorConfigurationLoader.LoadFromFile(result.Result.SelectedFilePath);
                if (settings != null)
                {
                    _configManager.Get<ScannerSettings>().Scanners = settings;
                    _configManager.SaveNow();
                    _eventAggregator.GetEvent<OnScanatorsConfigurationChangedEvent>().Publish();
                    LoadConfiguration();
                }
            }
        }

        private void LoadConfiguration()
        {
            var settings = _configManager.Get<ScannerSettings>();

            if (settings.Scanners.Count > 0)
                Scanner1 = settings.Scanners[0];

            if (settings.Scanners.Count > 1)
                Scanner2 = settings.Scanners[1];

            Console.WriteLine($"Configuration loaded: Scanner1 IP={Scanner1?.CardInfo?.IpAddress}, Scanner2 IP={Scanner2?.CardInfo?.IpAddress}");
        }

        public string FormatList(List<float> list)
        {
            return list != null ? string.Join(", ", list) : "—";
        }
    }
}
