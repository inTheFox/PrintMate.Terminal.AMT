using System;
using System.Collections.Generic;
using Opc2Lib;

namespace HansDebuggerApp.Opc;

public class Subscription
{
    public Guid Id = Guid.NewGuid();
    public Dictionary<CommandInfo, CommandResponse> Cache = new Dictionary<CommandInfo, CommandResponse>();
    public Action<CommandResponse> Callback { get; set; }
    public CommandInfo[] Commands { get; set; }
    public object Parent { get; set; }
}