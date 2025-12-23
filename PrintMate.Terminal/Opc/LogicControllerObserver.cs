using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Opc2Lib;
using PrintMate.Terminal.Services;

namespace PrintMate.Terminal.Opc
{

    public class LogicControllerObserverProxy : ILogicControllerObserver
    {
        private readonly ILogicControllerProvider _logicControllerProvider;
        private readonly List<Subscription> _subscriptions = new();
        private readonly object _lockSubscriptions = new();
        private readonly CancellationTokenSource _pollingCts = new();
        private List<CommandInfo> _ignoreCommands = new List<CommandInfo>();
        private Dictionary<CommandInfo, object> _cache = new Dictionary<CommandInfo, object>();

        public LogicControllerObserverProxy(ILogicControllerProvider logicControllerProvider)
        {
            _logicControllerProvider = logicControllerProvider;
            StartPolling();
        }

        public Subscription Subscribe(object parent, Action<CommandResponse> callback, params CommandInfo[] commands)
        {
            if (commands == null || commands.Length == 0)
                throw new ArgumentException("At least one command must be provided.", nameof(commands));

            var sub = new Subscription
            {
                Callback = callback ?? throw new ArgumentNullException(nameof(callback)),
                Commands = commands,
                Parent = parent,
            };

            lock (_lockSubscriptions)
            {
                _subscriptions.Add(sub);
            }

            return sub;
        }

        public void Unsubscribe(Subscription subscription)
        {
            if (subscription == null) return;

            lock (_lockSubscriptions)
            {
                _subscriptions.RemoveAll(s => s.Id == subscription.Id);
            }
        }

        private void StartPolling()
        {
            Task.Factory.StartNew(PollLoop);
        }

        private async Task PollLoop()
        {
            while (true)
            {
                await Task.Delay(100);
                if (!PingObserver.PlcConnectionObserver.Result.Success) continue;
                if (!_logicControllerProvider.Connected) continue;
                try
                {
                    List<CommandInfo> commandsToRead;
                    lock (_lockSubscriptions)
                    {
                        // Получаем все уникальные CommandInfo из активных подписок
                        commandsToRead = _subscriptions
                            .SelectMany(s => s.Commands)
                            .Distinct()
                            .ToList();
                    }

                    commandsToRead.RemoveAll(p => p == null);

                    if (commandsToRead.Count == 0 || !_logicControllerProvider.Connected)
                        continue;

                    List<CommandResponse> results = new List<CommandResponse>();

                    foreach (var command in commandsToRead)
                    {
                        if (command == null) continue;
                        if (_ignoreCommands.FirstOrDefault(p=>p == command) != null) continue;

                        try
                        {
                            switch (command.ValueCommandType)
                            {
                                case ValueCommandType.Bool:
                                    var responce = new CommandResponse
                                    {
                                        Value = await _logicControllerProvider.GetAsync<bool>(command),
                                        CommandInfo = command
                                    };
                                    if (responce.Value == null)
                                    {
                                        responce.Value = false;
                                    }
                                    results.Add(responce);
                                    break;
                                case ValueCommandType.Dint:
                                    var responceDint = new CommandResponse
                                    {
                                        Value = await _logicControllerProvider.GetAsync<int>(command),
                                        CommandInfo = command
                                    };
                                    if (responceDint.Value == null)
                                    {
                                        responceDint.Value = 0;
                                    }
                                    results.Add(responceDint);
                                    break;
                                case ValueCommandType.Real:
                                    var responceReal = new CommandResponse
                                    {
                                        Value = await _logicControllerProvider.GetAsync<int>(command),
                                        CommandInfo = command
                                    };
                                    if (responceReal.Value == null)
                                    {
                                        responceReal.Value = 0f;
                                    }
                                    results.Add(responceReal);
                                    break;
                                case ValueCommandType.Unsigned:
                                    var responceUnsigned = new CommandResponse
                                    {
                                        Value = await _logicControllerProvider.GetAsync<int>(command),
                                        CommandInfo = command
                                    };
                                    if (responceUnsigned.Value == null)
                                    {
                                        responceUnsigned.Value = (ushort)0;
                                    }
                                    results.Add(responceUnsigned);
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine($"Ошибка чтения переменной: {command?.Title}");
                            _ignoreCommands.Add(command);
                            continue;
                        }
                    }

                    // Создаём словарь для быстрого поиска по CommandInfo
                    var responseMap = results.ToDictionary(r => r.CommandInfo, r => r);

                    _subscriptions.RemoveAll(p => p == null);
                    // Рассылаем значения подписчикам
                    lock (_lockSubscriptions)
                    {
                        foreach (var sub in _subscriptions.ToList())
                        {
                            foreach (var cmd in sub.Commands)
                            {
                                if (cmd == null)
                                {
                                    continue;
                                }
                                if (responseMap.TryGetValue(cmd, out var resp))
                                {
                                    if (resp != null && resp.Value != null)
                                    {
                                        // Вызываем callback в отдельной задаче, чтобы не блокировать цикл опроса
                                        _ = Task.Run(() => sub.Callback?.Invoke(resp));
                                    }
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ожидаемый выход при отмене
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in LogicControllerObserver polling loop: {ex.Message}");
                    Console.WriteLine($"Error in LogicControllerObserver polling loop: {ex.StackTrace}");

                    // Можно добавить логирование или повторную попытку
                }
            }
        }

    }
}