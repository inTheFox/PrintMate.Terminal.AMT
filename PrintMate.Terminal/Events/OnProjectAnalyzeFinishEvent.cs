using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Events
{
    public class OnProjectAnalyzeFinishEvent : PubSubEvent<Project>
    {
    }
}
