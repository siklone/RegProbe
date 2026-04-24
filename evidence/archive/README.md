# Archived Evidence

This directory holds evidence artifacts that are no longer referenced by
`research/records/*.json` and are not part of the active runtime/static
evidence lanes.

## 2026-04-24 orphan cleanup

- Archived `evidence/files/ghidra-v32/` because the legacy V32 probe outputs
  had no active repo callers and no linked research records.
- Archived `evidence/files/ida-v32/` because the legacy IDA branch-analysis
  export had no active repo callers and no linked research records.
- Archived `evidence/captures/power-request-override-etl-parsed-20260423.json`
  because the parsed ETL snapshot was no longer linked from any research
  record.
- Deleted stale binary payloads from the legacy `ghidra-v32` bundle
  (`ntoskrnl.exe`, cached `ntkrnlmp.pdb`, `cimwin32.dll`) because they were
  unreferenced and older than the active cleanup window.
- No unused scripts were detected by the current `scripts/` scan.

Archive-first is intentional here: when an orphaned artifact might still be
useful for historical comparison, we retain it outside the active evidence
surface instead of deleting it outright.
