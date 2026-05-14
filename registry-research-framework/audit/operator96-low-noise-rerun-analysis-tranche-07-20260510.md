# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T06:28:39Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `10`
- Errors: `0`

## Verdict Counts

- `harmful`: `8`
- `low_confidence`: `2`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-038-watchdogresumetimeout-0` | `low_confidence` | `low` | `ok` | `97.747` | apply io_read_mib_per_second changed by +97.75% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-038-watchdogresumetimeout-0.json` |
| `operator96-038-watchdogresumetimeout-1` | `low_confidence` | `low` | `ok` | `96.420` | apply io_read_mib_per_second changed by +96.42% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-038-watchdogresumetimeout-1.json` |
| `operator96-039-watchdogsleeptimeout-0` | `harmful` | `low` | `ok` | `-8.054` | post_reboot io_write_mib_per_second changed by -8.05% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-039-watchdogsleeptimeout-0.json` |
| `operator96-039-watchdogsleeptimeout-1` | `harmful` | `high` | `ok` | `-7.719` | apply cpu_multi_iterations_per_second changed by -7.72% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-039-watchdogsleeptimeout-1.json` |
| `operator96-040-skiptickoverride-0` | `harmful` | `low` | `ok` | `-7.452` | post_reboot io_read_mib_per_second changed by -7.45% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-040-skiptickoverride-0.json` |
| `operator96-040-skiptickoverride-1` | `harmful` | `low` | `ok` | `-14.131` | post_reboot io_write_mib_per_second changed by -14.13% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-040-skiptickoverride-1.json` |
| `operator96-041-win32calloutwatchdogbugcheckenabled-0` | `harmful` | `low` | `ok` | `-50.307` | post_reboot io_write_mib_per_second changed by -50.31% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-041-win32calloutwatchdogbugcheckenabled-0.json` |
| `operator96-041-win32calloutwatchdogbugcheckenabled-1` | `harmful` | `low` | `ok` | `-78.213` | post_reboot io_write_mib_per_second changed by -78.21% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-041-win32calloutwatchdogbugcheckenabled-1.json` |
| `operator96-042-idlescaninterval-0` | `harmful` | `low` | `ok` | `-27.841` | post_reboot io_write_mib_per_second changed by -27.84% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-042-idlescaninterval-0.json` |
| `operator96-042-idlescaninterval-1` | `harmful` | `low` | `ok` | `-23.205` | post_reboot io_write_mib_per_second changed by -23.20% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-042-idlescaninterval-1.json` |
