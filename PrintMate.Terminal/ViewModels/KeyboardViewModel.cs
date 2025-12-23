using HandyControl.Tools.Command;
using PrintMate.Terminal.Interfaces;
using PrintMate.Terminal.Views;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels
{
    public class KeyboardViewModel : BindableBase, IViewModelForm
    {
        public RelayCommand CloseCommand { get; set; }

        private string _value = string.Empty;
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        private string _title = "Введите значение";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private KeyboardType _keyboardType = KeyboardType.Full;
        public KeyboardType KeyboardType
        {
            get => _keyboardType;
            set => SetProperty(ref _keyboardType, value);
        }

        private KeyboardLanguage _initialLanguage = KeyboardLanguage.English;
        public KeyboardLanguage InitialLanguage
        {
            get => _initialLanguage;
            set => SetProperty(ref _initialLanguage, value);
        }

        public bool IsConfirmed { get; set; } = false;

        public KeyboardViewModel()
        {
            // CloseCommand будет установлен ModalService
        }
    }
}
