using PrintMate.Terminal.Views.ComponentsViews;
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

namespace PrintMate.Terminal.Views.Configure
{
    /// <summary>
    /// Логика взаимодействия для ConfigureParameters.xaml
    /// </summary>
    public partial class ConfigureParameters : UserControl
    {
        public ConfigureParameters()
        {
            InitializeComponent();
            Loaded += (sender, args) => this.LoadModel(this);
        }
    }
}
