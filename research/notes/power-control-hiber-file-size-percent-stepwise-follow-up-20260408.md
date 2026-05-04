# HiberFileSizePercent Stepwise Follow-Up

Date: 2026-04-08

## Scope

Re-audit the retained docs-first stepwise Procmon boot trace for `power.control.hiber-file-size-percent` and decide whether the previously uncounted exact boot-time read is strong enough to close the lane.

## Artifacts

- `registry-research-framework/audit/hiber-file-size-percent-stepwise-runtime-audit-20260408.json`
- `registry-research-framework/audit/hiber-file-size-percent-stepwise-runtime-audit-20260408.md`
- `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`
- `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/summary.json`
- `Docs/power/power.md:149`

## Findings

1. The retained docs-first stepwise boot trace contains an exact `RegQueryValue SUCCESS` for `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberFileSizePercent`.
2. The successful exact read comes from `smss.exe` during the rebooted `RegProbe-Baseline-Clean-20260329` boot cycle, with shell health preserved before and after capture.
3. The same repo power notes preserve the IDA-derived internal symbol note `PopHiberFileSizePercent` at `Docs/power/power.md:149`.
4. This means the candidate no longer depends on the later lightweight ETW no-query lane for its leading runtime story; the stronger retained proof is the earlier stepwise Procmon boot trace.

## Interpretation

`HiberFileSizePercent` now matches the same evidence shape that already promoted sibling docs-first power-control records such as `HibernateEnabled` and `LidReliabilityState`: baseline existence, repo docs, current-build string corroboration, a reviewable decompilation-derived static note, and an exact boot-time runtime read from the retained stepwise Procmon lane. The lane therefore no longer needs a separate `ghidra` or `runtime_no_read` gate on this branch.
