using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HansScannerHost.Models
{
    public class PipeCallbacksMessage
    {
        public string CallbackName { get; set; }
        public object[] Args { get; set; }
        public uint Code { get; set; }
    }
}
