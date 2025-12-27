# Architecture Documentation

## Overview

Windows Optimizer follows a clean, layered architecture with clear separation of concerns.

## Layer Diagram

```
┌─────────────────────────────────────────┐
│         WindowsOptimizer.App            │ ◄─── WPF UI Layer
│    (Views, ViewModels, Converters)      │
└─────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────┐
│       WindowsOptimizer.Engine           │ ◄─── Business Logic
│  (TweakExecutionPipeline, Providers)    │
└─────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────┐
│    WindowsOptimizer.Infrastructure      │ ◄─── External Services
│  (Registry, Metrics, Elevation, Files)  │
└─────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────┐
│       WindowsOptimizer.Core             │ ◄─── Domain Models
│   (Interfaces, Contracts, DTOs)         │
└─────────────────────────────────────────┘
```

## Projects

### WindowsOptimizer.Core
**Purpose**: Domain models and contracts

**Key Interfaces**:
- `ITweak`: Core tweak contract (Detect, Apply, Verify, Rollback)
- `ITweakProvider`: Provider pattern for modular tweaks
- `ITweakPlugin`: Plugin system interface
- `IRegistryAccessor`: Registry operations abstraction
- `IServiceManager`: Windows service management

**Models**:
- `TweakResult`: Execution result with status
- `TweakStatus`: Enum (Detected, Applied, Verified, RolledBack, Failed)
- `TweakRiskLevel`: Enum (Safe, Advanced, Risky)

### WindowsOptimizer.Engine
**Purpose**: Business logic and execution orchestration

**Key Components**:
- `TweakExecutionPipeline`: Orchestrates Detect → Apply → Verify → Rollback
- `ITweakProvider` implementations (10 categories)
- Concrete tweak types (RegistryValueTweak, ServiceTweak, CommandTweak, etc.)

**Provider Pattern**:
```csharp
public interface ITweakProvider
{
    string CategoryName { get; }
    IEnumerable<ITweak> CreateTweaks(
        TweakExecutionPipeline pipeline,
        TweakContext context,
        bool isElevated);
}
```

### WindowsOptimizer.Infrastructure
**Purpose**: External service implementations

**Key Services**:
- `LocalRegistryAccessor`: Direct registry access
- `ElevatedRegistryAccessor`: Registry via elevated host
- `MetricProvider`: Hardware metrics (CPU, RAM, temps)
- `ProcessMonitor`: Process tracking
- `NetworkMonitor`: Network adapter stats
- `DiskMonitor`: Disk activity
- `PluginLoader`: Dynamic plugin loading

**Metrics System**:
- Uses `PerformanceCounter` for CPU/RAM/Disk/Network
- Uses `LibreHardwareMonitor` for temperature sensors
- Uses `System.Management` (WMI) for hardware info

### WindowsOptimizer.App
**Purpose**: WPF presentation layer

**MVVM Pattern**:
- `MainViewModel`: Navigation and top-level state
- `TweaksViewModel`: Tweak management (4200+ lines)
- `MonitorViewModel`: Real-time metrics
- `DashboardViewModel`: Overview and health score

**Key Features**:
- ObservableCollection-based data binding
- INotifyPropertyChanged via ViewModelBase
- RelayCommand for button actions
- DispatcherTimer for metrics updates (1 sec interval)
- Health score is derived from *detected* tweak states (run Detect to populate current/applied status)

**WPF Stability Notes**:
- Avoid animating Freezables created in templates/resources (`DropShadowEffect`, `SolidColorBrush`, etc.) because they can be frozen/shared.
- Prefer animating named transforms (`TranslateTransform`, `ScaleTransform`, `RotateTransform`) and overlay `Opacity`.
- When using shared resources for transforms, set `x:Shared="False"` to avoid shared instances.

### WindowsOptimizer.ElevatedHost
**Purpose**: UAC elevation and privileged operations

**Architecture**:
- Named pipe communication (parent ↔ elevated child)
- JSON-based request/response protocol
- Automatic process lifetime management

**Executable Discovery**:
- The app is not always-admin; admin-required operations run via ElevatedHost.
- The UI resolves the ElevatedHost path via `WindowsOptimizer.App/Utilities/ElevatedHostLocator.cs`.
- You can override discovery with the env var `WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH`.
- Recommended publish layout: `WindowsOptimizer.App/.../win-x64/ElevatedHost/WindowsOptimizer.ElevatedHost.exe`.

## Design Patterns

### 1. Provider Pattern
Each tweak category has its own provider:
- `SystemTweakProvider`
- `PrivacyTweakProvider`
- `SecurityTweakProvider`
- etc.

**Benefits**:
- Modularity
- Easy to add new categories
- Independent testing
- Clear organization

