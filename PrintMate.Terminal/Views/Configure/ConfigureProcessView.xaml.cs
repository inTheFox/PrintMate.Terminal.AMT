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
using PrintMate.Terminal.Views.ComponentsViews;

namespace PrintMate.Terminal.Views.Configure
{
    /// <summary>
    /// Логика взаимодействия для ConfigureProcessView.xaml
    /// </summary>
    public partial class ConfigureProcessView : UserControl
    {
        public ConfigureProcessView()
        {
            InitializeComponent();
            Loaded += (sender, args) => this.LoadModel(this);
        }
    }
}
