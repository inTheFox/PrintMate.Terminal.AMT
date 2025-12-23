using System;
using System.Windows;
using System.Windows.Controls;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Services;
using Prism.Events;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Views.Modals
{
    public partial class ProjectPreviewModal : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        private SubscriptionToken _projectLoadedToken;
        private SubscriptionToken _layerChangedToken;

        public ProjectPreviewModal(IEventAggregator eventAggregator)
        {
            InitializeComponent();
            _eventAggregator = eventAggregator;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Отписываемся от событий при выгрузке контрола
            if (_projectLoadedToken != null)
                _eventAggregator.GetEvent<OnModalProjectLoadedEvent>().Unsubscribe(_projectLoadedToken);
            if (_layerChangedToken != null)
                _eventAggregator.GetEvent<OnModalLayerChangedEvent>().Unsubscribe(_layerChangedToken);
            ModalService.Instance.OnOpenAnimationFinish -= OnDialogAnimationFinished;

            Console.WriteLine("[ProjectPreviewModal] Unsubscribed from events");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine($"[ProjectPreviewModal] Loaded, DataContext: {DataContext?.GetType().Name}");

            // Переподписываемся при каждом показе (для корректной работы с view pooling)
            _projectLoadedToken = _eventAggregator.GetEvent<OnModalProjectLoadedEvent>().Subscribe(OnProjectLoaded);
            _layerChangedToken = _eventAggregator.GetEvent<OnModalLayerChangedEvent>().Subscribe(OnLayerChanged);
            ModalService.Instance.OnOpenAnimationFinish += OnDialogAnimationFinished;
        }

        private void OnDialogAnimationFinished(string modalId)
        {
            // Реагируем только если открывается именно это модальное окно
            if (DataContext is ProjectPreviewModalViewModel viewModel)
            {
                // Проверяем, что анимация завершилась для нашего окна
                if (modalId != viewModel.WindowId)
                    return;

                Console.WriteLine($"[ProjectPreviewModal] Animation finished for {modalId}, starting parsing...");

                // Запускаем парсинг после завершения анимации открытия окна
                if (viewModel.ProjectInfo != null)
                {
                    viewModel.StartLoadingAsync();
                }
            }
        }

        private void OnProjectLoaded(Project project)
        {
            Console.WriteLine($"[ProjectPreviewModal] Project loaded event received, Project ID: {project?.ProjectInfo?.Id}, Layers: {project?.Layers?.Count}");

            if (project == null || project.Layers == null || project.Layers.Count == 0)
            {
                Console.WriteLine("[ProjectPreviewModal] No layers to display");
                return;
            }

            // Проверяем, что загруженный проект соответствует текущему в ViewModel
            if (DataContext is ProjectPreviewModalViewModel viewModel)
            {
                if (viewModel.Project == null || viewModel.Project.ProjectInfo?.Id != project.ProjectInfo?.Id)
                {
                    Console.WriteLine($"[ProjectPreviewModal] Ignoring event - project mismatch. ViewModel Project ID: {viewModel.Project?.ProjectInfo?.Id}, Event Project ID: {project.ProjectInfo?.Id}");
                    return;
                }
            }

            // Отображаем первый слой в LayerCanvas
            Dispatcher.Invoke(() =>
            {
                LayerCanvas.SetLayer(project.Layers[0]);
                Console.WriteLine($"[ProjectPreviewModal] Set layer 0 to canvas, Regions count: {project.Layers[0].Regions?.Count}");
            });
        }

        private void OnLayerChanged(Layer layer)
        {
            Console.WriteLine($"[ProjectPreviewModal] Layer changed, Regions: {layer?.Regions?.Count}");

            if (layer == null)
            {
                Console.WriteLine("[ProjectPreviewModal] Layer is null");
                return;
            }

            // Обновляем отображение слоя в LayerCanvas
            Dispatcher.Invoke(() =>
            {
                LayerCanvas.SetLayer(layer);
                Console.WriteLine($"[ProjectPreviewModal] Updated canvas with new layer, Regions count: {layer.Regions?.Count}");
            });
        }

        private void OnSessionClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element &&
                element.DataContext is PrintSpectator.Shared.Models.PrintSession session)
            {
                if (DataContext is ProjectPreviewModalViewModel viewModel)
                {
                    viewModel.OnSessionClickedCommand?.Execute(session);
                }
            }
        }
    }
}
