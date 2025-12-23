using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views;
using Prism.Events;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels
{
    public class MonitoringTemplateViewModel : BindableBase
    {
        public event Action<MonitoringGroup> SelectedTabChanged;


        //private MonitoringTemplateView _view;
        //public MonitoringTemplateView View
        //{
        //    get => _view;
        //    set => Application.Current.Dispatcher.InvokeAsync(()=>valueCommand.AnimateTextChange("", SelectedGroup.Name));
        //}



        public ObservableCollection<IndicatorItemViewModel> Indicators { get; set; }
        public IEnumerable<IndicatorItemViewModel> FirstRow => Indicators;

        public ObservableCollection<MonitoringGroup> Groups { get; set; }
        public ObservableCollection<ColMode> ColModes { get; set; }

        private ColMode _selectedColMode;
        public ColMode SelectedColMode
        {
            get => _selectedColMode;
            set
            {
                SetProperty(ref _selectedColMode, value);
                SelectedColumnModeChanged?.Invoke();
            }
        }

        private MonitoringGroup _selectedGroup;
        public MonitoringGroup SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                SetProperty(ref _selectedGroup, value);
                SelectedTabChanged?.Invoke(SelectedGroup);
            }
        }

        public event Action SelectedColumnModeChanged;
        public RelayCommand<object> SelectAction { get;set;}

        private readonly MonitoringManager _monitoringManager;

        public MonitoringTemplateViewModel(MonitoringManager monitoringManager, IEventAggregator eventAggregator)
        {
            _monitoringManager = monitoringManager;

            Groups = new(monitoringManager.GetGroupsList());

            //Indicators = indicatorService.Indicators;
            ColModes = new ObservableCollection<ColMode>
            {
                new ColMode { Count = 2, ImageSource = "/images/gauges_two_64.png" },
                new ColMode { Count = 3, ImageSource = "/images/gauges_three_64.png" },
                new ColMode { Count = 5, ImageSource = "/images/gauges_five_64.png" }
            };

            SelectedColMode = ColModes[0]; // должен подсветиться первый
            SelectedGroup = Groups[0];
            SelectedTabChanged?.Invoke(SelectedGroup);

            eventAggregator.GetEvent<OnCommandRemoveFromFavouritesEvent>().Subscribe((data) =>
            {
                //MessageBox.ShowDialog($"SelectedGroup.Name: {SelectedGroup.Name}");
                if (SelectedGroup.Id == MonitoringManager.Saved)
                {
                    SelectedTabChanged?.Invoke(Groups[0]);
                }
            });
        }
    }
}
