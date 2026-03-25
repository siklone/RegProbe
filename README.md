# Open Trace Project

Open Trace Project is a .NET 8 desktop app for Windows 10/11. It focuses on safe, reversible tweaks, hardware details, and documented system research.

The project is for people who want more than a random "FPS boost" script:

- every SAFE tweak follows `Detect -> Apply -> Verify -> Rollback`
- the app reads the PC it is running on
- tweaks are tied to source notes and local validation
- elevated work runs through a separate host instead of forcing the whole app to run as admin

![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4)
![App](https://img.shields.io/badge/app-desktop-1f2937)
![Status](https://img.shields.io/badge/status-active%20development-ff5a5f)
![License](https://img.shields.io/badge/license-TBD-yellow)

## What It Does

### Configuration workspace

- browse tweak categories with search, filters, live status, and batch selection
- apply one tweak from the row or queue several for batch work
- use policy-aware and registry-aware entries instead of opaque one-line scripts
- work in dedicated sections for `Configuration`, `Policy Reference`, `Services`, `Bloatware`, and `Startup`

### Dashboard and hardware details

- hardware-first dashboard for OS, motherboard, CPU, GPU, memory, storage, displays, network, USB, and audio
- hardware detail sheets with charts, adapter details, and grouped summaries
- device detail windows with cleaner specs and grouped metadata

### Tweak engine

- SAFE pipeline: `Detect -> Apply -> Verify -> Rollback`
- risk levels: `Safe`, `Advanced`, `Risky`
- local logging and exportable tweak history
- preset-backed tweaks and registry-backed multi-value actions
- elevated registry and command execution through `elevated-host`

### Research and sources

- research records, evidence, and captured artifacts under `research/`
- supporting docs under `Docs/`
- uses Nohuto's research (`win-config`, `win-registry`) and cross-checks it against live builds
- Microsoft-backed and repo-backed coverage tests for tweak providers

## VM Validation Environment

Runtime validation happens in a VMware VM, not on the host machine.

- supported VM: `Win25H2Clean`
- use the VM for live app runs, registry experiments, performance testing, Procmon captures, WPR/WPA traces, and Ghidra headless analysis
- use the host only for source editing and offline prep
- tooling and wrapper paths are in [Docs/VM_WORKFLOW.md](Docs/VM_WORKFLOW.md)

Available in the VM:

- WPR / WPA / xperf
- Procmon with a safe capture wrapper
- Ghidra headless
- Java 21 for Ghidra

## Why It Exists

Most Windows tweak tools stop at "click button, hope for the best."

This project does not.

- the UI is readable instead of a script dump
- the behavior is reversible instead of blind mutation
- the shell is Windows-native instead of a web wrapper
- the hardware view shows real device context instead of generic labels
- the docs point back to sources instead of forum folklore

## Current Sections

### Dashboard

- system hero card
- quick actions
- hardware grid
- drivers and recommended installs

### Configuration

- searchable tweak workspace
- category rail
- inline apply actions
- batch queue / batch actions
- policy browser
- services browser
- bloatware remover
- startup manager

### Hardware Details

- detail sheets
- trend charts
- adapter / disk / GPU breakdowns
- hardware summary cards

### Settings

- theme and behavior preferences
- startup scan behavior
- preview hints and shell preferences

## Solution Layout

```text
app/              Desktop UI, view models, startup, assets, views
core/             Contracts, models, plugin and tweak abstractions
engine/           Tweak execution pipeline and concrete tweak types
infrastructure/   Registry, elevation, files, hardware info
elevated-host/    Elevated helper process for admin-required actions
cli/              CLI entry point for non-UI scenarios
plugins-devtools/ Bundled example/support plugin
tests/            Unit tests
research/         Records, evidence, captured files, generated audit outputs
Docs/             Supporting docs, workflows, and longer notes
Tools/            One-off developer utilities
scripts/          Build, publish, cleanup, asset generation
```

## Safety Model

SAFE tweak work is expected to stay reversible and logged.

- `Detect -> Apply -> Verify -> Rollback`
- no automatic system changes on startup
- elevated work stays out of the main app process
- tweak activity is logged
- the workflow stays preview-first

Things this project does not do under SAFE:

- disable Defender
- disable Firewall
- disable SmartScreen
- ship irreversible "trust me bro" system mutations

## Getting Started

### Prerequisites

- Windows 10 or Windows 11
- .NET 8 SDK
- PowerShell 7+ recommended

### Clone

```powershell
git clone https://github.com/siklone/Open-Trace-Project.git
cd Open-Trace-Project
```

### Build

```powershell
dotnet build OpenTraceProject.sln
```

### Run the app

```powershell
dotnet run --project app/app.csproj
```

### Run tests

```powershell
dotnet test tests/tests.csproj -v minimal
```

### Publish

```powershell
pwsh -File scripts/publish_release.ps1
```

### Clean build artifacts

```powershell
pwsh -File scripts/clean_build_outputs.ps1 -WhatIfMode:$false
```

## Elevated Host Notes

The app is not always-admin.

Admin-required operations are delegated to `elevated-host`, which is resolved by:

- normal publish layout
- app discovery logic
- optional override through:

```powershell
$env:OPEN_TRACE_PROJECT_ELEVATED_HOST_PATH = "C:\path\to\OpenTraceProject.ElevatedHost.exe"
```

## Utility Scripts

Useful scripts:

- `scripts/publish_release.ps1`
- `scripts/clean_build_outputs.ps1`

## Documentation

Good starting points:

- [research/README.md](research/README.md)
- [research/evidence-atlas.md](research/evidence-atlas.md)
- [Docs/VM_WORKFLOW.md](Docs/VM_WORKFLOW.md)
- [Docs/TWEAK_SOURCES.md](Docs/TWEAK_SOURCES.md)
- [Docs/RESEARCH_CREDITS.md](Docs/RESEARCH_CREDITS.md)
- [Docs/SERVICES_DOCUMENTATION.md](Docs/SERVICES_DOCUMENTATION.md)
