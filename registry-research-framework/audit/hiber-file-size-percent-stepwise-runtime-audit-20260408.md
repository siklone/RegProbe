# HiberFileSizePercent Stepwise Runtime Audit

Date: 2026-04-08

## Scope

Re-audit the retained docs-first stepwise boot trace for `power.control.hiber-file-size-percent` and preserve the exact boot-time read in a machine-readable form.

## Source Artifacts

- `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`
- `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/summary.json`
- `Docs/power/power.md:149`

## Findings

- Stepwise probe status: `exact-hit`
- Snapshot: `RegProbe-Baseline-Clean-20260329`
- Boot stop mode: `soft`
- Shell healthy before: `True`
- Shell healthy after: `True`
- Exact `RegQueryValue` rows for `HiberFileSizePercent`: `1`
- Successful exact reads: `1`

### Successful Exact Read

- Time: `2:42:38.9490329 PM`
- Process: `smss.exe` (PID `428`)
- Operation: `RegQueryValue`
- Path: `HKLM\System\CurrentControlSet\Control\Power\HiberFileSizePercent`
- Result: `SUCCESS`
- Detail: `Type: REG_DWORD, Length: 4, Data: 0`

## Interpretation

- The retained docs-first stepwise Procmon boot trace contains an exact `RegQueryValue SUCCESS` for `HKLM\System\CurrentControlSet\Control\Power\HiberFileSizePercent`.
- The hit comes from `smss.exe` during the rebooted boot cycle on `RegProbe-Baseline-Clean-20260329`.
- `Docs/power/power.md:149` also preserves the IDA-derived symbol note `PopHiberFileSizePercent`, which gives this candidate a static decompilation-derived naming layer alongside the runtime read.

## Artifacts

- `registry-research-framework/audit/hiber-file-size-percent-stepwise-runtime-audit-20260408.json`
- `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`
- `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/summary.json`
