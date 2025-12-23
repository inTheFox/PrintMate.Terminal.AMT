# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PrintMate.Terminal is a Windows-based industrial HMI (Human-Machine Interface) for controlling metal additive manufacturing (3D printing) equipment. It's a fullscreen WPF application (1920x1080) designed for ATM16/ATM32 laser-based metal 3D printers.

**Technology Stack:**
- .NET 9.0 / C# (WPF + Windows Forms)
- MVVM architecture using Prism.DryIoc 8.1.97
- Entity Framework Core 9.0 with SQLite
- OPC UA for PLC/industrial equipment communication
- SignalR for real-time web communication

## Common Commands

### Build and Run
```bash
# Restore dependencies for the entire solution
dotnet restore "C:\Users\inTheFox\Documents\GitHub\PrintMate.Terminal\PrintMate.NET.sln"

# Build the solution (Debug configuration)
dotnet build "C:\Users\inTheFox\Documents\GitHub\PrintMate.Terminal\PrintMate.NET.sln" --configuration Debug

# Build for specific hardware platforms
dotnet build --configuration ATM16_Debug
dotnet build --configuration ATM32_Debug

# Run the terminal application
dotnet run --project "C:\Users\inTheFox\Documents\GitHub\PrintMate.Terminal\PrintMate.Terminal\PrintMate.Terminal.csproj"
```

### Database Migrations
```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project PrintMate.Terminal.csproj

# Apply migrations (happens automatically on app startup via Bootstrapper.cs:60)
dotnet ef database update --project PrintMate.Terminal.csproj

# Remove last migration
dotnet ef migrations remove --project PrintMate.Terminal.csproj
```

Database file location: `{AppBaseDirectory}/database/printmate.db`

## Architecture Overview

### Application Bootstrapping

Entry point: [App.xaml.cs](App.xaml.cs) → [Bootstrapper.cs](Bootstrapper.cs)

The `Bootstrapper` class (Prism.DryIoc) orchestrates the startup sequence:

