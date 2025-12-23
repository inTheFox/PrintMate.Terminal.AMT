using System;

namespace Hans.NET.Models
{
    /// <summary>
    /// Типы команд для Hans Scanner через Named Pipe
    /// </summary>
    public enum HansCommandType
    {
        Connect,
        Disconnect,
        Configure,
        DownloadMarkFile,
        StartMark,
        StopMark,
        GetStatus,
        Ping,
        Shutdown,
        GetConnectStatus,
        GetMarkingState,
        IsDownloadMarkFileFinish
    }

    public enum MarkingState
    {
        Stop,
        Marking,
        Finish
    }

    /// <summary>
    /// Запрос к Hans Scanner Host процессу
    /// </summary>
    public class HansRequest
    {
        public Guid RequestId { get; set; } = Guid.NewGuid();
        public HansCommandType Command { get; set; }
        public string? Payload { get; set; } // JSON сериализованные параметры

        public HansRequest() { }

        public HansRequest(HansCommandType command, string? payload = null)
        {
            Command = command;
            Payload = payload;
        }
    }

    /// <summary>
    /// Ответ от Hans Scanner Host процесса
    /// </summary>
    public class HansResponse
    {
        public Guid RequestId { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Data { get; set; } // JSON сериализованные результаты
        public string? ErrorDetails { get; set; }

        public static HansResponse Ok(Guid requestId, string? message = null, string? data = null)
        {
            return new HansResponse
            {
                RequestId = requestId,
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static HansResponse Error(Guid requestId, string message, string? errorDetails = null)
        {
            return new HansResponse
            {
                RequestId = requestId,
                Success = false,
                Message = message,
                ErrorDetails = errorDetails
            };
        }
    }

    /// <summary>
    /// Параметры для команды Connect
    /// </summary>
    public class ConnectParams
    {
        public string IpAddress { get; set; } = string.Empty;
        public int BoardIndex { get; set; }
    }

    /// <summary>
    /// Параметры для команды Configure
    /// </summary>
    public class ConfigureParams
    {
        /// <summary>
        /// JSON конфигурация сканатора (ScanatorConfiguration сериализованная)
        /// </summary>
        public string ConfigurationJson { get; set; } = string.Empty;
    }

    /// <summary>
    /// Параметры для команды DownloadMarkFile
    /// </summary>
    public class DownloadMarkFileParams
    {
        public string UdmFilePath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Статус сканатора
    /// </summary>
    public class ScanatorStatus
    {
        public bool IsConnected { get; set; }
        public bool IsMarking { get; set; }
        public string? LastError { get; set; }
        public bool IsMarkFinish { get; set; }
        public int WorkingStatus { get; set; }
    }

    /// <summary>
    /// Событие от Hans Scanner Host к клиенту
    /// Отправляется асинхронно (не требует запроса)
    /// </summary>
    public class HansEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public HansEventType EventType { get; set; }
        public string? Data { get; set; } // JSON с данными события
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Типы событий от Hans Scanner
    /// </summary>
    public enum HansEventType
    {
        /// <summary>
        /// Устройство подключено к Hans SDK
        /// </summary>
        BoardConnected,

        /// <summary>
        /// Устройство отключено от Hans SDK
        /// </summary>
        BoardDisconnected,

        /// <summary>
        /// Маркировка завершена
        /// </summary>
        MarkingCompleted,

        /// <summary>
        /// Произошла ошибка
        /// </summary>
        Error,

        /// <summary>
        /// Прогресс маркировки изменился
        /// </summary>
        MarkingProgress,

        /// <summary>
        /// Статус устройства изменился
        /// </summary>
        DeviceStatusChanged,

        /// <summary>
        /// Загрузка UDM файла завершена
        /// </summary>
        DownloadCompleted,
        BoardReady
    }

    /// <summary>
    /// Данные для события MarkingCompleted
    /// </summary>
    public class MarkingCompletedData
    {
        public bool Success { get; set; }
        public int TotalTimeMs { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Данные для события MarkingProgress
    /// </summary>
    public class MarkingProgressData
    {
        public int ProgressPercent { get; set; }
        public int CurrentLayer { get; set; }
        public int TotalLayers { get; set; }
    }
}
