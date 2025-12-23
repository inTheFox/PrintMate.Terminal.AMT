using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Opc.Ua;
using OpcDebugger.Events;
using OpcDebugger.Services;
using OpcDebugger.Views;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace OpcDebugger.ViewModels
{
    public class ElementInfoModel : BindableBase
    {
        private string _name;
        private string _cmd;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public string Cmd
        {
            get => _cmd;
            set => SetProperty(ref _cmd, value);
        }
    }


    public class SelectedItemViewModel : BindableBase
    {
        private ElementInfoModel _selectedElement;

        public ElementInfoModel SelectedElement
        {
            get => _selectedElement;
            set => SetProperty(ref _selectedElement, value);
        }

        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly OpcService _opcService;

        public SelectedItemViewModel(OpcService opcService, IEventAggregator eventAggregator, IRegionManager regionManager)
        {
            //MessageBox.Show("Created");

            _eventAggregator = eventAggregator;
            _opcService = opcService;

            if (_opcService.SelectedItem != null)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedElement = new ElementInfoModel
                    {
                        Name = _opcService.SelectedItem.Name,
                        Cmd = _opcService.SelectedItem.Cmd,
                    };
                    //if (_opcService.SelectedItem.ValueType == "Bool")
                    //{
                    //    regionManager.RequestNavigate("RegisterEditRegion", nameof(BoolRegisterView));
                    //}
                    //else
                    //{
                    //    regionManager.RequestNavigate("RegisterEditRegion", nameof(NumericRegisterView));
                    //}
                });
            }
            else
            {
                SelectedElement = new ElementInfoModel
                {
                    Name = "N/A",
                    Cmd = "N/A",
                };
            }
            eventAggregator.GetEvent<SelectedItemEvent>().Subscribe((element) =>
            {
                //MessageBox.Show($"Name: {element.Name}, Command: {element.Cmd}");
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedElement = new ElementInfoModel
                    {
                        Name = element.Name,
                        Cmd = element.Cmd
                    };

                    //if (element.ValueType == "Bool")
                    //{
                    //    regionManager.RequestNavigate("RegisterEditRegion", nameof(BoolRegisterView));
                    //}
                    //else
                    //{
                    //    regionManager.RequestNavigate("RegisterEditRegion", nameof(NumericRegisterView));
                    //}
                });

            });
        }
    }
}
