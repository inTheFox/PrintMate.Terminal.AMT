using Prism.Events;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Events
{
    /// <summary>
    /// Событие изменения текущего отображаемого слоя
    /// </summary>
    public class OnLayerChangedEvent : PubSubEvent<Layer>
    {
    }
}
