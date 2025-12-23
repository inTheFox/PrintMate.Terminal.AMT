
using HandyControl.Controls;
using ImTools;
using Newtonsoft.Json;
using Opc.Ua;
using Opc2Lib;
using Prism.Events;
using Prism.Regions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup.Localizer;
using System.Xml.Linq;
using HansDebuggerApp.Events;
using HansDebuggerApp.OPC;
using HansDebuggerApp.Services;
using static Opc.Ua.RelativePathFormatter;
using MessageBox = System.Windows.MessageBox;

namespace HansDebuggerApp.Opc
{
    public class LogicControllerService : ILogicControllerProvider
    {
        private LogicControllerUaClient _client;

        public bool Connected
        {
            get
            {
                if (_client == null) return false;
                return _client.Connected;
            }
        }

        private readonly IEventAggregator _eventAggregator;
        public static readonly LogicControllerObserver Observer;

        private readonly Dictionary<string, Command> _commands = new();
        public List<CommandCallback> Callbacks = new();
        private bool _connecting;

        public LogicControllerService(
            IEventAggregator eventAggregator, 
            IRegionManager regionManager, 
            PingService pingService
            )
        {
            _eventAggregator = eventAggregator;

            // Проверяем пингуется ли ПЛК на момент инициализации 
            Task.Run(async () =>
            {
                if (PingObserver.PlcConnectionObserver != null &&
                    PingObserver.PlcConnectionObserver.Result != null
                   )
                {
                    if (PingObserver.PlcConnectionObserver.Result.Success)
                    {
                        Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await ConnectAsync();
                            Console.WriteLine("Подключились !");
                        });
                    }
                }
            });


            // Подписываемся на пинг ПЛК. Если пингуается, то подключаемся
            _eventAggregator.GetEvent<OnPingObserverTaskUpdatedEvent>().Subscribe(async (data) =>
            {
                try
                {
                    Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        if (data.Name != nameof(PingObserver.PlcConnectionObserver)) return;

                        if (data.Result.Success)
                        {
                            await ConnectAsync();
                            Console.WriteLine("Подключились по событию");
                        }
                        else
                        {
                            if (_client != null)
                                _client.Disconnect();
                        }
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }

        public async Task WaitBoolValue(CommandInfo info, bool value, int delay = 500)
        {
            
        }

        public async Task WaitBoolValue(CommandInfo info, bool value, int delay = 500, CancellationToken? cancellationToken = null)
        {
            
        }

        public async Task ConnectAsync()
        {
            _connecting = true;
            var connectionProperties = new PlcSettings();
            try
            {
                if (_client != null && _client.Connected)
                {
                    _client.Disconnect();
                }

                UserIdentity identity = new UserIdentity(connectionProperties.Login, connectionProperties.Password);

                _client = new LogicControllerUaClient(
                    commandPrefix: connectionProperties.VarSpace,
                    namespaceId: connectionProperties.NamespaceId,
                    serverAddress: connectionProperties.Address,
                    port: connectionProperties.Port,
                    timeoutMs: connectionProperties.Timeout,
                    policy: connectionProperties.Policy,
                    identity: identity);
    
                await _client.ConnectAsync();

                if (_client.Connected)
                {
                    Growl.Success("Успешное подключение к серверу");
                }
                else
                {
                    Growl.Error("Ошибка при подключении к серверу");
                }
            }
            catch (Exception e)
            {
                Growl.Error("Ошибка при подключении к серверу");
            }
            _connecting = false;
        }

        public async Task DisconnectAsync()
        {
            if (_client != null)
            { 
                _client.Disconnect();
            }
        }

        public async Task<T> GetAsync<T>(CommandInfo info)
        {
            try
            {
                return await _client.GetAsync<T>(info);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибочка");

                switch (info.ValueCommandType)
                {
                    case ValueCommandType.Bool:
                        return (T)(object)false;
                        break;
                    case ValueCommandType.Real:
                        return (T)(object)0f;
                        break;
                    case ValueCommandType.Unsigned:
                        return (T)(object)(ushort)0;
                        break;
                    case ValueCommandType.Dint:
                        return (T)(object)1;
                        break;
                }

                return default(T);
            }
        }

        public async Task<bool> GetBoolAsync(CommandInfo info) => await _client?.GetAsync<bool>(info);
        public async Task SetBoolAsync(CommandInfo info, bool value) => await _client?.SetAsync(info, value);
        public async Task<float> GetFloatAsync(CommandInfo info) => await _client?.GetAsync<float>(info);
        public async Task SetFloatAsync(CommandInfo info, float value) => await _client?.SetAsync(info, value);
        public async Task<double> GetDoubleAsync(CommandInfo info) => await _client?.GetAsync<double>(info);
        public async Task SetDoubleAsync(CommandInfo info, double value) => await _client?.SetAsync(info, value);
        public async Task<int> GetInt32Async(CommandInfo info) => await _client?.GetAsync<int>(info);
        public async Task SetInt32Async(CommandInfo info, int value) => await _client?.SetAsync(info, value);
        public async Task<short> GetInt16Async(CommandInfo info) => await _client?.GetAsync<short>(info);
        public async Task SetInt16Async(CommandInfo info, short value) => await _client?.SetAsync(info, value);
        public async Task<uint> GetUInt32Async(CommandInfo info) => await _client?.GetAsync<uint>(info);
        public async Task SetUInt32Async(CommandInfo info, uint value) => await _client?.SetAsync(info, value);
        public async Task<ushort> GetUInt16Async(CommandInfo info) => await _client?.GetAsync<ushort>(info);
        public async Task SetUInt16Async(CommandInfo info, ushort value) => await _client?.SetAsync(info, value);
    }
}
