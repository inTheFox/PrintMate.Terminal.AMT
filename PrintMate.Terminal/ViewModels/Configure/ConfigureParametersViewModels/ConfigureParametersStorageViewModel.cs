using HandyControl.Controls;
using HandyControl.Tools.Command;
using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels
{
    public class ConfigureParametersStorageViewModel : BindableBase
    {
        private readonly ConfigurationManager _configManager;
        private readonly ModalService _modalService;
        private readonly IEventAggregator _eventAggregator;

        private string _currentConfigPath;
        public string CurrentConfigPath
        {
            get => _currentConfigPath;
            set => SetProperty(ref _currentConfigPath, value);
        }

        private DateTime _lastSaveTime;
        public DateTime LastSaveTime
        {
            get => _lastSaveTime;
            set => SetProperty(ref _lastSaveTime, value);
        }

        public RelayCommand ExportCommand { get; set; }
        public RelayCommand ImportCommand { get; set; }
        public RelayCommand ResetAllCommand { get; set; }

        public ConfigureParametersStorageViewModel(
            ConfigurationManager configManager,
            ModalService modalService,
            IEventAggregator eventAggregator)
        {
            _configManager = configManager;
            _modalService = modalService;
            _eventAggregator = eventAggregator;

            ExportCommand = new RelayCommand(ExportCommandCallback);
            ImportCommand = new RelayCommand(ImportCommandCallback);
            ResetAllCommand = new RelayCommand(ResetAllCommandCallback);

            LoadInfo();
        }

        private void LoadInfo()
        {
            // Get configuration file path from ConfigurationManager
            var configField = typeof(ConfigurationManager).GetField("_configFilePath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (configField != null)
            {
                CurrentConfigPath = configField.GetValue(_configManager) as string ?? "Не найдено";

                if (File.Exists(CurrentConfigPath))
                {
                    LastSaveTime = File.GetLastWriteTime(CurrentConfigPath);
                }
            }
        }

        private async void ExportCommandCallback(object obj)
        {
            try
            {
                string modalId = Guid.NewGuid().ToString();
                var options = new Dictionary<string, object>
                {
                    { "ShowFiles", false },
                    { "AllowedTypes", new List<string>() },
                    { "ModalId", modalId },
                    { "Title", "Выберите папку для экспорта настроек" }
                };

                var result = await _modalService.ShowAsync<DirectoryPickerControl, DirectoryPickerControlViewModel>(modalId, options);

                if (result.IsSuccess && !string.IsNullOrEmpty(result.Result.CurrentDirectory))
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var exportFileName = $"config_export_{timestamp}.json";
                    var exportPath = Path.Combine(result.Result.CurrentDirectory, exportFileName);

                    // Save current configuration
                    _configManager.SaveNow();

                    // Copy current config file to export location
                    if (File.Exists(CurrentConfigPath))
                    {
                        File.Copy(CurrentConfigPath, exportPath, overwrite: true);
                        Growl.Success($"Настройки успешно экспортированы в:\n{exportPath}");
                        Console.WriteLine($"Configuration exported to: {exportPath}");
                    }
                    else
                    {
                        Growl.Error("Файл конфигурации не найден!");
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.Error($"Ошибка при экспорте настроек: {ex.Message}");
                Console.WriteLine($"Export error: {ex}");
            }
        }

        private async void ImportCommandCallback(object obj)
        {
            try
            {
                string modalId = Guid.NewGuid().ToString();
                var options = new Dictionary<string, object>
                {
                    { "ShowFiles", true },
                    { "AllowedTypes", new List<string> { ".json" } },
                    { "ModalId", modalId },
                    { "Title", "Выберите файл настроек для импорта" }
                };

                var result = await _modalService.ShowAsync<DirectoryPickerControl, DirectoryPickerControlViewModel>(modalId, options);

                if (result.IsSuccess && !string.IsNullOrEmpty(result.Result.SelectedFilePath))
                {
                    var importPath = result.Result.SelectedFilePath;

                    // Confirm import
                    var confirmResult = System.Windows.MessageBox.Show(
                        "Импорт настроек заменит все текущие настройки приложения.\n\n" +
                        "Приложение будет перезапущено после импорта.\n\n" +
                        "Продолжить?",
                        "Подтверждение импорта",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning
                    );

                    if (confirmResult == System.Windows.MessageBoxResult.Yes)
                    {
                        // Create backup of current config
                        var backupPath = CurrentConfigPath + ".before_import.bak";
                        if (File.Exists(CurrentConfigPath))
                        {
                            File.Copy(CurrentConfigPath, backupPath, overwrite: true);
                        }

                        // Copy imported file to config location
                        File.Copy(importPath, CurrentConfigPath, overwrite: true);

                        Growl.Success("Настройки успешно импортированы!\n\nПриложение будет перезапущено...");
                        Console.WriteLine($"Configuration imported from: {importPath}");

                        // Wait a bit for user to see the message
                        await System.Threading.Tasks.Task.Delay(2000);

                        // Restart application
                        System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                        Application.Current.Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.Error($"Ошибка при импорте настроек: {ex.Message}");
                Console.WriteLine($"Import error: {ex}");
            }
        }

        private void ResetAllCommandCallback(object obj)
        {
            var confirmResult = System.Windows.MessageBox.Show(
                "Вы уверены, что хотите сбросить ВСЕ настройки приложения к значениям по умолчанию?\n\n" +
                "Приложение будет перезапущено после сброса.",
                "Подтверждение сброса настроек",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning
            );

            if (confirmResult == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    // Create backup before reset
                    var backupPath = CurrentConfigPath + ".before_reset.bak";
                    if (File.Exists(CurrentConfigPath))
                    {
                        File.Copy(CurrentConfigPath, backupPath, overwrite: true);
                    }

                    // Delete current config file
                    if (File.Exists(CurrentConfigPath))
                    {
                        File.Delete(CurrentConfigPath);
                    }

                    Growl.Success("Настройки сброшены к значениям по умолчанию!\n\nПриложение будет перезапущено...");
                    Console.WriteLine("All settings reset to defaults");

                    // Wait a bit for user to see the message
                    System.Threading.Tasks.Task.Delay(2000).Wait();

                    // Restart application
                    System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    Growl.Error($"Ошибка при сбросе настроек: {ex.Message}");
                    Console.WriteLine($"Reset error: {ex}");
                }
            }
        }
    }
}
