using System;
using Opc2Lib;

namespace PrintMate.Terminal.Opc;

public interface ILogicControllerObserver
{
    public Subscription Subscribe(object parent, Action<CommandResponse> callback, params CommandInfo[] commands);
    public void Unsubscribe(Subscription subscription);
}