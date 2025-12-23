using OpcDebugger.Events;
using OpcDebugger.Services;
using OpcDebugger.ViewModels;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
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
using static Opc.Ua.RelativePathFormatter;

namespace OpcDebugger.Views
{
    /// <summary>
    /// Логика взаимодействия для SelectedItemViewModel.xaml
    /// </summary>
    public partial class SelectedItemView : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        private readonly BoolRegisterView _boolRegisterView;
        private readonly NumericRegisterView _numericRegisterView;

        public SelectedItemView(OpcService opcService, IEventAggregator eventAggregator, IRegionManager regionManager)
        {
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;

            _boolRegisterView = new BoolRegisterView(opcService);
            _numericRegisterView = new NumericRegisterView(opcService);

            InitializeComponent();

            if (opcService.SelectedItem != null)
            {
                StateChanged(opcService.SelectedItem);
            }
            opcService.OnSelectedItemChanged += ((element) =>
            {
                StateChanged(element);
            });
        }

        private void StateChanged(ElementInfo element)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SelectedItemName.Text = element?.Name ?? "N/A";
                SelectedItemVar.Text = element?.Cmd ?? "N/A";
                SelectedItemType.Text = element?.ValueType ?? "N/A";

                if (element.ValueType == "Bool")
                {
                    EditContent.Content = _boolRegisterView;
                    _boolRegisterView.StateChanged();
                }
                else
                {
                    EditContent.Content = _numericRegisterView;
                    _boolRegisterView.StateChanged();
                }
            });
        }
    }
}
