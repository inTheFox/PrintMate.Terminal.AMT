using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace HansDebuggerApp.Services
{

    public class PingService
    {
        public async Task<PingResult> PingHost(string hostnameOrIp, int timeout = 1000)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(hostnameOrIp, timeout);

                    return new PingResult
                    {
                        Success = reply.Status == IPStatus.Success,
                        Status = reply.Status.ToString(),
                        RoundtripTime = reply.RoundtripTime,
                        Address = reply.Address?.ToString() ?? "Unknown"
                    };
                }
            }
            catch (Exception ex)
            {
                return new PingResult
                {
                    Success = false,
                    Status = $"Error: {ex.Message}",
                    RoundtripTime = 0,
                    Address = "Unknown"
                };
            }
        }

        public async Task<PingResult> PingHost(string hostnameOrIp, int timeout, int ttl)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var options = new PingOptions
                    {
                        Ttl = ttl,
                        DontFragment = true
                    };

                    var buffer = new byte[32]; // 32 байта данных
                    var reply = await ping.SendPingAsync(hostnameOrIp, timeout, buffer, options);

                    return new PingResult
                    {
                        Success = reply.Status == IPStatus.Success,
                        Status = reply.Status.ToString(),
                        RoundtripTime = reply.RoundtripTime,
                        Address = reply.Address?.ToString() ?? "Unknown",
                        Ttl = reply.Options?.Ttl ?? 0
                    };
                }
            }
            catch (Exception ex)
            {
                return new PingResult
                {
                    Success = false,
                    Status = $"Error: {ex.Message}",
                    RoundtripTime = 0,
                    Address = "Unknown"
                };
            }
        }
    }
}
