using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using Prism.Events;

namespace PrintMate.Terminal.Events
{
    public class OnCameraSelectedEvent : PubSubEvent<CameraItem>
    {
    }
}
