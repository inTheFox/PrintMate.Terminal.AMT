using PrintMate.Terminal.Views.ComponentsViews;
using Prism.Regions;
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

namespace PrintMate.Terminal.Views.Configure.ConfigureProcessViews
{
    /// <summary>
    /// Логика взаимодействия для ConfigureProcessSystemView.xaml
    /// </summary>
    public partial class ConfigureProcessSystemView : UserControl
    {
        // Регистрируем анимируемое свойство
        public static readonly DependencyProperty ScrollOffsetProperty =
            DependencyProperty.Register(
                "ScrollOffset",
                typeof(double),
                typeof(ConfigureProcessSystemView),
                new PropertyMetadata(0.0, OnScrollOffsetChanged));

        public double ScrollOffset
        {
            get => (double)GetValue(ScrollOffsetProperty);
            set => SetValue(ScrollOffsetProperty, value);
        }

        private static void OnScrollOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ConfigureProcessSystemView view && view.SmoothScrollViewer != null)
            {
                view.SmoothScrollViewer.ScrollToVerticalOffset((double)e.NewValue);
            }
        }

        public ConfigureProcessSystemView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.LoadModel(this);

            Signals.Opacity = 0;
            Manage.Opacity = 0;
            Setups.Opacity = 0;
            Sensors.Opacity = 0;

            var duration = TimeSpan.FromMilliseconds(500); // длительность каждой анимации
            var delay = TimeSpan.FromMilliseconds(100);    // задержка между анимациями

            var storyboard = new Storyboard();

            AddOpacityAnimation(storyboard, Signals, 1.0, TimeSpan.Zero, duration);
            AddOpacityAnimation(storyboard, Manage, 1.0, delay, duration);
            AddOpacityAnimation(storyboard, Setups, 1.0, delay * 2, duration);
            AddOpacityAnimation(storyboard, Sensors, 1.0, delay * 3, duration);

            storyboard.Begin();
        }

        void AddOpacityAnimation(Storyboard sb, UIElement element, double toValue, TimeSpan beginTime, TimeSpan dur)
        {
            var animation = new DoubleAnimation
            {
                To = toValue,
                Duration = dur,
                BeginTime = beginTime,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
            sb.Children.Add(animation);
        }

        private void ScrollViewer_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true; // Подавляем "отскок" и передачу дальше
        }

    }
}
