using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HansScannerHost.Models
{
    public class PipeEventMessage
    {
        public string Event { get; set; }
        public object[] Args { get; set; }
        public uint Code { get; set; }
    }
}
