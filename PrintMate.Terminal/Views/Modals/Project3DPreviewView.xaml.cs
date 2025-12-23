using System.Windows;
using System.Windows.Controls;
using PrintMate.Terminal.ViewModels.ModalsViewModels;

namespace PrintMate.Terminal.Views.Modals
{
    /// <summary>
    /// 3D просмотр проекта с DirectX 11 рендерингом
    /// </summary>
    public partial class Project3DPreviewView : UserControl
    {
        public Project3DPreviewView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Подключаем Viewport к ViewModel
            if (DataContext is Project3DPreviewViewModel viewModel)
            {
                viewModel.SetViewport(Viewport);
            }
        }
    }
}
