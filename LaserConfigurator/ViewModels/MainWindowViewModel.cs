using Prism.Mvvm;

namespace LaserConfigurator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Laser Configurator - Настройка сканаторов Hans";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel()
        {

        }
    }
}
