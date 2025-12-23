namespace PrintMate.Terminal.Views;

public interface IAddaptiveUserControl
{
    void CallViewModelCommand(string name, object parameter);
    void CallViewModelCommand<T>(string name, object parameter);

}