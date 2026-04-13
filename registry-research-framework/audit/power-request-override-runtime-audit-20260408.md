# PowerRequestOverride Runtime Audit

Date: 2026-04-08
Target path: `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`

## Outcome

- Root subtree present in retained dump: `True`
- Runtime hits under subtree: `15`
- Processes: `{'svchost.exe': 15}`
- Operations: `{'RegCloseKey': 3, 'RegOpenKey': 9, 'RegQueryKey': 3}`
- Results: `{'NAME NOT FOUND': 3, 'REPARSE': 3, 'SUCCESS': 9}`
- KD override symbols found: `['PopPowerRequestHandleRequestOverrideQueryResponse', 'PopPowerRequestOverrideInitialize', 'PopUmpoSendPowerRequestOverrideQuery', 'PopUmpoSendPowerRequestOverrideCleanup']`

## Interpretation

- The retained root dump proves that `Control\Power\PowerRequestOverride` exists as a persisted subtree.
- The retained current-build runtime trace shows repeated `svchost.exe` access to the subtree root plus the `Driver`, `Process`, and `Service` leaves.
- The retained wildcard KD lineage exposes an override family around response handling, initialization, and UMPO override query / cleanup.
- Exact leaf values and a bounded Ghidra path are still unresolved, so this remains a draft subtree lane rather than an app-ready tweak.

## Artifacts

- `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`
- `evidence/files/vm-tooling-staging/registry-dumps/power-control-root-20260324-210206/power-control-root.txt`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reglineage-20260408a/stdout.txt`
- `registry-research-framework/audit/power-request-override-runtime-audit-20260408.json`
