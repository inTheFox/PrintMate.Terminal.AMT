
using HandyControl.Controls;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.Views;
using PrintMate.Terminal.Views.Pages;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Opc2Lib;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Models;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.Views.Modals;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using Prism.Events;
using Permissions = PrintMate.Terminal.AppConfiguration.Permissions;

namespace PrintMate.Terminal.ViewModels
{
    public class RightBarViewModel : BindableBase
    {
        public ObservableCollection<RightMenuItem> Items { get; set; }
        public ObservableCollection<RightMenuItem> BottomItems { get; set; }

        private RightMenuItem _selectedItem;
        public RightMenuItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnSelectionChanged(value);
            }
        }

        private readonly IRegionManager _regionManager;
        private readonly ILogicControllerProvider _logicControllerProvider;
        private readonly ILogicControllerObserver _logicControllerObserver;
        private readonly PrintService _printService;
        private readonly PermissionManagerService _permissionManagerService;
        private readonly DialogService _dialogService;


            public RelayCommand<RightMenuItem> TestCommand { get; set; }
        public RelayCommand Open3DPreviewCommand { get; }

        public RightBarViewModel(
            IRegionManager regionManager,
            ILogicControllerProvider logicControllerProvider,
            ILogicControllerObserver observer,
            PrintService printService,
            PermissionManagerService permissionManagerService,
            IEventAggregator eventAggregator,
            DialogService dialogService
            )
        {
            _printService = printService;
            _regionManager = regionManager;
            _logicControllerProvider = logicControllerProvider;
            _logicControllerObserver = observer;
            _permissionManagerService = permissionManagerService;
            _dialogService = dialogService;

            // Команда для открытия 3D просмотра
            Open3DPreviewCommand = new RelayCommand(Open3DPreview);

            Items = new ObservableCollection<RightMenuItem>
            {
                new RightMenuItem {Id = nameof(ManualControl),Name = "Управление", Image = "/images/manual_64.png"},
                new RightMenuItem {Id = nameof(ConfigureTemplateView),Name = "Конфигурация", Image = "/images/system_64.png"},
                new RightMenuItem {Id = nameof(ProjectsView),Name = "Проекты", Image = "/images/folder_64.png"},
                //new RightMenuItem {Id = nameof(PrintPageView),Name = "Печать", Image = "/images/print_64.png"},

                //new RightMenuItem {Id = nameof(DirectoryPickerControl),Name = "ffff", Image = "/images/gauges_64.png"},
                //new RightMenuItem {Id = "works", Name = "Работы", Image = "/images/joblist_64.png"},
                //new RightMenuItem {Id = "print", Name = "Печать", Image = "/images/manual_64.png"},
            };
            TestCommand = new((TestCommandCallback));
            ShowStartPage();

            _logicControllerObserver.Subscribe(this,
                async (e) =>
                {
                    if (e.CommandInfo == OpcCommands.Com_PChamber_CameraLock)
                    {
                        DoorState = (bool)e.Value;
                    }
                    else if (e.CommandInfo == OpcCommands.Com_PChamber_Light)
                    {
                        LightState = (bool)e.Value;
                    }
                },
                OpcCommands.Com_PChamber_CameraLock,
                OpcCommands.Com_PChamber_Light
            );

            eventAggregator.GetEvent<OnActiveProjectSelected>().Subscribe((project) =>
            {
                if (Items.FirstOrDefault(p=>p.Id == nameof(PrintPageView)) == null)
                    Items.Add(new RightMenuItem { Id = nameof(PrintPageView), Name = "Печать", Image = "/images/print_64.png" });
                //if (Items.FirstOrDefault(p => p.Id == nameof(Project3DView)) == null)
                //    Items.Add(new RightMenuItem { Id = nameof(Project3DView), Name = "3D", Image = "/images/print_64.png" });
            });
        }

        private void TestCommandCallback(RightMenuItem item)
        {
            SelectedItem = item;
        }

        private void ShowStartPage()
        {
            SelectedItem = Items.First();
            //_regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(ManualControl));
        }


        private string _lightBlockImageSrc = "/images/light_off.png";
        private string _doorBlockImageSrc = "/images/chamber_locked_64.png";
        private bool _lightState = false;
        private bool _doorState = false;

        public string LightBlockImageSrc
        {
            get => _lightBlockImageSrc;
            set => SetProperty(ref _lightBlockImageSrc, value);
        }
        public string DoorBlockImageSrc
        {
            get => _doorBlockImageSrc;
            set => SetProperty(ref _doorBlockImageSrc, value);
        }

        public bool LightState
        {
            get => _lightState;
            set
            {
                if (value)
                {
                    LightBlockImageSrc = "/images/light_on.png";
                }
                else
                {
                    LightBlockImageSrc = "/images/light_off.png";
                }

                SetProperty(ref _lightState, value);
            }
        }
        public bool DoorState
        {
            get => _doorState;
            set
            {
                if (value)
                {
                    DoorBlockImageSrc = "/images/chamber_unlocked_64.png";
                }
                else
                {
                    DoorBlockImageSrc = "/images/chamber_locked_64.png";
                }

                SetProperty(ref _doorState, value);
            }
        }

        private async void OnSelectionChanged(RightMenuItem item)
        {
            if (item != null)
            {
                var permission = Permissions.Perms.FirstOrDefault(p => p.Id == item.Id);
                if (permission != null && _permissionManagerService.HasPermission(permission))
                {
                    _regionManager.RequestNavigate(Bootstrapper.MainRegion, item.Id);
                }
                else if (permission == null)
                {
                    // Если разрешение не найдено, предполагаем, что доступ разрешен
                    _regionManager.RequestNavigate(Bootstrapper.MainRegion, item.Id);
                }
                else if (!_permissionManagerService.HasPermission(permission))
                {
                    await CustomMessageBox.ShowErrorAsync("Ошибка доступа", $"У вас нет доступа к этому разделу !\n[{permission.Id} access denied.]");
                }
            }
        }

        /// <summary>
        /// Открывает страницу 3D просмотра проекта
        /// </summary>
        private async void Open3DPreview(object parameter)
        {
            if (_printService.ActiveProject == null)
            {
                await CustomMessageBox.ShowErrorAsync("Ошибка", "Сначала выберите проект для просмотра");
                return;
            }

            _regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(Views.Pages.Project3DView));
        }
    }


}
