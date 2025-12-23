using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PrintMate.Terminal.ViewModels;

namespace PrintMate.Terminal.Views
{
    /// <summary>
    /// Логика взаимодействия для LogView.xaml
    /// </summary>
    public partial class LogView : UserControl
    {
        private ScrollViewer _logScrollViewer;

        public LogView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Находим ScrollViewer внутри ItemsControl
            _logScrollViewer = FindChild<ScrollViewer>(this, "LogScrollViewer");

            // Подписываемся на изменения коллекции
            if (DataContext is LogViewModel vm)
            {
                vm.LogEntries.CollectionChanged += (s, args) =>
                {
                    if (args.Action == NotifyCollectionChangedAction.Add &&
                        _logScrollViewer != null)
                    {
                        _logScrollViewer.ScrollToEnd();
                    }
                };
            }
        }

        // Вспомогательный метод для поиска элемента по имени
        private static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T foundChild && (childName == null || child.GetValue(FrameworkElement.NameProperty) as string == childName))
                {
                    return foundChild;
                }

                foundChild = FindChild<T>(child, childName);
                if (foundChild != null) return foundChild;
            }
            return null;
        }
    }
}
