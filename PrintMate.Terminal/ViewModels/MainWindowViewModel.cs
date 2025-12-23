using PrintMate.Terminal.Views;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Windows;

namespace PrintMate.Terminal.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private IRegionManager _regionManager;

        public MainWindowViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            //_regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(WelcomeView));

            Application.Current.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    //_regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(WelcomeView));

                    _regionManager.RequestNavigate("RootRegion", nameof(RootContainer));
                    _regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(MainView));
                    _regionManager.RequestNavigate(Bootstrapper.LeftBarRegion, nameof(LeftBarView));
                    _regionManager.RequestNavigate(Bootstrapper.RightBarRegion, nameof(RightBarView));
                }),
                System.Windows.Threading.DispatcherPriority.Loaded
            );
        }
    }
}
