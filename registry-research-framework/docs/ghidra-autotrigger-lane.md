# Ghidra Autotrigger Lane

The Ghidra autotrigger lane turns fresh `normalized-registry-bundle.json` evidence into a queue of static-analysis pivots for blocked `ghidra` candidates. It is designed for the current Wave 4 shape where caller stacks can arrive before we have a resolved static caller.

## What It Does

At a high level, the lane does five things:

1. Discover candidate bundles that contain caller stacks and can map back to queued `ghidra` records.
2. Convert unresolved stack frames into `ghidra-autotrigger-seed` rows.
3. Convert actionable unresolved frames into a symbol-resolution queue so module offsets and raw addresses do not stay implicit.
4. Enrich the existing dispatch batch so caller-stack-driven jobs are visible and prioritized.
5. Publish health surfaces so we can tell whether the lane is idle, ready, or blocked by tooling.

## Main Commands

Discover candidate inputs:

```bash
python3 registry-research-framework/scripts/generate_ghidra_autotrigger_inputs.py
```

Refresh the lane from one or more bundle roots:

```bash
python3 registry-research-framework/scripts/refresh_ghidra_autotrigger_pipeline.py \
  --refresh-bundle-manifest \
  --discover-input-root evidence
```

That refresh path now updates the symbol-resolution handoff files, transfer pack, pack verification, local import rehearsal, execution plan, dry-run execution surface, and dry-run validation in the same pass, so the operator view stays current without a second manual step.

Run the one-shot sync wrapper:

```bash
python3 registry-research-framework/scripts/sync_ghidra_autotrigger_lane.py
```

Run the synthetic smoke harness:

```bash
python3 registry-research-framework/scripts/run_ghidra_autotrigger_smoke.py
```

The smoke harness writes both `ghidra-autotrigger-smoke.json` and `ghidra-autotrigger-smoke-check.json`, so the latest run leaves an operator summary and a machine-checkable gate. The command exits nonzero when either the smoke assertions or the smoke-check gate fail.

Validate the latest synthetic smoke result:

```bash
python3 registry-research-framework/scripts/check_ghidra_autotrigger_smoke.py
```

Render the symbol-resolution handoff surface:

```bash
python3 registry-research-framework/scripts/generate_ghidra_symbol_resolution_handoff.py
```

Render the portable transfer pack:

```bash
python3 registry-research-framework/scripts/generate_ghidra_symbol_resolution_transfer.py
```

Materialize the portable transfer pack as a directory plus zip archive:

```bash
python3 registry-research-framework/scripts/materialize_ghidra_symbol_resolution_transfer_pack.py
```

Verify a materialized transfer pack before moving or executing it:

```bash
python3 registry-research-framework/scripts/check_ghidra_symbol_resolution_transfer_pack.py
```

Validate and unpack a transfer archive on the destination host:

```bash
python3 registry-research-framework/scripts/unpack_ghidra_symbol_resolution_transfer_pack.py
```

Generate the destination execution plan from an imported pack:

```bash
python3 registry-research-framework/scripts/generate_ghidra_transfer_pack_execution_plan.py
```

Dry-run the destination execution plan before running anything:

```bash
python3 registry-research-framework/scripts/run_ghidra_transfer_pack_execution_plan.py
```

Validate the execution dry-run surface:

```bash
python3 registry-research-framework/scripts/check_ghidra_transfer_pack_execution_run.py
```

Generate the ETW stackwalk capture plan for producing fresh caller-stack bundles:

```bash
python3 registry-research-framework/scripts/generate_etw_stackwalk_capture_plan.py
```

Run the matching elevated Windows guest helper when Windows Performance Toolkit is available:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\vm\guest-tools\run-etw-registry-stackwalk-capture.ps1 `
  -RunId wave4-registry-stackwalk `
  -RegistryPath 'HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel' `
  -ValueName TimerCheckFlags
```

Validate that the stackwalk plan still contains the registry stack flags, caller-stack handoff, and parser command:

```bash
python3 registry-research-framework/scripts/check_etw_stackwalk_capture_plan.py
```

Regenerate and validate health surfaces:

