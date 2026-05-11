# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T21:44:10Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `10`
- Errors: `0`

## Verdict Counts

- `harmful`: `7`
- `low_confidence`: `1`
- `noisy`: `2`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-080-maximumfrequencyoverride-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-080-maximumfrequencyoverride-0.json` |
| `operator96-080-maximumfrequencyoverride-100` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-080-maximumfrequencyoverride-100.json` |
| `operator96-081-pofxsystemirpwaitforreportdevicepowered-0` | `harmful` | `low` | `ok` | `-12.331` | apply io_write_mib_per_second changed by -12.33% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-081-pofxsystemirpwaitforreportdevicepowered-0.json` |
| `operator96-081-pofxsystemirpwaitforreportdevicepowered-1` | `harmful` | `low` | `ok` | `-8.025` | apply io_write_mib_per_second changed by -8.03% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-081-pofxsystemirpwaitforreportdevicepowered-1.json` |
| `operator96-082-allowsystemrequiredpowerrequests-0` | `harmful` | `low` | `ok` | `-37.592` | apply io_write_mib_per_second changed by -37.59% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-082-allowsystemrequiredpowerrequests-0.json` |
| `operator96-082-allowsystemrequiredpowerrequests-1` | `harmful` | `low` | `ok` | `-20.940` | post_reboot cpu_single_iterations_per_second changed by -20.94% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-082-allowsystemrequiredpowerrequests-1.json` |
| `operator96-083-coalescingflushinterval-0` | `low_confidence` | `low` | `ok` | `94.925` | apply io_read_mib_per_second changed by +94.92% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-083-coalescingflushinterval-0.json` |
| `operator96-083-coalescingflushinterval-1` | `harmful` | `high` | `ok` | `-7.904` | apply cpu_multi_iterations_per_second changed by -7.90% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-083-coalescingflushinterval-1.json` |
| `operator96-084-coalescingtimerinterval-0` | `harmful` | `high` | `ok` | `-7.518` | apply cpu_multi_iterations_per_second changed by -7.52% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-084-coalescingtimerinterval-0.json` |
| `operator96-084-coalescingtimerinterval-1` | `harmful` | `high` | `ok` | `-8.244` | apply cpu_multi_iterations_per_second changed by -8.24% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-084-coalescingtimerinterval-1.json` |
