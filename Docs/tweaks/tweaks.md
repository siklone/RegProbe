# Tweak Implementation Guide
> Update (2025-12-30): LegacyTweakProvider restored missing tweaks; verify this doc against the current catalog.

## Overview
Tweaks implement `ITweak` and expose four actions: Detect, Apply, Verify, and Rollback. The execution pipeline is handled by `TweakExecutionPipeline`, which logs every step and supports DryRun/Preview by default.

> **Note (2025-12-30):** Durable rollback state is persisted to `%AppData%\\OpenTraceProject\\rollback-state.json` for crash recovery and cross-session rollback.

## Safety guarantees (Detect -> Apply -> Verify -> Rollback)
- Detect always runs first to capture current configuration.
- Apply runs only when Detect succeeds and DryRun is false.
- Verify runs after Apply when `VerifyAfterApply` is enabled.
- Rollback runs automatically when Apply or Verify fails (default) or when the user requests it.

## Tweak catalog & sources
- Generated catalog: `Docs/tweaks/tweak-catalog.md` (now includes Changes + Risk)
- CSV export: `Docs/tweaks/tweak-catalog.csv` (includes Description + Risk fields)
- HTML catalog: `Docs/tweaks/tweak-catalog.html` (supports anchor links + Changes/Risk)
- Test template: `Docs/tweaks/tweak-test-template.csv`
- Regenerate: `python3 scripts/generate_tweak_catalog.py` (or `py -3` on Windows)
- The catalog maps tweak IDs to their source files, docs, and short change/risk summaries.
- The UI reads the CSV to surface a `Catalog entry` anchor link plus a `Source file` link for each tweak.
- Category docs now include an auto-generated **Tweak Index** section (between `

<!-- TWEAK INDEX START -->
## Tweak Index (Generated)

This section is generated from `Docs/tweaks/tweak-catalog.csv`.
Do not edit manually.

| ID | Name | Changes | Risk | Source |
| --- | --- | --- | --- | --- |
| <a id="developer.dotnet-telemetry-disable"></a> `developer.dotnet-telemetry-disable` | Disable .NET SDK Telemetry | Stops .NET SDK from sending usage data to Microsoft. Source: Microsoft .NET SDK Documentation | Safe | `OpenTraceProject.App\Services\TweakProviders\DeveloperTweakProvider.cs#L42` |
| <a id="developer.enable-windows-long-paths"></a> `developer.enable-windows-long-paths` | Enable Windows Long Paths | Enables the Windows long-path prerequisite for compatible applications, including development tools that work with deep directory trees.... | Safe | `OpenTraceProject.App\Services\TweakProviders\DeveloperTweakProvider.cs#L28` |
| <a id="developer.nodejs-performance"></a> `developer.nodejs-performance` | Optimize Node.js Performance | Increases Node.js memory limit and enables performance optimizations for large JavaScript projects. | Advanced | `OpenTraceProject.App\Services\TweakProviders\DeveloperTweakProvider.cs#L86` |
| <a id="developer.powershell-execution"></a> `developer.powershell-execution` | Allow Local PowerShell Scripts | Sets PowerShell execution policy to RemoteSigned, allowing local scripts to run while requiring signatures for remote scripts. Source: Mi... | Advanced | `OpenTraceProject.App\Services\TweakProviders\DeveloperTweakProvider.cs#L145` |
| <a id="developer.python-path-fix"></a> `developer.python-path-fix` | Fix Python Path Length Issues | Ensures Python can handle long paths on Windows, preventing import errors in deep directory structures. | Safe | `OpenTraceProject.App\Services\TweakProviders\DeveloperTweakProvider.cs#L114` |
| <a id="developer.ssh-agent-autostart"></a> `developer.ssh-agent-autostart` | Enable SSH Agent Auto-start | Automatically starts SSH agent on login for seamless Git SSH key authentication. | Safe | `OpenTraceProject.App\Services\TweakProviders\DeveloperTweakProvider.cs#L158` |
| <a id="developer.terminal-dev-mode"></a> `developer.terminal-dev-mode` | Enable Windows Terminal Developer Features | Enables advanced features in Windows Terminal like debug tap and developer mode settings. | Safe | `OpenTraceProject.App\Services\TweakProviders\DeveloperTweakProvider.cs#L70` |
| <a id="developer.vs-intellisense-cache"></a> `developer.vs-intellisense-cache` | Optimize VS IntelliSense Cache | Increases Visual Studio IntelliSense cache size for better code completion performance in large projects. | Safe | `OpenTraceProject.App\Services\TweakProviders\DeveloperTweakProvider.cs#L56` |
| <a id="developer.vs-solution-load"></a> `developer.vs-solution-load` | Speed Up Visual Studio Solution Load | Disables background solution load analysis for faster Visual Studio startup on large solutions. | Safe | `OpenTraceProject.App\Services\TweakProviders\DeveloperTweakProvider.cs#L176` |
| <a id="developer.vscode-git-autofetch"></a> `developer.vscode-git-autofetch` | Disable VS Code Git Autofetch | Disables automatic Git fetching in VS Code to reduce network usage and CPU spikes. | Safe | `OpenTraceProject.App\Services\TweakProviders\DeveloperTweakProvider.cs#L100` |
| <a id="developer.windows-dev-mode"></a> `developer.windows-dev-mode` | Enable Windows Developer Mode | Enables Windows Developer Mode for sideloading apps and accessing advanced development features. Source: Microsoft Windows Developer Docu... | Advanced | `OpenTraceProject.App\Services\TweakProviders\DeveloperTweakProvider.cs#L128` |
| <a id="system-check-disk-health"></a> `system-check-disk-health` | Check Disk Health (C:) | Performs a read-only check of the C: drive for file system errors without making any changes. Provides information about disk health and... | Safe | `OpenTraceProject.Engine\Tweaks\Commands\System\CheckDiskHealthTweak.cs#L14` |
<!-- TWEAK INDEX END -->

`).

## Manual testing checklist (Windows 10/11)
- Use the catalog to drive per-tweak verification on native Windows.
- For each tweak: Detect -> Preview -> Apply -> Verify -> Rollback.
- Capture results in your own checklist (CSV or spreadsheet).

## Elevation requirements
- Tweaks that touch HKLM/HKCR, services, drivers, scheduled tasks, BCD, or system directories must run elevated.
- HKCU and user-profile tweaks can run without elevation.
- Each tweak doc section includes a `Requires elevation:` line to indicate the expected privilege.

### ElevatedHost discovery (dev runs)
When running via `dotnet run`, you can override the elevated host location with:
`OPEN_TRACE_PROJECT_ELEVATED_HOST_PATH=C:\\path\\to\\OpenTraceProject.ElevatedHost.exe`.

## Execution logging

### Logging
- Every step writes to the app log and the structured CSV log (`tweak-log.csv`).
- CSV fields include `timestamp`, `tweak_id`, `tweak_name`, `action`, `status`, `message`, and `error`.

### Execution updates
- The pipeline reports `TweakExecutionUpdate` for each step with action, status, message, and timestamp.
- UI can render live indicators for Detect, Apply, Verify, and Rollback based on these updates.

### Export logs
- `ITweakLogStore.ExportCsvAsync(path)` copies the CSV log to a user-selected destination.

## How to apply/verify/rollback tweaks in the app
- Preview (default): run the pipeline with `DryRun = true` to see what would change.
- Apply: run the pipeline with `DryRun = false`.
- Verify: keep `VerifyAfterApply = true` or call `ITweak.VerifyAsync` explicitly.
- Rollback: restores values captured by the last Detect (same app session) and is also used automatically when Apply/Verify fails.
