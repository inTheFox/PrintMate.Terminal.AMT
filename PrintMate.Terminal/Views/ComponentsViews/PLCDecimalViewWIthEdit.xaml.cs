using Opc2Lib;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.Services;
using Prism.Events;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrintMate.Terminal.Views.ComponentsViews
{
    /// <summary>
    /// Логика взаимодействия для PLCDecimalView.xaml
    /// </summary>
    public partial class PLCDecimalViewWIthEdit : UserControl
    {
        public const string EnabledSrc = "/images/indicator_green_32.png";
        public const string DisabledSrc = "/images/indicator_red_32.png";

        private BitmapImage activeBitmapImage = new BitmapImage(new Uri(EnabledSrc, UriKind.Relative));
        private BitmapImage unActiveBitmapImage = new BitmapImage(new Uri(DisabledSrc, UriKind.Relative));


        public static readonly DependencyProperty CommandInfoProperty =
            DependencyProperty.Register(
                nameof(CommandInfo),
                typeof(CommandInfo),
                typeof(PLCDecimalViewWIthEdit),
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
                typeof(PLCDecimalViewWIthEdit),
                new PropertyMetadata("0"));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private readonly ILogicControllerObserver _logicControllerObserver;
        private readonly ILogicControllerProvider _logicControllerProvider;
        private readonly MonitoringManager _monitoringManager;
        private readonly IEventAggregator _eventAggregator;

        public PLCDecimalViewWIthEdit()
        {
            _monitoringManager = Bootstrapper.ContainerProvider.Resolve<MonitoringManager>();
            _eventAggregator = Bootstrapper.ContainerProvider.Resolve<IEventAggregator>();
            _logicControllerObserver = Bootstrapper.ContainerProvider.Resolve<ILogicControllerObserver>();
            _logicControllerProvider = Bootstrapper.ContainerProvider.Resolve<ILogicControllerProvider>();

            _eventAggregator.GetEvent<OnCommandAddToFavouritesEvent>().Subscribe((command) =>
            {
                if (command != CommandInfo) return;
                FavImage.Visibility = Visibility.Visible;
            });
            _eventAggregator.GetEvent<OnCommandRemoveFromFavouritesEvent>().Subscribe((command) =>
            {
                if (command != CommandInfo) return;
                FavImage.Visibility = Visibility.Collapsed;
            });


            InitializeComponent();
            Loaded += OnLoaded;
            //StartBlinkAnimation();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ImageActive.Source = unActiveBitmapImage;
            if (_monitoringManager.IsCommandInFavourites(CommandInfo))
            {
                FavImage.Visibility = Visibility.Visible;
            }
            else
            {
                FavImage.Visibility = Visibility.Collapsed;
            }
            if (CommandInfo.ValueCommandType == ValueCommandType.Bool)
            {
                IndicatorText.Visibility = Visibility.Collapsed;
                ImageActive.Visibility = Visibility.Visible;
            }
            else
            {
                IndicatorText.Visibility = Visibility.Visible;
                ImageActive.Visibility = Visibility.Collapsed;
                ImageActive.Source = unActiveBitmapImage;
            }

            _logicControllerObserver.Subscribe(this, (data) =>
            {
                if (data.Value is bool)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if ((bool)data.Value)
                        {
                            ImageActive.Source = activeBitmapImage;
                        }
                        else
                        {
                            ImageActive.Source = unActiveBitmapImage;
                        }
                    });
                }
                else
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (data.Value != null && data.Value is IFormattable formattable)
                            Value = formattable.ToString("F2", CultureInfo.InvariantCulture);
                        else
                            Value = "N/A"; // или другое значение по умолчанию
                    });
                }
            }, CommandInfo);

        }


        private void StartBlinkAnimation()
        {
            var opacityAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(opacityAnimation);
            Storyboard.SetTarget(opacityAnimation, ImageActive);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
            storyboard.Begin();
        }
    }
}
