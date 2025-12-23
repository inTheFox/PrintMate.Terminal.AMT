using HandyControl.Tools.Command;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Opc.Ua;
using System.Linq;
using MessageBox = HandyControl.Controls.MessageBox;

namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels
{
    public class UserDisplayModel : BindableBase
    {
        private readonly User _user;
        private string _roleDisplayName;

        public UserDisplayModel(User user, string roleDisplayName)
        {
            _user = user;
            _roleDisplayName = roleDisplayName;
        }

        public User User => _user;
        public int Id => _user.Id;
        public string Name => _user.Name;
        public string Family => _user.Family;
        public string Login => _user.Login;
        public string Password => _user.Password;
        public string Comment => _user.Comment;
        public System.Guid RoleId => _user.RoleId;

        public string RoleDisplayName
        {
            get => _roleDisplayName;
            set => SetProperty(ref _roleDisplayName, value);
        }
    }

    public class ConfigureParametersUsersViewModel : BindableBase
    {
        private ObservableCollection<UserDisplayModel> _users;
        private UserDisplayModel _selectedUser;

        private Visibility _usersListVisibility;
        private Visibility _emptyListVisibility;

        public ObservableCollection<UserDisplayModel> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public UserDisplayModel SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }


        public Visibility UsersListVisibility
        {
            get => _usersListVisibility;
            set => SetProperty(ref _usersListVisibility, value);
        }

        public Visibility EmptyListVisibility
        {
            get => _emptyListVisibility;
            set => SetProperty(ref _emptyListVisibility, value);
        }

        public RelayCommand SelectUser { get; set; }
        public RelayCommand CreateUserCommand { get; set; }
        public RelayCommand DeleteUserCommand { get; set; }
        public RelayCommand LoadUsersCommand { get; set; }


        private readonly UserService _userService;
        private readonly ModalService _modalService;
        private readonly RolesService _rolesService;

        public ConfigureParametersUsersViewModel(UserService userService, ModalService modalService, RolesService rolesService)
        {
            _userService = userService; 
            _modalService = modalService;
            _rolesService = rolesService;

            SelectUser = new RelayCommand(SelectUserCommandCallback);
            CreateUserCommand = new RelayCommand(CreateUser);
            DeleteUserCommand = new RelayCommand(DeleteUser);
            LoadUsersCommand = new RelayCommand(LoadUsers);
            LoadUsersCommand.Execute(null);
        }

        private void SelectUserCommandCallback(object obj)
        {
            if (obj is UserDisplayModel userDisplay)
            {
                SelectedUser = userDisplay;
            }
        }
        
        private async void CreateUser(object e)
        {
            var result = await _modalService.ShowAsync<AddUserViewModelForm, AddUserFormViewModel>(
                modalId: null  // автогенерация ID
            );
            if (result.Result.IsCreated)
            {
                await CustomMessageBox.ShowSuccessAsync("Успешно",$"Пользователь {result.Result.Login} успешно добавлен");
                var user = result.Result.Returned;
                var role = _rolesService.GetRoleById(user.RoleId);
                var userDisplay = new UserDisplayModel(user, role?.DisplayName ?? "Без роли");
                Users.Add(userDisplay);
                CheckUsersCount();
            }
        }

        private async void DeleteUser(object e)
        {
            if (SelectedUser == null) return;

            var result = await _modalService.ShowAsync<RemoveUserForm, RemoveUserFormViewModel>(
                modalId: null,  // автогенерация ID
                options: new System.Collections.Generic.Dictionary<string, object>
                {
                    {nameof(RemoveUserFormViewModel.Name), SelectedUser.Login}
                }
            );
            if (result.Result.IsDeleted == true)
            {
                await CustomMessageBox.ShowSuccessAsync("Успешно",$"Пользователь {SelectedUser.Login} удалён");
                Users.Remove(SelectedUser);
                CheckUsersCount();
            }
            else
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", $"Не удалось удалить пользователя {SelectedUser.Login}");
            }
        }

        private async void LoadUsers(object e)
        {
            var users = await _userService.GetUsers();
            var userDisplayModels = users.Select(user =>
            {
                var role = _rolesService.GetRoleById(user.RoleId);
                return new UserDisplayModel(user, role?.DisplayName ?? "Без роли");
            }).ToList();

            Users = new ObservableCollection<UserDisplayModel>(userDisplayModels);
            CheckUsersCount();
        }

        private void CheckUsersCount()
        {
            if (Users.Count == 0)
            {
                UsersListVisibility = Visibility.Collapsed;
                EmptyListVisibility = Visibility.Visible;
            }
            else
            {
                UsersListVisibility = Visibility.Visible;
                EmptyListVisibility = Visibility.Collapsed;
            }
        }
    }
}
