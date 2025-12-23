# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PrintMateMC is an industrial desktop control system for additive manufacturing (metal 3D printing) machines. The application provides machine control, real-time monitoring, job management, and remote access capabilities for AMTech metal 3D printers.

**Technology Stack:**
- Primary: Java with Swing GUI framework
- Secondary: C# .NET 7.0 CLI parser utility (for `.cli` file processing)
- Database: H2 embedded database
- Protocols: OPC UA, Modbus, S7 (Siemens PLC), MQTT, HTTP/WebSocket

## Build and Deployment

### Building the Application

The project uses **Apache ANT** for building. Main build file: `deploy/DeployANT.xml`

**Build JAR files:**
```bash
ant -f deploy/DeployANT.xml
```

This creates:
- Main application JAR in `deploy/jar/`
- Windows EXE files in `deploy/exe/` via Launch4j
- Multiple variants: AMT16, AMT32, Debug, Emulator, RMI server

**Build outputs:**
- `PrintMateMC_AMT16_*.exe` - 16-bit machine variant
- `PrintMateMC_AMT32_*.exe` - 32-bit machine variant
- `PrintMateMC_AMT16_Debug_*.exe` - Debug console version
- `PrintMateMC_AMT32_Debug_Emulator_*.exe` - Emulator mode
- `PrintMateMC_RMI_*.exe` - RMI admin server

### IDE Setup

The project supports both Eclipse and IntelliJ IDEA:
- Eclipse: `.project`, `.classpath` files present
- IntelliJ: `.iml`, `.idea/` directory present

### Runtime Configuration

JVM arguments are in `_vm_args.txt`:
```
-Demulator=false -Ddebug=false -Dexpert_mode=false -Dwarning_enable=false -XX:+UseParallelGC
```

**Development modes (set via system properties):**
- `-Demulator=true` - Emulator mode
- `-Ddebug=true` - Debug mode with detailed logging
- `-Dexpert_mode=true` - Expert mode with advanced features
- `-Dscanemu=true` - Scanner hardware emulator
- `-Dplcemu=true` - PLC emulator

## Architecture

### Entry Points

1. **Main GUI Application**: `src/project/MainWindow.java`
   - Main class: `project.MainWindow.main(String[] args)`
   - Initializes single-instance lock (`singleInstance.loc`)
   - Bootstraps via `AppLoader` class

2. **RMI Admin Server**: `src/utilities/rmi/RMIAdminServer.java`
   - Remote administration interface on port 1099

### Key Architectural Patterns

- **Card-Based Navigation**: UI uses card panels for navigation (`panels/CardManager.java`, `panels/RootCardPanel.java`)
- **Hardware Abstraction**: `hardware/HardElement.java` provides unified interface for machine hardware elements
- **State Machine**: Machine states managed through state enums and triggers
- **Event-Driven**: Extensive use of listeners and triggering system (`widgets/triggering/`)
- **Singleton Services**: Connection managers, user managers follow singleton pattern

### Core Directory Structure

```
src/
├── project/          - Application entry point and main window
├── panels/           - UI panels (card-based navigation system)
├── widgets/          - Reusable UI components and custom controls
├── dialogs/          - Dialog windows
├── hardware/         - Hardware abstraction layer and element definitions
├── connectors/       - Hardware connectors (PLC, Scanner, UPS, Laser)
├── connect/          - Connection management
├── jobparser/        - Job file parsing (.cli, .CNC, G-Code)
├── jobreport/        - Job reporting and history
├── commands/         - Command management system
├── emulator/         - Hardware emulators for testing
├── remote/           - Remote access (HTTP, WebSocket, cloud)
├── camera/           - Camera integration
├── credentials/      - User authentication and management
├── licensing/        - License validation
├── logging/          - Application logging framework
├── utilities/        - Utility classes and helpers
├── locale/           - Internationalization (i18n) resources
└── properties/       - Configuration property files
```

### Machine Hardware Integration

