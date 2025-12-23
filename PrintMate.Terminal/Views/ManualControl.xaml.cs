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
using Prism.Regions;

namespace PrintMate.Terminal.Views
{
    /// <summary>
    /// Логика взаимодействия для ManualControl.xaml
    /// </summary>
    public partial class ManualControl : UserControl
    {
        public ManualControl(IRegionManager regionManager)
        {
            InitializeComponent();
            Loaded += (sender, args) => regionManager.RequestNavigate(Bootstrapper.ManualContent, nameof(ManualAxesControl));
        }
    }
}
