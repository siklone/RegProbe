# Execution-Required Override Alignment Audit

Date: 2026-04-08
Path-hit artifact: `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`
KD wildcard artifact: `evidence/files/vm-tooling-staging/local-kd-powerrequest-reglineage-20260408a/stdout.txt`
Root dump artifact: `evidence/files/vm-tooling-staging/registry-dumps/power-control-root-20260324-210206/power-control-root.txt`

## Outcome

- Adjacent `PowerRequestOverride` path hits: `15`
- Processes: `{'svchost.exe': 15}`
- Operations: `{'RegCloseKey': 3, 'RegOpenKey': 9, 'RegQueryKey': 3}`
- Override symbols present: `{'PopPowerRequestHandleRequestOverrideQueryResponse': True, 'PopPowerRequestOverrideInitialize': True, 'PopUmpoSendPowerRequestOverrideQuery': True, 'PopUmpoSendPowerRequestOverrideCleanup': True}`
- Root dump contains `PowerRequestOverride` subtree: `False`

## Interpretation

- The visible runtime registry activity aligns with the already-proven current-build override family rather than with exact reads of `AllowSystemRequiredPowerRequests` or `AllowAudioToEnableExecutionRequiredPowerRequests`.
- This further narrows the execution-required pair: adjacent override flow is visible, while an exact pair read or earlier seeding path remains unresolved.

## Artifacts

- `registry-research-framework/audit/execution-required-override-alignment-20260408.json`
- `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`
- `evidence/files/vm-tooling-staging/local-kd-powerrequest-reglineage-20260408a/stdout.txt`
- `evidence/files/vm-tooling-staging/registry-dumps/power-control-root-20260324-210206/power-control-root.txt`
