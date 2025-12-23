using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels.Configure;

public class ConfigureParametersMenuItem : BindableBase
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string RegionName { get; set; }
    public string Path { get; set; }

}