# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T20:05:43Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `10`
- Errors: `0`

## Verdict Counts

- `harmful`: `4`
- `noisy`: `6`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-075-msdisabled-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-075-msdisabled-0.json` |
| `operator96-075-msdisabled-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-075-msdisabled-1.json` |
| `operator96-076-fxaccountingtelemetrydisabled-0` | `harmful` | `high` | `ok` | `-10.307` | apply cpu_multi_iterations_per_second changed by -10.31% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-076-fxaccountingtelemetrydisabled-0.json` |
| `operator96-076-fxaccountingtelemetrydisabled-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-076-fxaccountingtelemetrydisabled-1.json` |
| `operator96-077-win32kcalloutwatchdogtimeoutseconds-0` | `harmful` | `low` | `ok` | `-70.005` | post_reboot io_write_mib_per_second changed by -70.00% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-077-win32kcalloutwatchdogtimeoutseconds-0.json` |
| `operator96-077-win32kcalloutwatchdogtimeoutseconds-1` | `harmful` | `low` | `ok` | `-15.001` | apply io_write_mib_per_second changed by -15.00% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-077-win32kcalloutwatchdogtimeoutseconds-1.json` |
| `operator96-078-enableminimalhiberfile-0` | `harmful` | `low` | `ok` | `-7.268` | post_reboot io_write_mib_per_second changed by -7.27% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-078-enableminimalhiberfile-0.json` |
| `operator96-078-enableminimalhiberfile-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-078-enableminimalhiberfile-1.json` |
| `operator96-079-hiberbootenabled-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-079-hiberbootenabled-0.json` |
| `operator96-079-hiberbootenabled-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-079-hiberbootenabled-1.json` |
