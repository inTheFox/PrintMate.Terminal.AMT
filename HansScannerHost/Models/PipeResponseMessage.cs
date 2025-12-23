using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HansScannerHost.Models
{
    public class PipeResponseMessage
    {
        public string Response { get; set; }
        public uint Code { get; set; }

        public PipeResponseMessage()
        {
            
        }

        public PipeResponseMessage(string response, uint code)
        {
            Response = response;
            Code = code;
        }
    }
}
