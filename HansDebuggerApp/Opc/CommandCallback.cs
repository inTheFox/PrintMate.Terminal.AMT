using System;
using System.Threading.Tasks;
using Opc2Lib;

namespace HansDebuggerApp.Opc;

public class CommandCallback
{
    public CommandInfo CommandInfo { get; set; }
    public Func<CommandResponse, Task> Callback { get; set; }

    public CommandCallback(CommandInfo commandInfo, Func<CommandResponse, Task> callback)
    {
        CommandInfo = commandInfo;
        Callback = callback;
    }
}