using Opc2Lib;
using OpenTK.Graphics.ES10;
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.ViewModels;
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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrintMate.Terminal.Views.ComponentsViews
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

        // LED Colors
        private static readonly RadialGradientBrush GreenOnBrush = CreateLedBrush(
            Color.FromRgb(0x6B, 0xE0, 0x6B),  // Light green center
            Color.FromRgb(0x4C, 0xAF, 0x50),  // Medium green
            Color.FromRgb(0x38, 0x8E, 0x3C)); // Dark green edge

        private static readonly RadialGradientBrush RedOnBrush = CreateLedBrush(
            Color.FromRgb(0xF0, 0x6B, 0x6B),  // Light red center
            Color.FromRgb(0xE5, 0x39, 0x35),  // Medium red
            Color.FromRgb(0xC6, 0x28, 0x28)); // Dark red edge

        private static readonly RadialGradientBrush OffBrush = CreateLedBrush(
            Color.FromRgb(0x50, 0x50, 0x50),  // Gray center
            Color.FromRgb(0x35, 0x35, 0x35),  // Medium gray
            Color.FromRgb(0x25, 0x25, 0x25)); // Dark gray edge

        private static RadialGradientBrush CreateLedBrush(Color center, Color mid, Color edge)
        {
            var brush = new RadialGradientBrush();
            brush.GradientStops.Add(new GradientStop(center, 0));
            brush.GradientStops.Add(new GradientStop(mid, 0.6));
            brush.GradientStops.Add(new GradientStop(edge, 1));
            brush.Freeze();
            return brush;
        }

        private bool _currentState = false;

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

        private ILogicControllerObserver observer;
        public PLCLogicIndicator()
        {
            observer = Bootstrapper.ContainerProvider.Resolve<ILogicControllerObserver>();
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Image.Source = unActiveBitmapImage;
            SetLedState(false);

            observer.Subscribe(this, (data) =>
            {
                bool value = (bool)data.Value;
                if (value != _currentState)
                {
                    _currentState = value;
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (value)
                        {
                            Image.Source = activeBitmapImage;
                            SetLedState(true);
                        }
                        else
                        {
                            Image.Source = unActiveBitmapImage;
                            SetLedState(false);
                        }
                    });
                }
            }, CommandInfo);
        }

        private void SetLedState(bool isOn)
        {
            if (LedOff == null) return;

            if (isOn)
            {
                // Set green glow
                LedOff.Fill = GreenOnBrush;
                LedOff.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(0x4C, 0xAF, 0x50),
                    BlurRadius = 12,
                    ShadowDepth = 0,
                    Opacity = 0.8
                };

                // Animate glow appearance
                AnimateLedGlow(true);
            }
            else
            {
                // Set off state
                LedOff.Fill = OffBrush;
                LedOff.Effect = null;

                // Animate glow disappearance
                AnimateLedGlow(false);
            }
        }

        private void AnimateLedGlow(bool show)
        {
            if (LedOff?.Effect is DropShadowEffect effect)
            {
                var animation = new DoubleAnimation
                {
                    From = show ? 0 : 0.8,
                    To = show ? 0.8 : 0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                effect.BeginAnimation(DropShadowEffect.OpacityProperty, animation);
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
