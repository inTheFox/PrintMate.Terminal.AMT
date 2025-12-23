using Prism.Regions;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace PrintMate.Terminal.Views
{
    public partial class IntroVideoView : UserControl
    {
        private readonly IRegionManager _regionManager;
        private bool _hasNavigated;

        public IntroVideoView(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            InitializeComponent();
            Loaded += IntroVideoView_Loaded;
            Unloaded += IntroVideoView_Unloaded;
        }

        private void IntroVideoView_Loaded(object sender, RoutedEventArgs e)
        {
            _hasNavigated = false;
            VideoPlayer.Opacity = 1;

            var videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Videos", "intro.mp4");

            if (File.Exists(videoPath))
            {
                VideoPlayer.Source = new Uri(videoPath, UriKind.Absolute);
                VideoPlayer.Play();
            }
            else
            {
                Console.WriteLine($"[IntroVideoView] Video not found: {videoPath}");
                NavigateToWelcome();
            }
        }

        private void IntroVideoView_Unloaded(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Stop();
            VideoPlayer.Source = null;
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            FadeOutAndNavigate();
        }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Console.WriteLine($"[IntroVideoView] Media failed: {e.ErrorException?.Message}");
            NavigateToWelcome();
        }

        private void FadeOutAndNavigate()
        {
            if (_hasNavigated) return;
            _hasNavigated = true;

            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            fadeOut.Completed += (s, e) =>
            {
                VideoPlayer.Stop();
                VideoPlayer.Source = null;
                _regionManager.RequestNavigate("RootRegion", nameof(WelcomeView));
            };

            VideoPlayer.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void NavigateToWelcome()
        {
            if (_hasNavigated) return;
            _hasNavigated = true;

            VideoPlayer.Stop();
            VideoPlayer.Source = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _regionManager.RequestNavigate("RootRegion", nameof(WelcomeView));
            });
        }
    }
}
