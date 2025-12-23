using Hans.NET.libs;
using HansHostProvider.Services;

namespace HansHostProvider.Utils
{
    public class HansServiceUtils
    {
        public static DeviceInfo DeviceRefresh(int ipIndex, ulong uIp)
        {
            DeviceInfo device = new DeviceInfo
            {
                IPValue = uIp,
                Index = ipIndex,
                DeviceName = $"{(uIp >> 0) & 0xFF}.{(uIp >> 8) & 0xFF}.{(uIp >> 16) & 0xFF}.{(uIp >> 24) & 0xFF}"
            };
            return device;
        }
    }
}
