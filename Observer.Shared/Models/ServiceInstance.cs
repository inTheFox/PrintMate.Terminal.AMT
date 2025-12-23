using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observer.Shared.Models
{
    public class ServiceInstance
    {
        public Guid Id { get; set; }
        public ServiceInfo ServiceInfo { get; set; }
        public Process Process { get; set; }
    }
}
