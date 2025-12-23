using LaserLib;
using PrintMate.Terminal.AppConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Events;
using Prism.Events;

namespace PrintMate.Terminal.Services
{
    public class MultiLaserSystemService
    {   
        public readonly Dictionary<int, LaserService> LaserServices = new();

        public MultiLaserSystemService(IEventAggregator eventAggregator, ConfigurationManager configurationManager)
        {
            LaserSettings laserSettings = configurationManager.Get<LaserSettings>();
            var laser1 = new LaserService(laserSettings.Laser1Address);
            var laser2 = new LaserService(laserSettings.Laser2Address);

            LaserServices.Add(1, laser1);
            LaserServices.Add(2, laser2);
        }

        public LaserService GetService(int id) => LaserServices[id];
    }
}
