using HandyControl.Tools.Command;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
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

namespace PrintMate.Terminal.Views
{
    /// <summary>
    /// Логика взаимодействия для IndicatorPanelWithGraph.xaml
    /// </summary>
    public partial class IndicatorPanelWithGraph : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(IndicatorPanelWithGraph),
                new PropertyMetadata("Unset"));
        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register(
                nameof(Format),
                typeof(string),
                typeof(IndicatorPanelWithGraph),
                new PropertyMetadata("N/A"));
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(IndicatorPanelWithGraph),
                new PropertyMetadata((double)0));

        public static readonly DependencyProperty ValueStringProperty =
            DependencyProperty.Register(
                nameof(ValueString),
                typeof(string),
                typeof(IndicatorPanelWithGraph),
                new PropertyMetadata("0"));

        public static readonly DependencyProperty SeriesProperty =
            DependencyProperty.Register(
                nameof(Series),
                typeof(IEnumerable<ISeries>),
                typeof(IndicatorPanelWithGraph),
                new PropertyMetadata(null));

        public static readonly DependencyProperty XAxesProperty =
            DependencyProperty.Register(
                nameof(XAxes),
                typeof(IEnumerable<Axis>),
                typeof(IndicatorPanelWithGraph),
                new PropertyMetadata(null));
        public static readonly DependencyProperty YAxesProperty =
            DependencyProperty.Register(
                nameof(YAxes),
                typeof(IEnumerable<Axis>),
                typeof(IndicatorPanelWithGraph),
                new PropertyMetadata(null));
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand), // ← Вот здесь должен быть ICommand, а не RelayCommand
                typeof(IndicatorPanelWithGraph),
                new PropertyMetadata(null));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Format
        {
            get => (string)GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set
            {
                SetValue(ValueProperty, value);
                // Обновляем строковое представление с 3 знаками
                SetValue(ValueStringProperty, value.ToString("F2"));
            }
        }

        public string ValueString
        {
            get => Value.ToString("F2"); // ← F3 = 3 знака после запятой
            set => SetValue(ValueStringProperty, value); // на случай ручного задания
        }

        public IEnumerable<ISeries> Series
        {
            get => (IEnumerable<ISeries>)GetValue(SeriesProperty);
            set => SetValue(SeriesProperty, value);
        }

        public IEnumerable<Axis> XAxes
        {
            get => (IEnumerable<Axis>)GetValue(XAxesProperty);
            set => SetValue(XAxesProperty, value);
        }

        public IEnumerable<Axis> YAxes
        {
            get => (IEnumerable<Axis>)GetValue(YAxesProperty);
            set => SetValue(YAxesProperty, value);
        }

        public RelayCommand Command
        {
            get => (RelayCommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public IndicatorPanelWithGraph()
        {
            InitializeComponent();
        }
        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Command != null)
                Command?.Execute(null);
        }

        private void UIElement_OnTouchDown(object sender, TouchEventArgs e)
        {
            if (Command != null)
                Command?.Execute(null);
        }

    }
}
