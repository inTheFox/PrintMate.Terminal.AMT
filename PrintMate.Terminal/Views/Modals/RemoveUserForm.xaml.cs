using PrintMate.Terminal.Interfaces;
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

namespace PrintMate.Terminal.Views.Modals
{
    /// <summary>
    /// Логика взаимодействия для AddUserViewModelForm.xaml
    /// </summary>
    public partial class RemoveUserForm : UserControl
    {

        public RemoveUserForm()
        {
            InitializeComponent();
            //Loaded += (sender, args) => MessageBox.ShowDialog(DataContext.ToString());
        }
    }
}
