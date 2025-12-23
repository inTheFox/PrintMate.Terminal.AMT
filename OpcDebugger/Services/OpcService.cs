using HandyControl.Controls;
using Opc.Ua;
using Opc2Lib;
using OpcDebugger.Events;
using OpcDebugger.Views;
using Prism.Events;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Xml.Linq;

namespace OpcDebugger.Services
{
    public class ElementInfo
    {
        public string Cmd { get; set; }
        public string Name { get; set; }
        public string ValueType { get; set; }
        public object Value { get; set; }
        public bool IsSelected { get; set; }
        
        public bool Equals(ElementInfo other)
        {
            if (other == null) return false;
            return Name == other.Name && Cmd == other.Cmd;
        }

        public override bool Equals(object obj) => Equals(obj as ElementInfo);

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Cmd);
        }
    }

    public class OpcService
    {
        public List<ElementInfo> Elements { get; set; } = new List<ElementInfo>();
        public ElementInfo SelectedItem = null;
        public LogicControllerUaClient Client { get; set; }
        public bool IsConnect { get; set; }

        private readonly IEventAggregator _eventAggregator;

        public event Action<ElementInfo> OnSelectedItemChanged;

        public OpcService(IEventAggregator eventAggregator, IRegionManager regionManager)
        {
            _eventAggregator = eventAggregator;
            //_eventAggregator.GetEvent<SelectedItemEvent>().Subscribe((element) =>
            //{
            //    SelectedItem = element;
            //    regionManager.RequestNavigate("SelectedItemRegion", nameof(SelectedItemView));
            //});
            ParseDataset();
        }

        public void ParseDataset()
        {
            // Загружаем XML-документы

            //MessageBox.Show(Path.Combine(Environment.CurrentDirectory, "Dataset", "HardSignal_AMT32.xml"));

            var elementsDoc = XDocument.Load(Path.Combine(Environment.CurrentDirectory, "Dataset", "HardSignal_AMT32.xml"));
            var localesDoc = XDocument.Load(Path.Combine(Environment.CurrentDirectory, "Dataset", "HardSignal_Lang_AMT32.xml"));

            // Создаём словарь локалей: name -> ru
            var localeDict = localesDoc
                .Root
                ?.Elements("locale")
                .ToDictionary(
                    el => el.Attribute("name")?.Value ?? string.Empty,
                    el => el.Attribute("ru")?.Value ?? string.Empty
                ) ?? new Dictionary<string, string>();

            // Парсим элементы и сопоставляем с локализацией
            Elements = elementsDoc
                .Root
                ?.Elements("element")
                .Select(el =>
                {
                    var cmd = el.Attribute("name")?.Value ?? string.Empty;
                    var ruName = localeDict.TryGetValue(cmd, out var name) ? name : cmd; // если нет перевода — оставляем как есть
                    var type = el.Attribute("type")?.Value ?? "string";
                    return new ElementInfo { Cmd = cmd, Name = ruName, ValueType = type};
                })
                .ToList() ?? new List<ElementInfo>();
        }

        public List<ElementInfo> GetElements() => Elements.ToList();

        public async Task Connect(string address, int port)
        {
            try
            {
                UserIdentity identity = new UserIdentity("guiopc", "1");
                
                Client = new OpcUaClient(
                    serverAddress: "172.16.1.1",
                    port: 4840,
                    timeoutMs: 10000,
                    policy: OpcUaClient.SecurityPolicies.None,
                    identity: identity
                );

                await Client.ConnectAsync();
                IsConnect = true;
                Growl.SuccessGlobal("Успешное подключение к серверу");
            }
            catch (Exception e)
            {
                IsConnect = false;
                Growl.ErrorGlobal("Ошибка при подключении к серверу");
            }
        }

        public void SetSelected(ElementInfo element)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SelectedItem = element;
                OnSelectedItemChanged?.Invoke(element);
            });
        }
    }
}
