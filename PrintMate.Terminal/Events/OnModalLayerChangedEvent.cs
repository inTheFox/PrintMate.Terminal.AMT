using Prism.Events;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Events
{
    /// <summary>
    /// Событие смены слоя в модальном окне просмотра
    /// </summary>
    public class OnModalLayerChangedEvent : PubSubEvent<Layer>
    {
    }
}
