using System.Threading.Tasks;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Services;
using Prism.Events;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels;

public class LoginScreenViewModel : BindableBase
{
    private readonly UserService userService;
    public LoginScreenViewModel(IEventAggregator eventAggregator, UserService userService)
    {
        this.userService = userService;
        _eventAggregator = eventAggregator;
        //LoginCommand = new RelayCommand(OnLoginCommand);
    }

    private string _login;
    private string _password;

    private RelayCommand _loginCommand;

    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public RelayCommand LoginCommand
    {
        get => _loginCommand ??= new RelayCommand(async obj =>
        {
            await OnLoginCommand();
        });
    }

    private readonly IEventAggregator _eventAggregator;


    private async Task OnLoginCommand()
    {
        var user = await userService.GetByLogin(Login);
        if (user == null)
        {
            // пользователя с таким логином не существует
            return;
        }

        //if (BCrypt.Net.BCrypt.Verify(Password, user.Password))
        //{
        //    //следующая страница
        //}
        //else
        //{
        //    // неверный пароль
        //    return;
        //}

    }
}