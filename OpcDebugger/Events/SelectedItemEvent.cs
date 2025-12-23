using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpcDebugger.Services;
using Prism.Events;

namespace OpcDebugger.Events
{
    public class SelectedItemEvent : PubSubEvent<ElementInfo>
    {
    }
}
