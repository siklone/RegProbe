# Execution-Required Runtime Path Audit

Date: 2026-04-08
Source artifact: `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`

## Outcome

- Parsed rows: `120419`
- `AllowSystemRequiredPowerRequests` exact path hits: `0`
- `AllowAudioToEnableExecutionRequiredPowerRequests` exact path hits: `0`
- Adjacent `PowerRequestOverride` subtree hits: `15`
- Adjacent subtree processes: `{'svchost.exe': 15}`
- Adjacent subtree operations: `{'RegCloseKey': 3, 'RegOpenKey': 9, 'RegQueryKey': 3}`

## Interpretation

- The retained docs-first stepwise runtime path-hit capture contains zero exact path hits for both execution-required pair members.
- The same capture contains repeated adjacent registry activity under `HKLM\System\CurrentControlSet\Control\Power\PowerRequestOverride`.
- Visible runtime registry activity is therefore narrowed to the adjacent override subtree rather than exact reads of the pair values.

## Artifacts

- `registry-research-framework/audit/execution-required-runtime-path-audit-20260408.json`
- `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`
