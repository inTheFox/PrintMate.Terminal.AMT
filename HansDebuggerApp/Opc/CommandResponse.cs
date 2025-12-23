using Opc2Lib;

namespace HansDebuggerApp.Opc;

public class CommandResponse
{
    public CommandInfo CommandInfo { get; set; }

    private object _value = null;
    public object Value
    {
        set => _value = value;
        get
        {
            if (_value == null)
            {
                switch (CommandInfo.ValueCommandType)
                {
                    case ValueCommandType.Bool: return false;
                    case ValueCommandType.Real: return 0f;
                    case ValueCommandType.Unsigned: return (ushort)0;
                    case ValueCommandType.Dint: return (int)0;
                    default: return default;
                }
            }
            else
            {
                return _value;
            }
        }
    }

    public T Gett<T>()
    {
        if (Value == null) return default;
        return (T)Value;
    }
}