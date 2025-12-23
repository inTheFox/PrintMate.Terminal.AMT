using HandyControl.Controls;
using HandyControl.Themes;
using HandyControl.Tools;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MessageBox = HandyControl.Controls.MessageBox;

namespace TestAMT16Screen.Views
{
    public partial class MainWindow
    {
        private Point? _dragStartPoint = null;
        private bool _isDragging = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Сохраняем начальную точку
            _dragStartPoint = e.GetPosition(MainScrollViewer);
            _isDragging = false;
            // НЕ вызываем CaptureMouse() здесь!
            // Это позволяет кнопкам получать события, если не будет движения
        }

        private void ScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragStartPoint.HasValue)
            {
                var currentPoint = e.GetPosition(MainScrollViewer);
                var delta = currentPoint - _dragStartPoint.Value;

                // Если ещё не в режиме перетаскивания и движение достаточно большое
                if (!_isDragging && (Math.Abs(delta.X) > SystemParameters.MinimumHorizontalDragDistance ||
                                     Math.Abs(delta.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    // Активируем drag-режим
                    _isDragging = true;

                    // Только теперь захватываем мышь (если нужно)
                    // На сенсорных экранах это часто не обязательно, но может помочь при быстром свайпе
                    MainScrollViewer.CaptureMouse();
                }

                if (_isDragging)
                {
                    MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset - delta.Y);
                    MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset - delta.X);

                    _dragStartPoint = currentPoint;
                    e.Handled = true; // Блокируем дальнейшую обработку движения
                }
            }
        }

        private void ScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                // Завершаем перетаскивание
                if (MainScrollViewer.IsMouseCaptured)
                {
                    MainScrollViewer.ReleaseMouseCapture();
                }
            }

            // Сбрасываем состояние
            _dragStartPoint = null;
            _isDragging = false;

            // ВАЖНО: не помечаем e.Handled = true!
            // Это позволяет Click-событиям всплыть до кнопок
        }


        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Clicked!");
        }
    }
}
