using MongoDB.Driver;
using LoggingService.Shared.Models;

namespace LoggingService.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoClient _client;

        public MongoDbContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("MongoDbConnectionString")
                ?? "mongodb://localhost:27017";

            var databaseName = configuration.GetValue<string>("MongoDbDatabaseName")
                ?? "LoggingService";

            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(databaseName);

            // Создаем индексы при инициализации
            CreateIndexes();
        }

        public IMongoCollection<LogEntryDocument> Logs => _database.GetCollection<LogEntryDocument>("logs");

        private void CreateIndexes()
        {
            var logsCollection = Logs;

            // Индекс по времени (для сортировки и диапазонов)
            var timestampIndexModel = new CreateIndexModel<LogEntryDocument>(
                Builders<LogEntryDocument>.IndexKeys.Descending(x => x.Timestamp)
            );

            // Индекс по уровню логирования
            var levelIndexModel = new CreateIndexModel<LogEntryDocument>(
                Builders<LogEntryDocument>.IndexKeys.Ascending(x => x.Level)
            );

            // Индекс по приложению
            var applicationIndexModel = new CreateIndexModel<LogEntryDocument>(
                Builders<LogEntryDocument>.IndexKeys.Ascending(x => x.Application)
            );

            // Составной индекс по приложению и времени (для частых запросов)
            var compoundIndexModel = new CreateIndexModel<LogEntryDocument>(
                Builders<LogEntryDocument>.IndexKeys
                    .Ascending(x => x.Application)
                    .Descending(x => x.Timestamp)
            );

            // Текстовый индекс для полнотекстового поиска
            var textIndexModel = new CreateIndexModel<LogEntryDocument>(
                Builders<LogEntryDocument>.IndexKeys
                    .Text(x => x.Message)
                    .Text(x => x.Category)
            );

            try
            {
                logsCollection.Indexes.CreateMany(new[]
                {
                    timestampIndexModel,
                    levelIndexModel,
                    applicationIndexModel,
                    compoundIndexModel,
                    textIndexModel
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to create indexes: {ex.Message}");
            }
        }
    }
}
