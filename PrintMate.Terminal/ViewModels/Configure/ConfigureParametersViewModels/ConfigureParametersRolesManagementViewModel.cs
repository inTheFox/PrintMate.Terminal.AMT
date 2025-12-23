using HandyControl.Tools.Command;
using LaserLib;
using Microsoft.VisualBasic.ApplicationServices;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MessageBoxResult = PrintMate.Terminal.Models.MessageBoxResult;


namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels
{
    public class ConfigureParametersRolesManagementViewModel : BindableBase
    {
        private readonly RolesService _rolesService;
        private readonly ModalService _modalService;
        public ConfigureParametersRolesManagementViewModel(RolesService rolesService, ModalService modalService)
        {
            _rolesService = rolesService;
            _modalService = modalService;

            InitializePermissions();

            CreateRolesCommand = new RelayCommand(_ => CreateRole());
            EditRoleCommand = new RelayCommand(_ => EditRole());
            DeleteRoleCommand = new RelayCommand(_ => DeleteRole());
            LoadRolePermissionCommand = new RelayCommand(_ => LoadRolePermissions());
            SelectRole = new RelayCommand(Execute);

            LoadRoles();
        }

        private void Execute(object obj)
        {
            SelectedRole = (Models.Role)obj;
        }

        private ObservableCollection<Models.Role> _roles;

        public ObservableCollection<Models.Role> Roles
        {
            get => _roles;
            set => SetProperty(ref _roles, value);
        }

        private Models.Role _selectedRole;
        public Models.Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    RaisePropertyChanged(nameof(IsRoleSelected));
                    LoadRolePermissions();
                }
            }
        }

        public bool IsRoleSelected => SelectedRole != null;

        private ObservableCollection<ConfigureParametersPermissionViewModel> _availablePermissions;
        public ObservableCollection<ConfigureParametersPermissionViewModel> AvailablePermissions
        {
            get => _availablePermissions;
            set => SetProperty(ref _availablePermissions, value);
        }

        
        private Visibility _rolesListVisibility = Visibility.Collapsed;
        public Visibility RolesListVisibility
        {
            get => _rolesListVisibility;
            set => SetProperty(ref _rolesListVisibility, value);
        }

        private Visibility _emptyListVisibility = Visibility.Visible;
        public Visibility EmptyListVisibility
        {
            get => _emptyListVisibility;
            set => SetProperty(ref _emptyListVisibility, value);
        }

        public RelayCommand CreateRolesCommand { get; }
        public RelayCommand EditRoleCommand { get; }
        public RelayCommand DeleteRoleCommand { get; }
        public RelayCommand LoadRoleCommand { get; }
        public RelayCommand LoadRolePermissionCommand { get; }
        public RelayCommand SelectRole { get; set; }


        private void InitializePermissions()
        {
            AvailablePermissions = new ObservableCollection<ConfigureParametersPermissionViewModel>
            {
                new ConfigureParametersPermissionViewModel
                {
                    PermissionKey = Permissions.AxesMenu,
                    DisplayName = "permissions.axesMenu"
                },
                new ConfigureParametersPermissionViewModel
                {
                    PermissionKey = Permissions.LightControl,
                    DisplayName = "permissions.lightControl"
                }
            };
        }
        private async void CreateRole()
        {
            var result = await _modalService.ShowAsync<AddRoleViewModelForm, AddRoleFormViewModel>(
                modalId: null
            );

            if (result.Result.IsCreated)
            {
                await CustomMessageBox.ShowSuccessAsync("Успешно", $"Роль {result.Result.NewRoleDisplayName} успешно добавлена");
                Roles.Add(result.Result.Returned);
                UpdateVisibility();
            }
            // Если IsCreated == false, пользователь нажал "Отмена" - ничего не делаем
        }

        private async void EditRole()
        {
            if (SelectedRole == null) return;

            var roleIdToEdit = SelectedRole.Id;

            var options = new Dictionary<string, object>
            {
                { "RoleId", roleIdToEdit }
            };

            var result = await _modalService.ShowAsync<EditRoleViewModelForm, EditRoleFormViewModel>(
                modalId: null,
                options: options
            );

            if (result.Result.IsUpdated)
            {
                await CustomMessageBox.ShowSuccessAsync("Успешно", $"Роль {result.Result.UpdatedRole.DisplayName} успешно обновлена");

                // Перезагружаем список ролей из репозитория, чтобы получить актуальные данные
                LoadRoles();

                // Восстанавливаем выбор отредактированной роли
                SelectedRole = Roles.FirstOrDefault(r => r.Id == roleIdToEdit);
            }
        }
        private void LoadRoles()
        {
            var roles = _rolesService.GetAllRoles();
            Roles = new ObservableCollection<Models.Role>(roles);

            if (Roles.FirstOrDefault() != null && SelectedRole == null)
            {
                SelectedRole = Roles.First();
            }
            UpdateVisibility();
        }

        private void LoadRolePermissions()
        {
            if (SelectedRole == null) return;

            foreach (var permission in AvailablePermissions)
            {
                permission.IsEnabled = SelectedRole.Permissions?.Contains(permission.PermissionKey) ?? false;
            }
        }


        private async void DeleteRole()
        {
            if (SelectedRole == null) return;

            var result = await CustomMessageBox.ShowConfirmationAsync("Подтверждение удаления", $"Вы уверены, что хотите удалить роль '{SelectedRole.DisplayName}'?");

            if (result == MessageBoxResult.Yes)
            {
                var success = _rolesService.RemoveRole(SelectedRole.Id);
                if (success)
                {
                    LoadRoles();
                }
            }
        }
        private void UpdateVisibility()
        {
            if (Roles == null || Roles.Count == 0)
            {
                RolesListVisibility = Visibility.Collapsed;
                EmptyListVisibility = Visibility.Visible;
            }
            else
            {
                RolesListVisibility = Visibility.Visible;
                EmptyListVisibility = Visibility.Collapsed;
            }
        }
    }
}
