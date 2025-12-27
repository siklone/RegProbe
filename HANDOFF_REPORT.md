# Windows Optimizer — Handoff (for Claude/Agents)

**Last Updated:** 2025-12-28
**Branch:** `main`

This doc is a practical handoff for the next agent (Claude or others): what changed recently, what is still broken/unfinished, and where to look.

## Quick Start

- Build: `dotnet build`
- Run: `dotnet run --project WindowsOptimizer.App`
- Logs:
  - Debug log: `%TEMP%\WindowsOptimizer_Debug.log`
  - Tweak CSV log: created under the app's log directory (see `AppPaths`)

## Recent High-Impact Changes (Good to Know)

- **Durable rollback state persistence (NEW)**
  - Implemented `IRollbackAwareTweak` interface for tweaks to expose rollback snapshots
  - `RollbackStateStore` persists original values to `%AppData%\WindowsOptimizerSuite\rollback-state.json`
  - `TweakExecutionPipeline` now saves state before Apply, marks as applied/rolled back after
  - Enables crash recovery: pending rollbacks can be restored on next app launch
  - Files: `WindowsOptimizer.Core/TweakContracts.cs`, `WindowsOptimizer.Infrastructure/RollbackStateStore.cs`, `WindowsOptimizer.Engine/TweakExecutionPipeline.cs`, `WindowsOptimizer.Engine/Tweaks/RegistryValueTweak.cs`

- **Category detection cancellation (NEW)**
  - Categories now use CancellationTokenSource to cancel pending detection when collapsed
  - Prevents race conditions from rapid expand/collapse cycles
  - File: `WindowsOptimizer.App/ViewModels/CategoryGroupViewModel.cs`

- **Monitor chart improvements (NEW)**
  - Added gridlines (25%, 50%, 75%, 100%) to CPU/RAM history charts
  - Added Min/Max value indicators to chart headers
  - Added Y-axis scale labels
  - File: `WindowsOptimizer.App/Views/MonitorView.xaml`, `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`

- **ToolTip style fix (NEW)**
  - Removed invalid `PopupAnimation` property from ToolTip style that caused build failure
  - File: `WindowsOptimizer.App/Resources/Styles.xaml`

- **Tweaks page crash fixes**
  - Freezable animations were causing intermittent WPF crashes (`Cannot animate '(0).(1)'...`, `BorderThickness property...`).
  - Fix: avoid animating template Freezables; use overlay opacity + transforms only.
  - Commit: `fc21306`

- **ElevatedHost discovery improvements**
  - When running via `dotnet run`, the app can't rely on publish layout paths.
  - Fix: robust path discovery + env var override + better error logging.
  - Commits: `46abd80`, `969700f`

- **Dashboard health score UX**
  - Health now reflects *detected* tweak states only; shows `—` until at least one tweak has been detected.
  - Commit: `7c66e13`

## Current Known Issues / Incomplete Work (Prioritized)

1) **Windows 10/11 native testing required**
   - All recent changes need verification on actual Windows (not WSL2).
   - Test checklist in `CODEX_PLAN.md` Phase 2.
   - Priority: HIGH

2) **Network/Disk monitoring may show empty**
   - Delta-based throughput and LogicalDisk counters implemented but need real testing.
   - Relevant: `WindowsOptimizer.Infrastructure/Metrics/NetworkMonitor.cs`, `DiskMonitor.cs`

3) **Crash recovery dialog not implemented**
   - `RollbackStateStore.GetPendingRollbacksAsync()` is ready, but no UI prompts on app startup.
   - Next: check for pending rollbacks on startup, show recovery dialog.
   - Relevant: `WindowsOptimizer.App/ViewModels/MainViewModel.cs`

4) **Memory leak testing**
   - Monitor page should be stress-tested (1hr) to verify no memory leaks.
   - Relevant: `MonitorViewModel.cs`, `*Monitor.cs`

5) **ElevatedHost packaging verification**
   - csproj targets exist but need CI verification.
   - Relevant: `WindowsOptimizer.App/WindowsOptimizer.App.csproj`

## Guardrails (Do Not Break)

- **SAFE tweaks must be reversible**: Detect → Apply → Verify → Rollback
- **Default is Preview/DryRun**: no automatic system changes
- **Do not add "disable Defender/Firewall/SmartScreen" under SAFE**
- **Admin operations must run via ElevatedHost** (separate process); app is not always-admin
- **Always log actions**; logs must be exportable
- **WPF animation rule**: do not animate Freezables created in templates/resources; prefer named transforms and overlay opacity

## Operational Notes

- ElevatedHost path override: `WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH=C:\\path\\to\\WindowsOptimizer.ElevatedHost.exe`
- Recommended publish layout: include `ElevatedHost/WindowsOptimizer.ElevatedHost.exe` beside the main app output.

## Where to Look (Common Entry Points)

- Tweaks UI: `WindowsOptimizer.App/Views/TweaksView.xaml`
- Tweak VM logic: `WindowsOptimizer.App/ViewModels/TweakItemViewModel.cs`
- Tweaks aggregation/filtering: `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs`
- Dashboard health: `WindowsOptimizer.App/ViewModels/DashboardViewModel.cs`
- ElevatedHost discovery: `WindowsOptimizer.App/Utilities/ElevatedHostLocator.cs`
- Monitor UI/VM: `WindowsOptimizer.App/Views/MonitorView.xaml`, `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`
- Metrics sources: `WindowsOptimizer.Infrastructure/Metrics/*.cs`
- Pipeline orchestration: `WindowsOptimizer.Engine/TweakExecutionPipeline.cs`
- Rollback state: `WindowsOptimizer.Infrastructure/RollbackStateStore.cs`

## Suggested Next PRs (Order)

1. ~~Improve Monitor charts readability + CPU temp tooltip~~ ✅ DONE
2. ~~Add durable rollback state store (before Apply)~~ ✅ DONE
3. ~~Category detection cancellation~~ ✅ DONE
4. **Windows 10/11 native testing** (PRIORITY)
5. Crash recovery dialog on app startup
6. Memory optimization / leak testing

## Files Changed This Session

- `WindowsOptimizer.Core/TweakContracts.cs` - Added `IRollbackAwareTweak`, `TweakRollbackSnapshot`
- `WindowsOptimizer.Infrastructure/RollbackStateStore.cs` - Added snapshot methods
- `WindowsOptimizer.Engine/TweakExecutionPipeline.cs` - Integrated rollback store
- `WindowsOptimizer.Engine/Tweaks/RegistryValueTweak.cs` - Implemented `IRollbackAwareTweak`
- `WindowsOptimizer.App/ViewModels/TweaksViewModel.cs` - Wired up `RollbackStateStore`
- `WindowsOptimizer.App/Resources/Styles.xaml` - Fixed ToolTip style build error
