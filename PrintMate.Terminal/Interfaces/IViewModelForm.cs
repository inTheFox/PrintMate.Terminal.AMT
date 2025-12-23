using System.Windows;
using System.Windows.Input;
using HandyControl.Tools.Command;

namespace PrintMate.Terminal.Interfaces;

public interface IViewModelForm
{
    public RelayCommand CloseCommand { get; set; }
}