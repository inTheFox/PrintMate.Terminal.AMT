using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.PagesViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PrintMate.Terminal.Views.Pages
{
    /// <summary>
    /// 3D визуализация CLI проектов с DirectX 11
    /// </summary>
    public partial class ProjectViewer3D : UserControl
    {
        private readonly CameraService _cameraService;
        private bool _isCameraExpanded = false;

        public ProjectViewer3D(CameraService cameraService)
        {
            _cameraService = cameraService;
            _cameraService.OnUpdated += CameraServiceOnOnUpdated;
            Initialized += ProjectViewer3D_Initialized;
            InitializeComponent();
            //SizeChanged += ProjectViewer3D_SizeChanged;
        }

        private void ProjectViewer3D_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                UpdateLayout_Adaptive(e.NewSize.Width, e.NewSize.Height);
            }
        }

        private void UpdateLayout_Adaptive(double width, double height)
        {
            // Правое меню - всегда справа
            Canvas.SetLeft(RightMenu, width - 300);

            // Прогресс-бары снизу по центру (смещение с учётом правой панели)
            if (this.FindName("ProgressPanel") is FrameworkElement progressPanel)
            {
                Canvas.SetLeft(progressPanel, (width - 300 - progressPanel.ActualWidth) / 2);
                Canvas.SetTop(progressPanel, height - progressPanel.ActualHeight - 50);
            }

            // Индикатор загрузки по центру
            if (this.FindName("LoadingIndicator") is FrameworkElement loadingIndicator)
            {
                Canvas.SetLeft(loadingIndicator, (width - loadingIndicator.Width) / 2);
                Canvas.SetTop(loadingIndicator, (height - loadingIndicator.Height) / 2);
            }

            // Обновляем позицию камеры если она не развёрнута
            if (!_isCameraExpanded)
            {
                Canvas.SetLeft(CameraBorder, width - 300 - 250 - 60);
                Canvas.SetTop(CameraBorder, height - 250 - 50);
            }
        }

        private void CameraServiceOnOnUpdated(BitmapSource obj)
        {
            if (CameraSource != null && obj != null)
            {
                Application.Current.Dispatcher.InvokeAsync(()=> CameraSource.Source = obj);
            }
        }

        private void ProjectViewer3D_Initialized(object sender, System.EventArgs e)
        {
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Подключаем ViewModel к Viewport и LayerCanvas
            if (DataContext is ProjectViewer3DViewModel viewModel)
            {
                // Подключаем DX11 Viewport (основной 3D визуализатор)
                //viewModel.SetViewport(Viewport);

                // Опционально: подключаем LayerCanvas для 2D превью
                viewModel.SetLayerCanvas(LayerCanvasFullscreen);
            }
        }

        // Публичный доступ к viewport для ViewModel
        //public Controls.DX11ViewportControl GetViewport() => Viewport;

        private void CameraClickCallback(object sender, MouseButtonEventArgs e)
        {
            // Подключаем ViewModel к Viewport и LayerCanvas
            if (DataContext is ProjectViewer3DViewModel viewModel)
            {
                if (viewModel.ShadowVisibility == Visibility.Visible)
                {
                    // Разворачиваем камеру на весь экран
                    _isCameraExpanded = true;
                    viewModel.ShadowVisibility = Visibility.Collapsed;
                    Canvas.SetLeft(CameraBorder, 0);
                    Canvas.SetTop(CameraBorder, 0);
                    CameraBorder.Width = RootCanvas.ActualWidth;
                    CameraBorder.Height = RootCanvas.ActualHeight;
                    Panel.SetZIndex(CameraBorder, 10);
                    RightMenu.Opacity = 0.5;
                }
                else
                {
                    // Сворачиваем камеру обратно
                    _isCameraExpanded = false;
                    viewModel.ShadowVisibility = Visibility.Visible;
                    CameraBorder.Width = 250;
                    CameraBorder.Height = 250;
                    Panel.SetZIndex(CameraBorder, 5);
                    RightMenu.Opacity = 1;

                    // Вычисляем адаптивную позицию
                    Canvas.SetLeft(CameraBorder, RootCanvas.ActualWidth - 300 - 250 - 60);
                    Canvas.SetTop(CameraBorder, RootCanvas.ActualHeight - 250 - 50);
                }
            }
        }
    }
}
