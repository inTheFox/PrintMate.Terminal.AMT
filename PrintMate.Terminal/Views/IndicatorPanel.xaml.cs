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
using Opc2Lib;
using PrintMate.Terminal.ViewModels;
using PrintMate.Terminal.Views.ComponentsViews;

namespace PrintMate.Terminal.Views
{
    /// <summary>
    /// Логика взаимодействия для IndicatorPanel.xaml
    /// </summary>
    public partial class IndicatorPanel : UserControl
    {
        
        public static readonly DependencyProperty CommandInfoProperty =
            DependencyProperty.Register(
                nameof(CommandInfo),
                typeof(CommandInfo), // ← Вот здесь должен быть ICommand, а не RelayCommand
                typeof(IndicatorPanel),
                new PropertyMetadata(null));
        public CommandInfo CommandInfo
        {
            get => (CommandInfo)GetValue(CommandInfoProperty);
            set => SetValue(CommandInfoProperty, value);
        }

        public IndicatorPanel()
        {
            InitializeComponent();
            DataContext = new IndicatorPanelViewModel();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (CommandInfo == null) return; 

            if (CommandInfo.ValueCommandType == ValueCommandType.Bool)
            {
                BoolBlock.Visibility = Visibility.Visible;
                DecimalBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                BoolBlock.Visibility = Visibility.Collapsed;
                DecimalBlock.Visibility = Visibility.Visible;
            }

            this.LoadModel(this);
        }
    }
}