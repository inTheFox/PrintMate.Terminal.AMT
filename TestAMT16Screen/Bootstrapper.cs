using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
using TestAMT16Screen.Views;

namespace TestAMT16Screen
{
    public class Bootstrapper : PrismBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}
