using System;
using Prism.Events;
using Prism.Ioc;

namespace HansDebuggerApp.Services;

public class PingObserverTask
{
    public string Name { get; set; }
    public string Address { get; set; }
    public PingResult Result = null;
    public event Action StateChanged;

    private readonly IEventAggregator _eventAggregator;

    public PingObserverTask()
    {
        _eventAggregator = Bootstrapper.ContainerProvider.Resolve<IEventAggregator>();
    }

    public bool HasChanged(PingResult pingResult)
    {
        bool isUpdated = Result?.Success != pingResult.Success;

        if (isUpdated)
        {
            Result = pingResult;
            StateChanged?.Invoke();
            //Console.WriteLine($"Name: {Name}, Address: {Address}, Ok: {pingResult.Success}, TTL: {pingResult.Ttl}, Time: {DateTime.Now.ToString("HH:mm:ss")}");
        }
        return isUpdated;
    }

    public void SetName(string name)
    {
        Name = name;
    }

    public void SetAddress(string address)
    {
        Address = address;
        Result = null;
    }

    public PingObserverTask(string name, string address)
    {
        Name = name;
        Address = address;
    }
}