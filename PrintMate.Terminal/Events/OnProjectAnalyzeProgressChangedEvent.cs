using Prism.Events;

namespace PrintMate.Terminal.Events;

public class OnProjectAnalyzeProgressChangedEvent : PubSubEvent<double> // 0 - 100
{
    
}