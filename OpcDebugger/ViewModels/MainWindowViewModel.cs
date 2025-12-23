using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HandyControl.Tools.Command;
using Opc2Lib;
using OpcDebugger.Services;
using Prism.Mvvm;

namespace OpcDebugger.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Application";
        private string _address = "172.16.1.1";
        private int _port = 4840;
        private readonly OpcService _opcService;


        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value); 
        }
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public ICommand ConnectCommand { get; set; }

        public MainWindowViewModel(OpcService opcService)
        {
            _opcService = opcService;
            ConnectCommand = new RelayCommand(ConnectCommandCallback);
        }

        private void ConnectCommandCallback(object obj)
        {
            Application.Current.Dispatcher.InvokeAsync(()=> Task.Factory.StartNew(async ()=> await _opcService.Connect(Address, Port)));
        }
    }
}
