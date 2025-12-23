using System;
using HandyControl.Tools.Command;
using Microsoft.Extensions.Options;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views.Configure.ConfigureParametersViews;
using Prism.Mvvm;
using Prism.Regions;
using System.Collections.ObjectModel;
using System.Linq;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Views;
using Prism.Events;
using Permissions = PrintMate.Terminal.AppConfiguration.Permissions;

namespace PrintMate.Terminal.ViewModels.Configure;

public class ConfigureParametersViewModel : BindableBase
{
    private ConfigureParametersMenuItem _selectedItem;
    public ConfigureParametersMenuItem SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            OnSelectionChanged();
        }
    }

    public ObservableCollection<ConfigureParametersMenuItem> Items { get; set; }
    public RelayCommand<ConfigureParametersMenuItem> SelectCommand { get; set; }
    private readonly IRegionManager _regionManager;
    private readonly PermissionManagerService _permissionManagerService;
    private readonly IEventAggregator _eventAggregator;

    public ConfigureParametersViewModel(
        IRegionManager regionManager, 
        PermissionManagerService permissionManagerService,
        IEventAggregator eventAggregator
        )
    {
        _regionManager = regionManager;
        _permissionManagerService = permissionManagerService;
        _eventAggregator = eventAggregator;

        Items = new ObservableCollection<ConfigureParametersMenuItem>();
        BuildMenu();

        _eventAggregator.GetEvent<OnUserAuthorized>().Subscribe(OnUserAuthorized);
        _eventAggregator.GetEvent<OnUserQuit>().Subscribe(OnUserQuitCallback);

        SelectCommand = new RelayCommand<ConfigureParametersMenuItem>(OnSelectItem);
    }

    private void OnUserAuthorized(User obj)
    {
        BuildMenu();
    }

    private void BuildMenu()
    {
        Items.Clear();
        if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersUsers))
            Items.Add(new() { Id = nameof(ConfigureParametersUsers), Name = "Пользователи", Path = "/images/peoples.png" });

        if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersRoles))
            Items.Add(new() { Id = nameof(ConfigureParametersRoles), Name = "Роли", Path = "/images/peoples.png" });

        if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersPlc))
            Items.Add(new() { Id = nameof(ConfigureParametersPlc), Name = "Плк", Path = "/images/plc.png" });

        if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersScanator))
            Items.Add(new() { Id = nameof(ConfigureParametersScanator), Name = "Сканаторы", Path = "/images/scanator.png" });

        if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersLasers))
            Items.Add(new() { Id = nameof(ConfigureParametersLasers), Name = "Лазеры", Path = "/images/laser.png" });

        if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersAutomaticSettings))
            Items.Add(new() { Id = nameof(ConfigureParametersAutomaticSettings), Name = "Параметры", Path = "/images/params.png" });

        if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersStorage))
            Items.Add(new() { Id = nameof(ConfigureParametersStorage), Name = "Хранение", Path = "/images/bd.png" });

        //if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersPlc))
        //    Items.Add(new() { Id = nameof(ConfigureParametersPlc), Name = "Машина", Path = "/images/car.png" });

        if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersCamera))
            Items.Add(new() { Id = nameof(ConfigureParametersCamera), Name = "Камера", Path = "/images/camera.png" });

        //if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersPlc))
        //    Items.Add(new() { Id = nameof(ConfigureParametersPlc), Name = "Порошок", Path = "/images/powder.png" });

        if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersServicesStates))
            Items.Add(new() { Id = nameof(ConfigureParametersServicesStates), Name = "Сервисы", Path = "/images/debug.png" });
        //new() { Id = nameof(ConfigureParametersPlc), Name = "ИБП", Path = "/images/ibp.png" },

        if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersAdditionalSoftware))
            Items.Add(new() { Id = nameof(ConfigureParametersAdditionalSoftware), Name = "Стороннее ПО", Path = "/images/app.png" });

        if (_permissionManagerService.HasPermission(Permissions.ConfigureParametersComputerVision))
            Items.Add(new () { Id = nameof(ConfigureParametersComputerVision), Name = "Машинное \nзрение", Path = "/images/computer-vision.png"});

        Items.Add(new() { Id = nameof(ConfigureParametersLoggingView), Name = "Логирование", Path = "/images/app.png" });

        if (Items.Count > 0)
        {
            SelectedItem = Items.First();
        }
        else
        {
            Console.WriteLine("PermDenied region show");
            _regionManager.RequestNavigate("ConfigureParametersRegion", nameof(PermDeniedView));
        }
    }

    private void OnUserQuitCallback()
    {
        OnSelectItem(null);
        _regionManager.RequestNavigate("ConfigureParametersRegion", nameof(PermDeniedView));
    }

    private async void OnSelectItem(ConfigureParametersMenuItem obj)
    {
        if (obj == null)
        {
            SelectedItem = null;
            return;
        }
        var permission = Permissions.Perms.FirstOrDefault(p => p.Id == obj.Id);
        if (permission != null && _permissionManagerService.HasPermission(permission))
        {
            SelectedItem = obj;
        }
        else
        {
            SelectedItem = obj;
        }
    }

    private void OnSelectionChanged()
    {
        if (SelectedItem != null)
        {
            _regionManager.RequestNavigate("ConfigureParametersRegion", SelectedItem.Id);
        }
    }

    public void OnLoaded(object e)
    {
        if (Items.Count > 0)
        {
            OnSelectItem(Items.First());
        }
        else
        {
            Console.WriteLine("PermDenied region show");
            _regionManager.RequestNavigate("ConfigureParametersRegion", nameof(PermDeniedView));
        }
    }
}