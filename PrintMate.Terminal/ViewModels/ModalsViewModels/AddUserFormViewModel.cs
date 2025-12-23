using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Interfaces;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Services;
using Prism.Mvvm;
using MessageBox = HandyControl.Controls.MessageBox;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class AddUserFormViewModel : BindableBase, IViewModelForm
    {
        private readonly UserService userService;
        private readonly RolesService _rolesService;

        public AddUserFormViewModel(UserService userService, RolesService rolesService)
        {
            this.userService = userService;
            _rolesService = rolesService;

            FamilyDangerVisibility = Visibility.Collapsed;
            NameDangerVisibility = Visibility.Collapsed;
            PasswordDangerVisibility = Visibility.Collapsed;
            LoginDangerVisibility = Visibility.Collapsed;
            RoleDangerVisibility = Visibility.Collapsed;

            LoadRoles();
        }

        private string _family;
        private string _name;
        private string _login;
        private string _password;

        private Visibility _familyDangerVisibility;
        private Visibility _nameDangerVisibility;
        private Visibility _loginDangerVisibility;
        private Visibility _passwordDangerVisibility;
        private Visibility _roleDangerVisibility;

        private ObservableCollection<Role> _availableRoles;
        private Role _selectedRole;



        private RelayCommand _createCommand;

        public string Family
        {
            get => _family;
            set
            {
                FamilyDangerVisibility = string.IsNullOrEmpty(value) ? Visibility.Visible :
                        Visibility.Collapsed;
                SetProperty(ref _family, value);
            }
        }

        public string Name
        {
            get => _name;
            set
        {
                NameDangerVisibility = string.IsNullOrEmpty(value) ? Visibility.Visible :
                    Visibility.Collapsed;
                SetProperty(ref _name, value);
            }
        }

        public string Login
        {
            get => _login;
            set
            {
                LoginDangerVisibility = string.IsNullOrEmpty(value) ? Visibility.Visible :
                    Visibility.Collapsed;
                SetProperty(ref _login, value);
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                PasswordDangerVisibility = string.IsNullOrEmpty(value) ? Visibility.Visible :
                    Visibility.Collapsed;
                SetProperty(ref _password, value);
            }
        }

        public Visibility FamilyDangerVisibility
        {
            get => _familyDangerVisibility;
            set => SetProperty(ref _familyDangerVisibility, value);
        }
        public Visibility NameDangerVisibility
        {
            get => _nameDangerVisibility;
            set => SetProperty(ref _nameDangerVisibility, value);
        }
        public Visibility LoginDangerVisibility
        {
            get => _loginDangerVisibility;
            set => SetProperty(ref _loginDangerVisibility, value);
        }
        public Visibility PasswordDangerVisibility
        {
            get => _passwordDangerVisibility;
            set => SetProperty(ref _passwordDangerVisibility, value);
        }
        public Visibility RoleDangerVisibility
        {
            get => _roleDangerVisibility;
            set => SetProperty(ref _roleDangerVisibility, value);
        }

        public ObservableCollection<Role> AvailableRoles
        {
            get => _availableRoles;
            set => SetProperty(ref _availableRoles, value);
        }

        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                RoleDangerVisibility = value == null ? Visibility.Visible : Visibility.Collapsed;
                SetProperty(ref _selectedRole, value);
            }
        }


        public RelayCommand CreateCommand
        {
            get => _createCommand ??= new RelayCommand(async obj =>
            {
                await CreateUser();
            });
        }

        public RelayCommand CloseCommand { get; set; }
        public User Returned { get; set; }

        public bool IsCreated = false;
        //public bool IsClosed = false;

        private void LoadRoles()
        {
            var roles = _rolesService.GetAllRoles();
            AvailableRoles = new ObservableCollection<Role>(roles);
        }

        private async Task CreateUser()
        {
            if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(Family) ||
                string.IsNullOrEmpty(Name) || SelectedRole == null) return;
            User user = new()
            {
                Login = Login,
                Password = Password,
                Family = Family,
                Name = Name,
                Comment = "Comment",
                RoleId = SelectedRole.Id
            };
            Returned = user;
            var result = await userService.Add(user);
            if (result)
            {
                IsCreated = true;
                CloseCommand.Execute(null);
            }
            else
            {
                MessageBox.Show("Ошибка при добавлении пользователя");
            }
        }
    }
}
