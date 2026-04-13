# RegProbe

<p align="center">
  <img src="assets/brand/regprobe-logo-full.png" alt="RegProbe logo" width="320">
</p>

RegProbe started as a personal frustration with the state of Windows registry "optimization" advice. Most of it is folklore, some of it is actively harmful, and almost none of it is verified before people are told to run it. The goal here is simple: before RegProbe applies anything, prove what the setting is, where it lives, what Windows appears to do with it, and how to get back.

The app is the visible part of that idea. The repo is the trail behind it: research records, runtime captures, static analysis, VM notes, audits, and the scripts used to reproduce the evidence. RegProbe is intentionally preview-first and reversible because registry tooling should feel calmer than the problem it is trying to solve.

![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4)
![Shell](https://img.shields.io/badge/shell-WPF_MVVM-1f2937)
![Research](https://img.shields.io/badge/research-v3.2_pipeline-c0392b)
![License](https://img.shields.io/badge/license-MIT-22c55e)
[![CI](https://github.com/siklone/RegProbe/actions/workflows/dotnet.yml/badge.svg)](https://github.com/siklone/RegProbe/actions/workflows/dotnet.yml)

## What Ships Today

The shipped app is a focused three-surface shell. `Configuration` is the main workspace, `Repairs` handles recovery and cleanup actions, and `About` keeps repo, build, and log context close at hand. The current UI is deliberately tighter than older builds: dark, flat, list-first, and more interested in exposing research than in showing off.

That restraint is intentional. Older surfaces such as the hardware dashboard, services browser, bloatware browser, startup manager, disk-health area, and the old policy-heavy shell are no longer part of the shipped experience. Contributor-only evidence metadata still exists, but it stays behind repo and developer gating instead of turning the app into a research database with buttons.

## Core Principles

RegProbe does not mutate the system on startup. SAFE tweaks follow `Detect -> Apply -> Verify -> Rollback`, and elevated work goes through `RegProbe.ElevatedHost` instead of the main process. The happy path is not "click and hope"; it is "preview, apply deliberately, verify, and keep a restore story."

Runtime validation belongs in the VM, not on the host. If a setting touches kernel, boot, driver, power, or system policy behavior, the repo expects a mapped runtime lane before that evidence is treated as executed proof. Static analysis can narrow the path, but it does not get to pretend it is a live capture.

The research posture is evidence-first, not folklore-first. The v3.2 hardening pass made that more explicit: committed Ghidra artifacts are PDB-backed and bounded, the broken-link and ghidra-bloat queues are closed, Nohuto priority records were re-audited first, and `IDA` is optional while `Ghidra + PDB` remains the normal static lane.

## Evidence Contract

This section stays technical because it is the contributor contract. The first quality-hardening pass changed what counts as evidence:

- `full-evidence.json.artifact_refs` are structured objects, not loose strings.
- Every physical artifact carries `path`, `sha256`, `size`, and `collected_utc`.
- `staged` manifests are allowed as planning state, but they are not treated as captured evidence.
- Runtime lanes that claim live capture must point to physical ETL, PML, JSON, or equivalent artifacts, or they are downgraded to `missing-capture`.
- Kernel, boot, and driver-facing records must finish with a real mapped runtime lane before they can count as executed evidence.

If you see a manifest without capture artifacts, treat it as orchestration metadata, not proof.

## Where The Research Stands

The research workspace is now less about running one-off experiments and more about keeping a living, auditable map of what has been proven. It tracks evidence freshness by tested Windows build, keeps regression history for revalidation after major build changes, records tweak interactions and dependency datasets, and carries anti-cheat or DRM advisory risk tags where they are known. The current validation baseline also has a reproducibility manifest, so a future run can tell whether it is comparing like with like.

Negative evidence matters here. Archived and no-hit records are not just shrugged away; their failed traces, missing captures, and narrowed hypotheses are published so the same dead ends do not get rediscovered later. Useful entry points are the [Regression history](research/regression-history.json), the [Negative evidence index](research/evidence-not-found/index.json), and the still-useful [Pipeline v3.1](registry-research-framework/docs/pipeline-v3.1.md) notes.

Collection mode is explicit now. `evidence` is the safe default for research and audits: automatic rollback is suppressed, pre-change and post-change exports are expected, and manifests carry `rollback_pending = true` until a later explicit cleanup run. `operational` keeps the older convenience behavior for flows where automatic rollback is intentionally allowed, but it is not the default for evidence collection.

VM secret handling was also tightened. Repo-tracked VM scripts no longer keep plaintext guest passwords. Credentials are resolved from explicit input first, then environment variables such as `REGPROBE_VM_GUEST_USER` and `REGPROBE_VM_GUEST_PASSWORD`, and finally from a DPAPI-protected CLIXML credential file referenced outside the repo. `vmrun` still consumes credentials at invocation time because that is a VMware CLI limitation, but the repo avoids storing or logging those secrets directly and the shared VM helper masks them in runner output.

Hard runtime cases no longer stop at "reboot and idle." The current escalation path moves from targeted `ETW` or runtime trace work, to the safe mega-trigger runtime lane, to `WinDbg` boot registry tracing when QGA allows it, and then to source-enrichment cross-reference through `ReactOS`, `WRK`, `System Informer`, `Sandboxie`, `Wine`, `ADMX`, and `WDK`. The newer research work is less a banner than a rhythm now: ETL discovery feeds the queue, feature-area enrichment and triage keep the noise down, VM safety bench results can promote only the profiles that deserve it, and hard blockers say plainly what is missing instead of hiding behind generic review language. Some lanes are still intentionally held, but they are held with reasons: exact runtime reads are missing, the VM cannot expose the right power state, or the probe is boot-unsafe without a dedicated lane.

For the full validation flow, start with the [VM workflow](Docs/VM_WORKFLOW.md), [Runtime escalation](Docs/RUNTIME_ESCALATION.md), and [Pipeline v3.1](registry-research-framework/docs/pipeline-v3.1.md).

## Research Health

<!-- BEGIN:RESEARCH_HEALTH -->
| Metric | Value |
|--------|-------|
| Promoted | 250 |
| Blocked | 18 |
| Revalidation Pending | 0 |
| Gate Health | green |
| Schema Complete | 100% |
| Missing Docs | 0 |
<!-- END:RESEARCH_HEALTH -->

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
registry-research-framework/ v3.2 routing, phases, tools, manifests
Docs/                        Workflow and contributor-facing docs
scripts/                     Build, package, VM, and validation helpers
```

## VM Reality

The supported validation VM is `Win25H2Clean`, and the current canonical snapshot is `RegProbe-Baseline-ToolsHardened-20260330`.

The baseline is tooling-first. Defender stays enabled, exclusions are bounded to trusted tooling, app payloads do not persist in the saved baseline, and app launch smoke is allowed only as an ephemeral deploy/validate/cleanup lane. The details matter because registry evidence collected from a messy VM is worse than no evidence: it looks authoritative while quietly carrying someone else's state.

Start with the [VM workflow](Docs/VM_WORKFLOW.md) when you need the whole flow, and use [Runtime escalation](Docs/RUNTIME_ESCALATION.md) when a value needs more than a simple before/after check.

## Scripts

The repo has a lot of PowerShell, but not every script has the same job. Some scripts are everyday build, package, clean, baseline maintenance, shell-health, and app-smoke helpers. Some are active research runners for current escalation lanes. Others are historical reproducibility scripts kept because old notes, audits, and evidence bundles still depend on them.

Regenerable clutter such as `bin/`, `obj/`, `publish/`, `dist/`, and `TestResults/` can be cleaned freely. Narrow `.ps1` runners should be treated more carefully; many exist so an old evidence claim can still be replayed. The full map lives in the [Script catalog](Docs/SCRIPT_CATALOG.md), and it is worth reading before deleting anything that looks oddly specific.

## Where To Start If You Want To Learn

Start with this [README](README.md) and [Contributing](CONTRIBUTING.md), then move to the [VM workflow](Docs/VM_WORKFLOW.md) and the [script catalog](Docs/SCRIPT_CATALOG.md). After that, open the [research atlas](research/evidence-atlas.md), pick one record under [research/records](research/records), and read it next to its matching bundle under [evidence/records](evidence/records). That path gives you the "what", the "how", and the proof trail in the same order most contributors discover it.

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

Most day-to-day contributors will want [Contributing](CONTRIBUTING.md), [VM workflow](Docs/VM_WORKFLOW.md), [Runtime escalation](Docs/RUNTIME_ESCALATION.md), [Script catalog](Docs/SCRIPT_CATALOG.md), [Tweak sources](Docs/TWEAK_SOURCES.md), the [Research readme](research/README.md), the [Evidence atlas](research/evidence-atlas.md), and the current [Evidence audit](research/evidence-audit.json).

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).

## Studio Note

Built with Codex for the heavy lifting and occasional Claude passes on design and review. Everything here is hand-directed and repo-specific. The tools help carry the weight; the direction, judgment, and weird little scars are all from working through this repo for real.
