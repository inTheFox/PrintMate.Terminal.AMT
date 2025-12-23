using HandyControl.Controls;
using HandyControl.Tools.Command;
using ImTools;
using PrintMate.Terminal.Database;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Parsers.Shared.Models;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.Views.Modals;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using ProjectParserTest.Parsers.CliParser;
using ProjectParserTest.Parsers.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using MessageBox = System.Windows.MessageBox;
using MessageBoxResult = PrintMate.Terminal.Models.MessageBoxResult;

namespace PrintMate.Terminal.ViewModels
{
    public class ProjectsViewViewModel : BindableBase, IRegionMemberLifetime
    {
        private ObservableCollection<ProjectInfo> _projects;
        public ObservableCollection<ProjectInfo> Projects
        {
            get => _projects;
            set => SetProperty(ref _projects, value);
        }

        private ProjectInfo _selectedProject;
        public ProjectInfo SelectedProject
        {
            get => _selectedProject;
            set => SetProperty(ref _selectedProject, value);

        }

        public RelayCommand AddProjectCommand { get; set; }
        public RelayCommand SelectProject { get; set; }

        // Событие для запроса прокрутки к началу списка
        public event Action ScrollToTopRequested;

        private readonly ModalService _modalService;
        private readonly ProjectsRepository _projectsRepository;
        private readonly ProjectManager _cliProvider;
        private readonly IEventAggregator _eventAggregator;

        public ProjectsViewViewModel(ModalService modalService, ProjectsRepository projectsRepository, ProjectManager cliProvider, IEventAggregator eventAggregator)
        {
            Console.WriteLine("[ProjectsViewViewModel] Constructor called");

            _modalService = modalService;
            _eventAggregator = eventAggregator;
            _cliProvider = cliProvider;
            _projectsRepository = projectsRepository;
            //AddProjectCommand = new RelayCommand(OnProjectAddButtonClickCallback);

            _ = Task.Run(() =>
            {
                var list = projectsRepository.GetList();
                Application.Current.Dispatcher.InvokeAsync(() => Projects = new ObservableCollection<ProjectInfo>(list));
            });
            SelectProject = new RelayCommand(OnSelectProjectCallback);

            Console.WriteLine("[ProjectsViewViewModel] Subscribing to OnProjectListUpdated");
            _eventAggregator.GetEvent<OnProjectListUpdated>().Subscribe(() =>
            {
                Console.WriteLine("[ProjectsViewViewModel] OnProjectListUpdated event received - INSIDE HANDLER");

                var list = projectsRepository.GetList();
                Console.WriteLine($"[ProjectsViewViewModel] Loaded {list.Count} projects");
                Projects = new ObservableCollection<ProjectInfo>(list);

                // Запрашиваем прокрутку к началу списка
                ScrollToTopRequested?.Invoke();
            }, ThreadOption.UIThread, true);  // keepSubscriberReferenceAlive = true
            Console.WriteLine("[ProjectsViewViewModel] Subscription complete");
        }

        private async void OnSelectProjectCallback(object e)
        {
            // Показываем окно подтверждения выбора проекта
            var result = await CustomMessageBox.ShowConfirmationAsync(
                "Выбрать проект?",
                "Будет предоставлена детальная информация и превью проекта."
            );

            if (result == MessageBoxResult.Yes)
            {
                // После подтверждения открываем окно предпросмотра (парсинг начнётся автоматически внутри)
                // Fire-and-forget - не ждём закрытия окна


                _modalService.Show<ProjectPreviewModal, ProjectPreviewModalViewModel>(
                    options: new Dictionary<string, object>
                    {
                        {"ProjectInfo", e},
                    },
                    showOverlay: true,
                    closeOnBackgroundClick: false,
                    modalId: null  // Генерируем новый ID каждый раз для создания нового экземпляра
                );
            }
        }

        public bool KeepAlive => false;
    }
}
