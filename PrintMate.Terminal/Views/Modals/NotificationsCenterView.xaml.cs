using System.Windows.Controls;
using System.Windows.Input;

namespace PrintMate.Terminal.Views.Modals
{
    public partial class NotificationsCenterView : UserControl
    {
        public NotificationsCenterView()
        {
            InitializeComponent();
        }

        private void OnNotificationClick(object sender, MouseButtonEventArgs e)
        {
            // Пометить уведомление как прочитанное при клике
            if (DataContext is ViewModels.ModalsViewModels.NotificationsCenterViewModel viewModel
                && sender is System.Windows.FrameworkElement element
                && element.DataContext is Models.Notification notification)
            {
                viewModel.MarkAsReadCommand?.Execute(notification.Id);
            }
        }
    }
}
