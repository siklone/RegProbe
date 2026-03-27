# Architecture Documentation

## Overview

RegProbe follows a clean, layered architecture with clear separation of concerns.

## Layer Diagram

```text
app            -> desktop UI
engine         -> tweak execution and orchestration
infrastructure -> registry, elevation, files, and adapters
core           -> contracts, models, and shared abstractions
```

## Projects

### core
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

### engine
**Purpose**: Business logic and execution orchestration

**Key Components**:
- `TweakExecutionPipeline`: Orchestrates Detect â†’ Apply â†’ Verify â†’ Rollback
- `ITweakProvider` implementations (11 categories + legacy bridge)
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

### infrastructure
**Purpose**: External service implementations

**Key Services**:
- `LocalRegistryAccessor`: Direct registry access
- `ElevatedRegistryAccessor`: Registry via elevated host
- Hardware info services and providers for OS, motherboard, storage, and display details
- `PluginLoader`: Dynamic plugin loading

### app
**Purpose**: Desktop presentation layer

**MVVM Pattern**:
- `MainViewModel`: Navigation and top-level state
- `TweaksViewModel`: Tweak management (4200+ lines)
- `DashboardViewModel`: Overview and hardware summary

**Key Features**:
- ObservableCollection-based data binding
- INotifyPropertyChanged via ViewModelBase
- RelayCommand for button actions
- Dashboard summaries are derived from detected tweak states and hardware snapshots (run Detect to populate current/applied status)

**UI Stability Notes**:
- Avoid animating Freezables created in templates/resources (`DropShadowEffect`, `SolidColorBrush`, etc.) because they can be frozen/shared.
- Prefer animating named transforms (`TranslateTransform`, `ScaleTransform`, `RotateTransform`) and overlay `Opacity`.
- When using shared resources for transforms, set `x:Shared="False"` to avoid shared instances.

### elevated-host
**Purpose**: UAC elevation and privileged operations

**Architecture**:
- Named pipe communication (parent â†” elevated child)
- JSON-based request/response protocol
- Automatic process lifetime management

**Executable Discovery**:
- The app is not always-admin; admin-required operations run via ElevatedHost.
- The UI resolves the ElevatedHost path via `app/Utilities/ElevatedHostLocator.cs`.
- You can override discovery with the env var `REGPROBE_ELEVATED_HOST_PATH`.
- Recommended publish layout: `app/.../win-x64/ElevatedHost/RegProbe.ElevatedHost.exe`.

## Design Patterns

### 1. Provider Pattern
Each tweak category has its own provider:
- `SystemTweakProvider`
- `PrivacyTweakProvider`
- `SecurityTweakProvider`
- `LegacyTweakProvider` (temporary parity layer)
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
    â†“
TweaksViewModel.ApplyCommand
    â†“
TweakExecutionPipeline.ExecuteAsync()
    â†“
1. ITweak.DetectAsync()
2. ITweak.ApplyAsync()
3. ITweak.VerifyAsync()
    â†“
Update UI via INotifyPropertyChanged
    â†“
Log to FileTweakLogStore
```

## Threading Model

- **UI Thread**: All ViewModels and UI binding
- **Background Threads**:
  - Tweak execution (async/await)
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
- Hardware snapshot resources are disposed when the app shuts down

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
