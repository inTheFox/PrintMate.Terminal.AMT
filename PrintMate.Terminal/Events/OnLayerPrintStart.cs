using Prism.Events;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Events;

public class OnLayerPrintStart : PubSubEvent<Layer>
{
    
}

public class OnLayerPrintFinish : PubSubEvent<Layer>
{

}