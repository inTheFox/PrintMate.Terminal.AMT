
namespace Opc2Lib;

public interface ILogicControllerProvider
{
    public bool Connected { get; }
    public Task<T> GetAsync<T>(CommandInfo info);
    public Task<bool> GetBoolAsync(CommandInfo info);
    public Task SetBoolAsync(CommandInfo info, bool value);
    public Task<float> GetFloatAsync(CommandInfo info);
    public Task SetFloatAsync(CommandInfo info, float value);
    public Task<double> GetDoubleAsync(CommandInfo info);
    public Task SetDoubleAsync(CommandInfo info, double value);
    public Task<int> GetInt32Async(CommandInfo info);
    public Task SetInt32Async(CommandInfo info, int value);
    public Task<short> GetInt16Async(CommandInfo info);
    public Task SetInt16Async(CommandInfo info, short value);
    public Task<uint> GetUInt32Async(CommandInfo info);
    public Task SetUInt32Async(CommandInfo info, uint value);
    public Task<ushort> GetUInt16Async(CommandInfo info);
    public Task SetUInt16Async(CommandInfo info, ushort value);
    public Task WaitBoolValue(CommandInfo info, bool value, int delay = 500, CancellationToken? cancellationToken = null);
    public Task ConnectAsync();
    public Task DisconnectAsync();
}