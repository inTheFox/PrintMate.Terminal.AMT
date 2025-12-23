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

namespace PrintMate.Terminal.Views.Configure.ConfigureParametersViews
{
    /// <summary>
    /// Логика взаимодействия для ConfigureParametersAdditionalSoftware.xaml
    /// </summary>
    public partial class ConfigureParametersAdditionalSoftware : UserControl
    {
        public ConfigureParametersAdditionalSoftware()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //var position = TestBorder.GetActualPosition();
            //MessageBox.ShowDialog($"X: {position.X}, Y: {position.Y}");
        }

        private void ScrollViewer_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true; // Подавляем "отскок" и передачу дальше
        }
    }
}
