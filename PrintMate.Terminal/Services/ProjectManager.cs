using ImTools;
using PrintMate.Terminal.Database;
using PrintMate.Terminal.Events;
using Prism.Events;
using ProjectParserTest.Parsers.CliParser;
using ProjectParserTest.Parsers.Shared.Interfaces;
using ProjectParserTest.Parsers.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using PrintMate.Terminal.Parsers.CncParser;
using PrintMate.Terminal.Parsers.Shared.Models;

namespace PrintMate.Terminal.Services
{
    public enum ProjectLoadRequestStep
    {
        Start,
        Parse,
        Save,
        Finish
    }
    public class ProjectLoadRequest
    {
        public Guid Id { get; set;}
        public ProjectLoadRequestStep Step { get; set; }

        public double Progress { get; set; }
    }

    public class ProjectManager
    {
        public const string CliFormat = ".cli";
        public const string CncFormat = ".cnc";

        public static string ProjectsDirectoryPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Projects");

        private readonly CliProvider _cliProvider;
        private readonly CncProvider _cncProvider;

        private readonly IEventAggregator _eventAggregator;
        private readonly ProjectsRepository _projectRepository;
        private readonly NotificationService _notificationService;

        public ProjectManager(IEventAggregator eventAggregator, ProjectsRepository projectRepository, NotificationService notificationService)
        {
            _notificationService = notificationService;
            _eventAggregator = eventAggregator;
            _projectRepository = projectRepository;

            // Инициализация CLI парсера
            _cliProvider = new CliProvider();
            _cliProvider.ParseProgressChanged += OnParserProgressChanged;
            _cliProvider.ParseStarted += (path) => _eventAggregator.GetEvent<OnProjectAnalyzeStart>().Publish(path);
            _cliProvider.ParseError += OnParserError;

            // Инициализация CNC парсера
            _cncProvider = new CncProvider();
            _cncProvider.ParseProgressChanged += OnParserProgressChanged;
            _cncProvider.ParseStarted += (path) => _eventAggregator.GetEvent<OnProjectAnalyzeStart>().Publish(path);
            _cncProvider.ParseError += OnParserError;

            CreateProjectsDirectory();
        }

        private void CreateProjectsDirectory()
        {
            if (!Directory.Exists(ProjectsDirectoryPath))
            {
                Directory.CreateDirectory(ProjectsDirectoryPath);
            }
        }

        private void OnParserProgressChanged(double progress)
        {
            _eventAggregator.GetEvent<OnProjectAnalyzeProgressChangedEvent>().Publish(progress);
            _eventAggregator.GetEvent<OnProjectImportStatusProgressChangedEvent>().Publish((int)progress);
        }

        private void OnParserError(string errorMessage)
        {
            _eventAggregator.GetEvent<OnProjectImportError>().Publish(errorMessage);
            Console.WriteLine($"Parser Error: {errorMessage}");
        }

        public ProjectLoadRequest CreateLoadProjectRequest() => new ProjectLoadRequest
            { Id = Guid.NewGuid(), Step = ProjectLoadRequestStep.Start };

        public void Load(string projectPath)
        {
            // Запускаем весь процесс в фоновом потоке
            Task.Run(async () => await LoadAsync(projectPath));
        }

        private async Task LoadAsync(string projectPath)
        {
            Console.WriteLine($"[LoadAsync] START: {projectPath}");

            // Определяем имя проекта в зависимости от типа
            bool isDirectory = Directory.Exists(projectPath);
            bool isFile = File.Exists(projectPath);
            string projectName = isFile ? Path.GetFileNameWithoutExtension(projectPath) : Path.GetFileName(projectPath);

            Console.WriteLine($"[LoadAsync] Checking if project exists: {projectName}");
            if (await _projectRepository.GetProjectByName(projectName) != null)
            {
                Console.WriteLine($"[LoadAsync] Project already exists!");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ModalService.Instance.Close();
                    _notificationService.Error("Ошибка", "Проект с таким названием уже существует!", 5);
                });
                return;
            }

            Console.WriteLine($"[LoadAsync] Publishing OnProjectAnalyzeStart");
            await Application.Current.Dispatcher.InvokeAsync(() =>
                _eventAggregator.GetEvent<OnProjectAnalyzeStart>().Publish(projectPath));

