using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HansDebuggerApp.Events;
using HansDebuggerApp.OPC;
using Newtonsoft.Json;
using Prism.Events;

namespace HansDebuggerApp.Services
{
    public class PingObserver
    {
        public static PingObserverTask PlcConnectionObserver = null;
        public static PingObserverTask Laser1ConnectionObserver = null;
        public static PingObserverTask Laser2ConnectionObserver = null;
        public static PingObserverTask Scanator1ConnectionObserver = null;
        public static PingObserverTask Scanator2ConnectionObserver = null;

        private readonly PingService _pingService;
        private readonly IEventAggregator _eventAggregator;

        public PingObserver(PingService pingService, IEventAggregator eventAggregator)
        {
            _pingService = pingService;
            _eventAggregator = eventAggregator;
        }

        public void InitListeners()
        {
            var plcSettings = new PlcSettings();
            PlcConnectionObserver = new PingObserverTask(nameof(PlcConnectionObserver), plcSettings.Address);
        }

        public async Task StartObserver(PingObserverTask task)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (task.HasChanged(await _pingService.PingHost(task.Address, 1000)))
                        {
                            _eventAggregator.GetEvent<OnPingObserverTaskUpdatedEvent>().Publish(task);
                        }

                        await Task.Delay(2000);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Task.Factory.StartNew(async () => await StartObserver(task));
                    }
                }
            });
        }
    }
}
