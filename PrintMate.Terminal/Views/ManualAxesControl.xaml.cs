
using HandyControl.Controls;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Opc2Lib;
using PrintMate.Terminal.Services;
using MessageBox = HandyControl.Controls.MessageBox;
using Point = System.Windows.Point;

namespace PrintMate.Terminal.Views
{
    /// <summary>
    /// Логика взаимодействия для ManualAxesControl.xaml
    /// </summary>
    public partial class ManualAxesControl : UserControl
    {
        private readonly ILogicControllerProvider _logicControllerProvider;

        private int _currentRecouterPosition = 0;

        private int _platformSetPosition;

        private readonly KeyboardService _keyboardService;

        public ManualAxesControl(ILogicControllerProvider logicControllerProvider, KeyboardService keyboardService)
        {
            _logicControllerProvider = logicControllerProvider;
            _keyboardService = keyboardService;
            InitializeComponent();
        }

        private async void SetStepMouseDown(object sender, MouseButtonEventArgs e)
        {

            string result = _keyboardService.Show(KeyboardType.Numpad, "Введите новое значение для шага платформы",_platformSetPosition.ToString());
            if (!string.IsNullOrEmpty(result))
            {
                if (int.TryParse(result, out int value))
                {
                    if (!_logicControllerProvider.Connected)
                    {
                        await CustomMessageBox.ShowErrorAsync("Ошибка", "Отсутствует подключение к ПЛК");
                        return;
                    }

                    _platformSetPosition = value;
                    await _logicControllerProvider.SetInt32Async(OpcCommands.Set_Axes_PlatformStep, value);
                    (DataContext as ManualAxesControlViewModel).SetPlatformStep = value;
                }
            }
        }

        private void SetStepTouchDown(object sender, TouchEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void ExtremeToDown(object sender, MouseButtonEventArgs e)
        {
            if (!_logicControllerProvider.Connected)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Отсутствует подключение к ПЛК");
                return;
            }

            await _logicControllerProvider.SetBoolAsync(OpcCommands.Com_Axes_PlatformJogExtremeDown, true);
            await Task.Delay(100);
            await _logicControllerProvider.SetBoolAsync(OpcCommands.Com_Axes_PlatformJogExtremeDown, false);
        }
    }
}
