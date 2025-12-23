using System.Windows.Controls;
using System.Windows.Input;

namespace PrintMate.Terminal.Views.Configure.ConfigureParametersViews
{
    /// <summary>
    /// Логика взаимодействия для ConfigureParametersStorage.xaml
    /// </summary>
    public partial class ConfigureParametersStorage : UserControl
    {
        public ConfigureParametersStorage()
        {
            InitializeComponent();
        }

        private void ScrollViewer_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true; // Подавляем "отскок" и передачу дальше
        }
    }
}
