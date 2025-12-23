using HandyControl.Tools.Command;
using PrintMate.Terminal.Interfaces;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class EditRoleFormViewModel : BindableBase, IViewModelForm
    {
        private readonly RolesService _rolesService;

        public EditRoleFormViewModel(RolesService rolesService)
        {
            _rolesService = rolesService;
            InitializePermissions();
        }

        private Guid _roleId;
        public Guid RoleId
        {
            get => _roleId;
            set
            {
                if (SetProperty(ref _roleId, value))
                {
                    LoadRoleData();
                }
            }
        }

        private string _roleName;
        public string RoleName
        {
            get => _roleName;
            set => SetProperty(ref _roleName, value);
        }

        private string _roleDisplayName;
        public string RoleDisplayName
        {
            get => _roleDisplayName;
            set => SetProperty(ref _roleDisplayName, value);
        }

        private ObservableCollection<ConfigureParametersPermissionViewModel> _availablePermissions;
        public ObservableCollection<ConfigureParametersPermissionViewModel> AvailablePermissions
        {
            get => _availablePermissions;
            set
            {
                if (SetProperty(ref _availablePermissions, value))
                {
                    RaisePropertyChanged(nameof(SelectedPermissionsText));
                }
            }
        }

        public string SelectedPermissionsText
        {
            get
            {
                if (AvailablePermissions == null)
                    return "Права не выбраны";

                var selectedCount = AvailablePermissions.Count(p => p.IsEnabled);
                if (selectedCount == 0)
                    return "Права не выбраны";

                var selectedNames = AvailablePermissions
                    .Where(p => p.IsEnabled)
                    .Select(p => p.DisplayName);

                return $"Выбрано прав: {selectedCount} ({string.Join(", ", selectedNames)})";
            }
        }

        private RelayCommand _saveCommand;
        private RelayCommand _cancelCommand;

        public RelayCommand SaveCommand
        {
            get => _saveCommand ??= new RelayCommand(async obj =>
            {
                await SaveRole();
            });
        }

        public RelayCommand CancelCommand
        {
            get => _cancelCommand ??= new RelayCommand(obj =>
            {
                IsUpdated = false;
                CloseCommand?.Execute(null);
            });
        }

        public RelayCommand CloseCommand { get; set; }

        public Role UpdatedRole { get; set; }
        public bool IsUpdated { get; set; } = false;

        private void InitializePermissions()
        {
            AvailablePermissions = new ObservableCollection<ConfigureParametersPermissionViewModel>();

            foreach (var permission in AppConfiguration.Permissions.Perms)
            {
                AvailablePermissions.Add(new ConfigureParametersPermissionViewModel
                {
                    DisplayName = $"{permission.Name} - {permission.Id}",
                    PermissionKey = permission.Id,
                    IsEnabled = false
                });
            }

            // Подписываемся на изменения IsEnabled для обновления SelectedPermissionsText
            foreach (var permission in AvailablePermissions)
            {
                permission.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ConfigureParametersPermissionViewModel.IsEnabled))
                    {
                        RaisePropertyChanged(nameof(SelectedPermissionsText));
                    }
                };
            }
        }

        private void LoadRoleData()
        {
            var role = _rolesService.GetRoleById(RoleId);
            if (role != null)
            {
                RoleName = role.Name;
                RoleDisplayName = role.DisplayName;

                // Устанавливаем выбранные права
                if (role.Permissions != null)
                {
                    foreach (var permission in AvailablePermissions)
                    {
                        permission.IsEnabled = role.Permissions.Contains(permission.PermissionKey);
                    }
                }
            }
        }

        private async Task SaveRole()
        {
            try
            {
                // Проверка полей
                if (string.IsNullOrWhiteSpace(RoleName) || string.IsNullOrWhiteSpace(RoleDisplayName))
                {
                    MessageBox.Show("Заполните название и отображаемое имя роли",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем выбранные права
                var selectedPermissions = AvailablePermissions
                    .Where(p => p.IsEnabled)
                    .Select(p => p.PermissionKey)
                    .ToList();

                if (!selectedPermissions.Any())
                {
                    MessageBox.Show("Выберите хотя бы одно право для роли",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Обновляем роль
                var result = await Task.Run(() => _rolesService.UpdateRole(
                    RoleId,
                    RoleName,
                    RoleDisplayName,
                    selectedPermissions));

                if (result)
                {
                    // Получаем обновленную роль из репозитория
                    var updatedRole = _rolesService.GetRoleById(RoleId);

                    if (updatedRole != null)
                    {
                        UpdatedRole = updatedRole;
                        IsUpdated = true;
                        CloseCommand?.Execute(null);
                    }
                    else
                    {
                        MessageBox.Show("Роль обновлена, но не найдена в базе данных");
                    }
                }
                else
                {
                    MessageBox.Show("Ошибка при обновлении роли");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении роли: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
