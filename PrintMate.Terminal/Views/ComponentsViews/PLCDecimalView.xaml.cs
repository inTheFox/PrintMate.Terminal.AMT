using Opc2Lib;
using PrintMate.Terminal.Opc;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrintMate.Terminal.Views.ComponentsViews
{
    /// <summary>
    /// Логика взаимодействия для PLCDecimalView.xaml
    /// </summary>
    public partial class PLCDecimalView : UserControl
    {
        public static readonly DependencyProperty CommandInfoProperty =
            DependencyProperty.Register(
                nameof(CommandInfo),
                typeof(CommandInfo),
                typeof(PLCDecimalView),
                new PropertyMetadata(null));

        public CommandInfo CommandInfo
        {
            get => (CommandInfo)GetValue(CommandInfoProperty);
            set => SetValue(CommandInfoProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(PLCDecimalView),
                new PropertyMetadata(string.Empty));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ReadOnlyProperty =
            DependencyProperty.Register(
                nameof(ReadOnly),
                typeof(bool),
                typeof(PLCDecimalView),
                new PropertyMetadata(true));
        public bool ReadOnly
        {
            get => (bool)GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        private ILogicControllerObserver _logicControllerObserver;
        public ILogicControllerProvider _logicControllerProvider;

        public PLCDecimalView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _logicControllerObserver = Bootstrapper.ContainerProvider.Resolve<ILogicControllerObserver>();
            _logicControllerObserver.Subscribe(this, (data) =>
            {
                if (data.Value is bool) return;
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (data.Value != null && data.Value is IFormattable formattable)
                        Value = formattable.ToString("F3", CultureInfo.InvariantCulture);
                    else
                        Value = "N/A"; // или другое значение по умолчанию
                });
            }, CommandInfo);

            _logicControllerProvider = Bootstrapper.ContainerProvider.Resolve<ILogicControllerProvider>();

            if (!ReadOnly)
            {
                Image.Source = new BitmapImage(new Uri("/images/edit_rule_64.png", UriKind.Relative));
            }
            else
            {
                Image.Source = new BitmapImage(new Uri("/images/show.png", UriKind.Relative));
            }
        }
    }
}
