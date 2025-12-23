# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PrintMate.Terminal is an industrial HMI (Human-Machine Interface) application for controlling metal additive manufacturing equipment (ATM16/ATM32 laser-based 3D printers). It's a fullscreen Windows desktop application that communicates with PLCs and laser systems in real-time.

**Technology Stack:**
- .NET 9.0 / C# with WPF + Windows Forms hybrid
- MVVM architecture using Prism.DryIoc 8.1.97
- Entity Framework Core 9.0 with SQLite
- OPC UA for industrial equipment communication
- Material Design themes and HandyControls UI framework

**Platform Requirements:**
- Windows-only (uses WPF and P/Invoke for console)
- Fixed 1920x1080 fullscreen resolution
- .NET 9.0 runtime

## Common Commands

### Build & Run
```bash
# Restore dependencies
dotnet restore PrintMate.NET.sln

# Build solution (Debug)
dotnet build PrintMate.NET.sln --configuration Debug

# Hardware-specific builds
dotnet build --configuration ATM16_Debug  # For ATM16 printer
dotnet build --configuration ATM32_Debug  # For ATM32 printer

# Run the main application
dotnet run --project PrintMate.Terminal\PrintMate.Terminal.csproj
```

### Database Migrations
```bash
# Add new migration
dotnet ef migrations add <MigrationName> --project PrintMate.Terminal\PrintMate.Terminal.csproj

# Apply migrations (happens automatically on startup)
dotnet ef database update --project PrintMate.Terminal\PrintMate.Terminal.csproj

# Remove last migration
dotnet ef migrations remove --project PrintMate.Terminal\PrintMate.Terminal.csproj
```

Database location: `{AppBaseDirectory}/database/printmate.db`

## Architecture

### Application Bootstrap Flow

```
App.xaml.cs (OnStartup)
  └─> Bootstrapper.cs (Prism bootstrapper)
      ├─> DI Container Setup (~120 service registrations)
      ├─> Database Migration (EF Core - line 60)
      ├─> Configuration Loading (atmConfig.json from Configuration/)
      └─> Network Observers (PLC, Laser, Scanator ping monitoring)
```

### Shell Structure

The application uses a region-based navigation system with a frameless fullscreen window:

```
MainWindow (1920x1080 fullscreen, no frame)
└── RootContainer
    ├── LeftBarRegion → Navigation sidebar
    ├── MainRegion → Dynamic content area (primary view)
    └── RightBarRegion → Hardware status indicators
```

Region constants are defined in `Bootstrapper.cs`:
- `Bootstrapper.LeftBarRegion`
- `Bootstrapper.MainRegion`
- `Bootstrapper.RightBarRegion`
- `Bootstrapper.ManualContent`
- `Bootstrapper.ConfigureTemplateRegion`

### Key Architectural Layers

1. **Presentation Layer (MVVM + Prism Regions)**
   - Views (XAML) and ViewModels follow MVVM pattern
   - Navigation via `IRegionManager.RequestNavigate()`
   - Custom animated controls (`AnimatedContentControl`)

2. **Service Layer** (registered in Bootstrapper.cs)
   - `LogicControllerService` - OPC UA communication with PLC
   - `MonitoringManager` - Hardware signal grouping and monitoring
   - `ProjectManager` - CLI/CNC file parsing and project management (supports both single .cli files and CNC project folders)
   - `DialogService` - Modal dialog management with view pooling
   - `ConfigServiceProvider` - JSON configuration system
   - `PingObserver` - Network connectivity monitoring (PLC, lasers, scanators)

3. **Industrial Communication Layer**
   - `ILogicControllerProvider` abstraction for PLC communication
   - `LogicControllerObserver` - 500ms polling cycle for OPC UA tags
   - Event-driven architecture via Prism's `IEventAggregator`
   - Network monitoring with automatic reconnection

4. **Data Layer**
   - `DatabaseContext` - EF Core DbContext
   - SQLite database with automatic migrations on startup
   - Entities: Users, Projects

