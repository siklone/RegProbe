# system.executive-uuid-sequence-number Procmon boot-log follow-up - 2026-04-08

## Summary

- `system.executive-uuid-sequence-number` already had static and ETW-backed early-boot coverage.
- The missing runtime layer in audit terms was Procmon, not a new semantics gap.
- A retained clean-baseline Procmon boot-log run already exists on the adjacent Executive worker-thread family and explicitly filtered `UuidSequenceNumber`.
- That Procmon lane produced a real boot-log `PML` and `CSV`, stayed shell-safe, and still returned `MATCH_COUNT=0` for `UuidSequenceNumber`.

## Canonical retained artifacts

- Procmon note: `research/notes/system-executive-additional-worker-threads-procmon-bootlog-20260328.md`
- Runner summary: `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-bootlog-20260328-172645/summary.json`
- Collect phase: `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-bootlog-20260328-172645/summary-collect.json`
- Raw placeholder: `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-bootlog-20260328-172645/executive-worker-threads-procmon-bootlog.pml.md`

## Why this counts for the candidate

- The retained boot-log lane targeted the same Session Manager Executive family as the existing `UuidSequenceNumber` draft.
- The note for that run explicitly lists `UuidSequenceNumber` among the filtered values and records `MATCH_COUNT=0`.
- The collected Procmon boot-log proved a real runtime capture path:
  - `status = procmon-bootlog-captured`
  - `pml_captured = true`
  - `csv_captured = true`
  - `collect_summary.match_count = 0`
- That means the candidate is no longer missing a Procmon attempt. The missing piece is narrower: an exact live read or cleaner trigger context for `UuidSequenceNumber` itself.

## Decision impact

- keep `system.executive-uuid-sequence-number` at `Class B`
- drop the audit-level `procmon` gap
- keep the record blocked by:
  - `runtime_no_read`
  - `trigger_context_unclear`

## Next step

- Do not spend more time on another generic Executive Procmon boot-log rerun.
- If this lane moves again, it moves through a narrower exact-read runtime path or a concrete caller/branch proof rather than another broad boot capture.
