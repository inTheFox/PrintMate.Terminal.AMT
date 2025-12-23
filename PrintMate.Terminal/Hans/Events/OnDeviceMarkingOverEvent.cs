using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hans.NET.libs;
using Prism.Events;

namespace HansScannerHost.Models.Events
{
    public class OnDeviceMarkingOverEvent : PubSubEvent<DeviceInfo>
    {
    }
}
