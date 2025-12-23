using Hans.NET.libs;
using HansScannerHost.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Opc2Lib;
using PrintMate.Terminal.AppConfiguration;
using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Database;
using PrintMate.Terminal.Interfaces;
using PrintMate.Terminal.Opc;
using PrintMate.Terminal.Region;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels;
using PrintMate.Terminal.ViewModels.Configure;
using PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels;
using PrintMate.Terminal.ViewModels.Configure.ConfigureProcessViewModels;
using PrintMate.Terminal.ViewModels.ModalsViewModels;
using PrintMate.Terminal.ViewModels.PagesViewModels;
using PrintMate.Terminal.Views;
using PrintMate.Terminal.Views.Configure;
using PrintMate.Terminal.Views.Configure.ConfigureParametersViews;
using PrintMate.Terminal.Views.Configure.ConfigureProcessViews;
using PrintMate.Terminal.Views.Modals;
using PrintMate.Terminal.Views.Pages;
using Prism.DryIoc;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using ProjectParserTest.Parsers.CliParser;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LoggingService.Client;
using AddProjectModalSelectProjectType = PrintMate.Terminal.Views.Modals.AddProjectModalSelectProjectType;

namespace PrintMate.Terminal
{
    public class Bootstrapper : PrismBootstrapper
    {
        public const string LeftBarRegion = "LeftBarRegion";
        public const string MainRegion = "MainRegion";
        public const string RightBarRegion = "RightBarRegion";
        public const string ManualContent = "ManualContent";
        public const string ConfigureTemplateRegion = "ConfigureTemplateRegion";

        public static IContainerProvider ContainerProvider;
        private static ConfigurationManager _configurationManager;

