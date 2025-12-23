using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Opc2Lib
{
    public class CommandProvider
    {
        public readonly Dictionary<CommandId, CommandInfo> Commmands;

        public CommandProvider()
        {
            Commmands = Parse();
        }

        private Dictionary<CommandId, CommandInfo> Parse()
        {
            Dictionary<CommandId, CommandInfo> commands = new Dictionary<CommandId, CommandInfo>();

            foreach (var field in typeof(OpcCommands).GetFields())
            {

                if (Enum.TryParse<CommandId>(field.Name, out var v))
                {
                    commands.Add(Enum.Parse<CommandId>(field.Name), (CommandInfo)field.GetValue(null));
                }
            }
            return commands;
        }

        public CommandInfo? GetCommandOrDefault(CommandId commandId)
        {
            if (!Commmands.ContainsKey(commandId)) { return null; }
            return Commmands[commandId];
        }
    }
}
