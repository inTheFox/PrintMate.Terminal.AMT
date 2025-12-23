using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using Prism.Ioc;
using MessageBox = System.Windows.MessageBox;

namespace PrintMate.Terminal.Views.Modals
{
    /// <summary>
    /// Логика взаимодействия для DirectoryPickerControl.xaml
    /// </summary>
    public partial class ProjectDirectoryPicker : UserControl
    {

        public static readonly DependencyProperty ShowFilesProperty =
            DependencyProperty.Register(
                nameof(ShowFiles),
                typeof(bool),
                typeof(ProjectDirectoryPicker),
                new PropertyMetadata(false));
        public bool ShowFiles
        {
            get => (bool)GetValue(ShowFilesProperty);
            set => SetValue(ShowFilesProperty, value);
        }

        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register(
                nameof(Format),
                typeof(string),
                typeof(ProjectDirectoryPicker),
                new PropertyMetadata(".cnc"));

        public string Format
        {
            get => (string)GetValue(FormatProperty);
            set
            {
                SetValue(FormatProperty, value);
                ViewModel!.SelectedFormat = value;
            }
        }

        public ProjectDirectoryPickerViewModel ViewModel = null;
        public event Action<string> OnSelected;


        public ProjectDirectoryPicker()
        {
            InitializeComponent();
            Initialized += OnInitialized;
            DataContext = Bootstrapper.ContainerProvider.Resolve<ProjectDirectoryPickerViewModel>();
            ViewModel = (ProjectDirectoryPickerViewModel)DataContext;
            ViewModel.OnNext += ViewModel_OnNext;
            ViewModel.OnClose += ViewModel_OnClose;
        }

        private void ViewModel_OnClose()
        {
            //MessageBox.Show("Close");
            ModalService.Instance.Close();
        }

        private void ViewModel_OnNext(string obj)
        {
            //MessageBox.Show("Selected");
            OnSelected?.Invoke(obj);
        }

        private void OnInitialized(object sender, EventArgs e)
        {
            // REMOVED: MessageBox.Show was blocking UI thread
            ViewModel = DataContext as ProjectDirectoryPickerViewModel;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
