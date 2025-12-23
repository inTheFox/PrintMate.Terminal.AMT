using Newtonsoft.Json;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Hans.NET.Models;

namespace HansScannerHost;

/// <summary>
/// Сервер для приема команд от основного приложения через Named Pipe
/// Команды: Download, StartMark, StopMark, GetStatus
/// </summary>
public class InvokePipeServer
{
    private bool _isRunning;
    private const int BUFFER_SIZE = 65536; // 64KB
    private const int MAX_MESSAGE_SIZE = 10 * 1024 * 1024; // 10MB
    private readonly string _pipeName;

    private NamedPipeServerStream? _pipeServer;
    private readonly HiddenMessageForm _form;

    public InvokePipeServer(HiddenMessageForm form, string pipeId)
    {
        _form = form ?? throw new ArgumentNullException(nameof(form));
        _pipeName = $"PrintMate.Hans.Commands.{pipeId}";
        Console.WriteLine($"[InvokePipeServer] Creating server with pipe: {_pipeName}");
        Task.Factory.StartNew(StartAsync, TaskCreationOptions.LongRunning);
    }

    private async Task StartAsync()
    {
        _isRunning = true;
        Console.WriteLine($"[InvokePipeServer] Starting on pipe: {_pipeName}");

        while (_isRunning)
        {
            try
            {
                _pipeServer = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous,
                    BUFFER_SIZE,
                    BUFFER_SIZE);

                Console.WriteLine($"[InvokePipeServer] Waiting for client connection on: {_pipeName}");

                await _pipeServer.WaitForConnectionAsync();
                Console.WriteLine($"[InvokePipeServer] ✓ Client connected to: {_pipeName}");

                await HandleClientAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InvokePipeServer error: {ex.Message}");
                await Task.Delay(1000); // Wait before retry
            }
            finally
            {
                _pipeServer?.Dispose();
                _pipeServer = null;
            }
        }
    }

    private async Task HandleClientAsync()
    {
        // Используем StreamReader/StreamWriter для текстового протокола
        using var reader = new StreamReader(_pipeServer!, Encoding.UTF8, leaveOpen: false);
        using var writer = new StreamWriter(_pipeServer!, Encoding.UTF8, bufferSize: 64000, leaveOpen: false)
        {
            AutoFlush = true
        };

        while (_pipeServer!.IsConnected)
        {
            try
            {
                var message = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(message))
                    continue;

                //Console.WriteLine($"Command received: {message}");
                var response = ProcessMessage(message);
                //Console.WriteLine($"Response: {response}");
                await writer.WriteLineAsync(response);
            }
            catch (IOException) when (!_pipeServer.IsConnected)
            {
                Console.WriteLine("Commands pipe disconnected");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing command: {ex.Message}");
                try
                {
                    var errorResponse = JsonConvert.SerializeObject(new { success = false, error = ex.Message });
                    await writer.WriteLineAsync(errorResponse);
                }
                catch
                {
                    // Ignore if can't send error response
                }
            }
        }
    }

    private string ProcessMessage(string jsonMessage)
    {
        try
        {
            // Десериализуем HansRequest
            var request = JsonConvert.DeserializeObject<HansRequest>(jsonMessage);
            if (request == null)
            {
                return JsonConvert.SerializeObject(HansResponse.Error(
                    Guid.Empty,
                    "Failed to deserialize HansRequest"));
            }

            // Обрабатываем команду
            HansResponse response = request.Command switch
            {
                HansCommandType.Ping => HansResponse.Ok(request.RequestId, data: "Pong"),
                HansCommandType.Connect => HandleConnect(request),
                HansCommandType.Disconnect => HandleDisconnect(request),
                HansCommandType.Configure => HandleConfigure(request),
                HansCommandType.DownloadMarkFile => HandleDownload(request),
                HansCommandType.StartMark => HandleStartMark(request),
                HansCommandType.StopMark => HandleStopMark(request),
                HansCommandType.GetStatus => HandleGetStatus(request),
                HansCommandType.GetConnectStatus => HandleGetConnectStatus(request),
                HansCommandType.Shutdown => HandleShutdown(request),
                HansCommandType.GetMarkingState => HandleGetMarkingState(),
                HansCommandType.IsDownloadMarkFileFinish => IsDownloadMarkFileFinish(),

                _ => HansResponse.Error(request.RequestId, $"Unknown command: {request.Command}")
            };

            return JsonConvert.SerializeObject(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return JsonConvert.SerializeObject(HansResponse.Error(
                Guid.Empty,
                ex.Message,
                ex.StackTrace));
        }
    }

    /// <summary>
    /// Обработка команды загрузки UDM файла
    /// </summary>
    private HansResponse HandleDownload(HansRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Payload))
            {
                return HansResponse.Error(request.RequestId, "Payload is required for DownloadMarkFile command");
            }

            var downloadParams = JsonConvert.DeserializeObject<DownloadMarkFileParams>(request.Payload);
            if (downloadParams == null || string.IsNullOrEmpty(downloadParams.UdmFilePath))
            {
                return HansResponse.Error(request.RequestId, "UdmFilePath parameter is required");
            }

            bool result = _form.DownloadMarkFile(downloadParams.UdmFilePath);

            return result
                ? HansResponse.Ok(request.RequestId, "Download initiated")
                : HansResponse.Error(request.RequestId, "Download failed");
        }
        catch (Exception ex)
        {
            return HansResponse.Error(request.RequestId, ex.Message, ex.StackTrace);
        }
    }

    /// <summary>
    /// Обработка команды начала маркировки
    /// </summary>
    private HansResponse HandleStartMark(HansRequest request)
    {
        try
        {
            bool result = _form.StartMark();

            return result
                ? HansResponse.Ok(request.RequestId, "Mark started")
                : HansResponse.Error(request.RequestId, "Failed to start mark");
        }
        catch (Exception ex)
        {
            return HansResponse.Error(request.RequestId, ex.Message, ex.StackTrace);
        }
    }

    /// <summary>
    /// Обработка команды остановки маркировки
    /// </summary>
    private HansResponse HandleStopMark(HansRequest request)
    {
        try
        {
            bool result = _form.StopMark();

            return result
                ? HansResponse.Ok(request.RequestId, "Mark stopped")
                : HansResponse.Error(request.RequestId, "Failed to stop mark");
        }
        catch (Exception ex)
        {
            return HansResponse.Error(request.RequestId, ex.Message, ex.StackTrace);
        }
    }

    /// <summary>
    /// Обработка команды получения статуса
    /// </summary>
    private HansResponse HandleGetStatus(HansRequest request)
    {
        try
        {
            var statusData = new ScanatorStatus
            {
                IsConnected = _form.IsConnected,
                IsMarking = _form.IsMarking,
                LastError = null
            };

            return HansResponse.Ok(request.RequestId, data: JsonConvert.SerializeObject(statusData));
        }
        catch (Exception ex)
        {
            return HansResponse.Error(request.RequestId, ex.Message, ex.StackTrace);
        }
    }

    private HansResponse HandleConnect(HansRequest request)
    {
        // В текущей реализации Connect происходит автоматически при создании HiddenMessageForm
        // Здесь можно добавить дополнительную логику, если нужно
        return HansResponse.Ok(request.RequestId, "Already connected");
    }

    private HansResponse HandleDisconnect(HansRequest request)
    {
        return HansResponse.Ok(request.RequestId, "Disconnected");
    }

    private HansResponse HandleConfigure(HansRequest request)
    {
        // Здесь можно добавить логику конфигурации, если нужно
        return HansResponse.Ok(request.RequestId, "Configured");
    }

    private HansResponse HandleGetConnectStatus(HansRequest request)
    {
        try
        {
            var connectStatus = _form.GetConnectStatus();
            return HansResponse.Ok(request.RequestId, data: connectStatus.ToString());
        }
        catch (Exception ex)
        {
            return HansResponse.Error(request.RequestId, ex.Message, ex.StackTrace);
        }
    }

    private HansResponse HandleShutdown(HansRequest request)
    {
        Task.Run(() =>
        {
            Thread.Sleep(100); // Даем время отправить ответ
            Environment.Exit(0);
        });
        return HansResponse.Ok(request.RequestId, "Shutting down");
    }

    public HansResponse HandleGetMarkingState()
    {
        return new HansResponse { Success = true, Data = _form.GetMarkingState().ToString() };
    }

    public HansResponse IsDownloadMarkFileFinish()
    {
        return new HansResponse { Success = true, Data = _form.IsDownloadMarkFileFinish.ToString() };
    }

    public void Stop()
    {
        _isRunning = false;
        _pipeServer?.Dispose();
    }
}