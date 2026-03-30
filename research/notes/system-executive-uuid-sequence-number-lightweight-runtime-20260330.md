# Executive UUID Sequence Number Lightweight Runtime Follow-Up (2026-03-30)

This follow-up moved `system.executive-uuid-sequence-number` from the residual string-hit queue into a tools-hardened lightweight ETW lane on `RegProbe-Baseline-ToolsHardened-20260330`.

What changed:
- the trigger was made guest-safe and no longer depended on broad copy-back of raw ETL artifacts
- the guest performed a UUID / RPC / COM burst with:
  - repeated `[guid]::NewGuid()`
  - `GenerateGuidForType`
  - COM object creation through `WScript.Shell`, `Shell.Application`, and `Scripting.Dictionary`
  - CIM and `wmic` UUID lookups
- the lane kept both a short trigger ETW pass and a split trace start / trigger / stop pass

Final result:
- the clean final run is:
  - `evidence/files/vm-tooling-staging/executive-uuid-sequence-number-lightweight-runtime-20260330-150344/summary.json`
  - `evidence/files/vm-tooling-staging/executive-uuid-sequence-number-lightweight-runtime-20260330-150344/system-executive-uuid-sequence-number/summary.json`
- `short-trigger-etw` stayed `no-hit`
- `split-trace-stop` also stayed `no-hit`
- both passes produced real ETL plus CSV output
- both passes recorded:
  - `exact_query_hits = 0`
  - `exact_line_count = 0`
  - `path_line_count = 0`

Why this matters:
- this lane is no longer blocked by the earlier guest trigger quoting bug
- it also no longer depends on the older broad runtime copy-back pattern
- the result is still negative on the current tools-hardened VMware baseline, so `system.executive-uuid-sequence-number` does not promote from residual intake yet

Project decision:
- keep `system.executive-uuid-sequence-number` out of `Class A` for now
- treat the current result as a clean runtime `no-hit`, not a tooling failure
