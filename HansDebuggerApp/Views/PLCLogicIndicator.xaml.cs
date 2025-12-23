using Opc2Lib;
using Prism.Ioc;
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
using HansDebuggerApp.Opc;

namespace HansDebuggerApp.Views
{
    /// <summary>
    /// Логика взаимодействия для PLCLogicIndicator.xaml
    /// </summary>
    public partial class PLCLogicIndicator : UserControl
    {
        public const string EnabledSrc = "/images/indicator_green_32.png";
        public const string DisabledSrc = "/images/indicator_red_32.png";

        private BitmapImage activeBitmapImage = new BitmapImage(new Uri(EnabledSrc, UriKind.Relative));
        private BitmapImage unActiveBitmapImage = new BitmapImage(new Uri(DisabledSrc, UriKind.Relative));


        public static readonly DependencyProperty CommandInfoProperty =
            DependencyProperty.Register(
                nameof(CommandInfo),
                typeof(CommandInfo),
                typeof(PLCLogicIndicator),
                new PropertyMetadata(null));

        public CommandInfo CommandInfo
        {
            get => (CommandInfo)GetValue(CommandInfoProperty);
            set => SetValue(CommandInfoProperty, value);
        }

        private ILogicControllerObserver _observer;
        public PLCLogicIndicator()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            //StartBlinkAnimation();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _observer = Bootstrapper.ContainerProvider?.Resolve<ILogicControllerObserver>();
                Image.Source = unActiveBitmapImage;
                //if (CommandInfo == null) return;
                _observer.Subscribe(this, (data) =>
                {
                    bool value = (bool)data.Value;
                    if (value)
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Image.Source = activeBitmapImage;
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Image.Source = unActiveBitmapImage;
                        });
                    }
                }, CommandInfo);

                StartBlinkAnimation();
            }
        }

        private void StartBlinkAnimation()
        {
            var opacityAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(2000)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(opacityAnimation);
            Storyboard.SetTarget(opacityAnimation, Image);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));

            storyboard.Begin();
        }
    }
}
