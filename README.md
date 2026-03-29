# RegProbe

RegProbe is a .NET 8 desktop app and research workspace for evidence-backed Windows configuration work. It focuses on safe, reversible tweaks and a registry research pipeline that ties each shipped setting back to source notes, VM validation, and captured evidence.

![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4)
![App](https://img.shields.io/badge/app-desktop-1f2937)
![Status](https://img.shields.io/badge/status-active-ff5a5f)
![License](https://img.shields.io/badge/license-TBD-yellow)

## What RegProbe Does

- exposes a Windows desktop UI for reviewing and applying reversible tweaks
- keeps tweak execution inside a `Detect -> Apply -> Verify -> Rollback` safety model
- routes admin-required actions through a separate elevated host instead of forcing the whole app to run as admin
- publishes research records, evidence manifests, audits, and captured artifacts alongside the app
- validates runtime behavior inside the `Win25H2Clean` VM with Procmon, WPR/WPA, ETW, and Ghidra headless workflows

## App Surface

The live product surface is configuration-focused:

- `Configuration`
- `Policy Reference`
- `Settings`
- `About`

The old hardware dashboard, services panel, bloatware browser, startup manager, and disk-health surface are no longer part of the shipped app. Config export/import now stays focused on the current workspace state and no longer carries startup payload data.

## Research Surface

RegProbe ships with a parallel research stack:

- [research](research)
  human-facing records, generated audits, evidence atlas, and notes
- [evidence](evidence)
  normalized evidence bundles and imported artifacts
- [registry-research-framework](registry-research-framework)
  the v3.1 pipeline, schemas, audit queue, routing rules, and tool wrappers

This repo cross-checks official Microsoft sources, repo evidence, and VM runtime captures instead of relying on one-line tweak folklore.

## VM Validation

Runtime validation happens in the VMware guest, not on the host machine.

- supported VM: `Win25H2Clean`
- canonical runtime baseline snapshot: `RegProbe-Baseline-20260328`
- use the VM for live app runs, registry experiments, performance tests, Procmon captures, WPR/WPA traces, ETW, and Ghidra headless analysis
- the canonical baseline keeps Defender enabled and applies bounded exclusions only for trusted tooling paths and processes
- use the host only for source edits, docs, and offline artifact prep
- workflow details live in [Docs/VM_WORKFLOW.md](Docs/VM_WORKFLOW.md)

## Solution Layout

```text
app/                        Desktop UI, views, view models, startup
core/                       Contracts, models, and abstractions
engine/                     Tweak execution pipeline and tweak implementations
infrastructure/             Registry, elevation, files, and adapters
elevated-host/              Elevated helper process for admin-required actions
cli/                        CLI entry point
plugins-devtools/           Example/support plugin assembly
tests/                      Unit tests
research/                   Published research records and generated outputs
evidence/                   Evidence bundles and imported artifacts
registry-research-framework/ Pipeline v3.1 orchestration and schemas
Docs/                       Supporting docs and workflows
Tools/                      Developer utilities
scripts/                    Build, publish, cleanup, and validation scripts
```

## Safety Model

SAFE tweak work stays preview-first, reversible, and logged.

- `Detect -> Apply -> Verify -> Rollback`
- no automatic system mutations on startup
- elevated work stays outside the main app process
- tweak activity is logged and exportable

Under SAFE, RegProbe does not add one-click flows that disable Defender, Firewall, or SmartScreen.

## Getting Started

### Prerequisites

- Windows 10 or Windows 11
- .NET 8 SDK
- PowerShell 7+ recommended

### Clone

```powershell
git clone https://github.com/siklone/RegProbe.git
cd RegProbe
```

### Build

```powershell
dotnet build RegProbe.sln -c Release
```

### Run

```powershell
dotnet run --project app/app.csproj
```

### Test

```powershell
dotnet test tests/tests.csproj -c Release --no-build -v minimal
```

### Publish

```powershell
pwsh -File scripts/publish_release.ps1
```

## Elevated Host Override

If the app cannot discover the elevated host automatically, use:

```powershell
$env:REGPROBE_ELEVATED_HOST_PATH = "C:\path\to\RegProbe.ElevatedHost.exe"
```

## Useful Entry Points

- [research/README.md](research/README.md)
- [research/evidence-atlas.md](research/evidence-atlas.md)
- [Docs/VM_WORKFLOW.md](Docs/VM_WORKFLOW.md)
- [Docs/TWEAK_SOURCES.md](Docs/TWEAK_SOURCES.md)
