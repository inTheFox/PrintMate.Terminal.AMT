using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels;

public class ManualControlViewItem : BindableBase
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
}