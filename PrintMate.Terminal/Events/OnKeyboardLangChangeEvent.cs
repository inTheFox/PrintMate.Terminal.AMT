using Prism.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.Events
{
    public class OnKeyboardLangChangeEvent : PubSubEvent<CultureInfo>
    {
    }
}
