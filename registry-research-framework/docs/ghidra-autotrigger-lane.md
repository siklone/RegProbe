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

Run the one-shot sync wrapper:

```bash
python3 registry-research-framework/scripts/sync_ghidra_autotrigger_lane.py
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

The new symbol-resolution queue sits between seeds and dispatch. When the lane is not idle, that queue gives us an explicit list of offsets or addresses that still need names before we can expect clean decompiler pivots.
