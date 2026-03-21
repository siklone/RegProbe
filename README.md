# Windows Optimizer Suite

Windows Optimizer Suite is a WPF/.NET 8 desktop app for Windows 10/11 focused on **safe, reversible tweaks**, **hardware-aware diagnostics**, and **traceable system research**.

It is built for people who want more than a random "FPS boost" script:

- 🛡️ every SAFE tweak follows `Detect -> Apply -> Verify -> Rollback`
- 🧠 the app understands the PC it is running on
- 🔍 tweaks are increasingly backed by documented sources and local audit notes
- ⚡ elevated actions run out-of-process through a dedicated host instead of forcing the whole app to run as admin

![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4)
![UI](https://img.shields.io/badge/UI-WPF-1f2937)
![Status](https://img.shields.io/badge/status-active%20development-ff5a5f)
![License](https://img.shields.io/badge/license-TBD-yellow)

## ✨ What It Does

### 🧩 Configuration Workspace
- Browse tweak categories with search, filters, live status, and batch selection
- Apply single tweaks directly from the row or queue multiple tweaks for bulk actions
- Use richer policy-aware and registry-aware entries instead of opaque one-line scripts
- Explore dedicated sections for:
  - `Configuration`
  - `Policy Reference`
  - `Services`
  - `Bloatware`
  - `Startup`

### Dashboard & Hardware Details
- Hardware-first dashboard for OS, motherboard, CPU, GPU, memory, storage, displays, network, USB, and audio
- Hardware detail sheets with charts, adapter details, and hardware-specific summaries
- Device detail windows with cleaner specs, insights, and grouped metadata

### 🧪 Tweak Engine
- SAFE pipeline: `Detect -> Apply -> Verify -> Rollback`
- Risk levels: `Safe`, `Advanced`, `Risky`
- Local logging and exportable tweak history
- Support for preset-backed tweaks and registry-backed multi-value actions
- Elevated registry and command execution via `WindowsOptimizer.ElevatedHost`

### 📚 Research & Provenance
- Local documentation and source mapping under `Docs/`
- Nohuto-oriented research integration (`win-config`, `win-registry`, related audits)
- Microsoft-backed and repo-backed coverage tests for tweak providers

## 🧪 VM Validation Environment

Runtime validation for this project happens in a dedicated VMware VM, not on the host machine.

- Supported VM: `Win25H2Clean`
- Use the VM for live app runs, registry experiments, performance testing, Procmon captures, WPR/WPA traces, and Ghidra headless analysis
- Host usage is for source editing and offline preparation only
- Tooling and wrapper paths are documented in [Docs/VM_WORKFLOW.md](Docs/VM_WORKFLOW.md)

Available in the VM:

- WPR / WPA / xperf
- Procmon with a safe capture wrapper
- Ghidra headless
- Java 21 for Ghidra

## 🚀 Why This Project Feels Different

Most Windows tweak tools stop at "click button, hope for the best."

Windows Optimizer Suite aims for something better:

- 🧭 **Readable UI** instead of script-dump UX
- 🔁 **Reversible behavior** instead of blind mutation
- 🪟 **Windows-native shell** instead of a web wrapper
- 🛠️ **Real hardware context** instead of generic system labels
- 🧾 **Source-aware documentation** instead of folklore-only tuning

## 🖥️ Current Surface Areas

### Dashboard
- System hero card
- Quick actions
- Hardware grid
- Drivers & recommended installs

### Configuration
- Searchable tweak workspace
- Category rail
- Inline apply actions
- Batch queue / batch actions
- Policy browser
- Services browser
- Bloatware remover
- Startup manager

### Hardware Details
- Detail sheets
- Trend charts
- Adapter / disk / GPU breakdowns
- Hardware summary cards

### Settings
- Theme and behavior preferences
- Startup scan behavior
- Preview hints and shell preferences

## 🧱 Solution Layout

```text
WindowsOptimizer.App/              WPF UI, view models, startup, assets, views
WindowsOptimizer.Core/             Contracts, models, plugin and tweak abstractions
WindowsOptimizer.Engine/           Tweak execution pipeline and concrete tweak types
WindowsOptimizer.Infrastructure/   Registry, elevation, files, hardware info
WindowsOptimizer.ElevatedHost/     Elevated helper process for admin-required actions
WindowsOptimizer.CLI/              CLI entry point for non-UI scenarios
WindowsOptimizer.Plugins.DevTools/ Bundled example/support plugin
WindowsOptimizer.Tests/            Unit tests
Docs/                              Research notes, audits, tweak source maps
Tools/                             One-off developer utilities
scripts/                           Build, publish, cleanup, asset generation
```

## 🛡️ Safety Model

All SAFE tweak work is expected to remain reversible and auditable.

- ✅ `Detect -> Apply -> Verify -> Rollback`
- ✅ no automatic system modification on startup
- ✅ elevated work is isolated from the main app process
- ✅ tweak activity is logged
- ✅ preview-first workflow is preserved

Things this project intentionally avoids under SAFE:

- ❌ disable Defender
- ❌ disable Firewall
- ❌ disable SmartScreen
- ❌ irreversible "trust me bro" system mutations

## 🏁 Getting Started

### Prerequisites
- Windows 10 or Windows 11
- .NET 8 SDK
- PowerShell 7+ recommended

### Clone

```powershell
git clone https://github.com/siklone/WPF-Windows-optimizer-with-safe-reversible-tweaks.git
cd WPF-Windows-optimizer-with-safe-reversible-tweaks
```

### Build

```powershell
dotnet build WindowsOptimizerSuite.sln
```

### Run the app

```powershell
dotnet run --project WindowsOptimizer.App/WindowsOptimizer.App.csproj
```

### Run tests

```powershell
dotnet test WindowsOptimizer.Tests/WindowsOptimizer.Tests.csproj -v minimal
```

### Publish

Use the repo script for deterministic output:

```powershell
pwsh -File scripts/publish_release.ps1
```

### Clean build artifacts

```powershell
pwsh -File scripts/clean_build_outputs.ps1 -WhatIfMode:$false
```

## 🔐 Elevated Host Notes

The app is **not** always-admin.

Admin-required operations are delegated to `WindowsOptimizer.ElevatedHost`, which is resolved by:

- normal publish layout
- app discovery logic
- optional override through:

```powershell
$env:WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH = "C:\path\to\WindowsOptimizer.ElevatedHost.exe"
```

## 🧠 Optional Generated Assets

Hardware coverage has two layers:

- built-in rule-based fallbacks
- optional generated icon / hardware-db inputs for better matching

Useful scripts:

- `scripts/build_and_download_icon_db.ps1`
- `scripts/publish_release.ps1`
- `scripts/clean_build_outputs.ps1`

Generation reports such as `HardwareIconDownloadReport.json` are local artifacts, not core source files.

## 📖 Documentation

Good starting points:

- [Docs/VM_WORKFLOW.md](Docs/VM_WORKFLOW.md)
- [Docs/TWEAK_SOURCES.md](Docs/TWEAK_SOURCES.md)
- [Docs/RESEARCH_CREDITS.md](Docs/RESEARCH_CREDITS.md)
- [Docs/SERVICES_DOCUMENTATION.md](Docs/SERVICES_DOCUMENTATION.md)
- [Docs/NOHUTO_CONFIGURATION_AUDIT_2026-03-09.md](Docs/NOHUTO_CONFIGURATION_AUDIT_2026-03-09.md)
- [Docs/NOHUTO_TRANCHE_EVALUATION_2026-03-09.md](Docs/NOHUTO_TRANCHE_EVALUATION_2026-03-09.md)
- [ARCHITECTURE.md](ARCHITECTURE.md)

## 🧪 Development Notes

- WPF + MVVM shell architecture
- Admin work goes through `ElevatedHost`
- Prefer small, composable services
- Add tests when changing engine contracts, registry routing, command-backed tweaks, or provider coverage
- `main` is the intended remote branch

## 🤝 Contributing

Before opening a PR or pushing a larger change:

1. Build the solution
2. Run tests
3. Keep docs in sync with tweak/source changes
4. Do not commit temp outputs, scratch folders, or generated junk

## 🙏 Acknowledgments

- [nohuto/win-config](https://github.com/nohuto/win-config)
- [nohuto/win-registry](https://github.com/nohuto/win-registry)
- Microsoft Learn / Windows documentation
- LibreHardwareMonitor

## ⚠️ Disclaimer

This app can modify Windows settings.

Use it carefully, review what a tweak changes, and create a restore point before doing broad system changes.

## 📄 License

`LICENSE` has not been committed yet, so the project is currently **license TBD**.
