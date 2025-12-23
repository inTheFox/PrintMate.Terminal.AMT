using System.Windows;
using System.Windows.Controls;
using PrintMate.Terminal.ViewModels.PagesViewModels;

namespace PrintMate.Terminal.Views.Pages
{
    /// <summary>
    /// Страница 3D просмотра проекта с DirectX 11 рендерингом
    /// </summary>
    public partial class Project3DView : UserControl
    {
        public Project3DView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Подключаем Viewport к ViewModel
            if (DataContext is Project3DViewModel viewModel)
            {
                viewModel.SetViewport(Viewport);
            }
        }
    }
}
