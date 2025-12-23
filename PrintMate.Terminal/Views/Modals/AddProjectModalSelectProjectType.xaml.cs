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
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using Prism.Ioc;

namespace PrintMate.Terminal.Views.Modals
{
    /// <summary>
    /// Логика взаимодействия для AddProjectModalSelectProjectType.xaml
    /// </summary>
    public partial class AddProjectModalSelectProjectType : UserControl
    {
        public event Action<string> OnNext;

        private readonly AddProjectModalSelectProjectTypeViewModel _viewModel;

        public AddProjectModalSelectProjectType()
        {
            InitializeComponent();
            _viewModel = Bootstrapper.ContainerProvider.Resolve<AddProjectModalSelectProjectTypeViewModel>();
            DataContext = _viewModel;
        }

        private void Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Получаем Border, по которому кликнули
            var border = sender as Border;
            if (border == null) return;

            // Получаем родительский ListBoxItem через TemplatedParent
            var listBoxItem = border.TemplatedParent as ListBoxItem;
            if (listBoxItem == null) return;

            // Устанавливаем IsSelected вручную
            listBoxItem.IsSelected = true;

            // Помечаем событие как обработанное
            e.Handled = true;
        }

        private void NextButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Помечаем событие как обработанное
            if (e != null)
                e.Handled = true;

            // Проверяем, что выбран тип проекта
            if (_viewModel.SelectedProjectType == null)
            {
                MessageBox.Show("Пожалуйста, выберите тип проекта");
                return;
            }

            //MessageBox.Show(_viewModel.SelectedProjectType.Name);
            OnNext?.Invoke(_viewModel.SelectedProjectType.Format);
        }
    }
}
