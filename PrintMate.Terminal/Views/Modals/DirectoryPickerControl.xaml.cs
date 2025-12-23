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
using HandyControl.Controls;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using Prism.Ioc;
using MessageBox = System.Windows.MessageBox;

namespace PrintMate.Terminal.Views.Modals
{
    /// <summary>
    /// Логика взаимодействия для DirectoryPickerControl.xaml
    /// </summary>
    public partial class DirectoryPickerControl : UserControl
    {

        public static readonly DependencyProperty ShowFilesProperty =
            DependencyProperty.Register(
                nameof(ShowFiles),
                typeof(bool),
                typeof(DirectoryPickerControl),
                new PropertyMetadata(false));
        public bool ShowFiles
        {
            get => (bool)GetValue(ShowFilesProperty);
            set => SetValue(ShowFilesProperty, value);
        }

        public static readonly DependencyProperty AllowedTypesProperty =
            DependencyProperty.Register(
                nameof(AllowedTypes),
                typeof(IEnumerable<string>),
                typeof(DirectoryPickerControl),
                new PropertyMetadata(null));

        public IEnumerable<string> AllowedTypes
        {
            get => (IEnumerable<string>)GetValue(AllowedTypesProperty);
            set
            {
                SetValue(AllowedTypesProperty, value);
                ViewModel!.AllowedTypes = AllowedTypes?.ToList();
            }
        }

        public DirectoryPickerControlViewModel ViewModel = null;

        public DirectoryPickerControl()
        {
            InitializeComponent();
            Initialized += OnInitialized;
            DataContext = Bootstrapper.ContainerProvider.Resolve<DirectoryPickerControlViewModel>();
            ViewModel = (DirectoryPickerControlViewModel)DataContext;
        }

        private void OnInitialized(object sender, EventArgs e)
        {
            MessageBox.Show(ViewModel.GetType().Name);
            ViewModel = DataContext as DirectoryPickerControlViewModel;
            ViewModel!.AllowedTypes = AllowedTypes?.ToList();
            if (ShowFiles)
            {
                ViewModel!.ShowFiles = true;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
