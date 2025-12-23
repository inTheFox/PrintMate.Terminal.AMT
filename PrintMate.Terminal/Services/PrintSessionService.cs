using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PrintMate.Terminal.Database;
using PrintMate.Terminal.Events;
using PrintSpectator.Shared.Enums;
using PrintSpectator.Shared.Models;
using Prism.Events;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Services
{
    /// <summary>
    /// Сервис управления сессиями печати.
    /// Отслеживает состояние печати и сохраняет прогресс в базу данных.
    /// Позволяет обнаружить некорректно завершённые сессии при запуске приложения.
    /// </summary>
    public class PrintSessionService
    {
        private readonly DatabaseContext _db;
        private readonly IEventAggregator _eventAggregator;

        /// <summary>
        /// Текущая активная сессия печати (null если печать не идёт)
        /// </summary>
        public PrintSession CurrentSession { get; internal set; }

        /// <summary>
        /// Текущее состояние слоя
        /// </summary>
        public LayerState CurrentLayerState { get; private set; }

        public PrintSessionService(DatabaseContext db, IEventAggregator eventAggregator)
        {
            _db = db;
            _eventAggregator = eventAggregator;
        }

        #region Session Management

        /// <summary>
        /// Создаёт новую сессию печати при запуске проекта
        /// </summary>
        public async Task<PrintSession> StartSessionAsync(Project project, int? userId = null, string userName = null)
        {
            var session = new PrintSession
            {
                Id = Guid.NewGuid(),
                ProjectInfoId = project.ProjectInfo?.Id ?? 0,
                ProjectName = project.ProjectInfo?.Name ?? "Unknown",
                StartedAt = DateTime.Now,
                Status = ProjectStatus.Started,
                TotalLayers = project.Layers?.Count ?? 0,
                LastCompletedLayer = -1,
                UserId = userId,
                UserName = userName ?? "Operator"
            };

            _db.PrintSessions.Add(session);
            await _db.SaveChangesAsync();

            CurrentSession = session;
            Console.WriteLine($"[PrintSessionService] Session started: {session.Id}, Project: {session.ProjectName}");

            return session;
        }

        /// <summary>
        /// Завершает сессию печати успешно
        /// </summary>
        public async Task FinishSessionAsync()
        {
            if (CurrentSession == null) return;

            CurrentSession.Status = ProjectStatus.Finished;
            CurrentSession.FinishedAt = DateTime.Now;

            _db.PrintSessions.Update(CurrentSession);
            await _db.SaveChangesAsync();

            Console.WriteLine($"[PrintSessionService] Session finished: {CurrentSession.Id}");
            CurrentSession = null;
            CurrentLayerState = null;
        }

        /// <summary>
        /// Останавливает сессию печати (оператор нажал Стоп)
        /// </summary>
        public async Task StopSessionAsync()
        {
            if (CurrentSession == null) return;

            CurrentSession.Status = ProjectStatus.Stopped;
            CurrentSession.FinishedAt = DateTime.Now;

            _db.PrintSessions.Update(CurrentSession);
            await _db.SaveChangesAsync();

            Console.WriteLine($"[PrintSessionService] Session stopped by operator: {CurrentSession.Id}");
            CurrentSession = null;
            CurrentLayerState = null;
        }

        /// <summary>
        /// Проверяет наличие незавершённых сессий при запуске приложения
        /// </summary>
        public async Task<PrintSession> GetUnfinishedSessionAsync()
        {
            return await _db.PrintSessions
                .Include(s => s.LayerStates)
                .Where(s => s.Status == ProjectStatus.Started)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync(p=>p.ShowOn == false);
        }

        public async Task ShowOn(PrintSession session)
        {
            session.ShowOn = true;
            _db.PrintSessions.Update(session);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Помечает незавершённую сессию как прерванную (аварийное завершение)
        /// </summary>
        public async Task MarkSessionAsInterruptedAsync(PrintSession session)
        {
            if (session == null) return;

            // Статус остаётся Started, но добавляем информацию что это было аварийное завершение
            // Это позволит показать пользователю информацию о прерванной печати
            Console.WriteLine($"[PrintSessionService] Found interrupted session: {session.Id}, " +
                              $"Project: {session.ProjectName}, " +
                              $"Last layer: {session.LastCompletedLayer}/{session.TotalLayers}");
        }

        #endregion

        #region Layer State Tracking

        /// <summary>
        /// Начинает отслеживание нового слоя
        /// </summary>
        public async Task<LayerState> StartLayerAsync(int layerNumber)
        {
            if (CurrentSession == null)
            {
                Console.WriteLine("[PrintSessionService] Warning: No active session for layer tracking");
                return null;
            }

            var layerState = new LayerState
            {
                Id = Guid.NewGuid(),
                SessionId = CurrentSession.Id,
                LayerNumber = layerNumber,
                Status = LayerStatus.Started,
                StartedAt = DateTime.Now,
                IsPlatformDown = false,
                IsPowderApplied = false,
                IsMarkingStarted = false,
                IsMarkingFinished = false
            };

            _db.LayersStates.Add(layerState);
            await _db.SaveChangesAsync();

            CurrentLayerState = layerState;
            Console.WriteLine($"[PrintSessionService] Layer {layerNumber} started");

            return layerState;
        }

        /// <summary>
        /// Обновляет состояние текущего слоя: платформа опущена
        /// </summary>
        public async Task UpdateLayerPlatformDownAsync()
        {
            if (CurrentLayerState == null) return;

            CurrentLayerState.IsPlatformDown = true;
            _db.LayersStates.Update(CurrentLayerState);
            await _db.SaveChangesAsync();

            Console.WriteLine($"[PrintSessionService] Layer {CurrentLayerState.LayerNumber}: Platform down");
        }

        /// <summary>
        /// Обновляет состояние текущего слоя: порошок нанесён
        /// </summary>
        public async Task UpdateLayerPowderAppliedAsync()
        {
            if (CurrentLayerState == null) return;

            CurrentLayerState.IsPowderApplied = true;
            _db.LayersStates.Update(CurrentLayerState);
            await _db.SaveChangesAsync();

            Console.WriteLine($"[PrintSessionService] Layer {CurrentLayerState.LayerNumber}: Powder applied");
        }

        /// <summary>
        /// Обновляет состояние текущего слоя: сканирование начато
        /// </summary>
        public async Task UpdateLayerMarkingStartedAsync()
        {
            if (CurrentLayerState == null) return;

            CurrentLayerState.IsMarkingStarted = true;
            _db.LayersStates.Update(CurrentLayerState);
            await _db.SaveChangesAsync();

            Console.WriteLine($"[PrintSessionService] Layer {CurrentLayerState.LayerNumber}: Marking started");
        }

        /// <summary>
        /// Завершает отслеживание текущего слоя
        /// </summary>
        public async Task FinishLayerAsync()
        {
            if (CurrentLayerState == null) return;
            if (CurrentSession == null) return;

            CurrentLayerState.IsMarkingFinished = true;
            CurrentLayerState.Status = LayerStatus.Finished;
            CurrentLayerState.FinishedAt = DateTime.Now;

            // Обновляем номер последнего завершённого слоя в сессии
            CurrentSession.LastCompletedLayer = CurrentLayerState.LayerNumber;

            _db.LayersStates.Update(CurrentLayerState);
            _db.PrintSessions.Update(CurrentSession);
            await _db.SaveChangesAsync();

            Console.WriteLine($"[PrintSessionService] Layer {CurrentLayerState.LayerNumber} finished. " +
                              $"Progress: {CurrentSession.LastCompletedLayer + 1}/{CurrentSession.TotalLayers}");

            CurrentLayerState = null;
        }

        #endregion

        #region History

        /// <summary>
        /// Получает историю сессий печати
        /// </summary>
        public async Task<PrintSession[]> GetSessionHistoryAsync(int take = 50)
        {
            return await _db.PrintSessions
                .OrderByDescending(s => s.StartedAt)
                .Take(take)
                .ToArrayAsync();
        }

        /// <summary>
        /// Получает сессию по ID с состояниями слоёв
        /// </summary>
        public async Task<PrintSession> GetSessionWithLayersAsync(Guid sessionId)
        {
            return await _db.PrintSessions
                .Include(s => s.LayerStates)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        /// <summary>
        /// Получает все сессии для конкретного проекта
        /// </summary>
        /// <param name="projectInfoId">ID проекта (ProjectInfo.Id)</param>
        /// <param name="take">Максимальное количество записей</param>
        public async Task<PrintSession[]> GetSessionsByProjectAsync(int projectInfoId, int take = 50)
        {
            return await _db.PrintSessions
                .Where(s => s.ProjectInfoId == projectInfoId)
                .OrderByDescending(s => s.StartedAt)
                .Take(take)
                .ToArrayAsync();
        }

        /// <summary>
        /// Получает все сессии для проекта по имени (для случаев когда ID не задан)
        /// </summary>
        /// <param name="projectName">Название проекта</param>
        /// <param name="take">Максимальное количество записей</param>
        public async Task<PrintSession[]> GetSessionsByProjectNameAsync(string projectName, int take = 50)
        {
            return await _db.PrintSessions
                .Where(s => s.ProjectName == projectName)
                .OrderByDescending(s => s.StartedAt)
                .Take(take)
                .ToArrayAsync();
        }

        public async Task<LayerState?> GetLastLayerBySessionIdAndLayerNumber(PrintSession session, int layerNumber)
        {
            return await _db.LayersStates.FirstOrDefaultAsync(p =>
                p.SessionId == session.Id && p.LayerNumber == layerNumber);
        }

        #endregion
    }
}
