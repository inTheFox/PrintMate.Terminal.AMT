using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeLib
{


    public class PipeClient
    {
        private bool _isRunning;
        private readonly string _serverName;
        private readonly string _pipeName;
        private const int BUFFER_SIZE = 65536;
        private const int CONNECT_TIMEOUT = 5000; // 5 секунд

        private NamedPipeClientStream _pipeClient;

        public event Func<string, Task> HandleEventMessage;

        public PipeClient(string pipeName, string serverName = ".")
        {
            _pipeName = pipeName;
            _serverName = serverName;
            Task.Factory.StartNew(ConnectAsync);
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _pipeClient = new NamedPipeClientStream(
                    _serverName,
                    _pipeName,
                    PipeDirection.InOut);

                await _pipeClient.ConnectAsync(CONNECT_TIMEOUT);

                if (_pipeClient.IsConnected)
                {
                    Console.WriteLine("Подключение к основному серверу установлено");
                    Task.Factory.StartNew(StartServerReader, TaskCreationOptions.LongRunning);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения: {ex.Message}");
                await Task.Delay(1000);
                Task.Factory.StartNew(ConnectAsync);

                return false;
            }
        }

        private async Task StartServerReader()
        {
            var buffer = new byte[1024];
            var encoding = Encoding.UTF8;
            StreamReader reader = new StreamReader(_pipeClient);

            try
            {
                while (_pipeClient.IsConnected)
                {
                    string message = await reader.ReadLineAsync();
                    HandleEventMessage?.Invoke(message);
                    Console.WriteLine(message);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Ошибка ввода-вывода: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                _pipeClient?.Dispose();
            }

            // Попробуем переподключиться
            await Task.Delay(1000);
            await ReconnectAsync();
        }

        private async Task ReconnectAsync()
        {
            try
            {
                _pipeClient = new NamedPipeClientStream(".", "PrlPipe.Callbacks", PipeDirection.InOut);
                await _pipeClient.ConnectAsync(CONNECT_TIMEOUT);
                Console.WriteLine("Переподключение к серверу событий успешно");
                Task.Factory.StartNew(StartServerReader, TaskCreationOptions.LongRunning);
            }
            catch
            {
                Console.WriteLine("Не удалось переподключиться к событиям. Повтор...");
                await Task.Delay(2000);
                await ReconnectAsync();
            }
        }

    }
}
