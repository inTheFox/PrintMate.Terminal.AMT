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
    /// Логика взаимодействия для BoolSwitchContainer.xaml
    /// </summary>
    public partial class BoolSwitchContainer : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(BoolSwitchContainer),
                new PropertyMetadata(string.Empty));
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(
                nameof(IsChecked),
                typeof(bool),
                typeof(BoolSwitchContainer),
                new PropertyMetadata(false));
        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }


        public BoolSwitchContainer()
        {
            InitializeComponent();
        }
    }
}
