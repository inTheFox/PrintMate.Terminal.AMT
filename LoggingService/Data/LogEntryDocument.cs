using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using LogLevel = LoggingService.Shared.Models.LogLevel;

namespace LoggingService.Data
{
    /// <summary>
    /// MongoDB документ для хранения записей логов
    /// </summary>
    public class LogEntryDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("sessionId")]
        [BsonRepresentation(BsonType.String)]
        public Guid SessionId { get; set; }

        [BsonElement("timestamp")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; }

        [BsonElement("level")]
        [BsonRepresentation(BsonType.String)]
        public LogLevel Level { get; set; }

        [BsonElement("application")]
        public string Application { get; set; } = string.Empty;

        [BsonElement("category")]
        public string Category { get; set; } = string.Empty;

        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;

        [BsonElement("exception")]
        [BsonIgnoreIfNull]
        public string? Exception { get; set; }

        [BsonElement("properties")]
        [BsonIgnoreIfNull]
        public Dictionary<string, object>? Properties { get; set; }
    }
}
