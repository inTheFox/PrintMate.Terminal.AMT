using HandyControl.Tools.Command;
using PrintMate.Terminal.Views.Configure.ConfigureProcessViews;
using Prism.Mvvm;
using Prism.Regions;
using System.Collections.ObjectModel;
using System.Linq;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.Services;

namespace PrintMate.Terminal.ViewModels.Configure;

public class ConfigureProcessViewModel : BindableBase
{
    private ConfigureProcessMenuItem _selectedItem;
    public ConfigureProcessMenuItem SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            OnSelectionChanged();
        }
    }

    public ObservableCollection<ConfigureProcessMenuItem> Items { get; set; }
    public RelayCommand<ConfigureProcessMenuItem> SelectCommand { get; set; }
    private readonly IRegionManager _regionManager;
    private readonly PermissionManagerService _permissionManagerService;


    public ConfigureProcessViewModel(IRegionManager regionManager, PermissionManagerService permissionManagerService)
    {
        _regionManager = regionManager;
        _permissionManagerService = permissionManagerService;

        Items = new ObservableCollection<ConfigureProcessMenuItem>
        {
            new() { Id = nameof(ConfigureProcessSystemView), Name = "Система", Image = "/images/system_64.png"},
            new() { Id = nameof(ConfigureProcessGas), Name = "Газ", Image = "/images/gas_filters_system_64.png"},
            new() { Id = nameof(ConfigureProcessLaser), Name = "Лазер", Image = "/images/laser.png"},
            new() { Id = nameof(ConfigureProcessPowder), Name = "Порошок", Image = "/images/powder.png"},
        };

        SelectCommand = new RelayCommand<ConfigureProcessMenuItem>(OnSelectItem);
    }

    private async void OnSelectItem(ConfigureProcessMenuItem obj)
    {
        var permission = Permissions.Perms.FirstOrDefault(p => p.Id == obj.Id);

        if (permission != null && _permissionManagerService.HasPermission(permission))
        {
            SelectedItem = obj;
        }
        else
        {
            await CustomMessageBox.ShowErrorAsync("Ошибка доступа", "У вас нет доступа к этому разделу !");
        }
    }

    private void OnSelectionChanged()
    {
        if (SelectedItem != null)
        {
            _regionManager.RequestNavigate("ConfigureProcessRegion", SelectedItem.Id);
        }
    }

    public void OnLoaded(object e)
    {
        OnSelectItem(Items.First());
    }
}