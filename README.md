# RegProbe

RegProbe is a Windows desktop configuration shell plus a research workspace for evidence-backed tweak work. The app stays reversible and preview-first, while the repo keeps the proof trail: records, runtime captures, static analysis, audits, and VM orchestration.

![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4)
![Shell](https://img.shields.io/badge/shell-WPF_MVVM-1f2937)
![Research](https://img.shields.io/badge/research-v3.1_pipeline-c0392b)
![License](https://img.shields.io/badge/license-MIT-22c55e)
[![CI](https://github.com/siklone/RegProbe/actions/workflows/dotnet.yml/badge.svg)](https://github.com/siklone/RegProbe/actions/workflows/dotnet.yml)

## What Ships Today

The shipped app is a focused three-surface shell: `Configuration` for the main workspace, `Repairs` for recovery and cleanup actions, and `About` for repo, build, and log context. The current UI is deliberately tighter than the older builds. It is a dark, flat-row workspace with a custom chrome, a left category rail, and a denser list-first layout instead of boxed cards. Contributor-only evidence metadata still exists, but it stays behind repo and developer gating.

Older surfaces such as the hardware dashboard, services browser, bloatware browser, startup manager, disk-health area, and the old policy-heavy shell are no longer part of the shipped experience.

## Core Principles

RegProbe stays preview-first and reversible. SAFE tweaks follow `Detect -> Apply -> Verify -> Rollback`, the app does not mutate the system on startup, and elevated work runs through `RegProbe.ElevatedHost` instead of the main process. Runtime validation happens in the VM, not on the host, and research classification is evidence-first, not folklore-first.

## Repo Shape

```text
app/                         WPF shell, views, view models, resources
core/                        Contracts and shared models
engine/                      Tweak implementations and execution pipeline
infrastructure/              Registry, file, process, and elevation adapters
elevated-host/               Separate admin helper process
cli/                         Command-line entry point
tests/                       Unit and behavior tests
research/                    Human-facing records, notes, audit outputs
evidence/                    Bundles and imported runtime/static artifacts
registry-research-framework/ v3.1 routing, phases, tools, manifests
Docs/                        Workflow and contributor-facing docs
scripts/                     Build, package, VM, and validation helpers
```

## VM Reality

The supported validation VM is `Win25H2Clean`.

Current canonical snapshot:

- `RegProbe-Baseline-ToolsHardened-20260330`

Current baseline policy:

- tooling-first
- Defender stays enabled
- bounded exclusions only for trusted tooling
- app payloads do not persist in the saved baseline
- app launch smoke is allowed only as an ephemeral deploy/validate/cleanup lane

Start here for the full flow:

- [VM workflow](Docs/VM_WORKFLOW.md)

## Scripts: What We Actually Use

The repo has a lot of PowerShell, but not all of it serves the same purpose.

- everyday scripts
  build, package, clean, baseline maintenance, shell health, app smoke
- active research scripts
  current v3.1 runners and follow-up lanes
- historical reproducibility scripts
  older one-off runners kept because notes, audits, and evidence bundles still depend on them
- regenerable clutter
  `bin/`, `obj/`, `publish/`, `dist/`, and `TestResults/`

The full script map lives here:

- [Script catalog](Docs/SCRIPT_CATALOG.md)

If you are trying to learn the repo, read that file before deleting any `.ps1`. Many narrow runners are intentionally preserved so an old evidence claim can still be replayed.

## Where To Start If You Want To Learn

Start with [README](README.md) and [Contributing](CONTRIBUTING.md), then move to the [VM workflow](Docs/VM_WORKFLOW.md) and the [script catalog](Docs/SCRIPT_CATALOG.md). After that, open the [research atlas](research/evidence-atlas.md), pick one record under [research/records](research/records), and read it next to its matching bundle under [evidence/records](evidence/records). That path gives you the "what", the "how", and the proof trail in the same order most contributors discover it.

## Build And Run

### Prerequisites

- Windows 10 or Windows 11
- .NET 8 SDK
- PowerShell 7+ recommended for script work

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

### Package

```powershell
pwsh -File scripts/package_windows.ps1 -Configuration Release -Runtime win-x64
```

### Publish

```powershell
pwsh -File scripts/publish_release.ps1
```

## Useful Entry Points

- [Contributing](CONTRIBUTING.md)
- [VM workflow](Docs/VM_WORKFLOW.md)
- [Script catalog](Docs/SCRIPT_CATALOG.md)
- [Tweak sources](Docs/TWEAK_SOURCES.md)
- [Research readme](research/README.md)
- [Evidence atlas](research/evidence-atlas.md)
- [Evidence audit](research/evidence-audit.json)

## License

This project is licensed under the MIT License.

- [LICENSE](LICENSE)

## Studio Note

Built with Codex for the heavy lifting and occasional Claude passes on design and review. Everything here is hand-directed and repo-specific.
