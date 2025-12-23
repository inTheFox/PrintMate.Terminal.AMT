using HandyControl.Controls;
using HandyControl.Tools.Command;
using HandyControl.Tools.Extension;
using Opc.Ua;
using OpcDebugger.Events;
using OpcDebugger.Services;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpcDebugger.ViewModels
{
    public class SelectorViewModel : BindableBase
    {
        private readonly OpcService _opcService;
        private ObservableCollection<ElementInfo> _elements;
        private List<ElementInfo> _originalList;
        private string _filterText;

        public ObservableCollection<ElementInfo> Elements
        {
            get => _elements;
            set => SetProperty(ref _elements, value);
        }
        public string FilterText
        {
            get => _filterText;
            set
            {
                SetProperty(ref _filterText, value);
                ApplyFilter();
            }
        }


        private ElementInfo _selectedElement;
        public ElementInfo SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (!_opcService.IsConnect)
                {
                    MessageBox.Show("OPC сервер не подключен");
                    return;
                }


                SetProperty(ref _selectedElement, value);
                OnElementSelected(value);
                
            }
        }


        private readonly IEventAggregator _eventAggregator;

        public SelectorViewModel(OpcService opcService, IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _opcService = opcService;
            _originalList = _opcService.GetElements(); // вызываем ОДИН раз
            Elements = new ObservableCollection<ElementInfo>(_originalList); // используем те же объекты
        }


        private void OnElementSelected(ElementInfo element)
        {
            if (element != null)
            {
                _opcService.SelectedItem = element;
                _opcService.SetSelected(element);
                _eventAggregator.GetEvent<SelectedItemEvent>().Publish(element);
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                Elements.Clear();
                Elements.AddRange(_originalList);
            }
            else
            {
                var lowerFilter = FilterText.ToLower();
                var filtered = _originalList
                    .Where(e => e.Name.ToLower().Contains(lowerFilter) || e.Cmd.ToLower().Contains(lowerFilter))
                    .ToList();
                Elements.Clear();
                Elements.AddRange(filtered);
            }
        }
    }
}
