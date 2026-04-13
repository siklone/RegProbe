# Ghidra Autotrigger Lane

The Ghidra autotrigger lane turns fresh `normalized-registry-bundle.json` evidence into a queue of static-analysis pivots for blocked `ghidra` candidates. It is designed for the current Wave 4 shape where caller stacks can arrive before we have a resolved static caller.

## What It Does

At a high level, the lane does four things:

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

That refresh path now updates the symbol-resolution handoff files in the same pass, so the operator view stays current without a second manual step.

Run the one-shot sync wrapper:

```bash
python3 registry-research-framework/scripts/sync_ghidra_autotrigger_lane.py
```

Run the synthetic smoke harness:

```bash
python3 registry-research-framework/scripts/run_ghidra_autotrigger_smoke.py
```

Render the symbol-resolution handoff surface:

```bash
python3 registry-research-framework/scripts/generate_ghidra_symbol_resolution_handoff.py
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
- `registry-research-framework/queue/ghidra-dispatch-batch.json`
  Prepared headless-analysis jobs, enriched with autotrigger context when available.
- `registry-research-framework/queue/ghidra-dispatch-run.json`
  Dry-run or execution plan for the dispatch batch.
- `registry-research-framework/audit/ghidra-autotrigger-health.json`
  Machine-readable health summary for the lane.
- `registry-research-framework/audit/ghidra-autotrigger-health.md`
  Human-readable health snapshot.
- `registry-research-framework/audit/ghidra-autotrigger-sync.json`
  One-shot sync result with status `ok`, `idle`, or `error`.
- `registry-research-framework/audit/ghidra-autotrigger-sync.md`
  Operator-facing sync snapshot with the current blocker and next action.
- `registry-research-framework/audit/ghidra-autotrigger-smoke.json`
  Synthetic end-to-end proof that the lane can leave `idle` and produce symbol-resolution-ready work without waiting on a fresh real capture.
- `registry-research-framework/audit/ghidra-autotrigger-smoke.md`
  Short operator summary for the latest smoke run, including candidate coverage and assertion failures.

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

Because fresh caller-stack bundles are still intermittent, the lane now also has a synthetic smoke harness. It fabricates a small normalized bundle from the active blocked `ghidra` queue, runs the same sync path in an isolated audit directory, and proves that symbol-resolution-ready work would be emitted when matching stack-bearing bundles arrive.
