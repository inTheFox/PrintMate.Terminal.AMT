using Emgu.CV;
using HandyControl.Tools.Command;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels
{
    public class ConfigureParametersCameraViewModel : BindableBase
    {
        public RelayCommand SelectCameraCommand { get; set; }

        private readonly ModalService _modalService;

        private CameraItem _selectedCamera;
        public CameraItem SelectedCamera
        {
            get => _selectedCamera;
            set => SetProperty(ref _selectedCamera, value, OnSelectedCameraChanged);
        }

        private string _selectedCameraText;
        public string SelectedCameraText
        {
            get => _selectedCameraText;
            set => SetProperty(ref _selectedCameraText, value);
        }

        private ObservableCollection<CameraItem> _availableCameras;
        public ObservableCollection<CameraItem> AvailableCameras
        {
            get => _availableCameras;
            set => SetProperty(ref _availableCameras, value);
        }

        private void OnSelectedCameraChanged()
        {
            if (SelectedCamera != null)
            {
                SelectedCameraText = SelectedCamera.Name;
                _eventAggregator.GetEvent<OnCameraSelectedEvent>().Publish(SelectedCamera);

                // Обновляем флаг IsSelected для всех камер
                foreach (var camera in AvailableCameras)
                {
                    camera.IsSelected = (camera.Id == SelectedCamera.Id);
                }
            }
        }

        private readonly IEventAggregator _eventAggregator;
        private readonly ConfigurationManager _configurationManager;

        public ConfigureParametersCameraViewModel(ModalService modalService, IEventAggregator eventAggregator, ConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
            _eventAggregator = eventAggregator;
            _modalService = modalService;

            AvailableCameras = new ObservableCollection<CameraItem>();
            LoadAvailableCameras();
            LoadSelectedCamera();

            SelectCameraCommand = new RelayCommand(OnCameraSelectCommand);
        }

        private void LoadAvailableCameras()
        {
            AvailableCameras.Clear();

            // Получаем список камер через Emgu.CV
            int cameraIndex = 0;
            while (cameraIndex < 10)
            {
                using (var capture = new VideoCapture(cameraIndex))
                {
                    if (capture.IsOpened)
                    {
                        AvailableCameras.Add(new CameraItem
                        {
                            Id = cameraIndex,
                            Name = $"Camera {cameraIndex}"
                        });
                    }
                    else
                    {
                        break;
                    }
                }
                cameraIndex++;
            }
        }

        private void LoadSelectedCamera()
        {
            var settings = _configurationManager.Get<CameraSettings>();
            int selectedIndex = settings.SelectedCameraIndex;

            // Находим камеру по индексу
            foreach (var camera in AvailableCameras)
            {
                if (camera.Id == selectedIndex)
                {
                    SelectedCamera = camera;
                    return;
                }
            }

            // Если не найдена, выбираем первую
            if (AvailableCameras.Count > 0)
            {
                SelectedCamera = AvailableCameras[0];
            }
            else
            {
                SelectedCameraText = "Камеры не обнаружены";
            }
        }

        private void OnCameraSelectCommand(object obj)
        {
            if (obj is CameraItem camera)
            {
                SelectedCamera = camera;
                // Сохраняем индекс камеры в настройки
                _configurationManager.Get<CameraSettings>().SelectedCameraIndex = camera.Id;
                _configurationManager.SaveNow();
            }
        }
    }
}
