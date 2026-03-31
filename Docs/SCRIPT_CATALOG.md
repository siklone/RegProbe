# Script Catalog

This repository contains two kinds of PowerShell:

- everyday operational scripts that build, package, validate, and maintain the VM baseline
- research runners that exist to reproduce a specific evidence lane or classification decision

That second category matters. A script that is not part of the daily workflow is not automatically dead. Many `run-*` scripts are intentionally preserved because a note, audit package, or evidence bundle depends on that exact runner shape.

Today, the practical "unused" bucket is mostly generated local output, not source-controlled `.ps1` files. Source-controlled runners that are off the hot path are usually better described as historical or reproducibility helpers.

## Status Labels

- `Core`
  current day-to-day workflow; safe to treat as the default entry point
- `Active Research`
  currently relevant to the v3.1 pipeline and present evidence classes
- `Historical / Repro`
  not part of the daily happy path, but intentionally kept so a published record can be reproduced or audited
- `Generated / Safe To Delete`
  local outputs that can be removed and regenerated at any time

## Root Scripts

### `Core`

- `scripts/clean_build_outputs.ps1`
  removes regenerable `bin/`, `obj/`, and `publish/` trees
- `scripts/package_windows.ps1`
  creates the normal packaged desktop build and zip output
- `scripts/publish_release.ps1`
  creates a deterministic publish folder for release review or local smoke checks
- `scripts/build_brand_assets.ps1`
  regenerates the checked-in RegProbe logo PNG and ICO assets from the SVG brand sources
- `scripts/sanitize_public_artifacts.ps1`
  replaces machine-specific user, repo, temp, and VM path strings in tracked evidence and audit artifacts before a public push

## VM Platform Scripts

### `Core`

- `scripts/vm/_resolve-vm-baseline.ps1`
  shared snapshot resolver used by active VM lanes
- `scripts/vm/new-regprobe-tools-hardened-baseline.ps1`
  creates the current canonical VM baseline
- `scripts/vm/new-regprobe-parallel-vm.ps1`
  clones the canonical baseline into the optional secondary VM and validates it for parallel research work
- `scripts/vm/apply-defender-tooling-exclusions.ps1`
  applies bounded Defender exclusions for trusted tooling only
- `scripts/vm/apply-vmtools-hardening.ps1`
  hardens VMware Tools recovery for fragile runtime lanes
- `scripts/vm/run-vm-tooling-minimal-diagnostic.ps1`
  basic "is the guest still usable?" smoke
- `scripts/vm/get-vm-shell-health.ps1`
  confirms the desktop shell is alive
- `scripts/vm/run-app-launch-smoke-host.ps1`
  deploys the app ephemerally, validates launch, then cleans up
- `scripts/vm/app-launch-smoke.ps1`
  guest-side launch smoke helper used by the host wrapper
- `scripts/vm/open-regprobe-in-vm.ps1`
  visible interactive app launch for manual validation
- `scripts/vm/cleanup-host-validation-artifacts.ps1`
  removes host-side validation clutter
- `scripts/vm/cleanup-guest-validation-artifacts.ps1`
  removes guest-side validation clutter

### `Active Research`

- `scripts/vm/run-cpu-idle-states-runtime-probe.ps1`
  reference implementation for the stepwise WPR + reboot pattern
- `scripts/vm/run-cpu-idle-states-orchestration-step.ps1`
  generic step runner behind the CPU idle orchestration wrappers
- `scripts/vm/run-cpu-idle-states-orchestration-step-a.ps1`
- `scripts/vm/run-cpu-idle-states-orchestration-step-b.ps1`
- `scripts/vm/run-cpu-idle-states-orchestration-step-c1.ps1`
- `scripts/vm/run-cpu-idle-states-orchestration-step-c2.ps1`
- `scripts/vm/run-cpu-idle-states-orchestration-step-c3.ps1`
- `scripts/vm/run-cpu-idle-states-orchestration-step-c4.ps1`
- `scripts/vm/run-cpu-idle-states-orchestration-step-d.ps1`
  direct step inspection wrappers for the canonical stepwise lane
- `scripts/vm/run-registry-batch-existence-probe.ps1`
  broad Phase 0 value/path existence intake
- `scripts/vm/run-registry-batch-string-probe.ps1`
  broad binary-string narrowing pass for grouped candidates
- `scripts/vm/run-targeted-string-probe.ps1`
  exact single-candidate static triage when the broad grouped probe is too blunt
- `scripts/vm/run-targeted-string-batch-probe.ps1`
  exact family-routed broad-batch triage for the remaining path-only queue
- `scripts/vm/run-ghidra-string-xref-probe.ps1`
  VM-safe Ghidra xref batch runner

### `Historical / Repro`

- `scripts/vm/new-regprobe-defender-excluded-baseline.ps1`
  older baseline builder preserved because published audit packages still refer to it
