# MongoDB Setup для LoggingService

Проект успешно мигрирован с SQLite на MongoDB. Этот документ содержит инструкции по установке и настройке MongoDB.

## Установка MongoDB

### Windows

#### Вариант 1: Установка MongoDB Community Server

1. Скачайте MongoDB Community Server с официального сайта:
   https://www.mongodb.com/try/download/community

2. Запустите установщик и следуйте инструкциям:
   - Выберите "Complete" установку
   - Установите флажок "Install MongoDB as a Service"
   - Data Directory: `C:\Program Files\MongoDB\Server\8.0\data`
   - Log Directory: `C:\Program Files\MongoDB\Server\8.0\log`

3. После установки MongoDB будет запущен как Windows Service автоматически

4. Проверьте, что MongoDB запущен:
   ```bash
   # Откройте PowerShell или CMD
   mongosh
   ```

#### Вариант 2: Использование Docker (рекомендуется для разработки)

1. Установите Docker Desktop для Windows:
   https://www.docker.com/products/docker-desktop

2. Запустите MongoDB контейнер:
   ```bash
   docker run -d -p 27017:27017 --name mongodb mongo:latest
   ```

3. Проверьте статус контейнера:
   ```bash
   docker ps
   ```

4. Для остановки контейнера:
   ```bash
   docker stop mongodb
   ```

5. Для запуска существующего контейнера:
   ```bash
   docker start mongodb
   ```

### Вариант 3: MongoDB Atlas (облачная версия)

1. Создайте бесплатный аккаунт на https://www.mongodb.com/cloud/atlas

2. Создайте новый кластер (выберите Free Tier - M0)

3. Настройте доступ:
   - Добавьте IP адрес в whitelist (или 0.0.0.0/0 для доступа отовсюду)
   - Создайте пользователя базы данных

4. Получите connection string (например: `mongodb+srv://username:password@cluster.mongodb.net/`)

5. Обновите `appsettings.json` с вашим connection string

## Настройка приложения

### appsettings.json

```json
{
  "MongoDbConnectionString": "mongodb://localhost:27017",
  "MongoDbDatabaseName": "LoggingService",
  "LogDirectory": "Logs"
}
```

### Параметры конфигурации

- **MongoDbConnectionString**: Строка подключения к MongoDB
  - Локальный: `mongodb://localhost:27017`
  - С аутентификацией: `mongodb://username:password@localhost:27017`
  - Atlas: `mongodb+srv://username:password@cluster.mongodb.net/`

- **MongoDbDatabaseName**: Имя базы данных (по умолчанию: "LoggingService")

- **LogDirectory**: Директория для текстовых файлов логов (по умолчанию: "Logs")

## Управление MongoDB

### Через mongosh (MongoDB Shell)

```bash
# Подключиться к MongoDB
mongosh

# Показать все базы данных
show dbs

# Переключиться на базу LoggingService
use LoggingService

# Показать все коллекции
show collections

# Посмотреть последние 10 логов
db.logs.find().sort({timestamp: -1}).limit(10)

# Посмотреть количество документов
db.logs.countDocuments()

# Посмотреть логи за последний час
db.logs.find({
  timestamp: {
    $gte: new Date(Date.now() - 3600000)
  }
})

# Посмотреть логи определенного приложения
db.logs.find({application: "PrintMate.Terminal"})

# Удалить старые логи (старше 30 дней)
db.logs.deleteMany({
  timestamp: {
    $lt: new Date(Date.now() - 30 * 24 * 3600000)
  }
})
```

### Через MongoDB Compass (GUI инструмент)

1. Скачайте MongoDB Compass: https://www.mongodb.com/products/compass

2. Подключитесь к `mongodb://localhost:27017`

3. Используйте графический интерфейс для просмотра и управления данными

## Индексы

Приложение автоматически создает следующие индексы при первом запуске:

1. **Timestamp** (по убыванию) - для быстрой сортировки
2. **Level** - для фильтрации по уровню логирования
3. **Application** - для фильтрации по приложению
4. **Application + Timestamp** (составной) - для частых запросов
5. **Text index** (Message + Category) - для полнотекстового поиска

## Резервное копирование

### Создание бэкапа

```bash
# Бэкап всей базы данных
mongodump --db LoggingService --out ./backup

# Бэкап с сжатием
mongodump --db LoggingService --archive=./backup.archive --gzip
```

### Восстановление из бэкапа

```bash
# Восстановление из директории
mongorestore --db LoggingService ./backup/LoggingService

# Восстановление из архива
mongorestore --db LoggingService --archive=./backup.archive --gzip
```

## Миграция данных из SQLite (опционально)

Если у вас есть существующие данные в SQLite (logs.db), вы можете их мигрировать:

```bash
# Старый файл сохранен как Data/LogDbContext.cs.old для справки
# Данные в logs.db можно экспортировать в JSON и импортировать в MongoDB
```

## Производительность

MongoDB значительно быстрее SQLite для операций чтения/записи логов:

- ✅ Нативная поддержка JSON документов (не нужна сериализация Properties)
- ✅ Эффективные индексы для быстрого поиска
- ✅ Поддержка полнотекстового поиска
- ✅ Горизонтальное масштабирование (sharding)
- ✅ Репликация для высокой доступности

## Устранение неполадок

### MongoDB не запускается

1. Проверьте, что порт 27017 не занят другим приложением:
   ```bash
   netstat -ano | findstr :27017
   ```

2. Проверьте логи MongoDB:
   - Windows Service: `C:\Program Files\MongoDB\Server\8.0\log\mongod.log`
   - Docker: `docker logs mongodb`

### Ошибка подключения

1. Убедитесь, что MongoDB запущен:
   ```bash
   # Windows Service
   sc query MongoDB

   # Docker
   docker ps | findstr mongodb
   ```

2. Проверьте connection string в appsettings.json

3. Проверьте firewall и security группы (для Atlas)

## Запуск приложения

```bash
# Запустите LoggingService
dotnet run --project LoggingService.csproj http://localhost:5300

# Или через опубликованный exe
LoggingService.exe http://localhost:5300
```

## API Endpoints

Приложение предоставляет следующие endpoints:

- `POST /api/logs` - Запись одного лога
- `POST /api/logs/batch` - Запись пакета логов
- `POST /api/logs/query` - Поиск логов с фильтрацией

Все endpoints работают так же, как и с SQLite - миграция прозрачна для клиентов.
