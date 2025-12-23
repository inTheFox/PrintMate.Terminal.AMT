using Observer.Shared.Models;

namespace Observer.Services
{
    public class ServiceMonitorService
    {
        private readonly ServiceWorker _serviceWorker;

        public ServiceMonitorService(ServiceWorker serviceWorker)
        {
            _serviceWorker = serviceWorker;
        }

        public List<ServiceStatusDto> GetServicesStatus()
        {
            return _serviceWorker.GetServicesStatus();
        }

        public List<ServiceInfo> GetServicesInfo()
        {
            return _serviceWorker.GetServicesInfo();
        }

        public void StartService(ServiceInfo serviceInfo)
        {
            _serviceWorker.StartService(serviceInfo);
        }

        public void StopService(ServiceInfo serviceInfo)
        {
            _serviceWorker.StopService(serviceInfo);
        }

        public bool IsServiceRunning(string serviceId)
        {
            return _serviceWorker.IsServiceRunning(serviceId);
        }
    }
}
