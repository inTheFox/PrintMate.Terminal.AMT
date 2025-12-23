using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HansScannerHost.Models
{
    public class PipeRequestMessage
    {
        public string Method { get; set; }
        public object[] Args { get; set; }

        public PipeRequestMessage()
        {
            
        }

        public PipeRequestMessage(string method, object[] args)
        {
            Method = method;
            Args = args;
        }
    }
}