### 2. Plugin System
External DLLs can extend functionality:
```csharp
var loader = new PluginLoader();
var plugins = loader.LoadPlugins("Plugins");
foreach (var plugin in plugins)
{
    var tweaks = plugin.GetTweaks();
}
```

### 3. Repository Pattern
- `FileTweakLogStore`: Persists tweak execution logs
- `ProfileManager`: Saves/loads tweak configurations

### 4. Pipeline Pattern
`TweakExecutionPipeline` orchestrates execution:
1. **Detect**: Check current state
2. **Apply**: Make changes
3. **Verify**: Confirm success
4. **Rollback**: Undo on failure

### 5. Observer Pattern
- INotifyPropertyChanged for UI updates
- ObservableCollection for dynamic lists

## Data Flow

### Tweak Execution
```
User clicks "Apply" on tweak
    ↓
TweaksViewModel.ApplyCommand
    ↓
TweakExecutionPipeline.ExecuteAsync()
    ↓
1. ITweak.DetectAsync()
2. ITweak.ApplyAsync()
3. ITweak.VerifyAsync()
    ↓
Update UI via INotifyPropertyChanged
    ↓
Log to FileTweakLogStore
```

### Metrics Update (Monitor Page)
```
DispatcherTimer.Tick (every 1 second)
    ↓
MetricProvider.GetCpuUsage()
ProcessMonitor.GetTopProcessesByCpu()
NetworkMonitor.GetActiveAdapters()
DiskMonitor.GetDiskActivity()
    ↓
Update ObservableCollections
    ↓
WPF Data Binding → UI updates
```

## Threading Model

- **UI Thread**: All ViewModels, WPF binding
- **Background Threads**:
  - Tweak execution (async/await)
  - Metrics collection (DispatcherTimer)
  - Plugin loading (Task.Run)
  - Hardware discovery (Task.Run)

**Thread Safety**:
- `Dispatcher.InvokeAsync` for UI updates from background
- `lock` statements in shared resources
- Immutable result objects (TweakResult, TweakExecutionReport)

## Extensibility Points

### 1. Add New Tweak Provider
```csharp
public class MyTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "My Category";

    public override IEnumerable<ITweak> CreateTweaks(...)
    {
        return new List<ITweak>
        {
            CreateRegistryTweak(...),
            CreateServiceTweak(...)
        };
    }
}
```

### 2. Create Plugin
```csharp
public class MyPlugin : ITweakPlugin
{
    public string PluginName => "My Plugin";
    public string Author => "Me";
    public string Version => "1.0";

    public IEnumerable<ITweak> GetTweaks()
    {
        return new[] { new MyTweak() };
    }
}
```

### 3. Add Custom Tweak Type
```csharp
public class MyCustomTweak : ITweak
{
    public Task<TweakResult> DetectAsync(CancellationToken ct) { ... }
    public Task<TweakResult> ApplyAsync(CancellationToken ct) { ... }
    public Task<TweakResult> VerifyAsync(CancellationToken ct) { ... }
    public Task<TweakResult> RollbackAsync(CancellationToken ct) { ... }
}
```

## Performance Considerations

### Memory
- ObservableCollections bounded to reasonable sizes (60 points for history)
- Sliding window pattern (remove oldest, add newest)
- Dispose pattern for PerformanceCounters
- LibreHardwareMonitor cleanup on exit

### CPU
- 1-second timer interval (balance between real-time and performance)
- Async/await for I/O operations
- Cached hardware profiles
- Lazy initialization of providers

### Disk
- Append-only log files
- JSON serialization for profiles
- No database (file-based storage)

## Security

### Elevation
- Separate process for privileged operations
- Named pipes with process ID validation
- JSON-only communication (no code execution)
- Automatic cleanup on parent exit
- If ElevatedHost is missing, Tweaks UI shows a warning with the expected path and override hint.

### Registry Safety
- All changes via abstraction (IRegistryAccessor)
- Rollback support for all tweaks
- Ownership handling for protected keys
- No direct registry manipulation in UI

### Plugin Sandboxing
- Plugins run in same process (trust model)
- Assembly.LoadFrom with path validation
- Exception handling prevents crashes
- Future: AppDomain isolation

## Testing Strategy

### Unit Tests
- Core domain logic
- Tweak execution pipeline
- Provider instantiation

### Integration Tests
- Registry operations (mocked)
- Service management (mocked)
- File operations (temp directories)

### Manual Testing
- Full UI flow
- Elevation scenarios
- Plugin loading
- Profile import/export

## Future Enhancements

1. **AppDomain Plugin Isolation**: Sandbox untrusted plugins
2. **Database Backend**: SQLite for logs and profiles
3. **Web API**: Remote management
4. **Scheduled Tasks**: Automatic tweak application
5. **Backup/Restore**: System state snapshots
6. **Telemetry** (opt-in): Usage analytics
7. **Auto-Update**: Self-updating mechanism
8. **Multi-Language**: i18n support
