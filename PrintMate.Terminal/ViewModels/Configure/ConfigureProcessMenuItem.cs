using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels.Configure;

public class ConfigureProcessMenuItem : BindableBase
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string RegionName { get; set; }
    public string Image { get; set; }
}