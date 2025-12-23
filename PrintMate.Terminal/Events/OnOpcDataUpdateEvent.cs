using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc2Lib;
using PrintMate.Terminal.Opc;
using Prism.Events;

namespace PrintMate.Terminal.Events
{
    public class OnOpcDataUpdateEvent : PubSubEvent<List<CommandResponse>>
    {
    }
}
