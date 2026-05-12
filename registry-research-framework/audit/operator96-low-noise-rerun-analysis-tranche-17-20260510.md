# Registry Value Experiment Analysis

- Generated UTC: `2026-05-12T00:42:38Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `10`
- Errors: `0`

## Verdict Counts

- `harmful`: `4`
- `low_confidence`: `5`
- `noisy`: `1`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-090-powerwatchdogpowerongditimeoutmsec-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-090-powerwatchdogpowerongditimeoutmsec-0.json` |
| `operator96-090-powerwatchdogpowerongditimeoutmsec-1` | `harmful` | `low` | `ok` | `-7.671` | post_reboot io_write_mib_per_second changed by -7.67% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-090-powerwatchdogpowerongditimeoutmsec-1.json` |
| `operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-0` | `harmful` | `low` | `ok` | `-32.312` | apply io_write_mib_per_second changed by -32.31% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-0.json` |
| `operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-1` | `low_confidence` | `low` | `ok` | `96.485` | apply io_write_read_mib_per_second changed by +96.48% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-1.json` |
| `operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-0` | `low_confidence` | `low` | `ok` | `552.730` | post_reboot io_write_mib_per_second changed by +552.73% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-0.json` |
| `operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-1` | `harmful` | `low` | `ok` | `-15.388` | apply io_write_mib_per_second changed by -15.39% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-1.json` |
| `operator96-094-enabledsnetrefresh-0` | `harmful` | `low` | `ok` | `-25.516` | apply io_write_mib_per_second changed by -25.52% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-094-enabledsnetrefresh-0.json` |
| `operator96-094-enabledsnetrefresh-1` | `low_confidence` | `low` | `ok` | `88.855` | apply io_read_mib_per_second changed by +88.86% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-094-enabledsnetrefresh-1.json` |
| `operator96-095-enabledactions-0` | `low_confidence` | `low` | `ok` | `96.495` | apply io_read_mib_per_second changed by +96.50% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-095-enabledactions-0.json` |
| `operator96-095-enabledactions-1` | `low_confidence` | `low` | `ok` | `95.063` | apply io_read_mib_per_second changed by +95.06% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-095-enabledactions-1.json` |
