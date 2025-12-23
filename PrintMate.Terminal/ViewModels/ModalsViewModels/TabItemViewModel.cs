using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels;

public class TabItemViewModel : BindableBase
{
    private string _header;

    public string Header
    {
        get { return _header; }
        set { SetProperty(ref _header, value); }
    }

    public TabItemViewModel(string header)
    {
        Header = header;
    }
}