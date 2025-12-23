using HansDebuggerApp.Services;
using HansDebuggerApp.Views;
using Opc2Lib;
using Prism.DryIoc;
using Prism.Ioc;
using System.Threading.Tasks;
using System.Windows;
using HansDebuggerApp.Opc;

namespace HansDebuggerApp
{
    public class Bootstrapper : PrismBootstrapper
    {
        public static IContainerProvider ContainerProvider;


        protected override void OnInitialized()
        {
            ContainerProvider = Container;
            ContainerProvider.Resolve<ScannerService>();
            ContainerProvider.Resolve<ILogicControllerObserver>();
            ContainerProvider.Resolve<ILogicControllerProvider>();



            // Работаем с сетевыми обсерверами
            PingObserver pingObserverService = ContainerProvider.Resolve<PingObserver>();
            Task.Run((async () =>
            {
                pingObserverService.InitListeners();
                await pingObserverService.StartObserver(PingObserver.PlcConnectionObserver);
            }));

            base.OnInitialized();
        }


        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ScannerService>();
            containerRegistry.RegisterSingleton<PingService>();

            containerRegistry.RegisterSingleton<ILogicControllerProvider, LogicControllerService>();
            containerRegistry.RegisterSingleton<ILogicControllerObserver, LogicControllerObserver>();
        }
    }
}