        /// <summary>
        /// Access to the new ConfigurationSystem (thread-safe, encrypted, validated).
        /// </summary>
        public static ConfigurationManager Configuration => _configurationManager;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            try
            {
                // Сохраняем ссылку на контейнер
                ContainerProvider = Container;

                // ConfigurationSystem already initialized in RegisterTypes
                ContainerProvider.Resolve<IRegionManager>();
                //ContainerProvider.Resolve<MultiScanatorSystem>();
                //_ = Task.Run(async () =>
                //{
                //    await ContainerProvider.Resolve<MultiScanatorSystem>().Test();
                //});

                _ = Task.Run(async () =>
                {
                    var multiScanatorProxy = ContainerProvider.Resolve<MultiScanatorSystemProxy>();
                    multiScanatorProxy.CreateProxy(new ScanatorProxyClient("172.18.34.227"));
                    multiScanatorProxy.CreateProxy(new ScanatorProxyClient("172.18.34.228"));
                });

                _ = Task.Run(() =>
                {
                    DatabaseContext db = ContainerProvider.Resolve<DatabaseContext>();
                    db.Database.Migrate();
                    db.Database.EnsureCreated();
                });

                // Работаем с сетевыми обсерверами
                PingObserver pingObserverService = ContainerProvider.Resolve<PingObserver>();
                Task.Run((async () =>
                {
                    // Инициализируем задачи для слежения (ПЛК, два лазера и два сканатора)
                    pingObserverService.InitListeners();
                    // Запускаем наблюдение за ПЛК
                    await pingObserverService.StartObserver(PingObserver.PlcConnectionObserver);
                    // Запускаем наблюдение за лазером 1
                    await pingObserverService.StartObserver(PingObserver.Laser1ConnectionObserver);
                    // Запускаем наблюдение за лазером 2
                    await pingObserverService.StartObserver(PingObserver.Laser2ConnectionObserver);
                    // Запускаем наблюдение за сканатором 1
                    //await pingObserverService.StartObserver(PingObserver.Scanator1ConnectionObserver);
                    //// Запускаем наблюдение за сканатором 2
                    //await pingObserverService.StartObserver(PingObserver.Scanator2ConnectionObserver);
                }));
                // 

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
        {
            base.ConfigureRegionAdapterMappings(regionAdapterMappings);

            // Создаём адаптер вручную (т.к. в старом Prism нет DI-контейнера в этом месте)
            var behaviorFactory = Container.Resolve<IRegionBehaviorFactory>();
            var adapter = new AnimatedContentControlRegionAdapter(behaviorFactory);

            regionAdapterMappings.RegisterMapping(typeof(AnimatedContentControl), adapter);
        }

        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Initialize ConfigurationSystem BEFORE registering
            InitializeConfigurationSystem();

            containerRegistry.RegisterForNavigation<LeftBarView>();
            containerRegistry.RegisterForNavigation<MainView>();
            containerRegistry.RegisterForNavigation<RightBarView>();
            containerRegistry.RegisterForNavigation<IntroVideoView>();
            containerRegistry.RegisterForNavigation<WelcomeView, WelcomeViewModel>();
            containerRegistry.RegisterForNavigation<ManualControl, ManualControlViewModel>();
            containerRegistry.RegisterForNavigation<ManualAxesControl>();
            containerRegistry.RegisterForNavigation<ManualControlSystems, ManualControlSystemsModel>();
            containerRegistry.RegisterForNavigation<MonitoringTemplateView, MonitoringTemplateViewModel>();
            containerRegistry.RegisterForNavigation<RootContainer>();
            containerRegistry.RegisterForNavigation<ConfigureTemplateView, ConfigureTemplateViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureProcessView, ConfigureProcessViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureView, ConfigureViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureParameters, ConfigureParametersViewModel>();
            containerRegistry.RegisterForNavigation<LoginScreenView, LoginScreenViewModel>();
            containerRegistry.RegisterForNavigation<ProjectsView, ProjectsViewViewModel>();
            containerRegistry.RegisterForNavigation<ProjectPreview, ProjectPreviewViewModel>();
            containerRegistry.RegisterForNavigation<Layer3DView, Layer3DViewModel>();
            containerRegistry.RegisterForNavigation<PrintPageView, PrintPageViewModel>();
            containerRegistry.RegisterForNavigation<Project3DView, Project3DViewModel>();

            // Конфигурация - процесс
            containerRegistry.RegisterForNavigation<ConfigureProcessSystemView, ConfigureProcessSystemViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureProcessGas, ConfigureProcessGasViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureProcessLaser, ConfigureProcessLaserViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureProcessPowder, ConfigureProcessPowderViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureProcessService, ConfigureProcessServiceViewModel>();
            // Config - Parameters
            containerRegistry.RegisterForNavigation<ConfigureParametersPlc, ConfigureParametersPlcViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureParametersScanator, ConfigureParametersScanatorViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureParametersUsers, ConfigureParametersUsersViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureParametersLasers, ConfigureParametersLasersViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureParametersStorage, ConfigureParametersStorageViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureParametersAdditionalSoftware, ConfigureParametersAdditionalSoftwareViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureParametersCamera, ConfigureParametersCameraViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureParametersAutomaticSettings, ConfigureParametersAutomaticSettingsViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureParametersServicesStates, ConfigureParametersServicesStatesViewModel>(); 
            containerRegistry.RegisterForNavigation<ConfigureParametersRoles, ConfigureParametersRolesManagementViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureParametersLoggingView, ConfigureParametersLoggingViewModel>();
            containerRegistry.RegisterForNavigation<ConfigureParametersComputerVision, ConfigureParametersComputerVisionViewModel>();




            // Forms
            containerRegistry.RegisterForNavigation<AddUserViewModelForm, AddUserFormViewModel>();
            containerRegistry.RegisterForNavigation<SelectFolderView, SelectFolderViewModel>();
            containerRegistry.RegisterForNavigation<DirectoryPickerControl, DirectoryPickerControlViewModel>();
            containerRegistry.RegisterForNavigation<RemoveUserForm, RemoveUserFormViewModel>();
            containerRegistry.RegisterForNavigation<SelectCncProjectModal, SelectCncProjectModalViewModel>();
            containerRegistry.RegisterForNavigation<AddProjectModalSelectProjectType, AddProjectModalSelectProjectTypeViewModel>();
            containerRegistry.RegisterForNavigation<ProjectDirectoryPicker, ProjectDirectoryPickerViewModel>();
            containerRegistry.RegisterForNavigation<AddProjectWizard, AddProjectWizardViewModel>();
            containerRegistry.RegisterForNavigation<AddProjectLoadingProgressView, AddProjectLoadingProgressViewModel>();
            containerRegistry.RegisterForNavigation<ProjectPreviewModal, ProjectPreviewModalViewModel>();
            containerRegistry.RegisterForNavigation<Project3DPreviewView, Project3DPreviewViewModel>();
            containerRegistry.RegisterForNavigation<CameraSelectModal, CameraSelectModalViewModel>();
            containerRegistry.RegisterForNavigation<AppExitModalView, AppExitModalViewModel>();
            containerRegistry.RegisterForNavigation<ResumeSessionModalView, ResumeSessionModalViewModel>();
            containerRegistry.RegisterForNavigation<ProjectViewer3D, ProjectViewer3DViewModel>();
            containerRegistry.RegisterForNavigation<AddRoleViewModelForm, AddRoleFormViewModel>();
            containerRegistry.RegisterForNavigation<AccountManagementView, AccountManagementViewModel>();
            containerRegistry.RegisterForNavigation<ChangePasswordView, ChangePasswordViewModel>();
            containerRegistry.RegisterForNavigation<PermDeniedView>();
            containerRegistry.RegisterForNavigation<NotificationsCenterView, NotificationsCenterViewModel>();


            // Keyboard
            containerRegistry.Register<Keyboard>();
            containerRegistry.Register<KeyboardViewModel>();



            containerRegistry.RegisterSingleton<LoggerService>();
            //containerRegistry.RegisterSingleton<MultiScanatorSystem>();
            //containerRegistry.RegisterSingleton<UdmBuilder>();
            //containerRegistry.RegisterSingleton<IParserProvider, CliProvider>();
            //containerRegistry.RegisterSingleton<ProjectService>();
            containerRegistry.RegisterScoped<UserService>();


            // SignalR Proxy
            //containerRegistry.RegisterSingleton<ILogicControllerProvider, LogicControllerWebProxy>();
            //containerRegistry.RegisterSingleton<ILogicControllerObserver, LogicControllerObserverProxy>();

            // Default
            containerRegistry.RegisterSingleton<ILogicControllerProvider, LogicControllerService>();
            containerRegistry.RegisterSingleton<ILogicControllerObserver, LogicControllerObserver>();
            //containerRegistry.RegisterSingleton<IRolesRepositoryService, ConfigureParametersRolesManagementViewModel>();


            //containerRegistry.RegisterSingleton<OpcManager>();
            containerRegistry.RegisterSingleton<PingService>();
            containerRegistry.RegisterSingleton<PingObserver>();
            containerRegistry.RegisterSingleton<MonitoringManager>();
            containerRegistry.RegisterSingleton<KeyboardService>();
            containerRegistry.RegisterSingleton<InputLanguageService>();
            containerRegistry.RegisterSingleton<DialogService>();
            containerRegistry.RegisterSingleton<ModalService>();
            containerRegistry.RegisterSingleton<MultiLaserSystemService>();
            containerRegistry.RegisterSingleton<DatabaseContext>();
            containerRegistry.RegisterSingleton<RolesService>();
            containerRegistry.RegisterSingleton<PermissionManagerService>();
            containerRegistry.RegisterSingleton<AuthorizationService>();
            containerRegistry.RegisterSingleton<LoggerService>();


            // Register DatabaseContext for DI with proper configuratio

            containerRegistry.RegisterSingleton<ProjectsRepository>();
            containerRegistry.RegisterSingleton<NotificationService>();
            containerRegistry.RegisterSingleton<ProjectManager>();
            //containerRegistry.RegisterSingleton<MultiScanatorSystem>();
            containerRegistry.RegisterSingleton<PrintService>();
            containerRegistry.RegisterSingleton<PrintResumeHandler>();
            containerRegistry.RegisterSingleton<PrintSessionService>();

            containerRegistry.RegisterSingleton<MultiScanatorSystemProxy>();

            // NEW ConfigurationSystem - register as singleton for DI
            containerRegistry.RegisterInstance<ConfigurationManager>(_configurationManager);
            containerRegistry.RegisterSingleton<CameraService>();

            // NEW MERGE

        }

