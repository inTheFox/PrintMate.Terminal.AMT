using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrintMate.Terminal.Models;

namespace PrintMate.Terminal.Events
{
    public class OnUserAuthorized : PubSubEvent<User>
    {
    }
}
