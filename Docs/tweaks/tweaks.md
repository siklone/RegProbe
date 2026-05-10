# Tweak Implementation Guide
> Update (2026-05-03): This file is retained as historical reference material. Use the checked-in tweak catalog and research-backed app surface as the live source of truth.

## Overview
Tweaks implement `ITweak` and expose four actions: Detect, Apply, Verify, and Rollback. The execution pipeline is handled by `TweakExecutionPipeline`, which logs every step and supports DryRun/Preview by default.

> **Note (2025-12-30):** Durable rollback state is persisted to `%AppData%\\RegProbe\\rollback-state.json` for crash recovery and cross-session rollback.

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
| <a id="developer.docker-performance"></a> `developer.docker-performance` | Docker Desktop WSL 2 Backend | Docker Desktop can use the WSL 2 backend on Windows. The app writes Docker's documented settings-file field and does not claim a measured... | Medium | `research/records/developer.docker-performance.review.json` |
| <a id="developer.dotnet-telemetry-disable"></a> `developer.dotnet-telemetry-disable` | .NET CLI Telemetry Opt-Out | The .NET SDK and CLI can send telemetry about tool usage. This tweak sets the current user's opt-out environment variable so new .NET CLI... | Medium | `research/records/developer.dotnet-telemetry-disable.json` |
| <a id="developer.enable-windows-long-paths"></a> `developer.enable-windows-long-paths` | Windows Long Paths | Windows can remove the old 260-character path limit. That helps many compatible applications, especially developer tools and projects wit... | Medium | `research/records/developer.enable-windows-long-paths.review.json` |
| <a id="developer.nodejs-performance"></a> `developer.nodejs-performance` | Global Node.js Memory Limit Override | This tweak makes Node.js start with a larger memory limit by default. It can help very large builds or monorepos, but it also makes Node... | Medium | `research/records/developer.nodejs-performance.json` |
| <a id="developer.powershell-execution"></a> `developer.powershell-execution` | PowerShell Script Execution Policy | PowerShell execution policy controls which scripts are allowed to run. RemoteSigned allows local scripts and requires downloaded scripts... | Medium | `research/records/developer.powershell-execution.json` |
| <a id="developer.python-path-fix"></a> `developer.python-path-fix` | Enable Windows Long Paths for Python Workflows | Some Python environments fail on Windows when project folders get very deep. Enabling Windows long paths removes the old 260-character pa... | Medium | `research/records/developer.python-path-fix.review.json` |
| <a id="developer.ssh-agent-autostart"></a> `developer.ssh-agent-autostart` | SSH Agent Auto-start | ssh-agent keeps SSH keys loaded so Git and terminal sessions can use them more easily. Auto-starting it can make developer login sessions... | Medium | `research/records/developer.ssh-agent-autostart.review.json` |
| <a id="developer.terminal-dev-mode"></a> `developer.terminal-dev-mode` | Enable Windows Terminal Developer Features | Enables advanced features in Windows Terminal like debug tap and developer mode settings. | Safe | `app/Services/TweakProviders/DeveloperTweakProvider.cs#L40` |
| <a id="developer.vs-intellisense-cache"></a> `developer.vs-intellisense-cache` | VS IntelliSense DisableAutoUpdating Setting | Writes the Visual Studio DisableAutoUpdating IntelliSense value used by this tweak. | Safe | `app/Services/TweakProviders/DeveloperTweakProvider.cs#L26` |
| <a id="developer.vs-solution-load"></a> `developer.vs-solution-load` | VS Solution BackgroundAnalysis Setting | Writes the Visual Studio BackgroundAnalysis solution-loading value used by this tweak. | Safe | `app/Services/TweakProviders/DeveloperTweakProvider.cs#L70` |
| <a id="developer.vscode-git-autofetch"></a> `developer.vscode-git-autofetch` | Disable VS Code Git Autofetch | Disables automatic Git fetching in VS Code to reduce network usage and CPU spikes. | Safe | `app/Services/TweakProviders/DeveloperTweakProvider.cs#L56` |
| <a id="developer.windows-dev-mode"></a> `developer.windows-dev-mode` | Windows Developer Mode | Developer Mode turns on Windows support for building, sideloading, and debugging apps. Most casual users do not need it, but developers o... | Medium | `research/records/developer.windows-dev-mode.json` |
| <a id="developer.wsl2-memory"></a> `developer.wsl2-memory` | WSL 2 Memory Limit | WSL 2 can use a lot of RAM. Microsoft documents memory limits in .wslconfig, and this tweak writes that documented setting directly. | Medium | `research/records/developer.wsl2-memory.json` |
| <a id="policy.system.enable-virtualization"></a> `policy.system.enable-virtualization` | Enable Virtualization | Enable Virtualization controls legacy UAC file and registry virtualization under the Policies\System branch. | Medium | `research/records/policy.system.enable-virtualization.json` |
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
`REGPROBE_ELEVATED_HOST_PATH=C:\\path\\to\\RegProbe.ElevatedHost.exe`.

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
