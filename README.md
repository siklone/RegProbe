# RegProbe

RegProbe is a Windows desktop configuration shell plus a research workspace for evidence-backed tweak work. The app stays reversible and preview-first, while the repo keeps the proof trail: records, runtime captures, static analysis, audits, and VM orchestration.

![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4)
![Shell](https://img.shields.io/badge/shell-WPF_MVVM-1f2937)
![Research](https://img.shields.io/badge/research-v3.1_pipeline-c0392b)
![License](https://img.shields.io/badge/license-MIT-22c55e)
[![CI](https://github.com/siklone/RegProbe/actions/workflows/dotnet.yml/badge.svg)](https://github.com/siklone/RegProbe/actions/workflows/dotnet.yml)

## What Ships Today

The current app is a focused desktop shell with three top-level surfaces:

- `Configuration`
  the main workspace for browsing, filtering, previewing, and applying reversible tweaks
- `Repairs`
  the operational lane for recovery, cleanup, and repair-focused actions
- `About`
  repository, build, and log context

The UI is intentionally tighter than the older builds. The current shell is a dark, flat-row workspace with:

- a custom title bar
- a category rail on the left
- a dense tweak list instead of boxed cards
- contributor-only evidence metadata kept behind repo/developer gating

Older surfaces such as the hardware dashboard, services browser, bloatware browser, startup manager, disk-health area, and the old policy-heavy shell are not the current shipped experience.

## Core Principles

- SAFE tweaks follow `Detect -> Apply -> Verify -> Rollback`
- the app does not auto-mutate the system on startup
- elevated actions run through `RegProbe.ElevatedHost`, not the main app process
- runtime validation happens in the VM, not on the host
- research classification is evidence-first, not folklore-first

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

Read in this order:

1. [README](README.md)
2. [Contributing](CONTRIBUTING.md)
3. [VM workflow](Docs/VM_WORKFLOW.md)
4. [Script catalog](Docs/SCRIPT_CATALOG.md)
5. [Research atlas](research/evidence-atlas.md)
6. one record under [research/records](research/records)
7. the matching bundle under [evidence/records](evidence/records)

That path gives you the "what", the "how", and the proof trail in the same order contributors usually discover it.

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

This repo was built in a very human-in-the-loop way: mostly with Codex doing the heavy lifting, with a small Claude assist on a few design and review passes. The result is still hand-directed, repo-specific, and evidence-driven rather than generated-once-and-forgotten.
