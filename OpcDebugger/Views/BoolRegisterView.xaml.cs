using HandyControl.Controls;
using HandyControl.Tools.Extension;
using OpcDebugger.Services;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Opc.Ua.RelativePathFormatter;
using MessageBox = HandyControl.Controls.MessageBox;

namespace OpcDebugger.Views
{
    /// <summary>
    /// Логика взаимодействия для BoolRegisterView.xaml
    /// </summary>
    public partial class BoolRegisterView : UserControl
    {
        private readonly OpcService _opcService;
        private ElementInfo element => _opcService.SelectedItem;


        public BoolRegisterView(OpcService opcService)
        {
            _opcService = opcService;

            InitializeComponent();

            SwitchButton.Click += SwitchButton_Click;

            StateChanged();
        }

        private async void SwitchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_opcService.Client != null && _opcService.Client.Connected)
            {
                await _opcService.Client.SetBoolAsync(element.Cmd,
                    !await _opcService.Client.ReadBooleanAsync(element.Cmd));
                StateChanged();
            }
            else
            {
                Growl.ErrorGlobal("Opc not connected");
            }
        }

        public async void StateChanged()
        {
            bool currentValue = false;
            ElementInfo element = _opcService.SelectedItem;
            if (element != null && element.ValueType == "Bool")
            {
                if (_opcService.Client != null && _opcService.Client.Connected)
                {
                    currentValue = await _opcService.Client.ReadBooleanAsync(element.Cmd);
                }

                //MessageBox.Show(currentValue.ToString());
                if (currentValue)
                {
                    CurrentValueText.Text = "Включено";
                    SwitchButton.Content = "Отключить";
                    SwitchButton.Background = Brushes.Red;
                }
                else
                {
                    CurrentValueText.Text = "Отключено";
                    SwitchButton.Content = "Включить";
                    SwitchButton.Background = Brushes.GreenYellow;
                }
            }
        }
    }
}
