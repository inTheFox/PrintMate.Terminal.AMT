using HandyControl.Tools.Command;

namespace PrintMate.Terminal.Views;

public static class MethodsExtension
{
    public static void CallModelCommand(string methodName, object model, object args)
    {
        var method = model.GetType().GetProperty(methodName).GetValue(model);
        if (method != null && (method is RelayCommand))
        {
            RelayCommand command = (RelayCommand)method;
            command.Execute(args);
        }
    }
}