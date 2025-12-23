using Emgu.CV;
using System.Collections.ObjectModel;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Services;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class CameraItem : BindableBase
    {
        public int Id { get; set; }
        public string Name { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public class CameraSelectModalViewModel : BindableBase
    {
        private ObservableCollection<CameraItem> _cameraCollection;
        public ObservableCollection<CameraItem> CameraCollection
        {
            get => _cameraCollection;
            set => SetProperty(ref _cameraCollection, value);
        }

        private CameraItem _selectedCamera;
        public CameraItem SelectedCamera
        {
            get => _selectedCamera;
            set => SetProperty(ref _selectedCamera, value);
        }

        public RelayCommand SelectCameraCommand { get; set; }

        public CameraSelectModalViewModel()
        {
            CameraCollection = new ObservableCollection<CameraItem>();

            // Получаем список камер через Emgu.CV
            int cameraIndex = 0;
            while (cameraIndex < 10)
            {
                using (var capture = new VideoCapture(cameraIndex))
                {
                    if (capture.IsOpened)
                    {
                        CameraCollection.Add(new CameraItem
                        {
                            Id = cameraIndex,
                            Name = $"Camera {cameraIndex}"
                        });
                    }
                    else
                    {
                        break; // Если камера не открылась, прекращаем поиск
                    }
                }
                cameraIndex++;
            }

            SelectCameraCommand = new RelayCommand(OnCameraSelectCommand);
        }

        private void OnCameraSelectCommand(object obj)
        {
            SelectedCamera = (CameraItem)obj;
            ModalService.Instance.CloseAsync(isSuccess: true);
        }
    }
}