```bash
python3 registry-research-framework/scripts/generate_ghidra_autotrigger_health.py
python3 registry-research-framework/scripts/check_ghidra_autotrigger_health.py
```

## Core Surfaces

- `registry-research-framework/queue/ghidra-autotrigger-inputs.json`
  Discovery manifest of normalized bundles ranked by queued-candidate match and caller-stack coverage.
  It also includes diagnostics for scanned bundles and skip reasons such as `no-caller-stack` and `no-queue-match`.
- `registry-research-framework/queue/ghidra-autotrigger-seeds.jsonl`
  Seed rows produced from unresolved caller-stack frames.
- `registry-research-framework/queue/ghidra-symbol-resolution-queue.json`
  Aggregated symbol-resolution requests derived from actionable unresolved frames such as `module+0xoffset` and raw addresses.
- `registry-research-framework/queue/ghidra-symbol-resolution-batch.json`
  Prepared KVM guest symbolized-probe jobs built from the symbol-resolution queue.
- `registry-research-framework/queue/ghidra-symbol-resolution-run.json`
  Dry-run or execution plan for the prepared symbol-resolution jobs.
- `registry-research-framework/audit/ghidra-symbol-resolution-handoff.json`
  Operator-facing handoff summary for prepared symbol-resolution jobs, including runnable commands and blocked-job reasons.
- `registry-research-framework/audit/ghidra-symbol-resolution-handoff.md`
  Short markdown version of the same handoff package.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer.json`
  Portable transfer manifest for selected symbol-resolution jobs, including commands and required repo paths for another host.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer.md`
  Human-readable export summary for the transfer manifest.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack.json`
  Materialized export-pack summary showing the copied repo files, command files, and archive path.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack.md`
  Short operator summary of the materialized pack.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack.zip`
  Ready-to-move archive containing manifests, repo helpers, and per-request command files.
  The materializer also writes `CHECKSUMS.json` inside the pack and records the archive SHA-256 in the summary JSON, so the destination host can verify the transfer before running any commands.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack-check.json`
  Verification result for the materialized pack, covering file hashes, zip entries, command files, and archive SHA-256.
  The checker can also validate directly from the zip archive when the extracted pack directory is not present, which is the expected destination-host shape after transfer.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack-check.md`
  Human-readable verification summary.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack-import.json`
  Destination-host import result for a verified transfer archive.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack-import.md`
  Short import summary with extracted file counts and errors.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack-execution-plan.json`
  Destination-host execution plan generated from imported command files.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack-execution-plan.md`
  Human-readable list of ready destination commands and blockers.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack-execution-run.json`
  Dry-run or execution result for the imported transfer-pack commands, including cwd, argv, shell-safe command text, and per-job blockers.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack-execution-run.md`
  Human-readable run-ready checklist for the destination host.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack-execution-run-check.json`
  Validator result for the execution dry-run surface, covering job counts, cwd availability, argv, command text, and ready/blocked consistency.
- `registry-research-framework/audit/ghidra-symbol-resolution-transfer-pack-execution-run-check.md`
  Human-readable validation summary for the run-ready checklist.
- `registry-research-framework/queue/ghidra-dispatch-batch.json`
  Prepared headless-analysis jobs, enriched with autotrigger context when available.
- `registry-research-framework/queue/ghidra-dispatch-run.json`
  Dry-run or execution plan for the dispatch batch.
- `registry-research-framework/audit/ghidra-autotrigger-health.json`
  Machine-readable health summary for the lane.
- `registry-research-framework/audit/ghidra-autotrigger-health.md`
  Human-readable health snapshot, now including symbol handoff, transfer readiness, pack verification, execution dry-run readiness, and ETW stackwalk capture-plan readiness.
- `registry-research-framework/audit/ghidra-autotrigger-sync.json`
  One-shot sync result with status `ok`, `idle`, or `error`.
- `registry-research-framework/audit/ghidra-autotrigger-sync.md`
  Operator-facing sync snapshot with the current blocker and next action.
- `registry-research-framework/audit/ghidra-autotrigger-smoke.json`
  Synthetic end-to-end proof that the lane can leave `idle` and produce symbol-resolution-ready work without waiting on a fresh real capture.
