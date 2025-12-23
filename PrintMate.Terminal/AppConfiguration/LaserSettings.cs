using PrintMate.Terminal.ConfigurationSystem.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.AppConfiguration
{
    public class LaserSettings : ConfigurationModelBase
    {
        public string Laser1Address = "192.168.100.100";
        public string Laser2Address = "192.168.100.101";
    }
}
