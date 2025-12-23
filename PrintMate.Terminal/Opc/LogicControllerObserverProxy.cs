using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Opc2Lib;
using Subscription = Opc.Ua.Client.Subscription;

namespace PrintMate.Terminal.Opc
{
    public class LogicControllerObserver : ILogicControllerObserver
    {
        private readonly ILogicControllerProvider _logicControllerProvider;
        private readonly List<Subscription> _subscriptions = new();
        private readonly object _lockSubscriptions = new();
        private readonly CancellationTokenSource _pollingCts = new();
        private List<CommandInfo> _ignoreCommands = new List<CommandInfo>();

        public LogicControllerObserver(ILogicControllerProvider logicControllerProvider)
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
                if (!_logicControllerProvider.Connected) continue;

                try
                {
                    CommandInfo[] commandsToRead;
                    lock (_lockSubscriptions)
                    {
                        // Получаем все уникальные CommandInfo из активных подписок
                        commandsToRead = _subscriptions
                            .SelectMany(s => s.Commands)
                            .Distinct()
                            .ToArray();
                    }

                    if (commandsToRead.Length == 0 || !_logicControllerProvider.Connected)
                        continue;


                    List<CommandResponse> results = new List<CommandResponse>();
                    foreach (var command in commandsToRead)
                    {
                        if (command == null) continue;
                        if (_ignoreCommands.FirstOrDefault(p=>p == command) != null) continue;

                        try
                        {

                            results.Add(new CommandResponse { Value = await _logicControllerProvider.GetAsync<object>(command), CommandInfo = command });
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
                                    // Вызываем callback в отдельной задаче, чтобы не блокировать цикл опроса
                                    _ = Task.Run(() => sub.Callback?.Invoke(resp));
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