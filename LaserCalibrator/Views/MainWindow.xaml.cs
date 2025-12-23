using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LaserCalibrator.ViewModels;

namespace LaserCalibrator.Views
{
    public partial class MainWindow : HandyControl.Controls.Window
    {
        private MainWindowViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                _viewModel = vm;
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                UpdateVisualization();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(UpdateVisualization));
        }

        private void PreviewCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateVisualization();
        }

        private void UpdateVisualization()
        {
            var vm = _viewModel ?? DataContext as MainWindowViewModel;
            if (vm == null) return;

            var canvas = PreviewCanvas;
            if (canvas == null || canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return;

            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            double centerX = canvasWidth / 2;
            double centerY = canvasHeight / 2;

            // Определяем масштаб
            double maxFieldX = Math.Max(vm.Laser1FieldSizeX, vm.Laser2FieldSizeX);
            double maxFieldY = Math.Max(vm.Laser1FieldSizeY, vm.Laser2FieldSizeY);
            double maxOffsetY = Math.Max(Math.Abs(vm.Laser1OffsetY), Math.Abs(vm.Laser2OffsetY));
            double maxOffsetX = Math.Max(Math.Abs(vm.Laser1OffsetX), Math.Abs(vm.Laser2OffsetX));

            double totalWidth = maxFieldX + 2 * maxOffsetX + 100;
            double totalHeight = maxFieldY + 2 * maxOffsetY + 100;

            double scale = Math.Min(
                (canvasWidth - 80) / totalWidth,
                (canvasHeight - 80) / totalHeight
            );

            if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale))
                scale = 1;

            // Оси координат
            AxisX.X1 = 20;
            AxisX.Y1 = centerY;
            AxisX.X2 = canvasWidth - 20;
            AxisX.Y2 = centerY;

            AxisY.X1 = centerX;
            AxisY.Y1 = 20;
            AxisY.X2 = centerX;
            AxisY.Y2 = canvasHeight - 20;

            // Центр системы
            Canvas.SetLeft(SystemCenter, centerX - 4);
            Canvas.SetTop(SystemCenter, centerY - 4);

            // Поле лазера 1 (центр + offset)
            double laser1CenterX = centerX + vm.Laser1OffsetX * scale;
            double laser1CenterY = centerY - vm.Laser1OffsetY * scale;
            double laser1Width = vm.Laser1FieldSizeX * scale;
            double laser1Height = vm.Laser1FieldSizeY * scale;

            Laser1Field.Width = Math.Max(1, laser1Width);
            Laser1Field.Height = Math.Max(1, laser1Height);
            Canvas.SetLeft(Laser1Field, laser1CenterX - laser1Width / 2);
            Canvas.SetTop(Laser1Field, laser1CenterY - laser1Height / 2);

            // Поле лазера 2
            double laser2CenterX = centerX + vm.Laser2OffsetX * scale;
            double laser2CenterY = centerY - vm.Laser2OffsetY * scale;
            double laser2Width = vm.Laser2FieldSizeX * scale;
            double laser2Height = vm.Laser2FieldSizeY * scale;

            Laser2Field.Width = Math.Max(1, laser2Width);
            Laser2Field.Height = Math.Max(1, laser2Height);
            Canvas.SetLeft(Laser2Field, laser2CenterX - laser2Width / 2);
            Canvas.SetTop(Laser2Field, laser2CenterY - laser2Height / 2);

            // Целевая позиция (перекрестие)
            double targetScreenX = centerX + vm.TargetX * scale;
            double targetScreenY = centerY - vm.TargetY * scale;

            TargetCrossH.X1 = targetScreenX - 15;
            TargetCrossH.Y1 = targetScreenY;
            TargetCrossH.X2 = targetScreenX + 15;
            TargetCrossH.Y2 = targetScreenY;

            TargetCrossV.X1 = targetScreenX;
            TargetCrossV.Y1 = targetScreenY - 15;
            TargetCrossV.X2 = targetScreenX;
            TargetCrossV.Y2 = targetScreenY + 15;

            Canvas.SetLeft(TargetCircle, targetScreenX - 10);
            Canvas.SetTop(TargetCircle, targetScreenY - 10);

            Canvas.SetLeft(TargetLabel, targetScreenX + 15);
            Canvas.SetTop(TargetLabel, targetScreenY - 20);

            // Точка лазера 1 - используем текущую позицию в глобальной системе координат
            // Позиция в глобальной СК = CurrentPosition + Offset (смещение центра поля)
            double laser1GlobalX = vm.Laser1CurrentX + vm.Laser1OffsetX;
            double laser1GlobalY = vm.Laser1CurrentY + vm.Laser1OffsetY;
            double laser1PointX = centerX + laser1GlobalX * scale;
            double laser1PointY = centerY - laser1GlobalY * scale;

            Canvas.SetLeft(Laser1Point, laser1PointX - 8);
            Canvas.SetTop(Laser1Point, laser1PointY - 8);

            Canvas.SetLeft(Laser1Label, laser1PointX + 10);
            Canvas.SetTop(Laser1Label, laser1PointY - 8);

            // Точка лазера 2 - используем текущую позицию в глобальной системе координат
            double laser2GlobalX = vm.Laser2CurrentX + vm.Laser2OffsetX;
            double laser2GlobalY = vm.Laser2CurrentY + vm.Laser2OffsetY;
            double laser2PointX = centerX + laser2GlobalX * scale;
            double laser2PointY = centerY - laser2GlobalY * scale;

            Canvas.SetLeft(Laser2Point, laser2PointX - 8);
            Canvas.SetTop(Laser2Point, laser2PointY - 8);

            Canvas.SetLeft(Laser2Label, laser2PointX + 10);
            Canvas.SetTop(Laser2Label, laser2PointY - 8);

            // Зона перекрытия
            CalculateAndDrawOverlap(vm, scale, centerX, centerY);
        }

        private void CalculateAndDrawOverlap(MainWindowViewModel vm, double scale, double centerX, double centerY)
        {
            double laser1Left = vm.Laser1OffsetX - vm.Laser1FieldSizeX / 2;
            double laser1Right = vm.Laser1OffsetX + vm.Laser1FieldSizeX / 2;
            double laser1Top = vm.Laser1OffsetY + vm.Laser1FieldSizeY / 2;
            double laser1Bottom = vm.Laser1OffsetY - vm.Laser1FieldSizeY / 2;

            double laser2Left = vm.Laser2OffsetX - vm.Laser2FieldSizeX / 2;
            double laser2Right = vm.Laser2OffsetX + vm.Laser2FieldSizeX / 2;
            double laser2Top = vm.Laser2OffsetY + vm.Laser2FieldSizeY / 2;
            double laser2Bottom = vm.Laser2OffsetY - vm.Laser2FieldSizeY / 2;

            double overlapLeft = Math.Max(laser1Left, laser2Left);
            double overlapRight = Math.Min(laser1Right, laser2Right);
            double overlapTop = Math.Min(laser1Top, laser2Top);
            double overlapBottom = Math.Max(laser1Bottom, laser2Bottom);

            if (overlapLeft < overlapRight && overlapBottom < overlapTop)
            {
                double overlapWidth = (overlapRight - overlapLeft) * scale;
                double overlapHeight = (overlapTop - overlapBottom) * scale;
                double overlapCenterX = (overlapLeft + overlapRight) / 2;
                double overlapCenterY = (overlapTop + overlapBottom) / 2;

                OverlapZone.Width = Math.Max(1, overlapWidth);
                OverlapZone.Height = Math.Max(1, overlapHeight);
                OverlapZone.Visibility = Visibility.Visible;

                Canvas.SetLeft(OverlapZone, centerX + overlapCenterX * scale - overlapWidth / 2);
                Canvas.SetTop(OverlapZone, centerY - overlapCenterY * scale - overlapHeight / 2);
            }
            else
            {
                OverlapZone.Visibility = Visibility.Collapsed;
            }
        }
    }
}
