using HansScannerHost.Models;
using Hans.NET.Models;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;

namespace HansScannerHost;

/// <summary>
/// Сервер для отправки событий от Hans SDK в основное приложение через Named Pipe
/// События: Connected, Disconnected, DownloadProgress, DownloadCompleted, MarkProgress, MarkCompleted
/// </summary>
public class EventsPipeServer
{
    private const int BUFFER_SIZE = 65536; // 64KB
    private readonly string _pipeName;

    private NamedPipeServerStream? _eventsServer;
    private readonly ConcurrentQueue<HansEvent> _eventsQueue = new();
    private bool _isRunning;
    private StreamWriter? _writer;

    public EventsPipeServer(string pipeId)
    {
        _pipeName = $"PrintMate.Hans.Events.{pipeId}";
        Console.WriteLine($"[EventsPipeServer] Creating server with pipe: {_pipeName}");
        Task.Factory.StartNew(StartAsync, TaskCreationOptions.LongRunning);
    }

    private async Task StartAsync()
    {
        _isRunning = true;
        Console.WriteLine($"Starting EventsPipeServer on pipe: {_pipeName}");

        while (_isRunning)
        {
            try
            {
                _eventsServer = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous,
                    BUFFER_SIZE,
                    BUFFER_SIZE);

                Console.WriteLine($"[EventsPipeServer] Waiting for client connection on: {_pipeName}");

                await _eventsServer.WaitForConnectionAsync();
                Console.WriteLine($"[EventsPipeServer] ✓ Client connected to: {_pipeName}");

                await HandleClientEventsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EventsPipeServer error: {ex.Message}");
                await Task.Delay(1000); // Wait before retry
            }
            finally
            {
                _writer?.Dispose();
                _writer = null;
                _eventsServer?.Dispose();
                _eventsServer = null;
            }
        }
    }

    private async Task HandleClientEventsAsync()
    {
        _writer = new StreamWriter(_eventsServer!, Encoding.UTF8, bufferSize: 64000, leaveOpen: false)
        {
            AutoFlush = true // Важно для немедленной отправки
        };

        Console.WriteLine($"[EventsPipeServer] Event pipe client connected, ready to send events");

        // Основной цикл отправки событий
        while (_eventsServer!.IsConnected)
        {
            try
            {
                if (_eventsQueue.TryDequeue(out var message))
                {
                    await SendEventInternalAsync(message);
                }
                else
                {
                    await Task.Delay(10); // Снижаем нагрузку на CPU
                }
            }
            catch (IOException) when (!_eventsServer.IsConnected)
            {
                Console.WriteLine("Events pipe disconnected");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending event: {ex.Message}");
                break;
            }
        }
    }

    private async Task SendEventInternalAsync(HansEvent hansEvent)
    {
        if (_writer == null || !_eventsServer!.IsConnected)
            return;

        try
        {
            string json = JsonConvert.SerializeObject(hansEvent);
            await _writer.WriteLineAsync(json);
            Console.WriteLine($"Event sent: {hansEvent.EventType}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send event {hansEvent.EventType}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Отправить событие в основное приложение
    /// </summary>
    public void SendEvent(HansEventType eventType, string? data = null)
    {
        _eventsQueue.Enqueue(new HansEvent
        {
            EventType = eventType,
            Data = data
        });
    }

    /// <summary>
    /// Отправить событие в основное приложение (устаревший метод для обратной совместимости)
    /// </summary>
    [Obsolete("Use SendEvent(HansEventType, string) instead")]
    public void SendEventMessage(string eventName, params object[] args)
    {
        // Преобразуем старые имена событий в новый формат
        HansEventType eventType = eventName switch
        {
            "DeviceReady" => HansEventType.DeviceStatusChanged,
            "Connected" => HansEventType.BoardConnected,
            "Disconnected" => HansEventType.BoardDisconnected,
            "DownloadProgress" => HansEventType.MarkingProgress,
            "DownloadCompleted" => HansEventType.DownloadCompleted,
            "MarkProgress" => HansEventType.MarkingProgress,
            "MarkCompleted" => HansEventType.MarkingCompleted,
            _ => HansEventType.DeviceStatusChanged
        };

        SendEvent(eventType, JsonConvert.SerializeObject(args));
    }

    public void Stop()
    {
        _isRunning = false;
        _writer?.Dispose();
        _eventsServer?.Dispose();
    }
}