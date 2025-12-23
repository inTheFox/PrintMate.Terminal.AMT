using Hans.NET.libs;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HansScannerHost.Models.Events
{
    public class OnDeviceStatusUpdateEvent : PubSubEvent<DeviceInfo>
    {
    }
} 



