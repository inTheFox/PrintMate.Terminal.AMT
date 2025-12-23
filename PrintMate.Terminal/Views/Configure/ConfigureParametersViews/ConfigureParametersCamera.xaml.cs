using PrintMate.Terminal.Events;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using Prism.Events;
using System.Windows;
using System.Windows.Controls;
using PrintMate.Terminal.ConfigurationSystem.Core;

namespace PrintMate.Terminal.Views.Configure.ConfigureParametersViews
{
    /// <summary>
    /// Логика взаимодействия для ConfigureParametersCamera.xaml
    /// </summary>
    public partial class ConfigureParametersCamera : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ConfigurationManager _configurationManager;
        public ConfigureParametersCamera(IEventAggregator eventAggregator, ConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<OnCameraSelectedEvent>().Subscribe(OnCameraSelected);

            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //StartCamera();
        }

        private void OnCameraSelected(CameraItem obj)
        {
            //_selectedCameraIndex = obj.Id;
            //StopCamera();
            //StartCamera();
        }

        private async void StartCamera()
        {
            //StopCamera();
            //_videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            //if (_videoDevices.Count <= 0)
            //{
            //    await CustomMessageBox.ShowErrorAsync("Ошибка", "Камеры не обнаружены !");
            //    return;
            //}

            //int deviceId = 0;

            //CameraSettings settings = _configurationManager.Get<CameraSettings>();
            //if (settings != null)
            //{
            //    deviceId = settings.SelectedCameraIndex;
            //}

            //_videoSource = new VideoCaptureDevice(_videoDevices[_selectedCameraIndex].MonikerString);
            //_videoSource.NewFrame += VideoSource_NewFrame;
            //_videoSource.Start();
        }

        private void VideoSource_NewFrame(object sender, object e)
        {
            // Закомментировано - теперь используется CameraService
        }

        private void StopCamera()
        {
            //try
            //{
            //    if (_videoSource != null)
            //    {
            //        _videoSource.NewFrame -= VideoSource_NewFrame;
            //        if (_videoSource.IsRunning)
            //        {
            //            _videoSource.SignalToStop();
            //            _videoSource.WaitForStop();
            //        }
            //        _videoSource = null;
            //    }

            //    Dispatcher.BeginInvoke(new Action(() => CameraImage.Source = null));
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //    throw;
            //}
        }
    }
}