        /// <summary>
        /// Initializes the new ConfigurationSystem with encryption and thread-safety.
        /// </summary>
        private void InitializeConfigurationSystem()
        {
            try
            {
                var configPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Configuration",
                    "appsettings.json"
                );

                // Get secure passphrase (from environment or DPAPI)
                var passphrase = GetConfigurationPassphrase();

                _configurationManager = new ConfigurationManager(
                    configPath,
                    encryptionPassphrase: passphrase,
                    autoSaveDebounceMs: 2000
                );

                // Automatically discover and preload all configuration models
                PreloadAllConfigurationModels();

                // Save immediately to create the file with all models
                _configurationManager.SaveNow();
                Console.WriteLine($"[ConfigurationSystem] All models saved to: {configPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigurationSystem] Failed to initialize: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the encryption passphrase for ConfigurationManager.
        /// Priority: Environment Variable -> Default (no encryption for dev).
        /// </summary>
        private string GetConfigurationPassphrase()
        {
            // Option 1: From environment variable (recommended for production)
            var passphrase = Environment.GetEnvironmentVariable("PRINTMATE_CONFIG_KEY", EnvironmentVariableTarget.User);
            if (!string.IsNullOrEmpty(passphrase))
            {
                Console.WriteLine("[ConfigurationSystem] Using passphrase from environment variable");
                return passphrase;
            }

            // Option 2: No encryption (development only)
            Console.WriteLine("[ConfigurationSystem] WARNING: Encryption disabled (no passphrase)");
            return string.Empty;

            // Option 3: DPAPI (Windows) - uncomment to use machine-bound encryption
            // return GetDPAPIProtectedPassphrase();
        }

        /// <summary>
        /// Automatically discovers and preloads all configuration models that inherit from ConfigurationModelBase.
        /// This ensures all models are saved to the configuration file even if they haven't been explicitly used yet.
        /// </summary>
        private void PreloadAllConfigurationModels()
        {
            Console.WriteLine("[ConfigurationSystem] Discovering configuration models...");

            // Find all types in current assembly that inherit from ConfigurationModelBase
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var configModelTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ConfigurationModelBase)))
                .ToList();

            Console.WriteLine($"[ConfigurationSystem] Found {configModelTypes.Count} configuration model(s)");

            // Use reflection to call Get<T>() for each discovered type
            var getMethod = typeof(ConfigurationManager).GetMethod("Get");

            foreach (var modelType in configModelTypes)
            {
                try
                {
                    var genericMethod = getMethod.MakeGenericMethod(modelType);
                    var model = genericMethod.Invoke(_configurationManager, null);
                    Console.WriteLine($"  - {modelType.Name} loaded");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  - WARNING: Failed to load {modelType.Name}: {ex.Message}");
                }
            }

            Console.WriteLine($"[ConfigurationSystem] Preloading completed");
        }
    }
}
