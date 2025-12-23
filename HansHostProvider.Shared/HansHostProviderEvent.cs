using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HansHostProvider.Shared
{
    public class HansHostProviderEvent
    {
        public Guid Id { get; set; }
        public string EventName { get; set; }
        public string EventArgsJson { get; set; }
    }
}
