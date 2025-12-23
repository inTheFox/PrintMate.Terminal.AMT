
using HandyControl.Controls;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Opc;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ImTools;
using Opc2Lib;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views;
using CommandResponse = PrintMate.Terminal.Opc.CommandResponse;
using MessageBox = System.Windows.MessageBox;

namespace PrintMate.Terminal.ViewModels
{
    public class ManualAxesControlViewModel : BindableBase
    {
        private int _platformTop = 0;
        private int _currentRecouterPosition = 0;
        private int _doseCounts = 0;

        public int DoseCounts
        {
            get => _doseCounts;
            set
            {
                SetProperty(ref _doseCounts, value);
            }
        }

        public int PlatformTop
        {
            get => _platformTop;
            set => SetProperty(ref _platformTop, value);
        }

        public int CurrentRecouterPosition
        {
            get => _currentRecouterPosition;
            set => SetProperty(ref _currentRecouterPosition, value);
        }

        private int _platformPosition = 0;
        public int PlatformPosition
        {
            get => _platformPosition;
            set => SetProperty(ref _platformPosition, value);
        }

        private int _preRecounterPosition;
        public int _prePlatformPosition;



        private int _currentPlatformStep;
        public int CurrentPlatformStep
        {
            get => _currentPlatformStep;
            set => SetProperty(ref _currentPlatformStep, value);
        }

        private int _setPlatformStep;
        public int SetPlatformStep
        {
            get => _setPlatformStep;
            set => SetProperty(ref _setPlatformStep, value);
        }

        public RelayCommand SelectDoseCountsCommand { get; set; }

        private readonly ILogicControllerObserver _logicControllerObserver;
        private readonly ILogicControllerProvider _logicControllerProvider;
        private readonly IEventAggregator _eventAggregator;

        private readonly KeyboardService _keyboardService;
        public ManualAxesControlViewModel(ILogicControllerObserver logicControllerObserver, ILogicControllerProvider logicControllerProvider, IEventAggregator eventAggregator, KeyboardService keyboardService)
        {
            _logicControllerObserver = logicControllerObserver;
            _logicControllerProvider = logicControllerProvider;
            _eventAggregator = eventAggregator;
            _keyboardService = keyboardService;

            _logicControllerObserver.Subscribe(
                this,
                SubscribeHandle,
                OpcCommands.AM_Axes_PlatformRELPosition,
                OpcCommands.AM_Axes_RecoaterABSPosition
            );
            _logicControllerObserver.Subscribe(
                this,
                async (data) =>
                {
                    if (data != null && data.Value != null && data.Value is int result && result != DoseCounts)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            DoseCounts = result;
                        });
                    }
                },
                OpcCommands.Set_Axes_DoserCounts
            );

            SelectDoseCountsCommand = new RelayCommand(SelectDoseCountsCommandCallback);
        }

        private async void SelectDoseCountsCommandCallback(object obj)
        {
            string result = _keyboardService.Show(KeyboardType.Numpad, "Введите количество доз", DoseCounts.ToString());
            if (!string.IsNullOrEmpty(result))
            {
                if (result.Contains("."))
                {
                    await CustomMessageBox.ShowErrorAsync("Ошибка", "Введите целое число");
                    return;
                }
                if (!_logicControllerProvider.Connected)
                {
                    await CustomMessageBox.ShowErrorAsync("Ошибка", "Нет подключения к ПЛК");
                    return;
                }
                if (int.TryParse(result, out int intResult))
                {
                    await _logicControllerProvider.SetUInt32Async(OpcCommands.Set_Axes_DoserCounts, (uint)DoseCounts);
                }
                else
                {
                    await CustomMessageBox.ShowErrorAsync("Ошибка", "Вы ввели некорректное значение");
                }
            }
        }

        private void SubscribeHandle(CommandResponse response)
        {
            if (response == null) return;
            if (response.CommandInfo == OpcCommands.AM_Axes_PlatformRELPosition)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    PlatformPosition = (int)response.Value;
                    CurrentPlatformStep = (int)response.Value;
                    PlatformTop = (int)response.Value;
                    PlatformTop = (int)(((double)PlatformPosition / 400000) * 200);
                    if (PlatformTop > 200) PlatformTop = 200;
                    if (PlatformTop < 0) PlatformTop = 0;
                });
                return;
            }
            if (response.CommandInfo == OpcCommands.AM_Axes_RecoaterABSPosition)
            {
                CurrentRecouterPosition = Convert.ToInt32(response.Value);
                if (CurrentRecouterPosition > 1000)
                {
                    CurrentRecouterPosition = 0;
                }
                double percent = (CurrentRecouterPosition / 519d) * 203;
                CurrentRecouterPosition = 318 - (int)percent;
                return;
            }
        }
    }
}