### Multi-Project Solution

The solution contains 20+ projects organized by function:

**Core Libraries:**
- `Hans.NET` - P/Invoke wrappers for Hans GMC scanner SDK (HM_HashuScan.dll, HM_UDM_DLL.dll)
- `LaserLib` - Laser communication abstractions
- `Opc2Lib` - OPC UA client library
- `Configurator` - Configuration management

**Web Services:**
- `OpcProvider` - Remote OPC UA provider
- `OpcLocalProvider` - Local OPC UA provider
- `OpcWebShared` - Shared web communication models

**Debug Tools:**
- `HansDebugger` - Laser system debugger
- `LaserDebugger` - Laser communication debugger
- `OpcDebugger` - OPC UA debugger
- `HansConsoleDebugger` - Console-based Hans system debugging
- `Opc2Console` - Console-based OPC UA debugging

**Proxy Services:**
- `PlcProxy` - PLC communication proxy
- `ProxyShared` - Shared proxy models

**Parser Libraries:**
- `ProjectParser` - Base parser interfaces and models
- `ProjectParser.CliProvider` - CLI file format parser

**Test Projects:**
- `ParserTest` - Parser testing console app (.NET 8.0)
- `ProjectParserTest` - Project parser unit tests
- `ConfigTest` - Configuration system tests

**Main Applications:**
- `PrintMate.Terminal` - Main HMI application (.NET 9.0)
- `PrintMateApplication` - Additional application
- `PrintMateWeb` - Web interface

## Critical Patterns

### 1. Prism Region Navigation
```csharp
// Navigate to a view
_regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(ProjectsView));

// Navigate with parameters
var parameters = new NavigationParameters();
parameters.Add("key", value);
_regionManager.RequestNavigate(Bootstrapper.MainRegion, nameof(MyView), parameters);
```

### 2. Event Aggregation (Loose Coupling)
```csharp
// Subscribe to events
_eventAggregator.GetEvent<OnOpcDataUpdateEvent>().Subscribe(OnDataUpdate);

// Publish events
_eventAggregator.GetEvent<OnProjectAnalyzeFinishEvent>().Publish(projectInfo);
```

### 3. Dialog Service with View Pooling
The `DialogService` reuses UserControl instances for performance:
```csharp
// Show dialog and get result
var result = _dialogService.ShowDialog<MyView, MyViewModel>();

// Close dialog from anywhere (static method)
DialogService.Close();
```

### 4. Configuration System
Configuration uses strongly-typed POCO classes that inherit from `CollectionBase`:
```csharp
public class PlcSettings : CollectionBase
{
    public string Address = "172.18.34.57";
    public int Port = 4840;
}
```

Configuration file: `Configuration/atmConfig.json` (created automatically if missing)

### 5. OPC UA Tag Subscription
```csharp
// Subscribe to tag updates
observer.SubscribeOnTag(myCommandInfo, OnTagValueChanged);

// Tags are polled at 500ms intervals by LogicControllerObserver
```

### 6. Service Registration Pattern
All services are registered in `Bootstrapper.cs`:
- **Singletons** - Infrastructure services (`LoggerService`, `MonitoringManager`)
- **Scoped** - User-specific services (`UserService`)
- **Transient** - Short-lived services

Access the container statically via `Bootstrapper.ContainerProvider`.

### 7. Project File Parsers

The application supports two types of project files:

**CLI Parser** (`Parsers/CliParser/`)
- Parses single `.cli` binary files
- Binary geometry format with header information
- Supports polylines and hatches
- Extracts parts, layers, regions with laser parameters

**CNC Parser** (`Parsers/CncParser/`)
- Parses `.cnc` files (G-code format) in folders
- Multiple CNC files per project (one file = one layer typically)
- Parses G-codes (G0, G1 for movement)
- Parses M-codes (M3/M5 for laser, M702/M704 for power/speed)
- Extracts configuration from comments

