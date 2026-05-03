# policy.system.enable-virtualization audit gate follow-up - 2026-04-08

## Summary

- `policy.system.enable-virtualization` was still showing `next_missing_layer = procmon` in audit.
- The retained evidence stack already makes that label too coarse.
- The lane has:
  - baseline existence
  - repo-doc semantics
  - current-build Ghidra/static clustering in `ntoskrnl.exe`
  - a primary path-aware ETW runtime lane that stayed a clean no-hit
  - a secondary-profile replay that reproduced the same no-hit

## Why the audit needed correction

- The remaining blocker is not "we never tested runtime."
- The remaining blocker is narrower:
  - runtime still does not show an exact read
  - intended-path context is still not decisive
  - the family still carries the nearby `EnableVirtualizationBasedSecurity` collision in `winload.exe`
- In this state, a generic `procmon` label overstates what is actually missing.

## Decision impact

- keep the record at `Class B`
- keep the blockers:
  - `runtime_no_read`
  - `path_context_unclear`
- treat the audit next step as `decision-gate`, not as a missing generic `procmon` lane

## Canonical retained references

- record: `research/records/policy.system.enable-virtualization.json`
- primary path-aware note: `research/notes/policy-system-enable-virtualization-path-aware-follow-up-20260330.md`
- secondary replay note: `research/notes/policy-system-enable-virtualization-path-aware-follow-up-20260331.md`
- static summary: `evidence/files/path-aware/path-aware-static-20260330-222908/policy-system-enable-virtualization/summary.json`
- ghidra matches: `evidence/raw/ghidra/policy-system-enable-virtualization-ntoskrnl-exe-path-aware-20260330-222908/ghidra-matches.md`
- primary runtime summary: `evidence/files/path-aware/path-aware-runtime-20260330-221529/policy-system-enable-virtualization/summary.json`
- secondary runtime summary: `evidence/files/path-aware/secondary/path-aware-runtime-secondary-20260331-110610/policy-system-enable-virtualization/summary.json`
- retained WPR/QGA no-hit audit: [policy-system-enable-virtualization-wpr-qga-runtime-read-20260413.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/policy-system-enable-virtualization-wpr-qga-runtime-read-20260413.json)
