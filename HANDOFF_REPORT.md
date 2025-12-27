# Windows Optimizer — Handoff (for Claude/Agents)

**Last Updated:** 2025-12-27  
**Branch:** `main`  

This doc is a practical handoff for the next agent (Claude or others): what changed recently, what is still broken/unfinished, and where to look.

## Quick Start

- Build: `dotnet build`
- Run: `dotnet run --project WindowsOptimizer.App`
- Logs:
  - Debug log: `%TEMP%\WindowsOptimizer_Debug.log`
  - Tweak CSV log: created under the app’s log directory (see `AppPaths`)

## Recent High-Impact Changes (Good to Know)

- **Tweaks page crash fixes**
  - Freezable animations were causing intermittent WPF crashes (`Cannot animate '(0).(1)'...`, `BorderThickness property...`).
  - Fix: avoid animating template Freezables; use overlay opacity + transforms only.
  - Commit: `fc21306`

- **ElevatedHost discovery improvements**
  - When running via `dotnet run`, the app can’t rely on publish layout paths.
  - Fix: robust path discovery + env var override + better error logging.
  - Commits: `46abd80`, `969700f`

- **Dashboard health score UX**
  - Health now reflects *detected* tweak states only; shows `—` until at least one tweak has been detected.
  - Commit: `7c66e13`

- **Tweaks UX improvements**
  - Compact “Impact: Current → Target” line on collapsed cards.
  - More robust “Policy/Documentation” link opening.
  - Commits: `d49a726`, `7a5ed0c`

## Current Known Issues / Incomplete Work (Prioritized)

1) **Monitoring charts are hard to read**
   - Current sparklines don’t show scale or context clearly.
   - Add: gridlines, min/max labels, and a consistent vertical range (or clear autoscale indicator).
   - Relevant: `WindowsOptimizer.App/Views/MonitorView.xaml`, `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`

2) **CPU temp often shows `N/A`**
   - This may be expected on some systems depending on sensor availability.
   - UX improvement: add a tooltip explaining why (`LibreHardwareMonitor`/sensor access) and/or a “last updated”/error indicator.
   - Relevant: `WindowsOptimizer.App/ViewModels/MonitorViewModel.cs`, `WindowsOptimizer.Infrastructure/Metrics/MetricProvider.cs`

3) **ElevatedHost packaging still needs verification**
   - Tweaks page warns if host is missing, but publish/build should reliably copy it.
   - Add: MSBuild/post-publish copy step + CI check.
   - Relevant: `WindowsOptimizer.App/WindowsOptimizer.App.csproj`, `WindowsOptimizer.ElevatedHost/*`

4) **Rollback persistence is session-scoped**
   - Current rollback behavior depends on the last Detect snapshot in the same app session.
   - A crash or restart loses “original state” for rollback.
   - Next: implement durable state capture (JSON/SQLite) before Apply.
   - Relevant: `WindowsOptimizer.Engine/TweakExecutionPipeline.cs`, tweak implementations in `WindowsOptimizer.Engine/Tweaks/*`

5) **Category detection cancellation/race**
   - Categories can still race if opened/closed quickly; timeouts exist but cancellation would be cleaner.
   - Relevant: `WindowsOptimizer.App/ViewModels/CategoryGroupViewModel.cs`

## Guardrails (Do Not Break)

- **SAFE tweaks must be reversible**: Detect → Apply → Verify → Rollback
- **Default is Preview/DryRun**: no automatic system changes
- **Do not add “disable Defender/Firewall/SmartScreen” under SAFE**
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

## Suggested Next PRs (Order)

1. Improve Monitor charts readability + CPU temp tooltip
2. Make ElevatedHost publish copying deterministic (build step + CI check)
3. Add durable rollback state store (before Apply)

