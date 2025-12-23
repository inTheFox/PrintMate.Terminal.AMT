using Hans.NET.libs;
using Prism.Events;

namespace HansScannerHost.Models.Events;

public class OnNewDeviceDetectedEvent : PubSubEvent<DeviceInfo> 
{
    
}