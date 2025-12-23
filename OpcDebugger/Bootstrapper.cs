using OpcDebugger.Services;
using OpcDebugger.ViewModels;
using OpcDebugger.Views;
using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
using Opc2Lib;

namespace OpcDebugger
{
    public class Bootstrapper : PrismBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<SelectorView>();
            containerRegistry.RegisterForNavigation<NotSelectedView>();
            containerRegistry.RegisterForNavigation<SelectedItemView>();
            containerRegistry.RegisterForNavigation<BoolRegisterView>();
            containerRegistry.RegisterForNavigation<NumericRegisterView>();



            containerRegistry.RegisterSingleton<OpcService>();

        }
    }
}
