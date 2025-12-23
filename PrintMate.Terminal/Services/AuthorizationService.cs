using HandyControl.Tools;
using Microsoft.VisualBasic.ApplicationServices;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Models;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using User = PrintMate.Terminal.Models.User;

namespace PrintMate.Terminal.Services
{
    public class AuthorizationService
    {
        private User _currentUser = null;
        private User _rootProfile = new User
        {
            Login = "a",
            Password = "a",
            Name = "Администратор",
            Family = ""
        };

        private readonly UserService _userService;
        private readonly IEventAggregator _eventAggregator;
        private readonly LoggerService _loggerService;

        public AuthorizationService(UserService userService, IEventAggregator eventAggregator, LoggerService loggerService)
        {
            _loggerService = loggerService;
            _eventAggregator = eventAggregator;
            _userService = userService;

            _currentUser = _rootProfile;
        }

        public async Task<bool> LoginAsync(string login, string password)
        {
            _currentUser = null;

            if (IsRootProfile(login, password))
            {
                _currentUser = _rootProfile;
                await _loggerService.InformationAsync(this, $"Администратор успешно вошел в систему");
                return true;
            }
            var user = await _userService.GetByLogin(login);
            if (user == null)
            {
                await _loggerService.ErrorAsync(this, $"Неудная попытка входа. Логин: {login}, Пароль {password}), Ошибка: LOGIN_WRONG");
                return false;
            } 
            if (user.Password == password)
            {
                _currentUser = user;
                await _loggerService.InformationAsync(this, $"Пользователь {login} ({user.Name} {user.Family}) успешно вошел в систему");
                return true;
            }
            else
            {
                await _loggerService.ErrorAsync(this, $"Неудная попытка входа. Логин: {login}, Пароль {password}), Ошибка: PASSWORD_WRONG");
                return false;
            }
        }

        public void Join()
        {
            _eventAggregator.GetEvent<OnUserAuthorized>().Publish(_currentUser);
        }

        public User GetUser() => _currentUser; 

        public async Task Logout()
        {
            await _loggerService.InformationAsync(this, $"Пользователь {_currentUser.Login} ({_currentUser.Name} {_currentUser.Family}) вышел из учетной записи");
            _currentUser = null;
            _eventAggregator.GetEvent<OnUserQuit>().Publish();
        }

        public bool IsRootAuthorized()
        {
            return _currentUser.Login == _rootProfile.Login;
        }

        private bool IsRootProfile(string login, string password)
        {
            return login == _rootProfile.Login && password == _rootProfile.Password;
        }
    }
}
