using HandyControl.Tools.Converter;
using Opc2Lib;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PrintMate.Terminal.Views
{
    public partial class MonitoringTemplateView : UserControl
    {
        private Brush _unselectedColumnModeColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2c2c2c"));
        private Brush _selectedColumnModeColor = Brushes.Orange;
        private string _currentHeaderText = string.Empty;
        private MonitoringTemplateViewModel _viewModel;
        private MonitoringGroup _selectedGroup;

        public MonitoringTemplateView()
        {
            InitializeComponent();

            Loaded += (sender, args) =>
            {
                var model = DataContext as MonitoringTemplateViewModel;
                if (model == null) return;

                _viewModel = model;
                model.SelectedColumnModeChanged += ModelOnSelectedColumnModeChanged;
                model.SelectedTabChanged += Model_SelectedTabChanged;

                // Инициализация первого заголовка
                if (model.SelectedGroup != null)
                {
                    _selectedGroup = model.SelectedGroup;
                    _currentHeaderText = model.SelectedGroup.Name;
                    OldTextBlock.Text = _currentHeaderText;
                    NewTextBlock.Text = _currentHeaderText;
                    NewTextBlock.Opacity = 1;
                    NewTextBlock.RenderTransform = new TranslateTransform(0, 0);
                    BuildElements();
                }
            };

            Unloaded += (sender, args) =>
            {
                if (_viewModel != null)
                {
                    _viewModel.SelectedColumnModeChanged -= ModelOnSelectedColumnModeChanged;
                    _viewModel.SelectedTabChanged -= Model_SelectedTabChanged;
                }
            };
        }

        private void Model_SelectedTabChanged(MonitoringGroup obj)
        {
            if (obj == null) return;
            _selectedGroup = obj;
            AnimateTextChange(_currentHeaderText, obj.Name);
            _currentHeaderText = obj.Name;
            BuildElements();
        }

        public void AnimateTextChange(string oldText, string newText)
        {
            OldTextBlock.Text = oldText;
            NewTextBlock.Text = newText;

            var duration = TimeSpan.FromMilliseconds(1000);

            // Сброс трансформаций
            ((TranslateTransform)OldTextBlock.RenderTransform).Y = 0;
            ((TranslateTransform)NewTextBlock.RenderTransform).Y = -10;

            // Анимация старого текста: вниз + прозрачность
            var oldTranslate = new DoubleAnimation(0, 10, duration);
            var oldOpacity = new DoubleAnimation(1, 0, duration);

            // Анимация нового текста: сверху → центр + появление
            var newTranslate = new DoubleAnimation(-10, 0, duration);
            var newOpacity = new DoubleAnimation(0, 1, duration);

            // Применяем анимации
            OldTextBlock.BeginAnimation(TextBlock.OpacityProperty, oldOpacity);
            OldTextBlock.BeginAnimation(TranslateTransform.YProperty, oldTranslate);
            OldTextBlock.BeginAnimation(TranslateTransform.XProperty, oldTranslate);

            NewTextBlock.BeginAnimation(TextBlock.OpacityProperty, newOpacity);
            NewTextBlock.BeginAnimation(TranslateTransform.YProperty, newTranslate);
        }

        private void ModelOnSelectedColumnModeChanged()
        {
            BuildElements();
        }

        private void BuildElements()
        {
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                ScrollViewer.ScrollToTop();
                if (_selectedGroup == null) return;

                var items = _selectedGroup.Commands;
                if (items == null) return;

                int colsCount = _viewModel.SelectedColMode?.Count ?? 1;
                int totalItems = items.Count;

                if (totalItems == 0)
                {
                    MainBorder.Child = null;
                    return;
                }

                var uniformGrid = new UniformGrid
                {
                    Columns = colsCount,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top
                };

                var elementsToAnimate = new List<IndicatorForMonitoring>();

                for (int index = 0; index < totalItems; index++)
                {
                    var item = items[index];

                    var control = Bootstrapper.ContainerProvider.Resolve(typeof(IndicatorForMonitoring)) as IndicatorForMonitoring;
                    //var model = Bootstrapper.ContainerProvider.Resolve(typeof(IndicatorForMonitoringViewModel)) as IndicatorForMonitoringViewModel;
                    //model.CommandInfo = item;

                    //control.DataContext = Bootstrapper.ContainerProvider.Resolve(typeof(IndicatorForMonitoringViewModel)) as IndicatorForMonitoringViewModel;
                    control.CommandInfo = item;
                    control.Opacity = 0;
                    control.HorizontalAlignment = HorizontalAlignment.Stretch;
                    control.VerticalAlignment = VerticalAlignment.Stretch;
                    //control.Height = 90; // Фиксированная высота
                    control.Width = double.NaN; // Auto width
                    //element.Changed();

                    control.RenderTransform = new TranslateTransform(0, -10);
                    uniformGrid.Children.Add(control);
                    elementsToAnimate.Add(control);
                }
                MainBorder.Child = uniformGrid;
                await AnimateElementsSequentially(elementsToAnimate);
            });
        }


        private void ScrollViewer_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true; // Подавляем "отскок" и передачу дальше
        }

        private async Task AnimateElementsSequentially(List<IndicatorForMonitoring> elements)
        {
            const double delayMs = 60;
            const double durationMs = 400;

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];

                var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durationMs));
                var translateAnim = new DoubleAnimation(-10, 0, TimeSpan.FromMilliseconds(durationMs));

                element.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
                element.RenderTransform.BeginAnimation(TranslateTransform.YProperty, translateAnim);

                if (i < elements.Count - 1)
                {
                    await Task.Delay((int)delayMs);
                }
            }
        }
    }
}