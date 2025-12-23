using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observer.Shared.Models
{
    public class ServiceInfo
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public string Url { get; set; }
        public string StartupArguments { get; set; }
    }
}
