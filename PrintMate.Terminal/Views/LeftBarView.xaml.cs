using System;
using System.Collections.Generic;
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
using Opc2Lib;
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;
using Prism.Regions;

namespace PrintMate.Terminal.Views
{
    /// <summary>
    /// Логика взаимодействия для LeftBarView.xaml
    /// </summary>
    public partial class LeftBarView : UserControl
    {
        private readonly IRegionManager _regionManager;
        private readonly ILogicControllerObserver _observer;

        public LeftBarView(IRegionManager regionManager, ILogicControllerObserver observer)
        {
            _regionManager = regionManager;
            _observer = observer;
            _observer.Subscribe(this, (data) =>
            {
                if (data.Value != null && data.Value is bool value && !value)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        BlockedScreenBorder.Visibility = Visibility.Visible;
                        FavoriteList.Height = 600;
                    });
                }
                else
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        BlockedScreenBorder.Visibility = Visibility.Collapsed;
                        FavoriteList.Height = 710;
                    });
                }
            }, OpcCommands.Trig_SensorKeyLocked);
            InitializeComponent();
        }

        private void MonitorButtonCallback(object sender, MouseButtonEventArgs e)
        {
            _regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(MonitoringTemplateView));


            var region = _regionManager.Regions[Bootstrapper.MainRegion];
            var activeView = region.ActiveViews.FirstOrDefault();
        }

        private void ScrollViewer_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true; // Подавляем "отскок" и передачу дальше
        }

        private async void OnClickToLogoType(object sender, MouseButtonEventArgs e)
        {
            var modalResult = await ModalService.Instance.ShowAsync<AppExitModalView, AppExitModalViewModel>(
                modalId: null,
                options: null,
                showOverlay: true,
                closeOnBackgroundClick: true
            );

            if (modalResult != null && modalResult.IsSuccess && modalResult.Result != null)
            {
                switch (modalResult.Result.Result)
                {
                    case AppExitResult.Minimize:
                        Application.Current.MainWindow.WindowState = WindowState.Minimized;
                        break;

                    case AppExitResult.Close:
                        Application.Current.Shutdown();
                        break;

                    case AppExitResult.Cancel:
                    case AppExitResult.None:
                    default:
                        break;
                }
            }
        }

        private async void OnProfileClick(object sender, MouseButtonEventArgs e)
        {
            await ModalService.Instance.ShowAsync<AccountManagementView, AccountManagementViewModel>(
                modalId: null,
                options: null,
                showOverlay: true,
                closeOnBackgroundClick: true
            );
        }

        private async void OnNotificationsClick(object sender, MouseButtonEventArgs e)
        {
            await ModalService.Instance.ShowAsync<NotificationsCenterView, NotificationsCenterViewModel>(
                modalId: null,
                options: null,
                showOverlay: true,
                closeOnBackgroundClick: true
            );

            // Обновляем счётчик непрочитанных после закрытия модального окна
            if (DataContext is LeftBarViewModel viewModel)
            {
                await viewModel.UpdateUnreadCountAsync();
            }
        }
    }
}
