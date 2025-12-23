using System;
using System.Collections.Generic;
using System.IO;
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
    /// Логика взаимодействия для DirectoryItemComponent.xaml
    /// </summary>
    public partial class DirectoryItemComponent : UserControl
    {
        public const string FolderPath = "/images/folder.png";
        public const string FilePath = "/images/file.png";

        public static readonly DependencyProperty PathProperty =
            DependencyProperty.Register(
                nameof(Path),
                typeof(string),
                typeof(DirectoryItemComponent),
                new PropertyMetadata("N/A"));
        public string Path
        {
            get => (string)GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(
                nameof(Name),
                typeof(string),
                typeof(DirectoryItemComponent),
                new PropertyMetadata("N/A"));
        protected string Name
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register(
                nameof(ImagePath),
                typeof(string),
                typeof(DirectoryItemComponent),
                new PropertyMetadata(FolderPath));
        public string ImagePath
        {
            get => (string)GetValue(ImagePathProperty);
            set => SetValue(ImagePathProperty, value);
        }

        public DirectoryItemComponent()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.Path == "back")
            {
                Name = "Назад";
                ImagePath = "/images/backspace_white.png";
            }
            else
            {
                Name = System.IO.Path.GetFileName(Path);
            }

            if (File.Exists(Path))
            {
                switch (System.IO.Path.GetExtension(Path))
                {
                    case ".cnc":
                        ImagePath = "/images/cnc_file.png";
                        break;
                    case ".cli":
                        ImagePath = "/images/cli_file.png";
                        break;
                    default:
                        this.Visibility = Visibility.Collapsed;
                        break;
                }
            }
            else
            {
                ImagePath = FolderPath;
            }
        }
    }
}
