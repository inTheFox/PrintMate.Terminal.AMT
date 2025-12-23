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

namespace PrintMate.Terminal.Views.ComponentsViews
{
    /// <summary>
    /// Логика взаимодействия для ParameterAndValueView.xaml
    /// </summary>
    public partial class ParameterAndValueView : UserControl
    {
        public static readonly DependencyProperty ParameterNameProperty =
            DependencyProperty.Register(
                nameof(ParameterName),
                typeof(string),
                typeof(ParameterAndValueView),
                new PropertyMetadata("ParameterName"));

        public string ParameterName
        {
            get => (string)GetValue(ParameterNameProperty);
            set => SetValue(ParameterNameProperty, value);
        }

        public static readonly DependencyProperty ParameterValueProperty =
            DependencyProperty.Register(
                nameof(ParameterValue),
                typeof(string),
                typeof(ParameterAndValueView),
                new PropertyMetadata("ParameterValue"));

        public string ParameterValue
        {
            get => (string)GetValue(ParameterValueProperty);
            set => SetValue(ParameterValueProperty, value);
        }



        public ParameterAndValueView()
        {
            InitializeComponent();
        }
    }
}
