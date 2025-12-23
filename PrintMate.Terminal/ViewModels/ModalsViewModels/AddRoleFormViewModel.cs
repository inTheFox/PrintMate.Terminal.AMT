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
    public class AddRoleFormViewModel : BindableBase, IViewModelForm
    {
        private readonly RolesService _rolesService;

        public AddRoleFormViewModel(RolesService rolesService)
        {
            _rolesService = rolesService;
            InitializePermissions();
            // CloseCommand будет установлена ModalService
        }

        private string _newRoleName;
        private string _newRoleDisplayName;
        private ObservableCollection<ConfigureParametersPermissionViewModel> _availablePermissions;
        public ObservableCollection<ConfigureParametersPermissionViewModel> AvailablePermissions
        {
            get => _availablePermissions;
            set => SetProperty(ref _availablePermissions, value);
        }

        // Свойство для выбранного элемента в ComboBox
        private ConfigureParametersPermissionViewModel _selectedPermissionItem;
        public ConfigureParametersPermissionViewModel SelectedPermissionItem
        {
            get => _selectedPermissionItem;
            set => SetProperty(ref _selectedPermissionItem, value);
        }


        public string NewRoleName
        {
            get => _newRoleName;
            set => SetProperty(ref _newRoleName, value);
        }


        public string NewRoleDisplayName
        {
            get => _newRoleDisplayName;
            set => SetProperty(ref _newRoleDisplayName, value);
        }

        //private Models.Role _returned;
        //public Models.Role Returned
        //{
        //    get => _returned;
        //    set => SetProperty(ref _returned, value);
        //}

        //private bool _isCreated;
        //public bool IsCreated
        //{
        //    get => _isCreated;
        //    set => SetProperty(ref _isCreated, value);
        //}

        private RelayCommand _createCommand;
        private RelayCommand _closeCommand;
        private RelayCommand _cancelCommand;

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

            // Установите первый элемент как выбранный
            if (AvailablePermissions.Any())
            {
                SelectedPermissionItem = AvailablePermissions.First();
            }
        }

        public RelayCommand CreateCommand
        {
            get => _createCommand ??= new RelayCommand(async obj =>
            {
                await CreateRole();
            });
        }

        public RelayCommand CancelCommand
        {
            get => _cancelCommand ??= new RelayCommand(obj =>
            {
                IsCreated = false;
                CloseCommand?.Execute(null);
            });
        }

        public RelayCommand CloseCommand { get; set; }

        public Models.Role Returned { get; set; }
        public bool IsCreated { get; set; } = false;

        //private bool CanCreateRole(object parameter)
        //{
        //    // Проверяем, заполнены ли обязательные поля
        //    return !string.IsNullOrWhiteSpace(NewRoleName) &&
        //           !string.IsNullOrWhiteSpace(NewRoleDisplayName) &&
        //           AvailablePermissions?.Any(p => p.IsEnabled) == true;
        //}
        private async Task CreateRole()
        {
            try
            {
                // Проверка полей
                if (string.IsNullOrWhiteSpace(NewRoleName) || string.IsNullOrWhiteSpace(NewRoleDisplayName))
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

                // Сохраняем роль
                var result = await Task.Run(() => _rolesService.AddRole(
                    NewRoleName,
                    NewRoleDisplayName,
                    selectedPermissions));

                if (result)
                {
                    // Получаем созданную роль из репозитория
                    var createdRole = _rolesService.GetAllRoles()
                        .FirstOrDefault(r => r.Name == NewRoleName);

                    if (createdRole != null)
                    {
                        Returned = createdRole;
                        IsCreated = true;
                        CloseCommand?.Execute(null);
                    }
                    else
                    {
                        MessageBox.Show("Роль создана, но не найдена в базе данных");
                    }
                }
                else
                {
                    MessageBox.Show("Ошибка при добавлении роли");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании роли: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

