# Windows Optimizer

Windows Optimizer is a WPF/.NET 8 desktop app for Windows 10/11 focused on safe, reversible tweaks, hardware-aware diagnostics, and traceable documentation.

![.NET Version](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-TBD-yellow)

## Features

### Monitoring and hardware details
- Real-time CPU, RAM, disk, network, and temperature monitoring
- Dashboard cards for OS, motherboard, CPU, GPU, memory, storage, displays, network, and USB
- Detail windows with richer hardware metadata and source-aware fallbacks
- Optional hardware knowledge DB and icon mapping for better model matching

### Tweak engine
- Safe pipeline: Detect -> Apply -> Verify -> Rollback
- Preview-first behavior by default
- Risk levels: Safe / Advanced / Risky
- Category navigation, search, multi-select, and batch execution
- Export/import profiles and built-in presets

### Documentation and research linkage
- Tweak entries can link back to generated docs and category docs
- Research attribution is centered around `nohuto/win-config` plus official Microsoft sources
- Local docs are copied into build/publish output for in-app navigation

### Extensibility
- Elevated operations are isolated in `WindowsOptimizer.ElevatedHost`
- Plugin loading is supported through the `Plugins` folder
- `WindowsOptimizer.Plugins.DevTools` acts as the bundled example/support plugin

## Solution layout

```text
WindowsOptimizer.App/              WPF UI, view models, assets, app startup
WindowsOptimizer.Core/             Contracts, models, plugin and tweak interfaces
WindowsOptimizer.Engine/           Tweak execution pipeline and providers
WindowsOptimizer.Infrastructure/   Registry, metrics, elevation, hardware access
WindowsOptimizer.ElevatedHost/     Elevated helper process
WindowsOptimizer.CLI/              CLI surface for non-UI scenarios
WindowsOptimizer.Plugins.DevTools/ Bundled plugin/support assembly
WindowsOptimizer.Tests/            Unit tests
Docs/                              Tweak docs, generated catalogs, source attribution
Tools/                             One-off developer utilities for icon work
scripts/                           Packaging, docs, cleanup, and asset generation scripts
```

## Getting started

### Prerequisites
- Windows 10 or Windows 11
- .NET 8 SDK
- PowerShell 7+ recommended for helper scripts

### Build

```powershell
git clone https://github.com/siklone/WPF-Windows-optimizer-with-safe-reversible-tweaks.git
cd WPF-Windows-optimizer-with-safe-reversible-tweaks
dotnet build WindowsOptimizerSuite.sln
```

### Run

```powershell
dotnet run --project WindowsOptimizer.App/WindowsOptimizer.App.csproj
```

If the app cannot locate the elevated helper when running from source, set:

```powershell
$env:WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH = "C:\path\to\WindowsOptimizer.ElevatedHost.exe"
```

### Publish a release build

Use the repo script instead of ad-hoc publish commands so output stays deterministic:

```powershell
pwsh -File scripts/publish_release.ps1
```

### Clean generated build output

```powershell
pwsh -File scripts/clean_build_outputs.ps1 -WhatIfMode:$false
```

## Optional generated assets

The hardware icon and knowledge-db work has two layers:

- Source code fallbacks: rule-based matching and default icons continue to work without generated datasets
- Optional generated inputs: `WindowsOptimizer.App/Assets/HardwareDb/*.json` and extended icon packs improve matching quality and UI coverage when present

Useful related scripts:

- `scripts/build_and_download_icon_db.ps1` regenerates icon source data and downloads icon assets
- The icon generator also scans `WindowsOptimizer.App/Assets/HardwareDb/hardware_db_*.json` so newly introduced `iconKey` values can automatically become downloadable icon targets
- `scripts/publish_release.ps1` creates a deterministic `publish_final/` folder
- `scripts/clean_build_outputs.ps1` removes generated `bin/`, `obj/`, and `publish/` folders

`WindowsOptimizer.App/Assets/HardwareDb/HardwareIconDownloadReport.json` is treated as a local generation report, not a source artifact.

## Safety model

All tweak work is expected to remain reversible and auditable:

- Reversible flow: Detect -> Apply -> Verify -> Rollback
- No automatic system modification on startup
- Logging to `%TEMP%\WindowsOptimizer_Debug.log` and tweak execution logs
- Elevated work runs out-of-process instead of forcing the app to run always-admin

## Documentation

- Main source map: [Docs/TWEAK_SOURCES.md](Docs/TWEAK_SOURCES.md)
- Research attribution: [Docs/RESEARCH_CREDITS.md](Docs/RESEARCH_CREDITS.md)
- Service notes: [Docs/SERVICES_DOCUMENTATION.md](Docs/SERVICES_DOCUMENTATION.md)
- Hardware dashboard manual checklist: [Docs/system/dashboard_hardware_live_checklist.md](Docs/system/dashboard_hardware_live_checklist.md)

## Contributing

`main` is the only intended remote branch. Keep changes small, testable, and reversible, especially around tweaks and elevated operations.

Before opening a PR or merging work:

1. Build the solution.
2. Add or update tests where behavior changed.
3. Keep docs in `Docs/` aligned with tweak/source changes.
4. Avoid committing build outputs, temp probes, or local scratch folders.

## License

TBD. No `LICENSE` file is committed yet.

## Acknowledgments

- [nohuto/win-config](https://github.com/nohuto/win-config)
- [nohuto/win-registry](https://github.com/nohuto/win-registry)
- LibreHardwareMonitor
- Microsoft Learn and related official Windows documentation

Use at your own risk. This app can modify Windows settings, so create a restore point before applying changes.