**Project Loading Logic** (`ProjectManager.cs`)
```csharp
// CLI: Single .cli file
if (File.Exists(path) && path.EndsWith(".cli"))
    → Copy file, parse with CliProvider

// CNC: Directory with .cnc files
if (Directory.Exists(path))
    → Find all *.cnc files, parse each with CncProvider
    → Combine layers from all files into one project
    → Copy entire directory
```

Both parsers implement `IParserProvider` interface with async parsing and progress events.

### 8. Async/Await Throughout
- CLI/CNC file parsing is fully async
- Database operations use EF Core async methods
- Network operations (ping, OPC) are non-blocking

### 9. Hans Dual Scanner System

The application includes a sophisticated dual-scanner system for controlling Hans GMC laser scanners:

**Core Components** (`PrintMate.Terminal/Hans/`):
- `MultiScanatorSystem` - Windows Forms component for Hans SDK message handling (singleton)
- `ScanatorSystem` - Controls individual Hans scanner (connect, configure, marking operations)
- `UdmBuilder` - Converts parsed CLI projects to UDM binary files for Hans SDK
- `ScanatorConfigurationLoader` - Loads/saves scanner configurations from JSON

**Key Features:**
- **Dual Scanner Coordination** - Synchronous operation of two scanners
- **LaserId-based Splitting** - Automatically distributes work based on LaserId field in CLI files
  - LaserId 0 or 1 → Scanner 1 (172.18.34.227)
  - LaserId 2+ → Scanner 2 (172.18.34.228)
- **Beam Diameter Control** - Calculates Z-offset from target beam diameter using Gaussian beam physics
- **UDM Generation** - Creates binary UDM mark files with proper laser parameters

**Configuration** (`ScanatorConfiguration`):
```csharp
// Scanner configuration includes:
- CardInfo: IP address, board index
- ScannerConfig: Field size, protocol, offsets
- BeamConfig: Beam diameter, Rayleigh length, M² quality factor
- LaserPowerConfig: Max power, correction curves
- ProcessVariablesMap: Speed-dependent parameters (delays, jump speeds)
```

**Usage Pattern:**
```csharp
// 1. Initialize multi-scanner system (Windows Forms for SDK callbacks)
var multiSystem = new MultiScanatorSystem(eventAggregator);

// 2. Load configurations
var configs = ScanatorConfigurationLoader.LoadFromFile("config.json");

// 3. Create scanner systems
var scanner1 = new ScanatorSystem(configs[0]);
var scanner2 = new ScanatorSystem(configs[1]);

// 4. Connect and configure
scanner1.Connect();
scanner1.Configure();

// 5. Download UDM mark file and execute
scanner1.DownloadMarkFile("layer_0000.bin");
scanner1.StartMark();
```

**Important Notes:**
- Hans SDK requires a Windows Forms message loop (handled by `MultiScanatorSystem`)
- UDM files are binary format specific to Hans GMC controllers
- Z-coordinate in UDM controls beam diameter through defocusing
- See `PrintMate.Terminal/Hans/README.md` for detailed documentation

## Directory Structure

