using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HansHostProvider.Shared
{
    public static class EventId
    {
        public const string OnConnected = "OnConnected";
        public const string ReadyToConnect = "ReadyToConnect";
        public const string Connected = "Connected";
        public const string Disconnected = "Disconnected";
        public const string StreamProgress = "StreamProgress";
        public const string StreamEnd = "StreamEnd";
        public const string MarkingProgress = "MarkingProgress";
        public const string MarkingComplete = "MarkingComplete";
        public const string Status = "Status";

    }
}
