using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HansScannerHost.Models;
using Prism.Events;

namespace PrintMate.Terminal.Hans.Events
{
    public class OnMarkingProgressEvent : PubSubEvent<ScanatorProxyClient>
    {
    }
    public class OnSingleModeMarkingProgressEvent : PubSubEvent<ScanatorProxyClient>
    {
    }
}
