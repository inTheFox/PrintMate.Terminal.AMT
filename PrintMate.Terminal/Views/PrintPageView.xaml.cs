using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels;
using PrintMate.Terminal.ViewModels.PagesViewModels;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrintMate.Terminal.Views
{
    /// <summary>
    /// Логика взаимодействия для PrintPageView.xaml
    /// </summary>
    public partial class PrintPageView : UserControl
    {
        private readonly CameraService _cameraService;
        public PrintPageView(CameraService cameraService)
        {
            _cameraService = cameraService;
            _cameraService.OnUpdated += CameraServiceOnOnUpdated;
            _cameraService.OnLoadingStateChanged += CameraServiceOnLoadingStateChanged;
            Loaded += UserControl_Loaded;
            InitializeComponent();
        }

        private void CameraServiceOnOnUpdated(BitmapSource obj)
        {
            if (CameraBorder.Visibility == Visibility.Visible && CameraImage != null && obj != null)
            {
                Application.Current.Dispatcher.InvokeAsync(() => CameraImage.Source = obj);
            }
        }

        private void CameraServiceOnLoadingStateChanged(bool isLoading)
        {
            CameraLoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Подключаем ViewModel к Viewport и LayerCanvas
            if (DataContext is PrintPageViewModel viewModel)
            {
                // Подключаем DX11 Viewport (основной 3D визуализатор)
                //viewModel.SetViewport(Viewport);

                // Опционально: подключаем LayerCanvas для 2D превью
                viewModel.SetLayerCanvas(LayerCanvas);
            }
        }

        private async void OnCameraSelect(object sender, MouseButtonEventArgs e)
        {
            CameraBorder.Visibility = Visibility.Visible;
            LayerCanvasBorder.Visibility = Visibility.Collapsed;

            // Ленивый запуск камеры при первом показе
            await _cameraService.EnsureStartedAsync();
        }

        private void On2DSelect(object sender, MouseButtonEventArgs e)
        {
            CameraBorder.Visibility = Visibility.Collapsed;
            LayerCanvasBorder.Visibility = Visibility.Visible;
        }
    }
}