- `registry-research-framework/audit/ghidra-autotrigger-smoke.md`
  Short operator summary for the latest smoke run, including candidate coverage and assertion failures.
- `registry-research-framework/audit/ghidra-autotrigger-smoke-check.json`
  Validator result for the latest smoke summary and its critical child surfaces.
- `registry-research-framework/audit/ghidra-autotrigger-smoke-check.md`
  Human-readable smoke validation summary.
- `registry-research-framework/audit/etw-stackwalk-capture-plan.json`
  Operator-ready xperf registry stackwalk plan for producing fresh `caller_stack` events.
- `registry-research-framework/audit/etw-stackwalk-capture-plan.md`
  Copy/paste command view of the same stackwalk capture plan.
- `registry-research-framework/audit/etw-stackwalk-capture-plan-check.json`
  Machine-readable validation result for the stackwalk capture plan.
- `registry-research-framework/audit/etw-stackwalk-capture-plan-check.md`
  Human-readable validation summary for the stackwalk capture plan.

## Status Semantics

- `idle`
  The lane is healthy, but no matching bundle inputs were discovered.
  Check the input manifest diagnostics first to see whether the lane found no bundles at all, or found bundles that failed the caller-stack or queue-match filters.
- `ok`
  Inputs were discovered, surfaces refreshed, and the health checker passed.
- `error`
  A surface was inconsistent or the sync path failed in a non-idle way.

## Current Reality

The lane is intentionally split between discovery and execution. Discovery, dispatch planning, health reporting, and validation now work locally. Real headless execution is still blocked by the host environment: we do not currently have `pwsh` plus a runnable Ghidra install on this machine.

The new symbol-resolution queue sits between seeds and dispatch. When the lane is not idle, that queue gives us an explicit list of offsets or addresses that still need names before we can expect clean decompiler pivots. The symbol-resolution batch now turns that list into prepared KVM guest symbolized-probe jobs, so the unresolved frames can move straight into a repeatable operator lane instead of staying as a passive note.

There is now a dedicated handoff surface for those prepared jobs. Instead of opening multiple JSON files to figure out what is runnable, what is blocked, and which command should move next, the handoff summary packages the selected jobs, blocked jobs, candidate coverage, and next action into one place.

On top of that, the transfer manifest turns the selected jobs into a small export contract, and the transfer-pack materializer turns that contract into a real folder tree plus zip archive. That means another KVM-capable host can pick up a ready-made pack with the manifests, repo-side helpers, and per-request commands already laid out.

Because fresh caller-stack bundles are still intermittent, the lane now also has a synthetic smoke harness. It fabricates a small normalized bundle from the active blocked `ghidra` queue, runs the same sync path in an isolated audit directory, and proves that symbol-resolution-ready work would be emitted when matching stack-bearing bundles arrive.

The first real capture path is now explicit too. The ETW stackwalk plan records the xperf kernel flags, registry stackwalk events, buffer settings, output paths, and tracerpt/repo parse handoff needed to produce a stack-bearing bundle. It does not touch a VM by itself; it gives the operator a reviewable elevated Windows command sequence before capture.

The smoke harness also materializes, verifies, imports, plans execution, dry-runs the transfer pack, and validates that dry-run surface. A passing smoke run now means the lane can produce symbol-resolution jobs, package the required helper files, hash the payload, validate the zip, prove the destination-side unpack path, emit ready destination commands, render a final run-ready checklist, and check that checklist before anyone moves it to another host.

For the destination side, the unpack helper validates the summary and archive before extraction, then writes an import surface. This keeps the transfer lane reversible: a pack can be checked in place, copied as a zip, checked again from the archive alone, and unpacked only after those checks pass.

After import, the execution-plan helper rewrites the original repo-relative commands into imported-pack commands that run from the extracted pack root. The execution-run helper then performs the final dry-run by default: it records cwd, argv, and shell-safe command text without touching the VM. It can execute with `--execute`, but the safe path is to review the dry-run surface first. The regular refresh and sync path now emits and validates that dry-run surface too, not just the smoke harness, so health can fail if a transfer pack is selected but no checked run-ready rehearsal exists.
