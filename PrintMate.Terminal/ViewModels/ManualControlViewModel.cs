using HandyControl.Tools.Command;
using PrintMate.Terminal.Views;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PrintMate.Terminal.ViewModels
{
    public class ManualControlViewModel : BindableBase
    {
        private ManualControlViewItem _selectedItem;
        public ManualControlViewItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnSelectionChanged();
            }
        }

        public ObservableCollection<ManualControlViewItem> Items { get; }
        public RelayCommand<ManualControlViewItem> SelectCommand { get; set; }


        private readonly IRegionManager _regionManager; 

        public ManualControlViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            Items = new ObservableCollection<ManualControlViewItem>
            {
                new ManualControlViewItem { Id = "axes", Name = "Оси", Icon = "/images/axes_64.png" },
                new ManualControlViewItem { Id = "systems", Name = "Системы", Icon = "/images/systems_64.png" },
            };

            _regionManager.RequestNavigate(Bootstrapper.ManualContent, nameof(ManualAxesControl));
            SelectCommand = new RelayCommand<ManualControlViewItem>(OnSelectCommandCallback);

            SelectedItem = Items.First();
        }

        private void OnSelectCommandCallback(ManualControlViewItem obj)
        {
            SelectedItem = obj;
        }

        private void OnSelectionChanged()
        {
            if (SelectedItem != null)
            {
                switch (SelectedItem.Id)
                {
                 case "systems":
                     _regionManager.RequestNavigate(Bootstrapper.ManualContent, nameof(ManualControlSystems));
                        // Логика для выбора "systems"
                        break;
                 case "axes":
                     _regionManager.RequestNavigate(Bootstrapper.ManualContent, nameof(ManualAxesControl));

                        break;
                    default:
                        // Логика для неизвестного выбора
                        break;
                }
            }
        }
    }
}