```
PrintMate.Terminal/
├── AppConfiguration/     - Hardware settings POCOs
├── Database/            - EF Core DbContext and migrations
├── Events/              - Prism event definitions
├── Hans/                - Hans dual scanner system
│   ├── Models/          - Scanner models and DTOs
│   ├── Events/          - Scanner-related Prism events
│   ├── MultiScanatorSystem.cs - Windows Forms for SDK messages
│   ├── ScanatorSystem.cs      - Individual scanner control
│   ├── UdmBuilder.cs          - CLI to UDM converter
│   ├── ScanatorConfigurationLoader.cs - JSON config management
│   ├── ZCorrectionTest.cs     - Z-offset calculation tests
│   └── README.md              - Detailed Hans system documentation
├── Opc/                 - OPC UA communication layer
│   └── Dataset/         - HardSignal XML configurations
├── Parsers/             - File parsers for project formats
│   ├── CliParser/       - CLI binary format parser
│   │   ├── CliProvider.cs          - Main CLI parser
│   │   ├── CliProviderExtension.cs - Helper methods
│   │   ├── CliSyntax.cs           - CLI format constants
│   │   └── HeaderKeys.cs          - Header parameter keys
│   ├── CncParser/       - CNC G-code format parser
│   │   ├── CncProvider.cs          - Main CNC parser
│   │   ├── CncProviderExtension.cs - Helper methods
│   │   └── CncSyntax.cs           - G-code constants
│   ├── Shared/          - Common models and interfaces
│   │   ├── Models/      - Project, Layer, Region, Part
│   │   ├── Interfaces/  - IParserProvider
│   │   └── Enums/       - ProjectType, GeometryRegion
│   └── ProjectManager.cs - Orchestrates parsing and file copying
├── Services/            - Business logic services
│   └── ConfigService/   - Configuration management
├── ViewModels/          - MVVM ViewModels
├── Views/               - WPF XAML views
│   ├── Configure/       - Configuration views
│   ├── Modals/          - Dialog views
│   └── Pages/           - Main page views
├── Configuration/       - Runtime config (atmConfig.json)
└── images/              - UI resources
```

## Hardware Configurations

The application supports two hardware variants controlled via build configurations:

- **ATM16_Debug** - For ATM16 printer (16-laser system)
- **ATM32_Debug** - For ATM32 printer (32-laser system)

Hardware-specific images and configurations are loaded based on the build configuration.

## Network Communication

### Monitored Devices
The `PingObserver` monitors connectivity to:
- PLC (Programmable Logic Controller)
- Laser 1 and Laser 2
- Scanator 1 and Scanator 2

These observers start automatically in `Bootstrapper.OnInitialized()` (lines 75-91).

### OPC UA Communication
- Server connection configured via `PlcSettings` in `atmConfig.json`
- Default PLC address: `172.18.34.57:4840`
- Tags are defined in XML files: `Opc/Dataset/HardSignal_AMT32.xml`
- Communication happens via `LogicControllerService`

## Testing

The solution includes several test projects:

### Test Projects
- **ParserTest** - Console application for testing CLI/CNC parsers (runs with .NET 8.0)
- **ProjectParserTest** - Unit tests for project parsing logic
- **ConfigTest** - Configuration system testing

### Running Tests
```bash
# Run parser test console app
dotnet run --project ParserTest\ParserTest.csproj

# Run project parser tests
dotnet run --project ProjectParserTest\ProjectParserTest.csproj

# Run config tests
dotnet run --project ConfigTest\ConfigTest.csproj
```

**Note:** These are console applications, not traditional unit test frameworks. They output results to console.

### In-App Testing
The main application includes test code that runs on startup in DEBUG builds:
```csharp
// App.xaml.cs line 92
ZCorrectionTest.RunAllTests();  // Tests Z-offset calculations for beam diameter
```

## Debug Console

In DEBUG builds, a console window is allocated via P/Invoke (`AllocConsole()`) for debugging output. This is Windows-specific and will not work on other platforms. The console window is created with a custom icon and managed through a mutex to prevent duplicate consoles.

## Important Notes

- **Database migrations** run automatically on application startup (Bootstrapper.cs:60)
- **Configuration file** is created automatically if missing
- The application is **frameless fullscreen** - no standard window chrome
- All **OPC UA tags** must be defined in XML dataset files before use
- **Dialog views are pooled** - don't assume fresh state when opening dialogs
- Use **Event Aggregator** for cross-module communication, not direct references
- **Hans.NET native dependencies** - The Hans.NET project contains native DLLs (HM_HashuScan.dll, HM_Comm.dll) that are copied to output directory. These are Windows x64 native libraries from Hans GMC Control Card SDK.
- **Windows Forms + WPF hybrid** - MultiScanatorSystem is a Windows Forms component embedded in a WPF application to handle Hans SDK callbacks
- **Application startup** - In DEBUG builds, `App.xaml.cs` runs `ZCorrectionTest.RunAllTests()` before initializing Bootstrapper (line 92). Comment this out if you need faster startup.
