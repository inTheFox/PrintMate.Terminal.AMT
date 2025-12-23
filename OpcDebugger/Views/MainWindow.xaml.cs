using HandyControl.Controls;
using HandyControl.Themes;
using HandyControl.Tools;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpcDebugger.Events;
using Prism.Events;
using Prism.Regions;

namespace OpcDebugger.Views
{
    public partial class MainWindow
    {
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;

        public MainWindow(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private bool _isSelected = false;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _regionManager.RequestNavigate("ItemsSelectorRegion", nameof(SelectorView));
            _regionManager.RequestNavigate("SelectedItemRegion", nameof(NotSelectedView));

            _eventAggregator.GetEvent<SelectedItemEvent>().Subscribe((element) =>
            {
                if (_isSelected) return;
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _regionManager.RequestNavigate("SelectedItemRegion", nameof(SelectedItemView));
                });
                _isSelected = true;
            });
        }
    }
}
