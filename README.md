# WPF-Windows-optimizer-with-safe-reversible-tweaks

A Windows optimizer built in WPF that focuses on safe, reversible tweaks. Every tweak follows Detect -> Apply -> Verify -> Rollback, with Preview (dry-run) as a first-class path.

## Purpose
This app aims to make system tuning repeatable and transparent. It separates UI from tweak logic, keeps admin-only operations in a separate elevated process, and logs every action so changes can be audited and reversed.

## Design goals
- Safety first: detect state before changes, verify after, rollback on failure.
- Preview by default: DryRun support is built into the pipeline.
- Explicit risk levels: Safe, Advanced, Risky (Safe does not include disabling Defender, Firewall, or SmartScreen).
- Admin operations run through ElevatedHost; the UI process is not always elevated.
- Logs and export: every step written to app log and CSV.

## How the code is written
- WPF MVVM shell with view models and commands that run tweaks in bulk or per-item.
- `ITweak` implementations stay small and composable; multi-step changes use composite tweaks.
- Engine pipeline owns execution flow, progress reporting, and rollback rules.
- Infrastructure adapters isolate OS concerns (registry, services, tasks, file system).
- ElevatedHost handles privileged actions over a named pipe with JSON messages.

## How tweaks are implemented
Tweaks implement `WindowsOptimizer.Core.ITweak` with four actions: `Detect`, `Apply`, `Verify`, `Rollback`.
The engine runs them through `TweakExecutionPipeline` which:
- captures state during Detect,
- applies only when DryRun is false,
- verifies when enabled,
- rolls back automatically on failures (or on demand).

Implemented tweak types include:
- Registry value tweaks (single + batch).
- Settings toggles stored in `settings.json`.
- Composite tweaks that chain multiple sub-tweaks.
- Service start mode batches with optional stop behavior.
- Scheduled task enable/disable batches.
- File rename toggles for system executables.

## Architecture overview
- `WindowsOptimizer.App`: WPF UI, filters, bulk commands, and execution status.
- `WindowsOptimizer.Core`: tweak contracts, enums, and result types.
- `WindowsOptimizer.Engine`: execution pipeline + tweak implementations.
- `WindowsOptimizer.Infrastructure`: adapters (registry, services, tasks, files), settings, logging.
- `WindowsOptimizer.ElevatedHost`: separate admin process (named pipes + JSON messages).
- `WindowsOptimizer.Tests`: unit tests for contracts, adapters, and tweak logic.

## Data and logs
- Settings: `%AppData%\\WindowsOptimizerSuite\\settings.json`
- App log: `%AppData%\\WindowsOptimizerSuite\\logs\\app.log`
- Tweak log: `%AppData%\\WindowsOptimizerSuite\\logs\\tweak-log.csv`
- Logs can be exported via `ITweakLogStore.ExportCsvAsync` and the UI action.

## Build and run
- `dotnet build WindowsOptimizerSuite.slnx`
- `dotnet run --project WindowsOptimizer.App`

## Docs and backlog
The `Docs` tree is the source of truth for tweak intent and safety notes. It also acts as the feature backlog for what should be implemented or validated next.

## Status dashboard (estimate)
![General completion](assets/progress.svg)

Auto snapshot based on Docs + current tweak list. Update with `python3 scripts/update_readme_progress.py`.

<!-- progress:summary:start -->
| Area | Progress | Notes |
| --- | --- | --- |
| Tweaks coverage (docs) | 73% (170/233) <progress value="73" max="100"></progress> | Top-level tweak IDs vs docs headings (coverage capped at 100%) |
| Monitoring | 30% <progress value="30" max="100"></progress> | Pipeline updates + logs exist, richer dashboards pending |
| UI/UX shell | 45% <progress value="45" max="100"></progress> | MVVM shell, filters, bulk actions done; polish ongoing |
| Elevation | 70% <progress value="70" max="100"></progress> | ElevatedHost + registry/services/tasks/files |
| Logging/export | 75% <progress value="75" max="100"></progress> | app.log + tweak-log.csv + export |
| Tests | 25% <progress value="25" max="100"></progress> | Unit tests for pipeline/tweaks/adapters |
| Docs/guides | 35% <progress value="35" max="100"></progress> | Docs exist, README expanding |
<!-- progress:summary:end -->

<!-- progress:tweaks:start -->
| Doc Area | Implemented | Total | Coverage |
| --- | --- | --- | --- |
| affinities | 0 | 1 | 0% |
| cleanup | 0 | 22 | 0% |
| misc | 0 | 13 | 0% |
| network | 27 | 22 | 100% |
| peripheral | 9 | 19 | 47% |
| policies | 0 | 1 | 0% |
| power | 4 | 22 | 18% |
| privacy | 64 | 38 | 100% |
| security | 19 | 24 | 79% |
| system | 25 | 39 | 64% |
| visibility | 22 | 32 | 69% |
| total | 170 | 233 | 73% |
<!-- progress:tweaks:end -->

<!-- progress:overall:start -->
General completion: 50%
<!-- progress:overall:end -->

## Automation
- `scripts/codex_pr.sh` opens or updates a PR from the current branch and leaves `@codex review`.
- Requires GitHub CLI (`gh`) with `gh auth login`.
- Usage: `scripts/codex_pr.sh "Title" "Body"`.
- Optional env vars: `BASE_BRANCH`, `CODEX_COMMENT`, `GH_BIN`.
