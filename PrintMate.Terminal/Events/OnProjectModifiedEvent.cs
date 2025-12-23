using Prism.Events;

namespace PrintMate.Terminal.Events
{
    /// <summary>
    /// Событие, которое публикуется при изменении проекта (удаление деталей, изменение параметров и т.д.)
    /// </summary>
    public class OnProjectModifiedEvent : PubSubEvent<string>
    {
    }
}