The application interfaces with:
- **Scanner System**: Hans scanner via native DLLs (`libs/Scanner/Hans/`)
  - `Hashu4Java_64.dll`, `HM_Comm.dll`, `HM_HashuScan.dll`
  - Configuration: `libs/Scanner/Hans/system.ini`
- **PLC Systems**: OPC UA, Modbus, Siemens S7 (Moka7)
- **Laser Monitoring**: IPG laser systems (`libs/LaserMonitoring/IPG/`)
- **Cameras**: Webcam capture for monitoring
- **UPS**: Power monitoring systems

## Testing and Emulation

**No dedicated test framework**, but comprehensive emulation capabilities exist:

### Running Emulators

**PLC Emulators:**
- `src/emulator/modbus/ModbusPLCEmulator.java` - Modbus protocol
- `src/emulator/MokaPLCEmulator.java` - Siemens S7
- `src/emulator/opc/` - OPC UA server emulation

**Scanner Emulator:**
- `src/emulator/scannator/ScannerInputPanel.java`

**Launch with emulators:**
```bash
java -Dplcemu=true -Dscanemu=true -jar PrintMateMC.jar
```

### RMI Testing
- Test client: `src/utilities/rmi/RMITestClient.java`

## Configuration and Logging

### Logging Configuration

Log4j 2 configuration: `src/log4j2.xml`

**Log locations (relative to user home):**
- `~/AMTech/debug/` - Debug logs (WARN level and above, 10MB rolling)
- `~/AMTech/messages/` - Message logs (INFO level, 10MB rolling)
- `~/AMTech/printDebug/` - Print debug logs (100MB rolling)

**Log levels:**
- Console: ERROR and above
- File appender: WARN and above
- Messages: INFO and above

### Configuration Files

Property files in `src/properties/`:
- `machine.default.properties` - Machine configuration defaults
- `user.default.properties` - User settings
- `connection.default.properties` - Connection settings

**Runtime data storage:**
- H2 database for persistent data
- Single instance lock: `singleInstance.loc`
- User data directory: `~/AMTech/`

## Machine Variants

The application supports two machine types:
- **AMT16**: 16-bit variant
- **AMT32**: 32-bit variant

Machine type is set via system property `-Dmachine=AMT16` or `-Dmachine=AMT32`

## Job File Formats