            try
            {
                if (string.IsNullOrWhiteSpace(projectPath))
                    throw new ArgumentException("Путь проекта не может быть пустым.", nameof(projectPath));

                string format = isFile ? Path.GetExtension(projectPath) : string.Empty;

                IParserProvider parser;
                string statusMessage;

                // Определяем тип проекта и выбираем парсер
                if (isFile && format == CliFormat)
                {
                    // CLI файл
                    parser = _cliProvider;
                    statusMessage = "Идёт анализ CLI файла...";
                }
                else if (isDirectory || isFile && format == CncFormat)
                {
                    // CNC проект (папка с CNC файлами или один CNC файл)
                    parser = _cncProvider;
                    statusMessage = "Идёт анализ CNC проекта...";
                }
                else
                {
                    throw new Exception($"Неизвестный формат проекта: {projectPath}");
                }

                // Парсинг проекта (легкий - только заголовок для сохранения в БД)
                Console.WriteLine($"[LoadAsync] Starting header-only parsing with {parser.GetType().Name}");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    _eventAggregator.GetEvent<OnProjectImportStatusChangedEvent>().Publish(statusMessage));

                // Используем легкий парсинг для CLI файлов
                Project project;
                if (parser is CliProvider cliParser)
                {
                    project = await cliParser.ParseHeaderOnlyAsync(projectPath);
                }
                else
                {
                    // Для CNC и других форматов пока используем полный парсинг
                    project = await parser.ParseAsync(projectPath);
                }
                Console.WriteLine($"[LoadAsync] Parsing COMPLETE");

                if (project == null)
                {
                    throw new Exception("Не удалось распарсить проект.");
                }

                // Сохранение для отладки (опционально) - ОТКЛЮЧЕНО для больших проектов
                // string debugFileName = isDirectory || format == CncFormat ? "cnc_example.json" : "cli_example.json";
                // await File.WriteAllTextAsync(debugFileName, JsonConvert.SerializeObject(project, Formatting.Indented));

