using System.Drawing;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels;

public class RightMenuItem : BindableBase
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Image { get; set; }

    private Brush _color;
    public Brush Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    public string RegionName { get; set; }
}