The application processes multiple job file formats:
- **`.CNC`** - Custom print file format (sample files in `print_files/`)
- **`.cli`** - CLI format (processed by C# parser utility)
- **G-Code** - Standard G-Code support

### CLI Parser (C# Utility)

Separate C# .NET 7.0 console application for parsing `.cli` files:
- Entry point: `src_cs/Program.cs`
- Core parsing: `src_cs/Core/CliJobParser.cs`, `src_cs/Core/CliLayerBuilder.cs`
- Extracts geometry, layers, and metadata from CLI files

## Remote Access

The application provides multiple remote access methods:
- **HTTP Server**: Jetty 11.0.x embedded server
- **WebSocket**: Real-time bidirectional communication
- **MQTT**: Message broker integration (Paho client)
- **RMI**: Remote method invocation for administration
- **mDNS**: Service discovery (JmDNS)

## User Management

Multi-user support with:
- User authentication (`src/credentials/UserManager.java`)
- Role-based permissions
- License validation (`src/licensing/LicenseStorage.java`)

## UI Customization

Custom Swing UI components in `src/widgets/uis/`:
- `ClickableButtonUI`, `ClickableToggleButtonUI` - Touch-friendly buttons
- `TouchComboBoxUI`, `TouchSliderUI` - Touch-optimized controls
- `TouchScrollBarUI` - Custom scrollbars
- `TouchTableHeaderUI` - Table headers
- Custom borders: `src/borders/InternalBorderFactory.java`

Color palette: `src/utilities/Palette.java`
Icons: `src/utilities/Icons.java`

## Internationalization

Locale support in `src/locale/`:
- Russian locale: `new Locale("ru")`
- English locale: `Locale.forLanguageTag("en-US")`
- Text resources: `src/widgets/gui/TextLocale.java`

## Key Libraries

Major third-party dependencies (in `libs/`):
- **OPC UA**: Industrial protocol client (0.6.13)
- **Modbus**: PLC communication (jlibmodbus 1.2.9.11)
- **Jackson**: JSON processing (2.17.1)
- **PDFBox**: PDF generation (3.0.1)
- **Apache Batik**: SVG rendering (1.17)
- **H2 Database**: Embedded database (2.3.232)
- **Jetty**: Web server (11.0.x)
- **Log4j 2**: Logging (2.23.1)
- **OpenCV**: Computer vision (custom extension)
- **JNA**: Native library access (5.16.0)

## Core System Architecture Details

### State Management System

**System States** (`project.SYSTEM_STATE` enum):
- `IDLE` (0): Machine idle, no active process
- `CONDITIONING` (1): Chamber conditioning (atmosphere preparation)
- `PRE_PRINT` (2): Pre-print preparation (powder spreading, heating)
- `PRINT` (4): Active printing
- `PRINT_PAUSE` (8): Print paused (can resume)
- `RE_PRINT_ACTION` (16): Re-print action in progress
- `POST_PRINT` (32): Post-print operations (cooldown, cleanup)

**State codes are hierarchical**: Higher codes = later stages in process workflow.

### Connection Management

**ConnectManager Singleton** (`connect.ConnectManager`):
- Manages all hardware connections (PLC, Scanner, Laser, UPS)
- **100ms update loops**:
  - GUI update loop: Updates UI elements
  - PLC transaction loop: Reads/writes PLC data
- **Connection sequence**:
  1. PLC connects first
  2. On first successful PLC connection triggers:
     - Scanner system startup (`ScanSystemConnector.startScannator()`)
     - Laser system hotstart check
     - UPS system hotstart check
     - HTTP remote server startup
     - Camera capture initialization

**Key methods**:
- `ConnectManager.getManager()`: Get singleton instance
- `isPLCConnected()`: Check PLC connection status
- `isScannerConnected()`: Check scanner connection status
- `isHardwareConnected()`: Check both PLC and scanner
- `sendHardState(element, state)`: Send hardware state to PLC
- `resetPLCConnection()`: Reinitialize PLC connection

### Hardware Element System

**HardElement** (`hardware.HardElement`):
- Base abstraction for all machine hardware elements
- **Data Types**: `Bool`, `Real`, `DInt`, `Int`, `Unsigned`, `String`
- **Data Severity**: `INFO`, `WARNING`, `ERROR`, `CRITICAL`

**Hardware Categories**:
- `PCHAMBER_CAT`: Process chamber elements
- `GASFILTER_CAT`: Gas filtration system
- `AXES_CAT`: Motion axes
- `LASER_CAT`: Laser system
- `POWDER_CAT`: Powder delivery system

**Hardware Groups** (communication direction):
- `Com`: Send boolean signals (commands to PLC)
- `Trig`: Receive boolean signals (triggers from PLC)
- `Am`: Receive analog/float values (measurements)
- `Setpoints`: Send float values (setpoints to PLC)
- `SCP`, `DISP`, `DM`: Display/diagnostic groups
- `Errors`, `Alarms`: Error and alarm handling

**HardElementState**:
- Holds current value and metadata for each hardware element
- Synchronized with PLC via ConnectManager
- Triggers UI updates via listener pattern

### Job Parsing System

**IJobParser Interface** (`jobparser.IJobParser`):
- `parseHeader(File)`: Parse job file header, return `JobBuilder`
- `parseGeometry(JobBuilder)`: Parse geometry data (async via SwingWorker)

**Implementations**:
1. **CLI Parser** (`jobparser.cli.CliJobParser`):
   - Parses `.cli` files (Common Layer Interface format)
   - Extracts layers, geometry, process parameters
   - C# alternative: `src_cs/Core/CliJobParser.cs` (.NET 7.0)

2. **G-Code Parser** (`jobparser.gcode.GCodeParser`):
   - Parses standard G-Code files
   - Converts toolpaths to internal format

**JobBuilder**:
- Accumulates parsed data from job files
- Methods: `getLayerCount()`, `getLayerThickness()`, `getName()`, etc.

### Event-Driven Triggering System

**Timer-based triggers** (`widgets.triggering/`):
- `Timer100ms`: 100ms periodic events for fast UI updates
- `Timer1s`: 1-second periodic events for slower updates
- All UI components and state managers subscribe to these timers

**Listener Pattern**:
- `IPLCState`: Interface for PLC state updates (`receiveState()`)
- `ISystemStateListener`: Interface for system state changes
- Components implement listeners to react to hardware/state changes

### Card-Based UI Navigation

**Navigation System**:
- `panels.RootCardPanel`: Root container for all card panels
- `panels.CardManager`: Manages card switching and navigation stack
- Navigation via `NavigationButton` widgets

**Main Card Groups**:
- Print panel: Active print monitoring and control
- Jobs panel: Job selection and management
- Configuration panel: Machine settings and calibration
- Process panel: Process parameters and recipes
- Manual panel: Manual machine control

Each panel follows card-based navigation pattern for hierarchical UI organization.

## Development Workflow

When modifying the codebase:

1. **Hardware Elements**: Define in XML, parsed by `src/hardware/XmlParser.java`
   - Add hardware signals to `utilities.Signals.HARD_SIGNAL` enum
   - Define data type, address, and group in XML
   - ConnectManager automatically synchronizes with PLC

2. **UI Panels**: Extend card-based navigation system in `src/panels/`
   - Extend `BaseGroupPanel` or similar base class
   - Register panel in `CardManager`
   - Implement `IPLCState` if panel needs PLC updates

3. **Connectors**: Implement in `src/connectors/` following existing patterns
   - Extend base connector interface
   - Implement connection lifecycle (connect, check, trans, disconnect)
   - Register with `ConnectManager` for startup sequence

4. **State Management**: Use `hardware/HardElement.java` for hardware state
   - Create HardElement instances for new hardware
   - Use `sendHardState()` to command hardware
   - Subscribe to state changes via listeners

5. **Logging**: Use appropriate logger from `src/logging/`
   - `CommonLogger`: General application logs
   - `DebugLogger`: Debug-level technical logs
   - `MessagesLogger`: User-facing messages and events
   - `PrintDebugLogger`: Print process detailed logs

6. **Internationalization**: Add text resources to locale bundles
   - Add keys to `src/locale/messages.properties`
   - Add translations to `messages_ru.properties`, `messages_en_US.properties`
   - Reference via `TextLocale.getInstance().getSoftString(key)`

7. **Job Parsers**: Implement `IJobParser` for new file formats
   - Implement `parseHeader()` for metadata extraction
   - Implement `parseGeometry()` as async SwingWorker
   - Return `JobBuilder` with complete job data

## Scanner System Integration

### Hans Scanner JSON Configuration

The application uses a comprehensive JSON-based configuration system for Hans scanners. Configuration files define:

**Scanner Card Configuration** (`ScannerConfigExamples.cs` provides C# examples):

1. **Card Info**: Network configuration
   - `ipAddress`: Scanner card IP (e.g., "172.18.34.227")
   - `seqIndex`: Card index for multi-laser systems (0, 1, 2...)

2. **Beam Config**: Optical parameters for focus calculations
   - `focalLengthMm`: F-theta lens focal length (e.g., 538.46 mm)
   - `minBeamDiameterMicron`: Minimum spot diameter at focus (e.g., 48.141 μm)
   - `m2`: Beam quality factor (1.0 = ideal Gaussian, typically 1.1-1.3)
   - `rayleighLengthMicron`: Depth of focus (e.g., 1426.715 μm)
   - `wavelengthNano`: Laser wavelength (e.g., 1070.0 nm for Yb fiber laser)

3. **Process Variables Map**: Speed-dependent parameters
   - `markSpeed`: Array of parameter sets for different scanning speeds (800, 1250, 2000 mm/s)
   - Each set includes:
     - `curBeamDiameterMicron`: Beam diameter for defocusing
     - `curPower`: Laser power in Watts
     - `markSpeed`, `jumpSpeed`: Scanning and jump speeds (mm/s)
     - `laserOnDelay`, `laserOffDelay`: Laser switching delays (nanoseconds)
     - `jumpDelay`, `polygonDelay`, `markDelay`: Timing delays (nanoseconds)
     - `swenable`: SkyWriting mode (laser stays on between segments)

4. **Scanner Config**: Field calibration
   - `fieldSizeX`, `fieldSizeY`: Scanning field size (mm)
   - `offsetX`, `offsetY`, `offsetZ`: Calibration offsets for multi-laser alignment
   - `scaleX`, `scaleY`, `scaleZ`: Scale factors for field correction
   - `rotateAngle`: Field rotation compensation (degrees)

5. **Third Axis Config**: Field curvature correction
   - Formula: `Z_correction = A×r² + B×r + C`
   - Where `r = sqrt(X² + Y²)` (distance from field center)
   - `afactor`: Quadratic coefficient (typically 0.0)
   - `bfactor`: Linear coefficient (e.g., 0.0139)
   - `cfactor`: Constant offset (e.g., -7.5 mm)

6. **Laser Power Config**: Power linearization
   - `maxPower`: Maximum laser power (Watts)
   - `actualPowerCorrectionValue`: Lookup table for power correction
   - `powerOffsetKFactor`, `powerOffsetCFactor`: Calibration coefficients

7. **Function Switcher Config**: Enable/disable corrections
   - `enableDiameterChange`: Allow dynamic beam diameter changes
   - `enableZCorrection`: Enable field curvature correction
   - `enablePowerCorrection`: Enable power linearization
   - `enableDynamicChangeVariables`: Allow real-time parameter changes

### Beam Diameter and Z-Offset Relationship

**Converting CLI diameter parameters to Hans Z-offset:**

CLI files specify `laser_beam_diameter` in microns. The scanner achieves different spot sizes via Z-axis defocusing:

**Formula**: `Z = (diameter - nominalDiameter) / 10.0 × coefficient`

Where:
- `nominalDiameter`: Beam diameter at Z=0 (typically 48-120 μm depending on optics)
- `coefficient`: Optical system coefficient (typically 0.2-0.4 mm/10μm for 538mm lens)

**Example for 538.46mm F-theta lens:**
- Nominal diameter: 120 μm (at Z=0)
- Coefficient: 0.3 mm/10μm
- For CLI diameter 80 μm: `Z = (80 - 120) / 10.0 × 0.3 = -1.2 mm`
- For CLI diameter 140 μm: `Z = (140 - 120) / 10.0 × 0.3 = +0.6 mm`

**Calibration procedure:**
1. Generate test file with multiple Z values (e.g., -0.6, -0.3, 0.0, +0.3, +0.6 mm)
2. Print test patterns and measure line widths with microscope
3. Calculate: `nominalDiameter` = width at Z=0
4. Calculate: `coefficient = ΔZ / (Δdiameter / 10)`

### Hans Scanner API Integration

**Key API points** (via `Hans4Java.jar` JNI wrapper):

**File generation workflow:**
```java
// 1. Initialize
HM_UDM_DLL.UDM_NewFile();
HM_UDM_DLL.UDM_SetProtocol(0, 1); // Protocol 0, Mode 1 (3D mode)

// 2. Set layer parameters
MarkParameter[] layers = new MarkParameter[1];
layers[0].MarkSpeed = 800; // mm/s
layers[0].LaserPower = 50.0f; // Convert W to % based on maxPower
layers[0].JumpSpeed = 5000;
// ... other parameters
HM_UDM_DLL.UDM_SetLayersPara(layers, layerCount);

// 3. Add geometry with Z for diameter control
structUdmPos[] points = new structUdmPos[] {
    new structUdmPos { x = 0, y = 0, z = zOffset },
    new structUdmPos { x = 10, y = 10, z = zOffset }
};
HM_UDM_DLL.UDM_AddPolyline3D(points, pointCount, layerIndex);

// 4. Generate and save
HM_UDM_DLL.UDM_Main();
HM_UDM_DLL.UDM_SaveToFile("output.bin");
HM_UDM_DLL.UDM_EndMain();
```

**Native DLL locations:**
- `libs/Scanner/Hans/Hashu4Java_64.dll`
- `libs/Scanner/Hans/HM_Comm.dll`
- `libs/Scanner/Hans/HM_HashuScan.dll`
- Configuration: `libs/Scanner/Hans/system.ini`

**Important notes:**
- Protocol 1 (3D mode) is required for Z-axis control (beam diameter changes)
- Z coordinates in `structUdmPos` control defocusing
- Power conversion: `powerPercent = (watts / maxPower) × 100.0`
- All coordinates in millimeters, delays in nanoseconds

### CLI to Hans Translation

When converting CLI files to Hans `.bin` files:

1. **Parse CLI regions** (from `$PARAMETER_SET` sections):
   - `edges_laser_beam_diameter` → Calculate Z for edge scanning
   - `downskin_hatch_laser_beam_diameter` → Calculate Z for downskin
   - `infill_hatch_laser_beam_diameter` → Calculate Z for infill
   - `support_hatch_laser_beam_diameter` → Calculate Z for supports

2. **Map parameters**:
   - CLI `laser_power` (W) → Hans `LaserPower` (%)
   - CLI `laser_scan_speed` (mm/s) → Hans `MarkSpeed`
   - CLI geometry → Hans `UDM_AddPolyline3D` with calculated Z

3. **Select process variables**:
   - Find closest matching speed in `processVariablesMap.markSpeed[]`
   - Use corresponding delays and timing parameters
   - Apply power correction via `actualPowerCorrectionValue` table

## Common Issues

- **Single Instance Lock**: Only one instance can run at a time. Lock file: `singleInstance.loc`
- **Native Libraries**: Scanner DLLs must be in correct path for Hans scanner integration
  - Ensure `libs/Scanner/Hans/*.dll` are accessible
  - Check `system.ini` configuration matches hardware
- **JVM Memory**: Adjust heap size if processing large job files
- **Log File Growth**: Monitor log directories under `~/AMTech/` for disk usage
- **Scanner Calibration**: Incorrect Z-offset or beam diameter coefficients cause focus errors
  - Verify `nominalDiameter` and `coefficient` values match actual optics
  - Run calibration test prints to measure actual beam sizes
- **Multi-laser Alignment**: For dual-laser systems, verify `offsetX` and `offsetY` in scanner config
  - Typical Y offset: ±105mm for 210mm laser spacing

## Practical Code Examples

### Example 1: Adding a New Hardware Element

```java
// 1. Define signal in utilities/Signals.java HARD_SIGNAL enum
public enum HARD_SIGNAL {
    // ... existing signals
    Com_NewMotor,  // Command to activate new motor
    Trig_NewMotorStatus,  // Status from motor
    Am_NewMotorSpeed  // Current speed measurement
}

// 2. Define in hardware XML configuration
// In hardware definition XML:
// <element name="Com_NewMotor" group="Com" type="Bool" address="100.0"/>
// <element name="Trig_NewMotorStatus" group="Trig" type="Bool" address="200.0"/>
// <element name="Am_NewMotorSpeed" group="Am" type="Real" address="300"/>

// 3. Access in code
HardElement motorCmd = HardwareManager.getElement(HARD_SIGNAL.Com_NewMotor);
HardElementState state = new HardElementState();
state.boolValue = true;
ConnectManager.getManager().sendHardState(motorCmd, state);
```

### Example 2: Creating a Custom UI Panel

```java
public class MyCustomPanel extends BaseGroupPanel implements IPLCState {

    public MyCustomPanel() {
        super();
        initComponents();
        // Panel auto-registered with CardManager via base class
    }

    private void initComponents() {
        // Use custom UI components
        SignalButton startButton = new SignalButton(HARD_SIGNAL.Com_StartProcess);
        LampButton statusLamp = new LampButton(HARD_SIGNAL.Trig_ProcessActive);

        // Add to panel
        add(startButton);
        add(statusLamp);
    }

    @Override
    public void receiveState() {
        // Called every 100ms when PLC updates
        // Update UI based on current hardware state
        updateDisplay();
    }

    @Override
    public void updateSoftProperty() {
        // Update non-hardware UI elements
    }
}
```

### Example 3: Implementing a Job Parser

```java
public class MyFormatParser implements IJobParser {

    @Override
    public JobBuilder parseHeader(File file) {
        JobBuilder builder = new JobBuilder();

        try (BufferedReader reader = new BufferedReader(new FileReader(file))) {
            // Parse header section
            String line;
            while ((line = reader.readLine()) != null) {
                if (line.startsWith("LAYERS:")) {
                    int layerCount = Integer.parseInt(line.split(":")[1].trim());
                    builder.setLayerCount(layerCount);
                }
                if (line.startsWith("THICKNESS:")) {
                    double thickness = Double.parseDouble(line.split(":")[1].trim());
                    builder.setLayerThickness(thickness);
                }
                // ... parse other header fields
            }
        } catch (IOException e) {
            DebugLogger.logger().error("Error parsing header", e);
            return null;
        }

        return builder;
    }

    @Override
    public SwingWorker<Boolean, Void> parseGeometry(JobBuilder builder) {
        return new SwingWorker<Boolean, Void>() {
            @Override
            protected Boolean doInBackground() throws Exception {
                // Parse geometry in background thread
                // Use builder.addLayer(), builder.addGeometry(), etc.

                for (int layer = 0; layer < builder.getLayerCount(); layer++) {
                    // Parse layer geometry
                    // Update progress: setProgress((layer * 100) / totalLayers)

                    if (isCancelled()) {
                        return false;
                    }
                }

                return true;
            }
        };
    }
}
```

### Example 4: Converting CLI Parameters to Hans Scanner

```java
public class CliToHansConverter {

    // Configuration from JSON
    private final double NOMINAL_DIAMETER = 120.0; // μm (from calibration)
    private final double Z_COEFFICIENT = 0.3;      // mm/10μm (from calibration)

    public float calculateZOffset(double cliDiameter) {
        // CLI diameter (μm) -> Hans Z offset (mm)
        return (float)((cliDiameter - NOMINAL_DIAMETER) / 10.0 * Z_COEFFICIENT);
    }

    public void convertCliRegionToHans(CliRegion region, int layerIndex) {
        // 1. Extract parameters
        double diameter = region.getLaserBeamDiameter(); // μm
        double power = region.getLaserPower();           // W
        double speed = region.getScanSpeed();            // mm/s

        // 2. Calculate Z offset
        float z = calculateZOffset(diameter);

        // 3. Set layer parameters
        MarkParameter params = new MarkParameter();
        params.MarkSpeed = (int)speed;
        params.LaserPower = (float)(power / 500.0 * 100.0); // W -> %
        params.JumpSpeed = 5000;
        // ... set other parameters from process variables map

        MarkParameter[] layers = new MarkParameter[] { params };
        HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

        // 4. Add geometry with Z
        List<CliPoint> geometry = region.getGeometry();
        structUdmPos[] points = new structUdmPos[geometry.size()];

        for (int i = 0; i < geometry.size(); i++) {
            points[i] = new structUdmPos();
            points[i].x = geometry.get(i).x;
            points[i].y = geometry.get(i).y;
            points[i].z = z;  // Apply calculated Z offset
        }

        HM_UDM_DLL.UDM_AddPolyline3D(points, points.length, layerIndex);
    }

    public void processFullCliFile(File cliFile, File outputBin) {
        // Initialize Hans
        HM_UDM_DLL.UDM_NewFile();
        HM_UDM_DLL.UDM_SetProtocol(0, 1); // 3D mode

        // Parse CLI file
        CliJobParser parser = new CliJobParser();
        JobBuilder builder = parser.parseHeader(cliFile);

        if (builder == null) {
            DebugLogger.logger().error("Failed to parse CLI header");
            return;
        }

        // Parse geometry
        SwingWorker<Boolean, Void> worker = parser.parseGeometry(builder);
        worker.execute();

        try {
            if (worker.get()) {
                // Convert each region
                int layerIndex = 0;
                for (CliRegion region : builder.getRegions()) {
                    convertCliRegionToHans(region, layerIndex++);
                }

                // Generate and save
                HM_UDM_DLL.UDM_Main();
                HM_UDM_DLL.UDM_SaveToFile(outputBin.getAbsolutePath());
                HM_UDM_DLL.UDM_EndMain();

                MessagesLogger.logger().info("Successfully converted CLI to Hans: " +
                                             outputBin.getName());
            }
        } catch (Exception e) {
            DebugLogger.logger().error("Error during CLI conversion", e);
        }
    }
}
```

### Example 5: Working with System States

```java
public class StateAwareComponent implements ISystemStateListener {

    private SYSTEM_STATE currentState = SYSTEM_STATE.IDLE;

    public StateAwareComponent() {
        // Register as system state listener
        ListenerManager.addSystemStateListener(this);
    }

    @Override
    public void onSystemStateChanged(SYSTEM_STATE newState) {
        SYSTEM_STATE oldState = currentState;
        currentState = newState;

        // React to state transitions
        if (oldState == SYSTEM_STATE.PRE_PRINT &&
            newState == SYSTEM_STATE.PRINT) {
            onPrintStarted();
        }

        if (newState == SYSTEM_STATE.PRINT_PAUSE) {
            onPrintPaused();
        }

        if (newState == SYSTEM_STATE.POST_PRINT) {
            onPrintCompleted();
        }
    }

    private void onPrintStarted() {
        // Start monitoring, enable controls, etc.
        MessagesLogger.logger().info("Print started");
    }

    private void onPrintPaused() {
        // Pause monitoring, show pause UI
        MessagesLogger.logger().info("Print paused");
    }

    private void onPrintCompleted() {
        // Stop monitoring, generate reports
        MessagesLogger.logger().info("Print completed");
    }

    public boolean canExecuteAction() {
        // Check if action is allowed in current state
        return currentState == SYSTEM_STATE.IDLE ||
               currentState == SYSTEM_STATE.PRINT_PAUSE;
    }
}
```

## Quick Reference

### Important File Paths
- **Main entry**: `src/project/MainWindow.java`
- **Hardware definitions**: XML files parsed by `src/hardware/XmlParser.java`
- **Connection manager**: `src/connect/ConnectManager.java`
- **Scanner integration**: `src/connectors/ScanSystemConnector.java`
- **Job parsers**: `src/jobparser/cli/CliJobParser.java`, `src/jobparser/gcode/GCodeParser.java`
- **Logging config**: `src/log4j2.xml`
- **Locale resources**: `src/locale/messages*.properties`
- **Hans API**: `libs/Scanner/Hans/Hans4Java.jar`
- **Build file**: `deploy/DeployANT.xml`

### Common Commands
```bash
# Build all variants
ant -f deploy/DeployANT.xml

# Run with emulators (for testing without hardware)
java -Dplcemu=true -Dscanemu=true -Ddebug=true -jar PrintMateMC.jar

# Run specific machine variant
java -Dmachine=AMT16 -jar PrintMateMC_AMT16.jar

# Run with expert mode
java -Dexpert_mode=true -jar PrintMateMC.jar
```

### Key Design Patterns
1. **Singleton**: ConnectManager, UserManager, CommandManager
2. **Observer/Listener**: IPLCState, ISystemStateListener, timer-based updates
3. **State Machine**: SYSTEM_STATE enum with hierarchical codes
4. **Strategy**: IJobParser implementations for different file formats
5. **MVC**: Panels (View) + HardElement (Model) + ConnectManager (Controller)
6. **Card Layout**: UI navigation via CardManager and RootCardPanel
