using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PipeLib
{
    public class PipeServer
    {
        private bool _isRunning;
        private const int BUFFER_SIZE = 65536; // 64KB
        private const int MAX_MESSAGE_SIZE = 10 * 1024 * 1024; // 10MB

        private NamedPipeServerStream _pipeServer;
        public PipeServer()
        {
            Task.Factory.StartNew(() => StartAsync("PrintMate"));
        }

        public async Task StartAsync(string pipeName)
        {
            _isRunning = true;

            while (_isRunning)
            {
                try
                {
                    // Увеличиваем размер буфера
                    _pipeServer = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous,
                        BUFFER_SIZE,
                        BUFFER_SIZE);

                    Console.WriteLine("Ожидание подключения клиента...");

                    await _pipeServer.WaitForConnectionAsync();
                    Console.WriteLine("Клиент подключен");

                    await HandleClientAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
                finally
                {
                    _pipeServer?.Dispose();
                }
            }
        }

        private async Task HandleClientAsync()
        {
            while (_pipeServer.IsConnected)
            {
                try
                {
                    var message = await ReadMessageAsync();
                    if (string.IsNullOrEmpty(message))
                        continue;

                    Console.WriteLine($"Req: {message}");
                    var response = ProcessMessage(message);
                    Console.WriteLine($"Res: {response}");
                    await SendMessageAsync(response);
                }
                catch (Exception ex)
                {
                    var errorResponse = JsonConvert.SerializeObject(new { success = false, error = ex.Message });
                    await SendMessageAsync(errorResponse);
                }
            }
        }

        private async Task<string> ReadMessageAsync()
        {
            var memoryStream = new MemoryStream();
            var buffer = new byte[BUFFER_SIZE];
            int totalBytesRead = 0;

            // Читаем длину сообщения (первые 4 байта)
            var lengthBuffer = new byte[4];
            int lengthBytesRead = 0;

            while (lengthBytesRead < 4)
            {
                var bytesRead = await _pipeServer.ReadAsync(lengthBuffer, lengthBytesRead, 4 - lengthBytesRead);
                if (bytesRead == 0) return null;
                lengthBytesRead += bytesRead;
            }

            var messageLength = BitConverter.ToInt32(lengthBuffer, 0);

            if (messageLength > MAX_MESSAGE_SIZE || messageLength <= 0)
            {
                throw new InvalidOperationException("Некорректная длина сообщения");
            }

            // Читаем само сообщение
            while (totalBytesRead < messageLength)
            {
                var bytesToRead = Math.Min(buffer.Length, messageLength - totalBytesRead);
                var bytesRead = await _pipeServer.ReadAsync(buffer, 0, bytesToRead);
                if (bytesRead == 0) break;

                memoryStream.Write(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;
            }

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }

        private string ProcessMessage(string jsonMessage)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(jsonMessage);
                var root = jsonDoc.RootElement;

                if (!root.TryGetProperty("command", out var commandElement))
                    return JsonConvert.SerializeObject(new { success = false, error = "Команда не указана" });

                var command = commandElement.GetString();

                return command switch
                {
                    "Init" => Init(root),
                    _ => JsonConvert.SerializeObject(new { success = false, error = $"Неизвестная команда: {command}" })
                };
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        private string Init(JsonElement root)
        {
            // Пример обработки команды OpenDevice
            return JsonConvert.SerializeObject(new { success = true, message = "Устройство открыто" });
        }

        public async Task SendMessageAsync(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var lengthBytes = BitConverter.GetBytes(messageBytes.Length);

            // Отправляем длину сообщения
            await _pipeServer.WriteAsync(lengthBytes, 0, 4);
            // Отправляем само сообщение
            await _pipeServer.WriteAsync(messageBytes, 0, messageBytes.Length);
            await _pipeServer.FlushAsync();
        }
    }
}
