using Microsoft.AspNetCore.Mvc;
using Observer.Services;
using Observer.Shared.Models;

namespace Observer.Controllers
{
    [Route("api/services/[action]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly ServiceWorker _serviceWorker;

        public ServiceController(ServiceWorker serviceWorker)
        {
            _serviceWorker = serviceWorker;
        }

        /// <summary>
        /// Получить список всех сервисов с информацией
        /// </summary>
        [HttpGet]
        [ActionName("getServices")]
        public ActionResult<List<ServiceInfo>> GetServicesInfo()
        {
            return Ok(_serviceWorker.GetServicesInfo());
        }

        /// <summary>
        /// Получить статусы всех сервисов (с IsRunning, ProcessId и т.д.)
        /// </summary>
        [HttpGet]
        [ActionName("getStatus")]
        public ActionResult<List<ServiceStatusDto>> GetServicesStatus()
        {
            return Ok(_serviceWorker.GetServicesStatus());
        }

        /// <summary>
        /// Получить информацию о сервисе по ID
        /// </summary>
        [HttpGet("{serviceId}")]
        [ActionName("getService")]
        public ActionResult<ServiceInfo> GetService(string serviceId)
        {
            var service = _serviceWorker.GetServicesInfo()
                .FirstOrDefault(p => p.Id == serviceId);

            if (service == null)
                return NotFound($"Service '{serviceId}' not found");

            return Ok(service);
        }

        /// <summary>
        /// Остановить сервис по ID
        /// </summary>
        [HttpPost("{serviceId}")]
        [ActionName("stop")]
        public ActionResult<ServiceInfo> StopService(string serviceId)
        {
            var service = _serviceWorker.GetServicesInfo()
                .FirstOrDefault(p => p.Id == serviceId);
            if (service == null)
                return NotFound($"Service '{serviceId}' not found");

            _serviceWorker.StopService(service);
            return Ok(service);
        }

        /// <summary>
        /// Запустить сервис по ID
        /// </summary>
        [HttpPost("{serviceId}")]
        [ActionName("start")]
        public ActionResult<ServiceInfo> StartService(string serviceId)
        {
            var service = _serviceWorker.GetServicesInfo()
                .FirstOrDefault(p => p.Id == serviceId);
            if (service == null)
                return NotFound($"Service '{serviceId}' not found");

            _serviceWorker.StartService(service);
            return Ok(service);
        }

        /// <summary>
        /// Перезапустить сервис по ID
        /// </summary>
        [HttpPost("{serviceId}")]
        [ActionName("restart")]
        public ActionResult<ServiceInfo> RestartService(string serviceId)
        {
            var service = _serviceWorker.GetServicesInfo()
                .FirstOrDefault(p => p.Id == serviceId);
            if (service == null)
                return NotFound($"Service '{serviceId}' not found");

            _serviceWorker.StopService(service, disableAutoRestart: false);
            System.Threading.Thread.Sleep(500); // Небольшая пауза перед перезапуском
            _serviceWorker.StartService(service, enableAutoRestart: true);
            return Ok(service);
        }
    }
}
