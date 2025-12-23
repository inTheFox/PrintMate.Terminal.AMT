
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.ViewModels;
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
using Newtonsoft.Json;
using Opc2Lib;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;

namespace PrintMate.Terminal.Views
{
    /// <summary>
    /// Логика взаимодействия для RightBarView.xaml
    /// </summary>
    public partial class RightBarView : UserControl
    {
        private readonly ILogicControllerProvider _logicControllerService;
        private readonly ModalService _modalService;
        private readonly NotificationService _notificationService;

        public RightBarView(ILogicControllerProvider logicControllerService, ModalService modalService, NotificationService notificationService)
        {
            _logicControllerService = logicControllerService;
            _notificationService = notificationService;
            _modalService = modalService;
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
        }

        private void UIElement_OnTouchDown(object sender, TouchEventArgs e)
        {
            //MessageBox.ShowDialog("Touch");
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.ShowDialog("Click");
        }


        private async void LightBorderOn_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (TouchScreenHelper.IsTouchScreenAvailable()) return;


            // Используем PreviewMouseDown - он обрабатывается раньше и предотвращает конфликты
            e.Handled = true;

            // Проверяем, что это левая кнопка мыши
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (!_logicControllerService.Connected)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Отсутствует подключение к ПЛК");
                return;
            }

            try
            {
                bool current = await _logicControllerService.GetBoolAsync(OpcCommands.Com_PChamber_Light);
                await _logicControllerService.SetBoolAsync(OpcCommands.Com_PChamber_Light, !current);
            }
            catch (Exception ex)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", ex.Message);

            }
        }

        private async void DoorOpen_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (TouchScreenHelper.IsTouchScreenAvailable()) return;
            // Используем PreviewMouseDown - он обрабатывается раньше и предотвращает конфликты
            e.Handled = true;

            // Проверяем, что это левая кнопка мыши
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (!_logicControllerService.Connected)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Отсутствует подключение к ПЛК");
                return;
            }

            try
            {
                bool current = await _logicControllerService.GetBoolAsync(OpcCommands.Com_PChamber_CameraLock);
                await _logicControllerService.SetBoolAsync(OpcCommands.Com_PChamber_CameraLock, !current);
            }
            catch (Exception ex)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", ex.Message);
            }
        }

        private async void LightBorderOff_OnTouchDown(object sender, TouchEventArgs e)
        {
            // Используем PreviewMouseDown - он обрабатывается раньше и предотвращает конфликты

            if (!_logicControllerService.Connected)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Отсутствует подключение к ПЛК");
                return;
            }

            try
            {
                bool current = await _logicControllerService.GetBoolAsync(OpcCommands.Com_PChamber_Light);
                await _logicControllerService.SetBoolAsync(OpcCommands.Com_PChamber_Light, !current);
            }
            catch (Exception ex)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", ex.Message);

            }
        }

        private async void DoorOpen_OnTouchDown(object sender, TouchEventArgs e)
        {
            // Используем PreviewMouseDown - он обрабатывается раньше и предотвращает конфликты

            if (!_logicControllerService.Connected)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Отсутствует подключение к ПЛК");
                return;
            }

            try
            {
                bool current = await _logicControllerService.GetBoolAsync(OpcCommands.Com_PChamber_CameraLock);
                await _logicControllerService.SetBoolAsync(OpcCommands.Com_PChamber_CameraLock, !current);
            }
            catch (Exception ex)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", ex.Message);

            }
        }

        private void Preview3D_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (TouchScreenHelper.IsTouchScreenAvailable()) return;

            e.Handled = true;

            if (e.ChangedButton != MouseButton.Left)
                return;

            // Вызываем команду из ViewModel
            if (DataContext is RightBarViewModel viewModel)
            {
                viewModel.Open3DPreviewCommand.Execute(null);
            }
        }
    }
}
