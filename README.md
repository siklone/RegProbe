# Windows Optimizer

A modern, safe, and reversible Windows optimization tool built with WPF and .NET 8.

![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![.NET Version](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-TBD-yellow)

## 🎯 Features

### 📊 **Real-Time System Monitoring**
- Task Manager-style hardware monitoring
- CPU, RAM, Disk, Network real-time metrics
- Temperature sensors (CPU/GPU) via LibreHardwareMonitor (may show N/A if sensors aren't available)
- Top 10 processes by CPU/RAM usage
- Top 10 processes by network (approx. process I/O bytes; may include disk activity)
- Network adapter statistics (all adapters listed separately, with throughput)
- Disk activity per drive with read/write speeds
- 60-second history graphs for CPU and RAM
- Save a CSV snapshot to your Desktop

### ⚙️ **System Tweaks**
- Safe/reversible pipeline: Detect → Apply → Verify → Rollback
- Default behavior is Preview/DryRun (no system changes until you Apply)
- Risk levels: Safe / Advanced / Risky
- Category-based navigation + search + batch actions

### 🔌 **Plugin System**
- Dynamic plugin loading from `Plugins` directory
- ITweakPlugin interface for custom tweaks
- Example plugin: HelloWorld (demonstrates safe, reversible tweak)
- Full SDK with documentation

### 📦 **Batch Operations**
- Multi-select tweaks with checkboxes
- Bulk apply/rollback operations
- Progress tracking

### 💾 **Profile Management**
- Export/Import tweak configurations
- 4 built-in presets:
  - 🎮 **Gaming**: Optimized for maximum gaming performance
  - 🔒 **Privacy**: Maximum privacy with telemetry disabled
  - 🏢 **Workstation**: Balanced for productivity
  - 🔋 **Laptop**: Battery-optimized settings
- Custom profile creation

### 🧠 **Hardware Intelligence**
- Automatic hardware detection
- Smart tweak recommendations based on system profile
- Adaptive optimization

## 🏗️ Architecture

### Provider Pattern
All tweaks are organized into modular `ITweakProvider` implementations:
- **BaseTweakProvider**: Abstract base with helper methods
- **Category-specific providers**: Each provider handles its domain
- **Dependency Injection**: Providers injected into TweaksViewModel
- **LegacyTweakProvider (temporary)**: Restores legacy tweaks pending full migration

### Plugin SDK
```csharp
public interface ITweakPlugin
{
    string PluginName { get; }
    string Author { get; }
    string Version { get; }
    IEnumerable<ITweak> GetTweaks();
}
```

### Project Structure
```
WindowsOptimizer/
├── WindowsOptimizer.Core/          # Domain models, interfaces
├── WindowsOptimizer.Engine/        # Business logic, tweak execution
├── WindowsOptimizer.Infrastructure/ # External services, metrics, registry
├── WindowsOptimizer.App/           # WPF UI, ViewModels
├── WindowsOptimizer.ElevatedHost/  # Elevated process for admin operations
├── WindowsOptimizer.Plugins.HelloWorld/ # Example plugin
└── WindowsOptimizer.Tests/         # Unit tests
```

## 🚀 Getting Started

### Prerequisites
- Windows 10/11
- .NET 8.0 SDK
- Administrator privileges (for some tweaks; admin operations run via `WindowsOptimizer.ElevatedHost`)

### Build
```powershell
git clone https://github.com/siklone/WPF-Windows-optimizer-with-safe-reversible-tweaks.git
cd WPF-Windows-optimizer-with-safe-reversible-tweaks
dotnet build
```

### Run
```powershell
dotnet run --project WindowsOptimizer.App
```

> Tip: If you run from `dotnet run` and the app can't find the ElevatedHost binary, set the env var:
> `WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH=C:\\path\\to\\WindowsOptimizer.ElevatedHost.exe`

### Build Release
```powershell
dotnet publish WindowsOptimizer.App -c Release -r win-x64 --self-contained
```

## 🔌 Creating Plugins

### 1. Create Plugin Project
```powershell
dotnet new classlib -n WindowsOptimizer.Plugins.MyPlugin
dotnet add reference ../WindowsOptimizer.Core/WindowsOptimizer.Core.csproj
```

### 2. Implement ITweakPlugin
```csharp
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Plugins;

public class MyPlugin : ITweakPlugin
{
    public string PluginName => "My Custom Plugin";
    public string Author => "Your Name";
    public string Version => "1.0.0";

    public IEnumerable<ITweak> GetTweaks()
    {
        return new List<ITweak>
        {
            new MyCustomTweak()
        };
    }
}
```

### 3. Deploy Plugin
```powershell
dotnet build
Copy-Item bin/Release/net8.0-windows/WindowsOptimizer.Plugins.MyPlugin.dll `
    -Destination ../WindowsOptimizer.App/bin/Release/net8.0-windows/win-x64/Plugins/
```

See `WindowsOptimizer.Plugins.HelloWorld` for a complete example.

## 📖 Usage

### Monitor Page
- Real-time hardware metrics
- Top processes by CPU/RAM
- Network and disk activity
- Temperature monitoring

### Tweaks Page
- Browse tweaks by category
- Search and filter
- Multi-select for batch operations
- One-click apply/rollback
- Profile export/import

### Profiles
1. Click **Export Profile** to save current configuration
2. Click **Import Profile** to load a saved configuration
3. Use preset profiles for quick optimization

## 🛡️ Safety

All tweaks are:
- ✅ **Reversible**: Detect → Apply → Verify → Rollback (rollback currently relies on the last Detect snapshot in the same app session)
- ✅ **Logged**: All actions logged for audit (`%TEMP%\\WindowsOptimizer_Debug.log` and `tweak-log.csv`)
- ✅ **Verified**: Post-apply verification step
- ✅ **Preview-first**: No system changes until you click Apply

**Risk Levels:**
- 🟢 **Safe**: No side effects, recommended for all users
- 🟡 **Advanced**: May affect functionality, understand before applying
- 🔴 **Risky**: Only for advanced users, may cause issues

## 🤝 Contributing

Contributions welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📝 License

TBD (no LICENSE file committed yet).

## 🙏 Acknowledgments

- **[nohuto/win-config](https://github.com/nohuto/win-config)** - Windows registry research and documentation (AGPL-3.0)
- **[nohuto/win-registry](https://github.com/nohuto/win-registry)** - Registry value documentation with traces (GPL-3.0)
- **LibreHardwareMonitor** - Hardware sensor data
- **Nord Theme** - UI color palette
- **WPF Community** - Framework and patterns

> See [Docs/RESEARCH_CREDITS.md](Docs/RESEARCH_CREDITS.md) for detailed attribution and links to source documentation.

## 📞 Support

- 🐛 **Issues**: GitHub Issues
- 💬 **Discussions**: GitHub Discussions
- 🧾 **Logs**: `%TEMP%\\WindowsOptimizer_Debug.log`

---

**⚠️ Disclaimer**: This tool modifies Windows registry settings. Always create a system restore point before applying tweaks. Use at your own risk.
