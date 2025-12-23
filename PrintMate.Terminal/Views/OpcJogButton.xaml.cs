using PrintMate.Terminal.Opc;
using PrintMate.Terminal.Services;
using Prism.Ioc;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Opc2Lib;

namespace PrintMate.Terminal.Views
{

    /// <summary>
    /// Логика взаимодействия для OpcJogButton.xaml
    /// </summary>
    public partial class OpcJogButton : UserControl
    {
        public static readonly DependencyProperty DisabledTitleProperty =
            DependencyProperty.Register(
                nameof(DisabledTitle),
                typeof(bool),
                typeof(OpcJogButton),
                new PropertyMetadata(null));

        public bool DisabledTitle
        {
            get => (bool)GetValue(DisabledTitleProperty);
            set => SetValue(DisabledTitleProperty, value);
        }

        public static readonly DependencyProperty CmdProperty =
            DependencyProperty.Register(
                nameof(Cmd),
                typeof(CommandInfo),
                typeof(OpcJogButton),
                new PropertyMetadata(null));

        public CommandInfo Cmd
        {
            get => (CommandInfo)GetValue(CmdProperty);
            set => SetValue(CmdProperty, value);
        }

        public static readonly DependencyProperty DisableTitleProperty =
            DependencyProperty.Register(
                nameof(DisableTitle),
                typeof(bool),
                typeof(OpcJogButton),
                new PropertyMetadata(false));

        public bool DisableTitle
        {
            get => (bool)GetValue(DisableTitleProperty);
            set => SetValue(DisableTitleProperty, value);
        }

        public static readonly DependencyProperty StartImagePathProperty =
            DependencyProperty.Register(
                nameof(StartImagePath),
                typeof(string),
                typeof(OpcJogButton),
                new PropertyMetadata(null));

        public string StartImagePath
        {
            get => (string)GetValue(StartImagePathProperty);
            set => SetValue(StartImagePathProperty, value);
        }

        public static readonly DependencyProperty EndPathProperty =
            DependencyProperty.Register(
                nameof(EndImagePath),
                typeof(string),
                typeof(OpcJogButton),
                new PropertyMetadata(null));

        public string EndImagePath
        {
            get => (string)GetValue(EndPathProperty);
            set => SetValue(EndPathProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(OpcJogButton),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private readonly ILogicControllerProvider _logicControllerService;

        public OpcJogButton()
        {
            _logicControllerService = Bootstrapper.ContainerProvider.Resolve<ILogicControllerProvider>();
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(StartImagePath))
            {
                LeftImageBlock.Visibility = Visibility.Visible;
                RightImageBlock.Visibility = Visibility.Collapsed;
                LeftImage.Source = new BitmapImage(new Uri(StartImagePath, UriKind.Relative));
                LeftTextBlock.Visibility = Visibility.Visible;
            }
            if (!string.IsNullOrEmpty(EndImagePath))
            {
                RightImageBlock.Visibility = Visibility.Visible;
                LeftImageBlock.Visibility = Visibility.Collapsed;
                RightImage.Source = new BitmapImage(new Uri(EndImagePath, UriKind.Relative));
                RightTextBlock.Visibility = Visibility.Visible;
            }
            if (DisableTitle)
            {
                LeftTextBlock.Visibility = Visibility.Collapsed;
                RightTextBlock.Visibility = Visibility.Collapsed;
            }
            if (!string.IsNullOrEmpty(Title))
            {
                LeftTextBlock.Text = Title;
                RightTextBlock.Text = Title;
            }
        }

        private void AnimateScale(double targetScale)
        {
            var animation = new DoubleAnimation
            {
                To = targetScale,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            MainBorder.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            MainBorder.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);

        }

        private async void OpcJogButton_OnTouchDown(object sender, TouchEventArgs e)
        {
            try
            {
                if (!_logicControllerService.Connected)
                {
                    await CustomMessageBox.ShowErrorAsync("Ошибка", "Отсутствует подключение к ПЛК");
                    return;
                }
                //if (!_logicControllerService.OpcProvider.Connected) return;

                AnimateScale(1.1);
                RightImageBlock.Background = Brushes.Red;
                LeftImageBlock.Background = Brushes.Red;

                await _logicControllerService.SetBoolAsync(Cmd, true);
            }
            catch (Exception ex)
            {  
                Console.WriteLine(ex);
            }

        }

        private async void OpcJogButton_OnTouchUp(object sender, TouchEventArgs e)
        {
            try
            {
                if (!_logicControllerService.Connected)
                {
                    await CustomMessageBox.ShowErrorAsync("Ошибка", "Отсутствует подключение к ПЛК");
                    return;
                }

                AnimateScale(1.0);
                RightImageBlock.Background = Brushes.Black;
                LeftImageBlock.Background = Brushes.Black;


                await _logicControllerService.SetBoolAsync(Cmd, false);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

        }

        private async void OpcJogButton_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (TouchScreenHelper.IsTouchScreenAvailable())
                {
                    //MessageBox.ShowDialog(TouchScreenHelper.IsTouchScreenAvailable().ToString());
                    return;
                }
                if (!_logicControllerService.Connected)
                {
                    await CustomMessageBox.ShowErrorAsync("Ошибка", "Отсутствует подключение к ПЛК");
                    return;
                }
                if (_logicControllerService.Connected)
                {
                    //MessageBox.ShowDialog("FFF");
                    await _logicControllerService.SetBoolAsync(Cmd, true);
                }

                AnimateScale(1.1);
                RightImageBlock.Background = Brushes.Red;
                LeftImageBlock.Background = Brushes.Red;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private async void OpcJogButton_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (TouchScreenHelper.IsTouchScreenAvailable()) return;
                if (!_logicControllerService.Connected)
                {
                    await CustomMessageBox.ShowErrorAsync("Ошибка", "Отсутствует подключение к ПЛК");
                    return;
                }
                if (_logicControllerService.Connected)
                {
                    await _logicControllerService.SetBoolAsync(Cmd, false);
                }


                AnimateScale(1.0);
                RightImageBlock.Background = Brushes.Black;
                LeftImageBlock.Background = Brushes.Black;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }

        }
    }
}