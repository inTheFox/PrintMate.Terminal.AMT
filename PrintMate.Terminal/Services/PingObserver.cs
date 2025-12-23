using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Events;
using Prism.Events;

namespace PrintMate.Terminal.Services
{
    public class PingObserver
    {
        public static PingObserverTask PlcConnectionObserver = null;
        public static PingObserverTask Laser1ConnectionObserver = null;
        public static PingObserverTask Laser2ConnectionObserver = null;

        private readonly PingService _pingService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ConfigurationManager _configurationManager;

        public PingObserver(PingService pingService, IEventAggregator eventAggregator, ConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
            _pingService = pingService;
            _eventAggregator = eventAggregator;
        }

        public void InitListeners()
        {
            PlcSettings plcSettings = _configurationManager.Get<PlcSettings>();
            LaserSettings laserSettings = _configurationManager.Get<LaserSettings>();


            PlcConnectionObserver = new PingObserverTask(nameof(PlcConnectionObserver), plcSettings.Address);
            Laser1ConnectionObserver = new PingObserverTask(nameof(Laser1ConnectionObserver), laserSettings.Laser1Address);
            Laser2ConnectionObserver = new PingObserverTask(nameof(Laser2ConnectionObserver), laserSettings.Laser2Address);
        }

        public async Task StartObserver(PingObserverTask task)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (task.HasChanged(await _pingService.PingHost(task.Address, 10000)))
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
