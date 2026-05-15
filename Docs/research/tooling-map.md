# Research Tooling Map

This page is the contributor-facing map for RegProbe tooling. It does not move
files or rename scripts; it explains which entry points are canonical today and
which ones are retained for compatibility or history.

## Audience Boundary

RegProbe has three audiences:

- End users use the WPF desktop app. They should see cards, verdicts, current
  value, known/default value when available, target value, and rollback story.
  They should not need to understand VM runners, tranches, host-noise gates, or
  raw ETW/Procmon/Ghidra artifacts.
- Contributors use Python scripts, JSON artifacts, VM runners, and research
  records. This includes human maintainers and agentic AI coding agents.
- The .NET CLI is an optional compatibility and advanced/headless surface. It is
  not the canonical research API.

## Canonical Contributor API

For research and app-retest workflows, call the Python scripts directly and
parse their JSON outputs. This is the stable contributor contract because it
works on Linux hosts that do not have `Microsoft.WindowsDesktop.App`, exposes
the exact artifact paths, and is already what agentic AI workflows use.

Use the .NET CLI only when you explicitly need the Windows app-side command
surface, such as advanced `tweak list`, `tweak apply`, or `tweak revert`
automation on a Windows/VM host.

## Active Scripts

These scripts are active entry points for day-to-day contributor work.

| Script | Status | Use |
|---|---|---|
| `registry-research-framework/scripts/check_single_tweak.py` | canonical | Inspect one tweak, value, or registry path and emit human/JSON results. |
| `registry-research-framework/scripts/check_single_tweak_app_qa.py` | canonical | Generate the app QA plan for one card before opening the desktop app. |
| `registry-research-framework/scripts/check_promoted_tweak_app_qa_batch.py` | canonical | Plan or run promoted app-QA batches, including optional KVM execution. |
| `registry-research-framework/scripts/check_app_retest_readiness.py` | canonical | Verify app-surface readiness before manual or VM app retesting. |
| `scripts/vm-kvm/vm-health-check.py` | canonical | Non-mutating KVM/QGA and optional `--snapshot-name` health check before ETW, Procmon, Ghidra, app-QA, or value-experiment lanes. |
| `scripts/vm-kvm/run-guest-app-tweak-qa-batch.py` | canonical | Host-driven KVM batch runner for shipped app-card QA. |
| `scripts/vm-kvm/run-guest-registry-value-campaign.py` | canonical | Snapshot-safe registry value campaign runner for user-supplied key/path/value experiments. |
| `scripts/vm-kvm/run-guest-registry-value-experiment.py` | canonical | One-value VM experiment runner with host noise gate, optional `--abort-on-noisy-host`, and reboot/rollback checks. |
| `registry-research-framework/scripts/analyze_registry_value_experiments.py` | canonical | Retro-analyze registry value experiment artifacts and produce verdict summaries. |
| `registry-research-framework/scripts/generate_research_artifact_map.py` | canonical | Generate the current artifact map so contributors start from canonical outputs instead of raw parse folders. |
| `registry-research-framework/scripts/generate_operator96_enriched_value_matrix.py` | canonical | Generate enriched candidate values with source boundaries and community-hint tags for the current custom-value seed batch; `operator96` is only the legacy file/campaign name. |
| `registry-research-framework/scripts/generate_operator96_app_surface_review.py` | canonical | Review enriched custom registry value experiments for app-surface eligibility; keep records in Contributor Lab unless the bounded-card gate passes. |
| `registry-research-framework/scripts/generate_operator96_low_noise_rerun_plan.py` | canonical | Plan low-noise reruns for custom-value records whose prior observations are not reference quality. |
| `registry-research-framework/scripts/aggregate_operator96_low_noise_rerun_campaign.py` | canonical | Aggregate custom-value low-noise tranche outputs into one campaign summary. |
| `scripts/refresh_research_publish_surfaces.py` | canonical | Refresh generated research publish surfaces after record or gate changes. |
| `scripts/generate_promotion_gates.py` | canonical | Rebuild app promotion gates from validated research state. |
| `scripts/research/generate_app_surface_manifest.py` | canonical | Rebuild the app-surface manifest from surfaceable research records. |

## VM Evidence Lanes

