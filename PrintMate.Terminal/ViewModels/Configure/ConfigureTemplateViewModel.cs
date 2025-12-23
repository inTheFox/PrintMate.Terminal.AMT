using HandyControl.Tools.Command;
using PrintMate.Terminal.Views;
using PrintMate.Terminal.Views.Configure;
using PrintMate.Terminal.Views.Pages;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.ViewModels.Configure
{
    class ConfigureTemplateViewModel : BindableBase
    {
        private ConfigureMenuItem _selectedItem;
        public ConfigureMenuItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnSelectionChanged();
            }
        }

        public ObservableCollection<ConfigureMenuItem> Items { get; set; }
        public RelayCommand<ConfigureMenuItem> SelectCommand { get; set; }



        private readonly IRegionManager _regionManager;

        public ConfigureTemplateViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            Items = new ObservableCollection<ConfigureMenuItem>
            {
                new() { Id = nameof(ConfigureProcessView), Name = "Процесс", Image = "/images/process.png"},
                new() { Id = nameof(ConfigureParameters), Name = "Параметры", Image = "/images/config_settings_64.png"},
            };

            SelectCommand = new RelayCommand<ConfigureMenuItem>(OnSelectItem);
            OnSelectItem(Items.First());
        }

        private void OnSelectItem(ConfigureMenuItem obj)
        {
            SelectedItem = obj;
        }

        private void OnSelectionChanged()
        {
            if (SelectedItem != null)
            {
                // Здесь вы можете обработать выбор элемента
                string selectedName = SelectedItem.Name;
                string selectedImage = SelectedItem.Image;

                _regionManager.RequestNavigate(Bootstrapper.ConfigureTemplateRegion, SelectedItem.Id);
            }
        }

        public void OnLoaded(object e)
        {
            OnSelectItem(Items.First());
        }
    }
}
