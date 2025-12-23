using HandyControl.Controls;
using HandyControl.Tools.Command;
using Microsoft.VisualBasic.Logging;
using Newtonsoft.Json;
using Opc2Lib;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Services;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;

namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels
{
    public class ConfigureParametersPlcViewModel : BindableBase, IRegionMemberLifetime
    {
        public bool KeepAlive => false;

        private const string _enabledSrc = "/images/indicator_green_32.png";
        private const string _disabledSrc = "/images/indicator_red_32.png";

        private string _address;
        private int _port;
        private int _timeout;
        private LogicControllerUaClient.SecurityPolicies _policy;
        private string _login;
        private string _password;
        private string _varSpace;
        private int _namespaceId;
        private int _test;
        private bool _isPing;
        private string _pingIconSource;
        private string _pingText;
        private Brush _pingTextColor;
        private string _connectIconSource;
        private string _connectText;
        private Brush _connectTextColor;

        public string Address
        {
            get => _address;
            set
            {
                SetProperty(ref _address, value);
            }
        }
        public int Port
        {
            get => _port;
            set
            {
                SetProperty(ref _port, value);

            }
        }
        public int Timeout
        {
            get => _timeout;
            set
            {
                SetProperty(ref _timeout, value);

            }
        }
        public LogicControllerUaClient.SecurityPolicies Policy
        {
            get => _policy;
            set
            {
                SetProperty(ref _policy, value);

            }
        }
        public string Login
        {
            get => _login;
            set
            {
                SetProperty(ref _login, value);

            }
        }
        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
            }
        }
        public string VarSpace
        {
            get => _varSpace;
            set
            {
                SetProperty(ref _varSpace, value);
            }
        }
        public int NamespaceId
        {
            get => _namespaceId;
            set
            {
                SetProperty(ref _namespaceId, value);
            }
        }
        public bool IsPing
        {
            get => _isPing;
            set
            {
                SetProperty(ref _isPing, value);
            }
        }
        public string PingIconSource
        {
            get => _pingIconSource;
            set => SetProperty(ref _pingIconSource, value);
        }
        public string PingText
        {
            get => _pingText;
            set => SetProperty(ref _pingText, value);
        }
        public Brush PingTextColor
        {
            get => _pingTextColor;
            set => SetProperty(ref _pingTextColor, value);
        }
        public string ConnectIconSource
        {
            get => _connectIconSource;
            set => SetProperty(ref _connectIconSource, value);
        }
        public string ConnectText
        {
            get => _connectText;
            set => SetProperty(ref _connectText, value);
        }
        public Brush ConnectTextColor
        {
            get => _connectTextColor;
            set => SetProperty(ref _connectTextColor, value);
        }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand ResetCommand { get; set; }

        private readonly ConfigurationManager _configManager;
        private readonly ILogicControllerProvider _logicControllerProvider;
        private readonly IEventAggregator _eventAggregator;


        public ConfigureParametersPlcViewModel(
            ConfigurationManager configManager,
            ILogicControllerProvider logicControllerProvider,
            IEventAggregator eventAggregator)
        {
            _configManager = configManager;
            _eventAggregator = eventAggregator;
            _logicControllerProvider = logicControllerProvider;

            PingObserver.PlcConnectionObserver.StateChanged += PlcPingObserverOnStateChanged;

            LoadConfiguration();

            // Получаем текущие значения
            PlcPingObserverOnStateChanged();
            PlcConnectionObserverOnStateChanged();

            // Подписываемся на обновления состояния PingObserver
            eventAggregator.GetEvent<OnPingObserverTaskUpdatedEvent>().Subscribe(PingObserverStateChanged);

            OkCommand = new RelayCommand(OkCommandHandler);
            ResetCommand = new RelayCommand(ResetCommandHandler);
        }

        private void LoadConfiguration()
        {
            var settings = _configManager.Get<PlcSettings>();

            Address = settings.Address;
            Port = settings.Port;
            Timeout = settings.Timeout;
            Policy = settings.Policy;
            Login = settings.Login;
            Password = settings.Password;
            VarSpace = settings.VarSpace;
            NamespaceId = settings.NamespaceId;

            Console.WriteLine($"PLC Configuration loaded: Address={Address}, Port={Port}");
        }

        private void PingObserverStateChanged(PingObserverTask task)
        {
            if (task.Name != nameof(PingObserver.PlcConnectionObserver)) return;
            PlcPingObserverOnStateChanged();
        }

        private void OkCommandHandler(object obj)
        {
            _configManager.Update<PlcSettings>(settings =>
            {
                settings.Address = Address;
                settings.Port = Port;
                settings.Timeout = Timeout;
                settings.Policy = Policy;
                settings.Login = Login;
                settings.Password = Password;
                settings.VarSpace = VarSpace;
                settings.NamespaceId = NamespaceId;
            });

            _configManager.SaveNow();
            PingObserver.PlcConnectionObserver.SetAddress(Address);

            Console.WriteLine("PLC settings saved successfully");
        }

        private void ResetCommandHandler(object obj)
        {
            _configManager.Reset<PlcSettings>();
            _configManager.SaveNow();
            LoadConfiguration();
            PingObserver.PlcConnectionObserver.SetAddress(Address);

            Console.WriteLine("PLC settings reset to default");
        }

        private void PlcConnectionObserverOnStateChanged()
        {
            bool connected = _logicControllerProvider.Connected;

            if (connected)
            {
                ConnectIconSource = _enabledSrc;
                ConnectText = "Подключен";
                ConnectTextColor = System.Windows.Media.Brushes.LimeGreen;
            }
            else
            {
                ConnectIconSource = _disabledSrc;
                ConnectText = "Не подключен";
                ConnectTextColor = System.Windows.Media.Brushes.IndianRed;
            }
        }

        private void PlcPingObserverOnStateChanged()
        {
            IsPing = PingObserver.PlcConnectionObserver.Result?.Success ?? false;

            if (IsPing)
            {
                PingIconSource = _enabledSrc;
                PingText = "В сети";
                PingTextColor = System.Windows.Media.Brushes.LimeGreen;
            }
            else
            {
                PingIconSource = _disabledSrc;
                PingText = "Не в сети";
                PingTextColor = System.Windows.Media.Brushes.IndianRed;
            }
        }
    }
}
