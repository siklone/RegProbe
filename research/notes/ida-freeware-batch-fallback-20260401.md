# IDA Freeware Batch Fallback Follow-Up

Date: 2026-04-01

This pass tested whether the current guest-side `ida64.exe` install can serve as a practical fallback for static cross-verification when `idat64.exe` and Hex-Rays are unavailable.

Current guest state:
- `C:\Tools\IDA\Freeware\ida64.exe` exists
- `idat64.exe` is still absent
- a local `.hexlic` file is present under both the install root and `%APPDATA%\Hex-Rays\IDA Pro`

What was tested:
- plain `ida64.exe -A -S script.py target.exe`
- `ida64.exe -A -Olicense:keyfile=...:setpref -S script.py target.exe`

Observed result:
- both variants failed to produce automation output
- the canonical failure line in the guest log is:
  - `License not yet accepted, cannot run in batch mode -> OK`

Implication for the pipeline:
- `run-ida-string-xref-probe.ps1` now treats `ida64.exe` as a real fallback candidate
- but when batch mode is blocked by the guest acceptance state, the probe emits `blocked-license-not-accepted`
- this keeps the Phase 3 lane honest: Freeware can remain available for manual GUI inspection, but it is not counted as operational headless cross-verification
- until that acceptance blocker is genuinely cleared, `IDA` should be treated as pipeline-external and optional rather than as a required static lane

Related audits:
- `registry-research-framework/audit/ida-provisioning-20260401b.json`
- `registry-research-framework/audit/ida-freeware-batch-fallback-20260401.json`
