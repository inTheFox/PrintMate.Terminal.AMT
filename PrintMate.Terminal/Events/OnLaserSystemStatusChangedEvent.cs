using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LaserLib.Models;
using Prism.Events;

namespace PrintMate.Terminal.Events
{
    public class OnLaserSystemStatusChangedEvent : PubSubEvent<(int, LaserStatus)>
    {
    }
}
