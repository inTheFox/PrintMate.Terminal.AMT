using LogViewerApp.Services;
using LogViewerApp.ViewModels;
using LogViewerApp.Views;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;
using System.Windows;

namespace LogViewerApp
{
    public class Bootstrapper : PrismBootstrapper
    {
        public const string MainRegion = "MainRegion";

        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Регистрация сервисов
            containerRegistry.RegisterSingleton<LoggingApiService>();

            // Регистрация ViewModels
            containerRegistry.Register<MainViewModel>();

            // Регистрация Views для навигации
            containerRegistry.RegisterForNavigation<MainView, MainViewModel>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate(MainRegion, nameof(MainView));

            var shell = (MainWindow)Shell;
            shell.Show();
        }
    }
}
