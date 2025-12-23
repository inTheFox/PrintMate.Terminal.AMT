using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LaserConfigurator.Models;
using Prism.Events;

namespace LaserConfigurator.Events
{
    public class OnScanatorStatusChanged : PubSubEvent<HansDeviceState>
    {

    }
}
