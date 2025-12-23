using Microsoft.AspNetCore.SignalR;

namespace HansHostProvider.Hubs
{
    public class EventsHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"Client with connection id {Context.ConnectionId} has connected to EventsHub.");
            return base.OnConnectedAsync();
        }
    }
}
