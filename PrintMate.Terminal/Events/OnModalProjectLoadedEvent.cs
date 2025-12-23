using Prism.Events;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Events
{
    /// <summary>
    /// Событие загрузки проекта в модальном окне просмотра
    /// </summary>
    public class OnModalProjectLoadedEvent : PubSubEvent<Project>
    {
    }
}
