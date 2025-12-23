using HandyControl.Controls;
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

namespace OpcDebugger.Views
{
    /// <summary>
    /// Логика взаимодействия для NumericRegisterView.xaml
    /// </summary>
    public partial class NumericRegisterView : UserControl
    {
        private readonly OpcService _opcService;
        private ElementInfo element => _opcService.SelectedItem;

        public NumericRegisterView(OpcService opcService)
        {
            _opcService = opcService;

            InitializeComponent();
            EnterButton.Click += EnterButton_Click;
            StateChanged();
        }

        private async void EnterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_opcService.Client != null && _opcService.Client.Connected)
            {

                switch (element.ValueType)
                {
                    case "Unsigned":
                        await _opcService.Client.SetInt32Async(element.Cmd, int.Parse(NewValueEdit.Text));
                        break;
                    case "Real":
                        await _opcService.Client.SetFloatAsync(element.Cmd, float.Parse(NewValueEdit.Text));
                        break;
                }

                //await _opcService.Client.Wr(element.Cmd,
                //    !await _opcService.Client.ReadBooleanAsync(element.Cmd));
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
            if (element != null)
            {
                //if (_opcService.Client != null && _opcService.Client.Connected)
                //{
                //    currentValue = await _opcService.Client.ReadBooleanAsync(element.Cmd);
                //}

                switch (element.ValueType)
                {
                    case "Unsigned":
                        CurrentValueText.Text = (await _opcService.Client.GetUInt16Async(element.Cmd)).ToString();
                        break;
                    case "Real":
                        CurrentValueText.Text = (await _opcService.Client.GetFloatAsync(element.Cmd)).ToString();
                        break;
                }
            }
        }
    }
}
