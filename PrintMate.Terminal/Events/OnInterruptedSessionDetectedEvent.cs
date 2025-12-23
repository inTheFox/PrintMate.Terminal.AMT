using PrintSpectator.Shared.Models;
using Prism.Events;

namespace PrintMate.Terminal.Events
{
    /// <summary>
    /// Событие обнаружения прерванной сессии печати при запуске приложения.
    /// </summary>
    public class OnInterruptedSessionDetectedEvent : PubSubEvent<PrintSession>
    {
    }
}