                // Обнуляем прогресс и переходим к копированию файлов проекта
                Console.WriteLine($"[LoadAsync] Starting file copy");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _eventAggregator.GetEvent<OnProjectImportStatusChangedEvent>().Publish("Сохраняем проект...");
                    _eventAggregator.GetEvent<OnProjectImportStatusProgressChangedEvent>().Publish(0);
                });

                string destinationPath;

                if (isFile)
                {
                    // Один файл (CLI или CNC)
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string fileName = $"{Path.GetFileNameWithoutExtension(projectPath)}_{timestamp}{Path.GetExtension(projectPath)}";
                    destinationPath = Path.Combine(ProjectsDirectoryPath, fileName);

                    Console.WriteLine($"[LoadAsync] Copying file: {projectPath} -> {destinationPath}");
                    await CopyFileWithProgressAsync(projectPath, destinationPath, new Progress<double>((progress) =>
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                            _eventAggregator.GetEvent<OnProjectImportStatusProgressChangedEvent>().Publish((int)progress));
                    }));
                    Console.WriteLine($"[LoadAsync] File copy COMPLETE");
                }
                else
                {
                    // Папка с CNC файлами
                    string folderName = Path.GetFileName(projectPath);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    destinationPath = Path.Combine(ProjectsDirectoryPath, $"{folderName}_{timestamp}");

                    if (Directory.Exists(destinationPath))
                    {
                        Directory.Delete(destinationPath, true);
                    }

                    Console.WriteLine($"[LoadAsync] Copying directory: {projectPath} -> {destinationPath}");
                    await CopyDirectoryWithProgressAsync(projectPath, destinationPath, new Progress<double>((progress) =>
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                            _eventAggregator.GetEvent<OnProjectImportStatusProgressChangedEvent>().Publish((int)progress));
                    }));
                    Console.WriteLine($"[LoadAsync] Directory copy COMPLETE");
                }

                // Сохранение в базу данных
                Console.WriteLine($"[LoadAsync] Saving to database");
                project.ProjectInfo.ManifestPath = destinationPath;
                await _projectRepository.AddAsync(project.ProjectInfo);

                // Публикация события завершения
                Console.WriteLine($"[LoadAsync] Publishing OnProjectAnalyzeFinishEvent");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _eventAggregator.GetEvent<OnProjectAnalyzeFinishEvent>().Publish(parser.Project);
                    _eventAggregator.GetEvent<OnProjectListUpdated>().Publish();
                });

                Console.WriteLine($"[LoadAsync] SUCCESS: {project.ProjectInfo.Name}");

                // Освобождение ресурсов распарсенного проекта
                Console.WriteLine($"[LoadAsync] Freeing project resources from memory");
                FreeProjectResources(project);
                parser.ClearProject();
                Console.WriteLine($"[LoadAsync] Resources freed");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка загрузки проекта: {e.Message}");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ModalService.Instance.Close();
                    _notificationService.Error("Ошибка", $"Не удалось загрузить проект: {e.Message}", 5);
                    _eventAggregator.GetEvent<OnProjectImportError>().Publish(e.Message);
                });
            }
        }

        public async Task RemoveProject(ProjectInfo projectInfo)
        {
            await _projectRepository.RemoveAsync(projectInfo);

            // Публикуем событие в UI-потоке
            Application.Current.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("[ProjectManager] Publishing OnProjectListUpdated after project removal");
                _eventAggregator.GetEvent<OnProjectListUpdated>().Publish();
            });
        }

        /// <summary>
        /// Получить проект по ID
        /// </summary>
        public async Task<ProjectInfo> GetProjectByIdAsync(int projectId)
        {
            return await _projectRepository.GetProjectById(projectId);
        }

        /// <summary>
        /// Получить проект по имени
        /// </summary>
        public async Task<ProjectInfo> GetProjectByNameAsync(string projectName)
        {
            return await _projectRepository.GetProjectByName(projectName);
        }

        public async Task CopyFileWithProgressAsync(string sourcePath, string destinationPath, IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {
            const int bufferSize = 8192; // 8KB
            var buffer = new byte[bufferSize];
            long totalBytesCopied = 0;
            var fileInfo = new FileInfo(sourcePath);
            long fileSize = fileInfo.Length;

            // Начальный прогресс
            progress?.Report(0);

            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous);

            int bytesRead;
            while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destinationStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalBytesCopied += bytesRead;

                // Вычисляем процент и ограничиваем максимум 100%
                double percentage = fileSize > 0 ? Math.Min((double)totalBytesCopied / fileSize * 100, 100) : 100;
                progress?.Report(percentage);
            }

            await destinationStream.FlushAsync(cancellationToken);

            // Гарантируем 100% в конце
            progress?.Report(100);
        }

        public async Task CopyDirectoryWithProgressAsync(
    string sourceDir,
    string destinationDir,
    IProgress<double> progress = null, // Прогресс в процентах: 0.0 – 100.0
    CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(sourceDir))
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

            Directory.CreateDirectory(destinationDir);

            // Получаем все файлы и их общий размер
            var files = EnumerateFilesRecursively(sourceDir).ToArray();
            long totalBytes = files.Sum(f => new FileInfo(f).Length);
            long copiedBytes = 0;
            int filesCopied = 0;

            // Немедленно отправляем 0%, если есть подписчик
            progress?.Report(0.0);

            foreach (var sourceFile in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                string destFile = Path.Combine(destinationDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

                // Размер текущего файла
                long currentFileLength = new FileInfo(sourceFile).Length;

                // Копируем файл с прогрессом
                await CopyFileWithProgressAsync(
                    sourceFile,
                    destFile,
                    new Progress<double>(fileProgressPercentage =>
                    {
                        // fileProgressPercentage от 0 до 100 для текущего файла
                        // Переводим в байты текущего файла
                        double bytesInCurrentFile = currentFileLength * (fileProgressPercentage / 100.0);
                        // Общий прогресс = уже скопированные файлы + текущий прогресс
                        double currentTotal = copiedBytes + bytesInCurrentFile;
                        double percentage = totalBytes == 0 ? 100.0 : (currentTotal / totalBytes * 100.0);
                        progress?.Report(Math.Min(percentage, 100.0));
                    }),
                    cancellationToken);

                // Файл полностью скопирован
                copiedBytes += currentFileLength;
                filesCopied++;

                // Обновляем прогресс (на случай, если CopyFileWithProgressAsync не вызвал последний репорт)
                double finalPercentage = totalBytes == 0 ? 100.0 : (double)copiedBytes / totalBytes * 100.0;
                progress?.Report(Math.Min(finalPercentage, 100.0));
            }

            // Гарантируем 100% в конце всего процесса
            progress?.Report(100.0);
        }

        private static IEnumerable<string> EnumerateFilesRecursively(string path)
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Освобождает ресурсы проекта из памяти после сохранения
        /// </summary>
        private void FreeProjectResources(Project project)
        {
            if (project == null) return;

            try
            {
                // Очищаем слои и их регионы
                if (project.Layers != null)
                {
                    foreach (var layer in project.Layers)
                    {
                        if (layer?.Regions != null)
                        {
                            foreach (var region in layer.Regions)
                            {
                                // Очищаем полилинии региона
                                if (region.PolyLines != null)
                                {
                                    foreach (var polyline in region.PolyLines)
                                    {
                                        polyline?.Points?.Clear();
                                    }
                                    region.PolyLines.Clear();
                                }
                            }
                            layer.Regions.Clear();
                        }
                    }
                    project.Layers.Clear();
                }

                // Очищаем конфигурацию и заголовки
                project.HeaderInfo?.DataList?.Clear();
                project.Configuration?.DataList?.Clear();

                // Обнуляем ссылки
                project.CurrentLayer = null;
                project.ProjectInfo = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FreeProjectResources] Error: {ex.Message}");
            }
        }
    }
}