- `scripts/vm/try-enable-full-acpi-vmx.ps1`
  failed ACPI widening experiment kept because it documents a real environment limit
- `scripts/vm/run-session-watchdog-timeouts-*.ps1`
  watchdog investigation family; no longer on the hot path, but retained for auditability
- `scripts/vm/run-executive-worker-threads-*.ps1`
  earlier executive worker-thread exploration family
- `scripts/vm/run-explorer-*.ps1`
  Explorer-specific research lanes preserved with their evidence
- `scripts/vm/run-defender-*.ps1`, `scripts/vm/defender-*.ps1`
  Defender-focused runtime probes for specific records
- `scripts/vm/run-audio-devicecpl-runtime-probe.ps1`
- `scripts/vm/run-fullscreen-optimizations-probe.ps1`
- `scripts/vm/run-jpeg-import-quality-runtime-probe.ps1`
- `scripts/vm/run-reliability-timestamp-probe.ps1`
- `scripts/vm/run-service-shutdown-timeout-probe.ps1`
  targeted repro runners for named tweak records

## Research Framework Scripts

### `Core`

- `registry-research-framework/pipeline/faz0-enrichment.ps1`
- `registry-research-framework/pipeline/faz1-runtime-trace.ps1`
- `registry-research-framework/pipeline/faz2-static-analysis.ps1`
- `registry-research-framework/pipeline/faz3-behavior-measure.ps1`
- `registry-research-framework/pipeline/faz4-dead-flag-check.ps1`
- `registry-research-framework/pipeline/faz5-classify.ps1`
- `registry-research-framework/pipeline/faz6-output.ps1`
- `registry-research-framework/pipeline/faz-retroactive-audit.ps1`
- `registry-research-framework/pipeline/_invoke-phase.ps1`
  the phase-based v3.1 backbone
- `registry-research-framework/tools/etw-registry-trace.ps1`
- `registry-research-framework/tools/procmon-registry-trace.ps1`
- `registry-research-framework/tools/wpr-boot-trace.ps1`
- `registry-research-framework/tools/typeperf-baseline.ps1`
- `registry-research-framework/tools/bingrep-scan.ps1`
- `registry-research-framework/tools/floss-scan.ps1`
- `registry-research-framework/tools/capa-scan.ps1`
- `registry-research-framework/tools/ghidra-headless-analyze.ps1`
- `registry-research-framework/tools/pdb-download.ps1`
- `registry-research-framework/tools/registry-sideeffect-diff.ps1`
- `registry-research-framework/tools/_resolve-tweak-runner.ps1`
  generic building blocks used by multiple lanes

### `Active Research`

- `registry-research-framework/tools/run-power-control-lightweight-runtime-followup.ps1`
  current lightweight runtime lane for power-control candidates
- `registry-research-framework/tools/run-executive-worker-threads-lightweight-runtime-followup.ps1`
  current executive lane
- `registry-research-framework/tools/run-watchdog-lightweight-runtime-followup.ps1`
  current watchdog follow-up lane
- `registry-research-framework/tools/run-path-aware-static-probe.ps1`
- `registry-research-framework/tools/run-path-aware-runtime-probe.ps1`
  current path-aware residual-candidate pair
- `registry-research-framework/tools/run-executive-uuid-sequence-number-lightweight-runtime-followup.ps1`
  current UUID-specific follow-up lane

### `Historical / Repro`

- `registry-research-framework/tools/run-power-control-docs-first-runtime-capture.ps1`
- `registry-research-framework/tools/run-power-control-docs-first-stepwise-runtime-capture.ps1`
- `registry-research-framework/tools/run-power-control-docs-first-postboot-trigger-capture.ps1`
- `registry-research-framework/tools/run-power-control-docs-first-trigger-etw-capture.ps1`
- `registry-research-framework/tools/run-power-control-docs-first-trigger-etw-guestvar.ps1`
  earlier power-control promotion runners retained so the published notes still map back to a real script

## What Is Safe To Delete Locally

These are not source assets:

- any `bin/`, `obj/`, or `publish/` folder
- top-level `dist/`
- any `TestResults/` folder

They are generated outputs. If you need space or want a cleaner tree, remove them and regenerate as needed.

## What Not To Delete Casually

Do not delete a PowerShell file only because it looks narrow or old.

Before removing a runner, check whether it is referenced from:

- `research/notes/`
- `registry-research-framework/audit/`
- `evidence/files/`
- `evidence/records/`

If yes, it is part of the repo's reproducibility story even if it is no longer the default path.

## Recommended Learning Path

If you want to understand how this repo works, read in this order:

1. `README.md`
2. `Docs/VM_WORKFLOW.md`
3. `CONTRIBUTING.md`
4. this file
5. one concrete record under `research/records/`
6. the matching evidence bundle under `evidence/records/<tweak-id>/`

Then trace the corresponding runner script back to the VM or framework tool that produced it.