These KVM runners are active when a record needs a specific runtime evidence
lane. Run `vm-health-check.py` first and keep QGA-first transport unless a task
explicitly calls for `send-key`.

| Script | Status | Use |
|---|---|---|
| `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py` | canonical | ETW stackwalk capture for registry read/write evidence. |
| `scripts/vm-kvm/run-guest-procmon-bootlog.py` | canonical | Procmon boot-log capture for startup/runtime registry access. |
| `scripts/vm-kvm/run-guest-wpr-boot-registry.py` | canonical | Boot-time WPR registry trace capture. |
| `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py` | canonical | Ghidra string xref probe for static lineage. |
| `scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py` | canonical | Ghidra symbolized branch probe for static lineage. |
| `scripts/vm-kvm/run-guest-reboot-observation.py` | canonical | Reboot observation for boot-safety and rollback behavior. |

## Compatibility Surfaces

The .NET research CLI commands are compatibility wrappers around the Python
scripts above. Prefer Python for new contributor work.

| Command | Status | Preferred replacement |
|---|---|---|
| `dotnet run --project cli/cli.csproj -- research inspect ...` | compatibility | `check_single_tweak.py` |
| `dotnet run --project cli/cli.csproj -- research qa-plan ...` | compatibility | `check_single_tweak_app_qa.py` |
| `dotnet run --project cli/cli.csproj -- research qa-batch ...` | compatibility | `check_promoted_tweak_app_qa_batch.py` |
| `dotnet run --project cli/cli.csproj -- research readiness ...` | compatibility | `check_app_retest_readiness.py` |

The `tweak list`, `tweak apply`, and `tweak revert` CLI commands remain useful
for advanced Windows/headless workflows. They are not required for normal app
users and should not be presented as the primary research path.

## WPF Contributor Lab

Repo/dev builds expose a gated Contributor Lab in the desktop app. It is a
Windows-first companion for contributors, not an end-user optimization screen.
Use it to check local readiness, copy canonical Python command packs, and review
custom registry value experiment observations without promoting them to normal
app cards. `operator96` is only the legacy artifact/campaign ID for the first
96-record seed batch; do not use it as product or public feature branding.

The observation browser is the preferred app-side view for the current custom
value seed batch. It surfaces bucket, app-card blockers, tested values,
verdict/confidence/noise summaries, hard-smoke receipt, and artifact pointers
so contributors can decide whether to rerun, research, or prepare a bounded
app-card review without opening raw audit folders first.

Contributor Lab v1 does not execute arbitrary commands. Certified mutation still
belongs to the VM scripts listed above, with a clean snapshot, healthy QGA, and
tight host-noise gate. The v1 command packs cover single-tweak lookup, app QA
planning, app readiness/contracts, custom value app-surface review,
representative promoted app-QA batches, single-value VM experiments, and small
custom-value tranche reruns. Custom key/value templates are intentionally
copy/edit commands: contributors replace the key path, value name, and DWORD
value in a repo shell, then run one value per snapshot-safe VM experiment.

## Legacy And Historical Tooling

Some scripts under `registry-research-framework/scripts/`, `scripts/vm/`, and
older `Docs/` pages are retained so historical artifacts remain reproducible.
Do not delete or move them during routine cleanup. If a script is replaced,
mark the replacement here first, then use a cleanup quarantine ledger before any
deletion.

## Agentic AI Workflow

Agentic AI contributors should follow this loop:

1. Read `README.md`, `CONTRIBUTING.md`, this tooling map, and
   `Docs/research/run-tiers.md`.
2. Open `Docs/research/artifact-map.md` to find the current app QA, VM health,
   custom registry value experiment, and cleanup-ledger surfaces before
   browsing raw audit folders.
3. Inspect one setting with `check_single_tweak.py --json`.
4. If app retest is needed, run `check_app_retest_readiness.py --json`.
5. If VM evidence is needed, run `scripts/vm-kvm/vm-health-check.py --json`
   before any guest runner.
6. Call the narrow Python runner for the task and preserve JSON artifacts. For
   certified low-noise registry reruns, include `--abort-on-noisy-host` so host
   load cannot silently downgrade a reference campaign.
7. Update records, app surfaces, docs, and tests.
8. Open a PR with the commands run and artifact paths.

Do not invent a new wrapper path when an existing canonical Python script emits
the needed JSON contract.
