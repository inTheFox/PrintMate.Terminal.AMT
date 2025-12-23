using Prism.Events;

namespace PrintMate.Terminal.Events;

public class OnProjectImportStatusChangedEvent : PubSubEvent<string>
{
    
}

public class OnProjectImportStatusProgressChangedEvent : PubSubEvent<int>
{

}