1. **DI Container Configuration** ([Bootstrapper.cs:120-196](Bootstrapper.cs#L120-L196))
   - Registers all views for navigation
   - Registers services (Singleton: LoggerService, PingObserver, MonitoringManager, DialogService, ProjectsService, etc.)
   - Registers OPC communication layer (ILogicControllerProvider, ILogicControllerObserver)

2. **Database Initialization** ([Bootstrapper.cs:57-62](Bootstrapper.cs#L57-L62))
   - Runs EF Core migrations asynchronously on startup
   - Creates database if it doesn't exist

3. **Configuration Loading** ([Bootstrapper.cs:66-72](Bootstrapper.cs#L66-L72))
   - Loads `{AppBaseDirectory}/Configuration/atmConfig.json`
   - Manages PlcSettings, LaserSettings, ScanatorSettings

4. **Network Observers** ([Bootstrapper.cs:76-91](Bootstrapper.cs#L76-L91))
   - Starts 5 concurrent ping observers (PLC, Laser1, Laser2, Scanator1, Scanator2)
   - Monitors network connectivity to industrial equipment

### MVVM + Prism Region Navigation

The application uses **Prism's region-based navigation** for modular UI composition:

**Shell Structure** ([MainWindow.xaml](MainWindow.xaml)):
```
MainWindow (1920x1080 fullscreen, frameless)
└── RootContainer
    ├── LeftBarRegion → LeftBarView (navigation sidebar)
    ├── MainRegion → Dynamic content (WelcomeView, ProjectsView, ManualControl, etc.)
    └── RightBarRegion → RightBarView (status indicators)
```

**Navigation Example:**
```csharp
// In ViewModels, inject IRegionManager
_regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(ProjectsView));
```

**Key Regions:**
- `LeftBarRegion` - Main navigation menu
- `MainRegion` - Primary content area
- `RightBarRegion` - Hardware status/monitoring
- `ManualContent` - Manual control sub-region
- `ConfigureTemplateRegion` - Configuration views

### Industrial Communication Layer

**OPC UA Architecture:**

The application communicates with PLCs and industrial equipment via OPC UA protocol:

- **ILogicControllerProvider** ([Opc/ILogicControllerProvider.cs](Opc/ILogicControllerProvider.cs))
  - Abstraction for OPC UA client operations
  - Default implementation: `LogicControllerService` (direct OPC UA)
  - Alternative: `LogicControllerWebProxy` (SignalR-based proxy, commented out in [Bootstrapper.cs:172-173](Bootstrapper.cs#L172-L173))

- **ILogicControllerObserver** ([Opc/LogicControllerObserver.cs](Opc/LogicControllerObserver.cs))
  - Polling-based observer (500ms interval)
  - Subscribes to hardware tags (Bool/Dint/Real/Unsigned)
  - Publishes OnOpcDataUpdateEvent via Prism EventAggregator

**Hardware Configuration:**
- OPC tags defined in [Opc/Dataset/*.xml](Opc/Dataset/)
- ViewModels subscribe to tag updates via callbacks

### Services Architecture

All services are singleton unless noted:

**Core Services:**
- **DialogService** ([Services/DialogService.cs](Services/DialogService.cs)) - Modal dialog management with animations
- **ProjectsService** ([Services/ProjectsService.cs](Services/ProjectsService.cs)) - CRUD operations for Projects table
- **UserService** ([Services/UserService.cs](Services/UserService.cs)) - User management (Scoped)
- **LoggerService** ([Services/LoggerService.cs](Services/LoggerService.cs)) - Application logging
- **KeyboardService** ([Services/KeyboardService.cs](Services/KeyboardService.cs)) - On-screen keyboard for touch displays

**Hardware Services:**
- **MonitoringManager** ([Services/MonitoringManager.cs](Services/MonitoringManager.cs)) - Groups hardware signals (Gas, Laser, Axes, Powder)
- **MultiLaserSystemService** ([Services/MultiLaserSystemService.cs](Services/MultiLaserSystemService.cs)) - Coordinates dual laser systems
- **PingObserver** ([Services/PingObserver.cs](Services/PingObserver.cs)) - Network connectivity monitoring

**Project Management:**
- **ProjectManager** ([Parsers/ProjectManager.cs](Parsers/ProjectManager.cs)) - Manages CLI file parsing
- **CliProvider** ([Parsers/CliParser/](Parsers/CliParser/)) - Parses .CLI laser job files asynchronously

### Event Aggregation Pattern

Prism's `IEventAggregator` is used for loosely-coupled communication:

**Key Events** ([Events/](Events/)):
- `OnOpcDataUpdateEvent` - OPC tag value changed
- `MarkingProgressChangedEvent` - Laser marking progress
- `MarkingCompletedEvent` - Laser job completed
- `OnProjectAnalyzeFinishEvent` - CLI file parsing complete
- `OnKeyboardLangChangeEvent` - Input language changed

**Usage:**
```csharp
// Subscribe
_eventAggregator.GetEvent<OnOpcDataUpdateEvent>().Subscribe(OnDataUpdate);

// Publish
_eventAggregator.GetEvent<OnProjectAnalyzeFinishEvent>().Publish(projectInfo);
```

### Dialog Service

**DialogService** ([Services/DialogService.cs](Services/DialogService.cs)) - Оптимизированный сервис для модальных окон с буферизацией

**Features:**
- Window pooling - переиспользование окон по типу View
- Анимированные переходы (fade-in/scale с easing)
- Статический метод Close() для закрытия из любого места
- Автоматическая блокировка взаимодействия во время анимации

**Usage:**
```csharp
// Показать модальное окно (блокирующее)
var result = _dialogService.ShowDialog<MyView, MyViewModel>();
if (result.IsSuccess)
{
    var data = result.Result.SomeProperty;
}

// Показать немодальное окно
_dialogService.Show<MyView, MyViewModel>();

// Закрыть текущее окно статически (из любого места кода)
DialogService.Close();

// Передача параметров в ViewModel
var options = new Dictionary<string, object>
{
    { "ProjectName", "MyProject" },
    { "UserId", 123 }
};
_dialogService.ShowDialog<MyView, MyViewModel>(options);

// Очистка буфера View (обычно не требуется, но полезно при утечках памяти)
DialogService.ClearViewPool();
```

**View Pooling:**
- Первое открытие View создаёт UserControl и сохраняет в буфер (ViewPool)
- Последующие открытия того же View переиспользуют сохранённый UserControl
- Window создаётся каждый раз новое (легковесная операция)
- Обновляется только DataContext и ViewModel
- Значительное улучшение производительности за счёт переиспользования View

### Configuration System

**ConfigServiceProvider** ([Services/ConfigService/](Services/ConfigService/)) - Custom JSON configuration management

**Features:**
- Type-safe configuration collections
- Hot-reload support
- Default value management
- File location: `{AppBaseDirectory}/Configuration/atmConfig.json`

**Configuration Classes:**
- [AppConfiguration/PlcSettings.cs](AppConfiguration/PlcSettings.cs)
- [AppConfiguration/LaserSettings.cs](AppConfiguration/LaserSettings.cs)
- [AppConfiguration/ScanatorSettings.cs](AppConfiguration/ScanatorSettings.cs)

## Directory Structure

```
PrintMate.Terminal/
├── AppConfiguration/       # Hardware configuration POCOs
├── Database/              # EF Core DbContext (SQLite)
│   └── DatabaseContext.cs # {BaseDir}/database/printmate.db
├── Events/                # Prism EventAggregator events
├── Hans/                  # Hans laser integration
├── Opc/                   # OPC UA communication layer
│   ├── Dataset/          # XML tag definitions
│   └── LogicControllerObserver.cs
├── Parsers/              # Project file parsers
│   ├── ProjectManager.cs
│   └── CliParser/       # .CLI format parser
├── Services/            # Application services (DI-registered)
│   ├── ConfigService/  # JSON configuration management
│   ├── DialogService.cs
│   ├── ProjectsService.cs
│   └── MonitoringManager.cs
├── ViewModels/         # MVVM ViewModels
│   ├── Configure/     # Configuration screens
│   ├── ModalsViewModels/ # Dialog ViewModels
│   └── ProjectsViewViewModel.cs
└── Views/             # WPF XAML views
    ├── MainWindow.xaml (1920x1080 fullscreen shell)
    ├── Configure/
    ├── Modals/
    └── RootContainer.xaml
```

## Important Implementation Patterns

### Adding a New View/ViewModel

1. Create View (XAML) in `Views/` and ViewModel in `ViewModels/`
2. Register in [Bootstrapper.cs:120-196](Bootstrapper.cs#L120-L196):
   ```csharp
   containerRegistry.RegisterForNavigation<MyView, MyViewModel>();
   ```
3. Navigate via IRegionManager:
   ```csharp
   _regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(MyView));
   ```

### Adding a New Service

1. Create service class in `Services/`
2. Register in [Bootstrapper.cs:163-194](Bootstrapper.cs#L163-L194):
   ```csharp
   containerRegistry.RegisterSingleton<MyService>();
   // or
   containerRegistry.RegisterScoped<MyService>();
   ```
3. Inject via constructor in ViewModels

### Working with OPC Tags

1. Subscribe to tag updates in ViewModel:
   ```csharp
   var observer = _container.Resolve<ILogicControllerObserver>();
   observer.SubscribeOnTag(myCommandInfo, OnTagValueChanged);
   ```
2. Tag definitions in [Opc/Dataset/*.xml](Opc/Dataset/)

### Opening Modal Dialogs

Use DialogService ([Services/DialogService.cs](Services/DialogService.cs)):
```csharp
await _dialogService.ShowDialogAsync(
    nameof(MyModalView),
    new DialogParameters { { "param", value } }
);
```

### Adding Database Entities

1. Add DbSet to [Database/DatabaseContext.cs](Database/DatabaseContext.cs)
2. Create migration: `dotnet ef migrations add MyMigration`
3. Migration runs automatically on next app startup

## Multi-Project Solution

This is part of a larger solution (`PrintMate.NET.sln`) with 15 projects:

**Referenced Libraries:**
- **Hans.NET** - Hans laser control library
- **LaserLib** - Laser operations
- **Opc2Lib** - OPC UA PLC communication
- **OpcWebShared** - SignalR web proxy

**Debugger Projects:**
- `HansDebugger` - Hans laser debugging tools
- `LaserDebugger` - Laser system testing
- `OpcDebugger` - OPC communication testing

## Platform-Specific Notes

- **Windows-only** (WPF dependency)
- **Fixed resolution:** 1920x1080 hardcoded
- **Fullscreen frameless window** (AllowsTransparency=true, WindowStyle=None)
- **DEBUG builds** allocate a console window for logging (P/Invoke kernel32.dll)
- **Hardware builds:** ATM16_Debug and ATM32_Debug configurations for different printer models
