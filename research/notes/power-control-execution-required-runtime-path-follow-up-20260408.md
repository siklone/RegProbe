# Execution-Required Runtime Path Follow-Up

Date: 2026-04-08

## Scope

Narrow the retained runtime-trace story for the execution-required pair beyond the broad reboot and mega-trigger retry audits.

## Artifacts

- `registry-research-framework/audit/execution-required-runtime-path-audit-20260408.json`
- `registry-research-framework/audit/execution-required-runtime-path-audit-20260408.md`
- `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`

## Findings

1. The only retained current-build `path-hits.csv` runtime capture for this lane is `power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`.
2. That capture contains zero exact path hits for:
   - `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowSystemRequiredPowerRequests`
   - `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowAudioToEnableExecutionRequiredPowerRequests`
3. The same capture contains 15 adjacent runtime hits under `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`:
   - process: `svchost.exe`
   - operations: 9 `RegOpenKey`, 3 `RegQueryKey`, 3 `RegCloseKey`
   - subkeys probed: `Driver`, `Process`, `Service`

## Interpretation

The retained runtime registry-trace layer no longer looks like a generic "we never saw anything" gap. It is narrower than that: visible runtime registry activity in the retained capture sits on the adjacent `PowerRequestOverride` subtree, while the execution-required pair itself still produces zero exact path hits. This keeps the pair in research-only draft status and pushes the open question toward either an earlier seeding path or a narrower exact-read runtime lane.
