using Prism.Mvvm;

namespace PrintMate.Terminal.Views;

public class ColMode : BindableBase
{
    private int _count;

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    public string ImageSource { get; set; } // путь к изображению
}