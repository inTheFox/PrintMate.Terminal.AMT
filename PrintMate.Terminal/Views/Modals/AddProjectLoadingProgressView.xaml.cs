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
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Views.Modals
{
    /// <summary>
    /// Логика взаимодействия для AddProjectLoadingProgressView.xaml
    /// </summary>
    public partial class AddProjectLoadingProgressView : UserControl
    {
        public AddProjectLoadingProgressView()
        {
            InitializeComponent();
            DataContext = Bootstrapper.ContainerProvider.Resolve<AddProjectLoadingProgressViewModel>();
        }
    }
}
