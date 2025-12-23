using HandyControl.Controls;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Database;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Parsers;
using PrintMate.Terminal.Parsers.CncParser;
using PrintMate.Terminal.Parsers.Shared.Models;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views.Modals;
using PrintSpectator.Shared.Models;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using ProjectParserTest.Parsers.CliParser;
using ProjectParserTest.Parsers.Shared.Interfaces;
using ProjectParserTest.Parsers.Shared.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Point = System.Windows.Point;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels;

public class ProjectPreviewModalViewModel : BindableBase
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ProjectsRepository _projectsRepository;
    private readonly IRegionManager _regionManager;
    private readonly ProjectManager _projectManager;
    private readonly ModalService _modalService;
    private readonly PrintService _printService;
    private readonly PrintSessionService _printSessionService;



    private IParserProvider _parser;

    public string WindowId { get; set; }  // Устанавливается DialogService

    private ProjectInfo _projectInfo;
    public ProjectInfo ProjectInfo
    {
        get => _projectInfo;
        set => SetProperty(ref _projectInfo, value);
    }

    private Project _project;
    public Project Project
    {
        get => _project;
        set => SetProperty(ref _project, value);
    }

    private bool _isLoading = true;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _statusMessage = "Загрузка проекта...";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // Видимость кнопок
    private Visibility _okButtonVisibility = Visibility.Visible;
    public Visibility OkButtonVisibility
    {
        get => _okButtonVisibility;
        set => SetProperty(ref _okButtonVisibility, value);
    }

    private Visibility _noButtonVisibility = Visibility.Visible;
    public Visibility NoButtonVisibility
    {
        get => _noButtonVisibility;
        set => SetProperty(ref _noButtonVisibility, value);
    }

    private Visibility _cancelButtonVisibility = Visibility.Visible;
    public Visibility CancelButtonVisibility
    {
        get => _cancelButtonVisibility;
        set => SetProperty(ref _cancelButtonVisibility, value);
    }

    // Навигация по слоям
    private int _currentLayerIndex = 0;
    public int CurrentLayerIndex
    {
        get => _currentLayerIndex;
        set
        {
            if (SetProperty(ref _currentLayerIndex, value))
            {
                UpdateCurrentLayer();
                UpdateLayerInfo();
            }
        }
    }

    private int _maxLayerIndex = 0;
    public int MaxLayerIndex
    {
        get => _maxLayerIndex;
        set => SetProperty(ref _maxLayerIndex, value);
    }

    private string _currentLayerInfo = "Слой: 0 / 0";
    public string CurrentLayerInfo
    {
        get => _currentLayerInfo;
        set => SetProperty(ref _currentLayerInfo, value);
    }

    // Сессии печати проекта
    private ObservableCollection<PrintSession> _sessions = new();
    public ObservableCollection<PrintSession> Sessions
    {
        get => _sessions;
        set => SetProperty(ref _sessions, value);
    }

    private bool _hasNoSessions = true;
    public bool HasNoSessions
    {
        get => _hasNoSessions;
        set => SetProperty(ref _hasNoSessions, value);
    }

    // Команды
    public RelayCommand OkCommand { get; set; }
    public RelayCommand NoCommand { get; set; }
    public RelayCommand CancelCommand { get; set; }
    public RelayCommand PreviousLayerCommand { get; set; }
    public RelayCommand NextLayerCommand { get; set; }
    public RelayCommand<PrintSession> OnSessionClickedCommand { get; set; }

    public ProjectPreviewModalViewModel(
        IEventAggregator eventAggregator,
        ProjectsRepository projectsRepository,
        IRegionManager regionManager,
        ProjectManager projectManager,
        ModalService modalService,
        PrintService printService,
        PrintSessionService printSessionService
        )
    {
        _printService = printService;
        _modalService = modalService;
        _projectManager = projectManager;
        _eventAggregator = eventAggregator;
        _projectsRepository = projectsRepository;
        _regionManager = regionManager;
        _printSessionService = printSessionService;

        OkCommand = new RelayCommand(_ => OnStartPrint());
        NoCommand = new RelayCommand(_ => OnDeleteProject());
        CancelCommand = new RelayCommand(_ => OnCancel());
        PreviousLayerCommand = new RelayCommand(_ => PreviousLayer(), _ => CanGoPreviousLayer());
        NextLayerCommand = new RelayCommand(_ => NextLayer(), _ => CanGoNextLayer());
        OnSessionClickedCommand = new RelayCommand<PrintSession>(session => OnSessionClicked(session));
    }

    public void StartLoadingAsync()
    {
        if (ProjectInfo != null)
        {
            // Запускаем парсинг в отдельной задаче, чтобы не блокировать UI
            Task.Run(() => LoadProjectDetails());
            // Загружаем историю сессий для этого проекта
            Task.Run(() => LoadSessionsAsync());
        }
    }

    private async Task LoadSessionsAsync()
    {
        try
        {
            Console.WriteLine($"[ProjectPreviewModal] Loading sessions for project: Id={ProjectInfo.Id}, Name={ProjectInfo.Name}");

            PrintSession[] sessions = Array.Empty<PrintSession>();

            // Пробуем загрузить по ID проекта
            if (ProjectInfo.Id > 0)
            {
                Console.WriteLine($"[ProjectPreviewModal] Searching by ProjectInfo.Id = {ProjectInfo.Id}");
                sessions = await _printSessionService.GetSessionsByProjectAsync(ProjectInfo.Id);
            }

            // Если по ID ничего не нашли, пробуем по имени проекта
            if (sessions.Length == 0 && !string.IsNullOrEmpty(ProjectInfo.Name))
            {
                Console.WriteLine($"[ProjectPreviewModal] No sessions found by Id, searching by Name = {ProjectInfo.Name}");
                sessions = await _printSessionService.GetSessionsByProjectNameAsync(ProjectInfo.Name);
            }

            Console.WriteLine($"[ProjectPreviewModal] Found {sessions.Length} sessions");
            foreach (var s in sessions)
            {
                Console.WriteLine($"  - Session: Id={s.Id}, ProjectInfoId={s.ProjectInfoId}, ProjectName={s.ProjectName}, Status={s.Status}");
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Sessions.Clear();
                foreach (var session in sessions)
                {
                    Sessions.Add(session);
                }
                HasNoSessions = Sessions.Count == 0;
                Console.WriteLine($"[ProjectPreviewModal] HasNoSessions = {HasNoSessions}, Sessions.Count = {Sessions.Count}");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProjectPreviewModal] Error loading sessions: {ex.Message}");
            Console.WriteLine($"[ProjectPreviewModal] StackTrace: {ex.StackTrace}");
        }
    }

    private async void LoadProjectDetails()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Загрузка детальной информации о проекте...";

            // Определяем тип проекта по расширению
            string manifestPath = ProjectInfo.ManifestPath;
            bool isDirectory = Directory.Exists(manifestPath);
            bool isFile = File.Exists(manifestPath);

            if (isFile && manifestPath.EndsWith(".cli"))
            {
                _parser = new CliProvider();
                StatusMessage = "Парсинг CLI файла...";
            }
            else if (isFile && manifestPath.EndsWith(".cnc") || isDirectory)
            {
                _parser = new CncProvider();
                StatusMessage = "Парсинг CNC файла(ов)...";
            }
            else
            {
                throw new Exception($"Неизвестный формат проекта: {manifestPath}");
            }

            // Подписываемся на события парсера
            _parser.ParseProgressChanged += (progress) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"Парсинг проекта... {(int)progress}%";
                });
            };

            // Парсим проект
            Project = await _parser.ParseAsync(manifestPath);

            if (Project != null)
            {
                // Связываем Project с ProjectInfo из базы данных для корректного сохранения сессий
                Project.ProjectInfo = ProjectInfo;
                ProjectInfo.ProjectLink = Project;
                Console.WriteLine($"[ProjectPreviewModalViewModel] Set Project.ProjectInfo: Id={ProjectInfo.Id}, Name={ProjectInfo.Name}");

                StatusMessage = $"Проект загружен: {Project.Layers.Count} слоёв";
                IsLoading = false;

                // Инициализируем навигацию по слоям
                MaxLayerIndex = Project.Layers.Count > 0 ? Project.Layers.Count - 1 : 0;
                CurrentLayerIndex = 0;
                UpdateLayerInfo();

                // Публикуем уникальное событие для модального окна в UI потоке
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _eventAggregator.GetEvent<OnModalProjectLoadedEvent>().Publish(Project);
                });
            }
            else
            {
                throw new Exception("Не удалось загрузить проект.");
            }
        }
        catch (Exception ex)
        {
            IsLoading = false;
            StatusMessage = $"Ошибка: {ex.Message}";

            await CustomMessageBox.ShowErrorAsync(
                "Ошибка загрузки проекта",
                $"Не удалось загрузить детальную информацию о проекте:\n\n{ex.Message}"
            );
        }
    }

    private async void OnStartPrint()
    {
        if (Project == null)
        {
            await CustomMessageBox.ShowWarningAsync("Ошибка", "Проект ещё не загружен.");
            return;
        }

        var result = await CustomMessageBox.ShowQuestionAsync(
            "Выбрать активный проект ?",
            $"Проект будет выбран для печати \"{ProjectInfo.Name}\"?\n\n" +
            $"Слоёв: {Project.Layers.Count}\n" +
            $"Высота: {ProjectInfo.ProjectHeight} мм\n" +
            $"Время печати: {ProjectInfo.PrintTime}"
        );

        if (result == Models.MessageBoxResult.Yes)
        {
            //// Показываем чек-лист подготовки к работе
            //bool checklistCompleted = await CustomMessageBox.ShowPreparationChecklistAsync();

            //if (!checklistCompleted)
            //{
            //    // Оператор нажал "Отмена" или не проставил все галочки
            //    await CustomMessageBox.ShowWarningAsync(
            //        "Печать отменена",
            //        "Операция печати была отменена. Убедитесь, что все подготовительные работы выполнены перед началом печати."
            //    );
            //    return;
            //}

            // Устанавливаем активный проект (публикует OnActiveProjectSelected)
            _printService.SetActiveProject(Project);

            // Публикуем событие для начала печати
            _eventAggregator.GetEvent<OnProjectAnalyzeFinishEvent>().Publish(Project);

            // Закрываем модальное окно
            _modalService.Close(WindowId, true);

            // Показываем сообщение об успехе
            var random = new Random();

            ConfettiCannon.Fire(new ConfettiCannon.Options
            {
                Angle = random.Next(55, 125),
                Spread = random.Next(50, 70),
                ParticleCount = random.Next(50, 100),
                Origin = new Point(0.5, 0.6)
            });

            await CustomMessageBox.ShowSuccessAsync(
                "Печать начата",
                $"Проект \"{ProjectInfo.Name}\" отправлен в сервис печати. Перейдите в раздел Печать"
            );
        }
    }

    private async void OnDeleteProject()
    {
        // Сохраняем имя проекта до удаления
        var projectName = ProjectInfo.Name;

        var result = await CustomMessageBox.ShowQuestionAsync(
            "Удалить проект?",
            $"Вы действительно хотите удалить проект \"{projectName}\"?\n\n" +
            $"Это действие нельзя отменить."
        );

        if (result == Models.MessageBoxResult.Yes)
        {
            try
            {
                // Ждём завершения удаления проекта
                await _projectManager.RemoveProject(ProjectInfo);

                // Закрываем окно предпросмотра по его ID
                _modalService.Close(WindowId, true);

                // Показываем сообщение об успехе
                await CustomMessageBox.ShowSuccessAsync(
                    "Проект удалён",
                    $"Проект \"{projectName}\" успешно удалён."
                );
            }
            catch (Exception ex)
            {
                await CustomMessageBox.ShowErrorAsync(
                    "Ошибка удаления",
                    $"Не удалось удалить проект:\n\n{ex.Message}"
                );
            }
        }
    }

    private void OnCancel()
    {
        _modalService.Close(WindowId, true);
    }

    // Навигация по слоям
    private void PreviousLayer()
    {
        if (CurrentLayerIndex > 0)
        {
            CurrentLayerIndex--;
        }
    }

    private void NextLayer()
    {
        if (CurrentLayerIndex < MaxLayerIndex)
        {
            CurrentLayerIndex++;
        }
    }

    private bool CanGoPreviousLayer()
    {
        return CurrentLayerIndex > 0;
    }

    private bool CanGoNextLayer()
    {
        return CurrentLayerIndex < MaxLayerIndex;
    }

    private void UpdateCurrentLayer()
    {
        if (Project != null && Project.Layers != null && CurrentLayerIndex >= 0 && CurrentLayerIndex < Project.Layers.Count)
        {
            // Публикуем уникальное событие для модального окна
            var layer = Project.Layers[CurrentLayerIndex];
            _eventAggregator.GetEvent<OnModalLayerChangedEvent>().Publish(layer);

            Console.WriteLine($"[ProjectPreviewModalViewModel] Changed to layer {CurrentLayerIndex}, Regions: {layer.Regions?.Count}");
        }
    }

    private void UpdateLayerInfo()
    {
        if (Project != null && Project.Layers != null)
        {
            CurrentLayerInfo = $"Слой: {CurrentLayerIndex + 1} / {Project.Layers.Count}";
        }
        else
        {
            CurrentLayerInfo = "Слой: 0 / 0";
        }
    }

    /// <summary>
    /// Обработчик клика по сессии в списке истории печати
    /// </summary>
    private async void OnSessionClicked(PrintSession session)
    {
        if (session == null) return;

        Console.WriteLine($"[ProjectPreviewModal] Session clicked: {session.ProjectName}, Status: {session.Status}");

        // Показываем модальное окно возобновления только для прерванных сессий
        if (session.Status == PrintSpectator.Shared.Enums.ProjectStatus.Started)
        {
            var modalResult = await _modalService.ShowAsync<ResumeSessionModalView, ResumeSessionModalViewModel>(
                modalId: "ResumeSessionModal",
                options: new System.Collections.Generic.Dictionary<string, object>
                {
                    { "Session", session }
                },
                showOverlay: true,
                closeOnBackgroundClick: false
            );

            if (modalResult == null || !modalResult.IsSuccess || modalResult.Result == null)
            {
                Console.WriteLine("[ProjectPreviewModal] Resume modal was cancelled");
                return;
            }

            switch (modalResult.Result.Result)
            {
                case ResumeSessionResult.Resume:
                    await HandleResumeSessionAsync(session);
                    break;

                case ResumeSessionResult.StartNew:
                    await HandleStartNewSessionAsync(session);
                    break;

                case ResumeSessionResult.Cancel:
                default:
                    break;
            }
        }
        else
        {
            // Для завершённых сессий просто показываем информацию
            await CustomMessageBox.ShowInformationAsync(
                "Информация о сессии",
                $"Проект: {session.ProjectName}\n" +
                $"Начало: {session.StartedAt:dd.MM.yyyy HH:mm:ss}\n" +
                $"Завершение: {session.FinishedAt:dd.MM.yyyy HH:mm:ss}\n" +
                $"Статус: {GetStatusText(session.Status)}\n" +
                $"Слоёв напечатано: {session.LastCompletedLayer + 1} / {session.TotalLayers}\n" +
                $"Оператор: {session.UserName}"
            );
        }
    }

    private async Task HandleResumeSessionAsync(PrintSession session)
    {
        try
        {
            // Закрываем текущее модальное окно
            _modalService.Close(WindowId, true);

            // Загружаем проект из ProjectManager
            var projectInfo = await _projectManager.GetProjectByIdAsync(session.ProjectInfoId);
            if (projectInfo == null)
            {
                projectInfo = await _projectManager.GetProjectByNameAsync(session.ProjectName);
            }

            if (projectInfo == null || Project == null)
            {
                await CustomMessageBox.ShowErrorAsync(
                    "Ошибка",
                    "Не удалось найти проект для возобновления печати."
                );
                return;
            }

            // Возобновляем печать
            await _printService.ResumePrintFromSession(session, Project);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProjectPreviewModal] Error resuming session: {ex.Message}");
            await CustomMessageBox.ShowErrorAsync(
                "Ошибка возобновления",
                $"Не удалось возобновить печать:\n\n{ex.Message}"
            );
        }
    }

    private async Task HandleStartNewSessionAsync(PrintSession session)
    {
        try
        {
            // Помечаем старую сессию как остановленную
            await _printSessionService.StopSessionAsync();

            // Устанавливаем текущий проект как активный
            _printService.SetActiveProject(Project);

            // Закрываем окно предпросмотра
            _modalService.Close(WindowId, true);

            await CustomMessageBox.ShowInformationAsync(
                "Проект готов",
                $"Проект \"{ProjectInfo.Name}\" установлен для печати.\n\n" +
                $"Перейдите в раздел \"Печать\" для запуска."
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProjectPreviewModal] Error starting new session: {ex.Message}");
            await CustomMessageBox.ShowErrorAsync(
                "Ошибка",
                $"Не удалось подготовить проект:\n\n{ex.Message}"
            );
        }
    }

    private string GetStatusText(PrintSpectator.Shared.Enums.ProjectStatus status)
    {
        return status switch
        {
            PrintSpectator.Shared.Enums.ProjectStatus.Started => "Прервано",
            PrintSpectator.Shared.Enums.ProjectStatus.Finished => "Завершено",
            PrintSpectator.Shared.Enums.ProjectStatus.Stopped => "Остановлено",
            _ => "Неизвестно"
        };
    }
}