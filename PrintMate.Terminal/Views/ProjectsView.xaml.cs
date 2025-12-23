using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;
using Prism.Regions;
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
using Veldrid.Sdl2;

namespace PrintMate.Terminal.Views
{
    /// <summary>
    /// Логика взаимодействия для ProjectsView.xaml
    /// </summary>
    public partial class ProjectsView : UserControl
    {
        private readonly ModalService _modalService;
        public ProjectsView(ModalService modalService)
        {
            _modalService = modalService;
            InitializeComponent();

            // Подписываемся на событие запроса прокрутки к началу
            this.DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Отписываемся от предыдущего ViewModel
            if (e.OldValue is ProjectsViewViewModel oldViewModel)
            {
                oldViewModel.ScrollToTopRequested -= ScrollToTop;
            }

            // Подписываемся на новый ViewModel
            if (e.NewValue is ProjectsViewViewModel newViewModel)
            {
                newViewModel.ScrollToTopRequested += ScrollToTop;
            }
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            _modalService.Show<AddProjectWizard, AddProjectWizardViewModel>(
                modalId: nameof(AddProjectWizard)
            );
        }

        /// <summary>
        /// Сбрасывает прокрутку списка проектов в начало
        /// </summary>
        private void ScrollToTop()
        {
            ProjectsScrollViewer?.ScrollToTop();
        }
    }
}
