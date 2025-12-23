using System;
using System.Windows;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Controls;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Services;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.ViewModels.PagesViewModels
{
    /// <summary>
    /// ViewModel для страницы 3D просмотра проекта
    /// </summary>
    public class Project3DViewModel : BindableBase, INavigationAware
    {
        #region Private Fields

        private readonly IEventAggregator _eventAggregator;
        private readonly PrintService _printService;

        private DX11ViewportControl _viewport;
        private Project _currentProject;

        private string _projectName = "Проект не загружен";
        private int _currentLayerIndex = 1;
        private int _totalLayers = 1;
        private double _projectHeight;
        private double _currentLayerZ;
        private bool _isLoading;
        private string _loadingMessage = "Загрузка...";

        #endregion

        #region Public Properties

        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        public int CurrentLayerIndex
        {
            get => _currentLayerIndex;
            set
            {
                if (SetProperty(ref _currentLayerIndex, value))
                {
                    UpdateLayerVisualization();
                    UpdateCurrentLayerZ();
                }
            }
        }

        public int TotalLayers
        {
            get => _totalLayers;
            set => SetProperty(ref _totalLayers, value);
        }

        public double ProjectHeight
        {
            get => _projectHeight;
            set => SetProperty(ref _projectHeight, value);
        }

        public double CurrentLayerZ
        {
            get => _currentLayerZ;
            set => SetProperty(ref _currentLayerZ, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            set => SetProperty(ref _loadingMessage, value);
        }

        #endregion

        #region Commands

        public RelayCommand NextLayerCommand { get; }
        public RelayCommand PreviousLayerCommand { get; }
        public RelayCommand ResetCameraCommand { get; }
        public RelayCommand TopViewCommand { get; }

        #endregion

        #region Constructor

        public Project3DViewModel(IEventAggregator eventAggregator, PrintService printService)
        {
            _eventAggregator = eventAggregator;
            _printService = printService;

            // Команды
            NextLayerCommand = new RelayCommand(OnNextLayer);
            PreviousLayerCommand = new RelayCommand(OnPreviousLayer);
            ResetCameraCommand = new RelayCommand(OnResetCamera);
            TopViewCommand = new RelayCommand(OnTopView);

            // Подписка на события
            _eventAggregator.GetEvent<OnActiveProjectSelected>().Subscribe(OnProjectSelected);

            // Загружаем текущий проект если он уже есть
            if (_printService.ActiveProject != null)
            {
                LoadProject(_printService.ActiveProject);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Устанавливает Viewport для рендеринга
        /// </summary>
        public void SetViewport(DX11ViewportControl viewport)
        {
            _viewport = viewport;

            // PULL модель: передаём функцию для получения текущего слоя
            _viewport.SetCurrentLayerGetter(() => CurrentLayerIndex);

            // Подключаем callback для отслеживания состояния загрузки
            _viewport.OnLoadingStateChanged = (isLoading) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsLoading = isLoading;
                    LoadingMessage = isLoading ? "Построение геометрии..." : "";
                });
            };

            // Подключаем callback для прогресса кеширования
            _viewport.OnCachingProgress = (maxCachedLayer) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (IsLoading && maxCachedLayer > 0)
                    {
                        int percent = TotalLayers > 0 ? (int)((double)maxCachedLayer / TotalLayers * 100) : 0;
                        LoadingMessage = $"Кеширование слоёв: {percent}%";
                    }
                });
            };

            // Если проект уже загружен, передаём его в viewport
            if (_currentProject != null)
            {
                _viewport.LoadProject(_currentProject);
            }
        }

        #endregion

        #region Private Methods

        private void LoadProject(Project project)
        {
            if (project == null) return;

            _currentProject = project;
            ProjectName = project.ProjectInfo?.Name ?? "Без имени";
            TotalLayers = project.Layers?.Count ?? 0;
            ProjectHeight = project.GetProjectHeight();

            // Сбрасываем на первый слой
            CurrentLayerIndex = 1;
            UpdateCurrentLayerZ();

            // Загружаем в viewport если он уже инициализирован
            _viewport?.LoadProject(project);

            Console.WriteLine($"[Project3DView] Project loaded: {ProjectName}, {TotalLayers} layers, height: {ProjectHeight:F2}mm");
        }

        private void UpdateLayerVisualization()
        {
            // Viewport использует PULL модель - он сам запрашивает CurrentLayerIndex
            // через callback переданный в SetCurrentLayerGetter
        }

        private void UpdateCurrentLayerZ()
        {
            if (_currentProject?.Layers == null || CurrentLayerIndex <= 0 || CurrentLayerIndex > _currentProject.Layers.Count)
            {
                CurrentLayerZ = 0;
                return;
            }

            // layer.Height содержит абсолютную Z позицию в мм
            int idx = CurrentLayerIndex - 1;
            double z = _currentProject.Layers[idx].Height;
            if (z < 0.001)
            {
                float layerThickness = _currentProject.GetLayerThicknessInMillimeters();
                z = CurrentLayerIndex * layerThickness;
            }
            CurrentLayerZ = z;
        }

        private void OnProjectSelected(Project project)
        {
            LoadProject(project);
        }

        #endregion

        #region Command Handlers

        private void OnNextLayer(object parameter)
        {
            if (CurrentLayerIndex < TotalLayers)
            {
                CurrentLayerIndex++;
            }
        }

        private void OnPreviousLayer(object parameter)
        {
            if (CurrentLayerIndex > 1)
            {
                CurrentLayerIndex--;
            }
        }

        private void OnResetCamera(object parameter)
        {
            _viewport?.ResetCamera();
        }

        private void OnTopView(object parameter)
        {
            _viewport?.SetTopView();
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // Проверяем параметры навигации
            if (navigationContext.Parameters.ContainsKey("project"))
            {
                var project = navigationContext.Parameters["project"] as Project;
                if (project != null)
                {
                    LoadProject(project);
                }
            }
            else if (_printService.ActiveProject != null)
            {
                LoadProject(_printService.ActiveProject);
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        #endregion
    }
}
