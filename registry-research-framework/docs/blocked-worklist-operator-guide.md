# Blocked Worklist Operator Guide

The blocked worklist is the handoff surface for research that is not promoted yet. It separates work that can move today from work that should stay on hold until a safer lane or better evidence exists.

## Quick Start

Use the summary first:

```bash
winopt research list-blocked --summary
```

Focus the summary on active work:

```bash
winopt research list-blocked --summary --actionability active
```

Focus the summary on one lane:

```bash
winopt research list-blocked --summary --lane runtime-trace
```

Work the active queue:

```bash
winopt research list-blocked --worklist --actionability active --top 5
```

Review intentional holds:

```bash
winopt research list-blocked --worklist --actionability hold
```

Open one candidate:

```bash
winopt research show-blocked <candidate-id>
```

## Lane Meanings

`restore-story` means the record needs exact rollback or restore behavior for the subtree or value.

`ghidra` means the record needs static reverse-engineering proof that names the exact reader, initializer, or leaf-level context.

`runtime-trace` means the record needs a stronger runtime capture for the exact key or value.

`intentional-hold` means the record is consciously parked because it is environment-limited, boot-unsafe, hardware-specific, or not mapped to a supported product surface yet.

## Candidate Loop

1. Start with `winopt research list-blocked --summary`.
2. Pick the first active lane target.
3. Run `winopt research show-blocked <candidate-id>`.
4. Inspect `recent_audit_artifacts`.
5. Execute the lane-specific next action.
6. Update the research record, regenerate gates and metrics, then commit.

Use this refresh sequence after a decision:

```bash
python3 registry-research-framework/scripts/generate_promotion_gates.py
python3 registry-research-framework/scripts/generate_publish_metrics.py
python3 registry-research-framework/scripts/check_blocked_worklist.py
python3 registry-research-framework/scripts/check_gate_thresholds.py
python3 registry-research-framework/scripts/check_mcp_readiness.py
```

`generate_publish_metrics.py` also runs the blocked worklist consistency check and returns non-zero if the worklist drifts. If that happens, run `check_blocked_worklist.py` directly to see the exact mismatch.

## Hold Policy

Do not force `intentional-hold` records through normal VM lanes. A hold is a safety decision, not backlog debt.

Move a hold only when one of these changes:

- A safer trigger lane becomes available.
- A bare-metal-only bench exists and is ready.
- A product surface maps the raw research value to something supportable.
- New current-build evidence removes the blocker.

## Output Surfaces

The canonical surfaces are:

- `registry-research-framework/audit/blocked-worklist.json`
- `registry-research-framework/audit/blocked-worklist.md`
- `registry-research-framework/metrics/publish-metrics.json`
- `registry-research-framework/README.md` Research Health block
