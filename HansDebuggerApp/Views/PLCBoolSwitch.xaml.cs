using Opc2Lib;
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
using HansDebuggerApp.Opc;
using Newtonsoft.Json;
using Prism.Ioc;

namespace HansDebuggerApp.Views
{
    /// <summary>
    /// Логика взаимодействия для PLCBoolSwitch.xaml
    /// </summary>
    public partial class PLCBoolSwitch : UserControl
    {
        public static readonly DependencyProperty CommandInfoProperty =
            DependencyProperty.Register(
                nameof(CommandInfo),
                typeof(CommandInfo),
                typeof(PLCBoolSwitch),
                new PropertyMetadata(null));

        public CommandInfo CommandInfo
        {
            get => (CommandInfo)GetValue(CommandInfoProperty);
            set => SetValue(CommandInfoProperty, value);
        }

        private ILogicControllerObserver _logicControllerObserver;
        public ILogicControllerProvider _logicControllerProvider;

        public PLCBoolSwitch()
        {
            InitializeComponent();
            Loaded += OnLoaded;

        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _logicControllerObserver = Bootstrapper.ContainerProvider?.Resolve<ILogicControllerObserver>();
                _logicControllerProvider = Bootstrapper.ContainerProvider?.Resolve<ILogicControllerProvider>();

                ToggleButton.Unchecked += async (o, args) =>
                {
                    await _logicControllerProvider.SetBoolAsync(CommandInfo, false);
                };
                ToggleButton.Checked += async (o, args) =>
                {
                    await _logicControllerProvider.SetBoolAsync(CommandInfo, true);
                };

                //HardShowValue();
                _logicControllerObserver.Subscribe(this, (data) =>
                {
                    if (data.Value is bool)
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            //MessageBox.ShowDialog(data.Value.ToString());
                            ToggleButton.IsChecked = (bool)data.Value;
                        });
                    }
                }, CommandInfo);
            }
        }

        private async void HardShowValue()
        {
            if (_logicControllerProvider == null || !_logicControllerProvider.Connected) return;
            bool current = await _logicControllerProvider.GetBoolAsync(CommandInfo);
            ToggleButton.IsChecked = current;
        }


        private async void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_logicControllerProvider == null || !_logicControllerProvider.Connected) return;
            bool current = await _logicControllerProvider.GetBoolAsync(CommandInfo);
            await _logicControllerProvider.SetBoolAsync(CommandInfo, !current);
        }
    }
}
