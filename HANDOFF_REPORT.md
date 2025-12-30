# Windows Optimizer — Handoff (for Claude/Agents)

**Last Updated:** 2025-12-30
**Branch:** `main`

This doc is a practical handoff for the next agent (Claude or others): what changed recently, what is still broken/unfinished, and where to look.

## Quick Start

- Build: `dotnet build`
- Run: `dotnet run --project WindowsOptimizer.App`
- Logs:
  - Debug log: `%TEMP%\WindowsOptimizer_Debug.log`
  - Tweak CSV log: created under the app's log directory (see `AppPaths`)

## Recent High-Impact Changes (Good to Know)

- **Phase 8: TweaksViewModel Refactoring (2025-12-30)**
  - Reduced `TweaksViewModel.cs` from ~4500 lines to ~1300 lines
  - Created 10 specialized TweakProvider classes
  - All tweaks now loaded dynamically via `IEnumerable<ITweakProvider>` injection
  - Removed legacy helper methods (`CreateRegistryTweak`, etc.)
  - Files: `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`, `WindowsOptimizer.App/Services/TweakProviders/*.cs`

- **TweakProvider Implementations (2025-12-30)**
  - `PrivacyTweakProvider` - 25+ privacy tweaks
  - `SecurityTweakProvider` - 20+ security tweaks
  - `NetworkTweakProvider` - 15+ network tweaks
  - `SystemTweakProvider` - 15+ system tweaks
  - `PerformanceTweakProvider` - 10+ performance tweaks
  - `PowerTweakProvider` - 8+ power management tweaks
  - `VisibilityTweakProvider` - 12+ UI/UX tweaks
  - `PeripheralTweakProvider` - 6+ peripheral tweaks
  - `AudioTweakProvider` - 4+ audio tweaks
  - `MiscTweakProvider` - 8+ misc tweaks

- **Durable rollback state persistence**
  - `RollbackStateStore` persists original values to `%AppData%\WindowsOptimizerSuite\rollback-state.json`

- **Crash recovery banner (Recover/Dismiss)**
  - Startup checks for pending rollbacks and surfaces a banner in `MainWindow` with recovery actions.

- **Category detection cancellation**
  - Categories now use CancellationTokenSource to cancel pending detection when collapsed

- **Monitor chart improvements**
  - Added gridlines (25%, 50%, 75%, 100%) to CPU/RAM history charts
  - Added Min/Max value indicators to chart headers

## Current Known Issues / Incomplete Work (Prioritized)

1) **Windows 10/11 native testing required**
   - All recent changes need verification on actual Windows (not WSL2).
   - Test checklist in `CODEX_PLAN.md` Phase 2.
   - Priority: HIGH

2) **WiX MSI Installer not implemented**
   - Professional installer needed for distribution
   - Priority: HIGH

3) **Network/Disk monitoring may show empty**
   - Delta-based throughput and LogicalDisk counters implemented but need real testing.
   - Relevant: `WindowsOptimizer.Infrastructure/Metrics/NetworkMonitor.cs`, `DiskMonitor.cs`

4) **Crash recovery UX could be improved (optional)**
   - Banner exists, but a modal dialog + detailed list of affected tweaks might be clearer.

5) **Memory leak testing**
   - Monitor page should be stress-tested (1hr) to verify no memory leaks.

## Guardrails (Do Not Break)

- **SAFE tweaks must be reversible**: Detect → Apply → Verify → Rollback
- **Default is Preview/DryRun**: no automatic system changes
- **Do not add "disable Defender/Firewall/SmartScreen" under SAFE**
- **Admin operations must run via ElevatedHost** (separate process); app is not always-admin
- **Always log actions**; logs must be exportable
- **WPF animation rule**: do not animate Freezables created in templates/resources

## Operational Notes

- ElevatedHost path override: `WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH=C:\\path\\to\\WindowsOptimizer.ElevatedHost.exe`
- Recommended publish layout: include `ElevatedHost/WindowsOptimizer.ElevatedHost.exe` beside the main app output.

## Where to Look (Common Entry Points)

- Tweaks UI: `WindowsOptimizer.App/Views/TweaksView.xaml`
- Tweak VM logic: `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`
- Tweaks aggregation/filtering: `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs` (~1300 lines)
- **TweakProviders: `WindowsOptimizer.App/Services/TweakProviders/*.cs` (modular tweak definitions)**
- Dashboard health: `WindowsOptimizer.App/ViewModels/DashboardViewModel.cs`
- ElevatedHost discovery: `WindowsOptimizer.App/Utilities/ElevatedHostLocator.cs`
- Monitor UI/VM: `WindowsOptimizer.App/Views/MonitorView.xaml`
- Metrics sources: `WindowsOptimizer.Infrastructure/Metrics/*.cs`
- Pipeline orchestration: `WindowsOptimizer.Engine/TweakExecutionPipeline.cs`
- Rollback state: `WindowsOptimizer.Infrastructure/RollbackStateStore.cs`

## Suggested Next PRs (Order)

1. ~~TweaksViewModel Refactoring~~ ✅ DONE (Phase 8)
2. ~~TweakProvider implementations~~ ✅ DONE
3. **Windows 10/11 native testing** (PRIORITY)
4. **WiX MSI Installer setup** (PRIORITY)
5. Memory optimization / leak testing
6. Optional: crash recovery modal + per-tweak selection

## Files Changed This Session (2025-12-30)

- `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs` - Major refactoring (4500→1300 lines)
- `WindowsOptimizer.App/Services/TweakProviders/PrivacyTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/SecurityTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/NetworkTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/SystemTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/PerformanceTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/PowerTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/VisibilityTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/PeripheralTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/AudioTweakProvider.cs` - New
- `WindowsOptimizer.App/Services/TweakProviders/MiscTweakProvider.cs` - New
