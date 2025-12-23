using LaserConfigurator.Services;
using LaserConfigurator.Views;
using LaserConfigurator.ViewModels;
using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;

namespace LaserConfigurator
{
    public class Bootstrapper : PrismBootstrapper
    {
        public const string ContentRegion = "ContentRegion";

        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register services as singletons
            containerRegistry.RegisterSingleton<IConfigurationService, ConfigurationService>();
            containerRegistry.RegisterSingleton<HansService>();
            containerRegistry.RegisterSingleton<IGeometryService, GeometryService>();
            containerRegistry.RegisterSingleton<IUdmService, UdmService>();

            // Register views for navigation
            containerRegistry.RegisterForNavigation<MainView, MainViewModel>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Navigate to MainView
            var regionManager = Container.Resolve<Prism.Regions.IRegionManager>();
            regionManager.RequestNavigate(ContentRegion, nameof(MainView));
        }
    }
}
