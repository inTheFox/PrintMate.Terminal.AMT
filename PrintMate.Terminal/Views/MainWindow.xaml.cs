using HandyControl.Controls;
using Microsoft.Extensions.DependencyInjection;
using Opc2Lib;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.Services;
using Prism.Events;
using Prism.Regions;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using HansScannerHost.Models;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace PrintMate.Terminal.Views
{
    public partial class MainWindow
    {
        private readonly LoggerService _loggerService;
        private readonly IRegionManager _regionManager;

        private const int WM_INPUTLANGCHANGE = 0x0051;

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        private readonly ILogicControllerProvider _provider;
        private readonly ILogicControllerObserver _observer;
        private readonly IEventAggregator _eventAggregator;



        public MainWindow(IEventAggregator eventsAggregator, LoggerService loggerService, IRegionManager regionManager, IServiceProvider serviceProvider, ILogicControllerProvider provider, ILogicControllerObserver observer, ModalService modalService)
        {
            _observer = observer;
            _observer.Subscribe(this, (data) =>
            {
                if (!_provider.Connected) return;

                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (data.Value != null && data.Value is bool value && !value)
                    {
                        CanvasBackground.Visibility = Visibility.Visible;
                        CanvasText.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        CanvasBackground.Visibility = Visibility.Collapsed;
                        CanvasText.Visibility = Visibility.Collapsed;
                    }
                });
            }, OpcCommands.Trig_SensorKeyLocked);


            _provider = provider;
            _loggerService = loggerService;
            _regionManager = regionManager;
            _eventAggregator = eventsAggregator;

            // Подписываемся на событие выхода пользователя
            _eventAggregator.GetEvent<OnUserQuit>().Subscribe(OnUserQuit);

            // Передаём DI-контейнер в BlazorWebView
            Resources.Add("services", serviceProvider);
            InitializeComponent();

            // Инициализация сервиса уведомлений
            NotificationService.Initialize(NotificationContainer);

            // Инициализация ModalService с поддержкой размытия фона
            modalService.Initialize(ModalContainer, ModalOverlay, RootBlurEffect);

#if ATM16_DEBUG
            Width = 1024;
            Height = 768;

#endif
#if ATM32_DEBUG
            Width = 1920;
            Height = 1080;
#endif

            //WindowStyle = WindowStyle.None;
            Loaded += MainWindow_Loaded;
        }


        private async void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            //OpcManager.Init();
            //await _provider.ConnectAsync();
            //MessageBox.ShowDialog($"Test: {await _provider.GetFloatAsync(OpcCommands.AM_PChamber_Pressure)}");



            var screens = Screen.AllScreens;
            if (screens.Length > 1)
            {
                var targetScreen = screens[1];
                this.Left = targetScreen.WorkingArea.Left + (targetScreen.WorkingArea.Width - this.ActualWidth) / 2;
                this.Top = targetScreen.WorkingArea.Top + (targetScreen.WorkingArea.Height - this.ActualHeight) / 2;
            }

            //_regionManager.RequestNavigate(Bootstrapper.LeftBarRegion, nameof(LeftBarView));
            //_regionManager.RequestNavigate(Bootstrapper.RightBarRegion, nameof(RightBarView));


            _regionManager.RequestNavigate("RootRegion", nameof(IntroVideoView));
            //Task.Factory.StartNew(async () =>
            //{
            //    await Task.Delay(3000);
            //    Application.Current.Dispatcher.InvokeAsync(() => _regionManager.RequestNavigate(Bootstrapper.LeftBarRegion, nameof(LeftBarView)));
            //    Application.Current.Dispatcher.InvokeAsync(() => _regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(MainView)));
            //    Application.Current.Dispatcher.InvokeAsync(() => _regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(RightBarView)));
            //});

            //_loggerService.StartLoggerEngine();
            //await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            //{
            //    await _projectService.Test();
            //});
        }

        private bool _blockScreenAnimationProcess = false;
        private async void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
            if (!_provider.Connected) return;

            //e.Handled = true;
            //return;


            if (_blockScreenAnimationProcess) return;

            // === Настройки анимации (можно вынести в поля или свойства) ===
            var fadeInDuration = TimeSpan.FromMilliseconds(500);   // длительность появления
            var visibleDuration = TimeSpan.FromMilliseconds(2000); // сколько блок будет виден
            var fadeOutDuration = TimeSpan.FromMilliseconds(500);  // длительность исчезновения

            BlockInfo.Visibility = Visibility.Visible;

            // Плавное появление
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = fadeInDuration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var backgroundIn = new DoubleAnimation
            {
                From = 0,
                To = 0.9,
                Duration = fadeInDuration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            _blockScreenAnimationProcess = true;
            BlockInfo.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            CanvasBackground.BeginAnimation(UIElement.OpacityProperty, backgroundIn);
            //CanvasBackground.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            // Ждём, пока блок будет виден
            await Task.Delay(visibleDuration);

            // Плавное исчезновение
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = fadeOutDuration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            var backgroundOut = new DoubleAnimation
            {
                From = 0.9,
                To = 0,
                Duration = fadeInDuration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            fadeOut.Completed += (o, args) =>
            {
                _blockScreenAnimationProcess = false;
                BlockInfo.Visibility = Visibility.Collapsed;
            };
            BlockInfo.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            CanvasBackground.BeginAnimation(UIElement.OpacityProperty, backgroundOut);
            //CanvasBackground.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void OnUserQuit()
        {
            // Переход на экран авторизации при выходе пользователя
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _regionManager.RequestNavigate("RootRegion", nameof(WelcomeView));
            });
        }
    }
}
