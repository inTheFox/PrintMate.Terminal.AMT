
using HandyControl.Controls;
using ImTools;
using Newtonsoft.Json;
using Opc.Ua;
using Opc2Lib;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Services;
using Prism.Events;
using Prism.Regions;
using ProjectParserTest.Parsers.Shared.Models;
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
using static Opc.Ua.RelativePathFormatter;
using MessageBox = System.Windows.MessageBox;

namespace PrintMate.Terminal.Opc
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
        private LoggerService _loggerService;

        private readonly Dictionary<string, Command> _commands = new();
        public List<CommandCallback> Callbacks = new();
        private bool _connecting;

        public LogicControllerService(
            IEventAggregator eventAggregator, 
            IRegionManager regionManager, 
            LoggerService loggerService,
            PingService pingService
            )
        {
            _eventAggregator = eventAggregator;
            _loggerService = loggerService;

            // Проверяем пингуется ли ПЛК на момент инициализации 
            Task.Run(async () =>
            {
                if (PingObserver.PlcConnectionObserver != null &&
                    PingObserver.PlcConnectionObserver.Result != null
                   )
                {
                    if (PingObserver.PlcConnectionObserver.Result.Success)
                    {
                        Console.WriteLine($"PING PLC ADDRESS: {PingObserver.PlcConnectionObserver.Result.Address}, STATUS: {PingObserver.PlcConnectionObserver.Result.StatusCode}");
                        Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            _ = Task.Run(async () =>
                            {
                                await ConnectAsync();
                                Console.WriteLine("Подключились !");
                            });
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

                        Console.WriteLine($"PING PLC ADDRESS: {data.Address}, STATUS: {data.Result.StatusCode}");

                        if (data.Result.Success)
                        {
                            _ = Task.Run(async () =>
                            {
                                await ConnectAsync();
                                Console.WriteLine("Подключились по событию!");
                            });
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

        public async Task WaitBoolValue(CommandInfo info, bool value, int delay = 500, CancellationToken? cancellationToken = null)
        {
            while (true)
            {
                if (cancellationToken != null && cancellationToken.HasValue &&
                    cancellationToken.Value.IsCancellationRequested) return;

                if (await GetBoolAsync(info) == value) break;
                await Task.Delay(delay);
            }
        }

        public async Task ConnectAsync()
        {
            _connecting = true;
            var connectionProperties = Bootstrapper.Configuration.Get<PlcSettings>();
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
                    //Connected = true;
                    _eventAggregator.GetEvent<OnLogicControllerConnectionStateChanged>().Publish(true);
                    Growl.Success("Успешное подключение к серверу");
                }
                else
                {
                    //Connected = false;
                    _eventAggregator.GetEvent<OnLogicControllerConnectionStateChanged>().Publish(false);
                    Growl.Error("Ошибка при подключении к серверу");
                }
            }
            catch (Exception e)
            {
                Growl.Error("Ошибка при подключении к серверу");
                _eventAggregator.GetEvent<OnLogicControllerConnectionStateChanged>().Publish(false);
            }
            _connecting = false;
        }

        public async Task DisconnectAsync()
        {
            if (_client != null)
            { 
                _client.Disconnect();
                //Connected = false;
                _eventAggregator.GetEvent<OnLogicControllerConnectionStateChanged>().Publish(false);
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

                if (PingObserver.PlcConnectionObserver.Result.Success && !_connecting)
                {
                    await ConnectAsync();
                }

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

        public async Task<bool> GetBoolAsync(CommandInfo info)
        {
            if (_client == null || !Connected)
            {
                return false;
            }
            return await _client?.GetAsync<bool>(info);
        }

        public async Task SetBoolAsync(CommandInfo info, bool value)
        {
            if (_client == null || !Connected)
            {
                return;
            }
            await _client?.SetAsync(info, value);
        }

        public async Task<float> GetFloatAsync(CommandInfo info)
        {
            if (_client == null || !Connected)
            {
                return 0;
            }
            return await _client?.GetAsync<float>(info);
        }

        public async Task SetFloatAsync(CommandInfo info, float value)
        {
            if (_client == null || !Connected)
            {
                return;
            }
            await _client?.SetAsync(info, value);
        }

        public async Task<double> GetDoubleAsync(CommandInfo info)
        {
            if (_client == null || !Connected)
            {
                return 0;
            }
            return await _client?.GetAsync<double>(info);
        }

        public async Task SetDoubleAsync(CommandInfo info, double value)
        {
            if (_client == null || !Connected)
            {
                return;
            }
            await _client?.SetAsync(info, value);
        }

        public async Task<int> GetInt32Async(CommandInfo info)
        {
            if (_client == null || !Connected)
            {
                return 0;
            }
            return await _client?.GetAsync<int>(info);
        }

        public async Task SetInt32Async(CommandInfo info, int value)
        {
            if (_client == null || !Connected)
            {
                return;
            }
            await _client?.SetAsync(info, value);
        }

        public async Task<short> GetInt16Async(CommandInfo info)
        {
            if (_client == null || !Connected)
            {
                return 0;
            }
            return await _client?.GetAsync<short>(info);
        }

        public async Task SetInt16Async(CommandInfo info, short value)
        {
            if (_client == null || !Connected)
            {
                return;
            }
            await _client?.SetAsync(info, value);
        }

        public async Task<uint> GetUInt32Async(CommandInfo info)
        {
            if (_client == null || !Connected)
            {
                return 0;
            }
            return await _client?.GetAsync<uint>(info);
        }

        public async Task SetUInt32Async(CommandInfo info, uint value)
        {
            if (_client == null || !Connected)
            {
                return;
            }
            await _client?.SetAsync(info, value);
        }

        public async Task<ushort> GetUInt16Async(CommandInfo info)
        {
            if (_client == null || !Connected)
            {
                return 0;
            }
            return await _client?.GetAsync<ushort>(info);
        }

        public async Task SetUInt16Async(CommandInfo info, ushort value)
        {
            if (_client == null || !Connected)
            {
                return;
            }
            await _client?.SetAsync(info, value);
        }
    }
}
