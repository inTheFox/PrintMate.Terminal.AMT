using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Parsers;
using PrintMate.Terminal.Parsers.CncParser;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;
using PrintSpectator.Shared.Models;
using Prism.Events;
using ProjectParserTest.Parsers.CliParser;
using ProjectParserTest.Parsers.Shared.Interfaces;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Services
{
    /// <summary>
    /// Сервис для обработки прерванных сессий печати и их возобновления
    /// </summary>
    public class PrintResumeHandler
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ModalService _modalService;
        private readonly PrintService _printService;
        private readonly ProjectManager _projectManager;
        private readonly PrintSessionService _printSessionService;

        public PrintResumeHandler(
            IEventAggregator eventAggregator,
            ModalService modalService,
            PrintService printService,
            ProjectManager projectManager,
            PrintSessionService printSessionService)
        {
            _eventAggregator = eventAggregator;
            _modalService = modalService;
            _printService = printService;
            _projectManager = projectManager;
            _printSessionService = printSessionService;

            // Подписываемся на событие обнаружения прерванной сессии
            _eventAggregator.GetEvent<OnInterruptedSessionDetectedEvent>()
                .Subscribe(OnInterruptedSessionDetected);

            Console.WriteLine("[PrintResumeHandler] Initialized and subscribed to interrupted session events");
        }

        /// <summary>
        /// Обработчик события обнаружения прерванной сессии
        /// </summary>
        private async void OnInterruptedSessionDetected(PrintSession session)
        {
            Console.WriteLine($"[PrintResumeHandler] Interrupted session detected: {session.ProjectName}");

            // Показываем модальное окно с выбором действия
            var modalResult = await _modalService.ShowAsync<ResumeSessionModalView, ResumeSessionModalViewModel>(
                modalId: "ResumeSessionModal",
                options: new Dictionary<string, object>
                {
                    { "Session", session }
                },
                showOverlay: true,
                closeOnBackgroundClick: false
            );

            if (modalResult == null || !modalResult.IsSuccess || modalResult.Result == null)
            {
                Console.WriteLine("[PrintResumeHandler] Modal was cancelled or closed");
                return;
            }

            switch (modalResult.Result.Result)
            {
                case ResumeSessionResult.Resume:
                    await HandleResumeAsync(session);
                    break;

                case ResumeSessionResult.StartNew:
                    await HandleStartNewAsync(session);
                    break;

                case ResumeSessionResult.Cancel:
                default:
                    Console.WriteLine("[PrintResumeHandler] User cancelled the action");
                    break;
            }
        }

        /// <summary>
        /// Возобновить печать с места остановки
        /// </summary>
        private async Task HandleResumeAsync(PrintSession session)
        {
            try
            {
                Console.WriteLine($"[PrintResumeHandler] Resuming print for session {session.Id}");

                // Загружаем проект из файловой системы
                var project = await LoadProjectFromSessionAsync(session);

                if (project == null)
                {
                    await CustomMessageBox.ShowErrorAsync(
                        "Ошибка загрузки",
                        $"Не удалось загрузить проект \"{session.ProjectName}\" для возобновления печати."
                    );
                    return;
                }

                // Возобновляем печать через PrintService
                await _printService.ResumePrintFromSession(session, project);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintResumeHandler] Error resuming print: {ex.Message}");
                await CustomMessageBox.ShowErrorAsync(
                    "Ошибка возобновления",
                    $"Не удалось возобновить печать:\n\n{ex.Message}"
                );
            }
        }

        /// <summary>
        /// Начать печать заново (создать новую сессию)
        /// </summary>
        private async Task HandleStartNewAsync(PrintSession session)
        {
            try
            {
                Console.WriteLine($"[PrintResumeHandler] Starting new print session for {session.ProjectName}");

                // Помечаем старую сессию как завершённую (остановлена оператором)
                await _printSessionService.StopSessionAsync();

                // Загружаем проект
                var project = await LoadProjectFromSessionAsync(session);

                if (project == null)
                {
                    await CustomMessageBox.ShowErrorAsync(
                        "Ошибка загрузки",
                        $"Не удалось загрузить проект \"{session.ProjectName}\"."
                    );
                    return;
                }

                // Устанавливаем как активный проект
                _printService.SetActiveProject(project);

                await CustomMessageBox.ShowInformationAsync(
                    "Проект готов",
                    $"Проект \"{session.ProjectName}\" готов к печати.\n\n" +
                    $"Перейдите в раздел \"Печать\" для запуска."
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintResumeHandler] Error starting new session: {ex.Message}");
                await CustomMessageBox.ShowErrorAsync(
                    "Ошибка",
                    $"Не удалось подготовить проект:\n\n{ex.Message}"
                );
            }
        }

        /// <summary>
        /// Загружает проект из файловой системы по информации из сессии
        /// </summary>
        private async Task<Project> LoadProjectFromSessionAsync(PrintSession session)
        {
            try
            {
                // Получаем информацию о проекте из ProjectManager
                var projectInfo = await _projectManager.GetProjectByIdAsync(session.ProjectInfoId);

                if (projectInfo == null)
                {
                    // Попытка найти по имени
                    projectInfo = await _projectManager.GetProjectByNameAsync(session.ProjectName);
                }

                if (projectInfo == null)
                {
                    Console.WriteLine($"[PrintResumeHandler] Project not found: Id={session.ProjectInfoId}, Name={session.ProjectName}");
                    return null;
                }

                // Парсим проект
                IParserProvider parser = DetermineParser(projectInfo.ManifestPath);
                var project = await parser.ParseAsync(projectInfo.ManifestPath);

                if (project != null)
                {
                    // Связываем с ProjectInfo
                    project.ProjectInfo = projectInfo;
                    projectInfo.ProjectLink = project;
                }

                return project;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PrintResumeHandler] Error loading project: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Определяет парсер на основе пути к файлу проекта
        /// </summary>
        private IParserProvider DetermineParser(string manifestPath)
        {
            if (manifestPath.EndsWith(".cli", StringComparison.OrdinalIgnoreCase))
            {
                return new CliProvider();
            }
            else
            {
                return new CncProvider();
            }
        }
    }
}
