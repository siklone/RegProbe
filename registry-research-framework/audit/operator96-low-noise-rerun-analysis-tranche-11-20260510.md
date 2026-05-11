# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T12:10:44Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `10`
- Errors: `0`

## Verdict Counts

- `harmful`: `4`
- `low_confidence`: `2`
- `noisy`: `4`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-058-idleprocessorsrequireqosmanagement-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-058-idleprocessorsrequireqosmanagement-0.json` |
| `operator96-058-idleprocessorsrequireqosmanagement-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-058-idleprocessorsrequireqosmanagement-1.json` |
| `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-0.json` |
| `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-1.json` |
| `operator96-061-deepiocoalescingenabled-0` | `harmful` | `low` | `ok` | `-23.938` | post_reboot io_write_mib_per_second changed by -23.94% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-061-deepiocoalescingenabled-0.json` |
| `operator96-061-deepiocoalescingenabled-1` | `harmful` | `low` | `ok` | `-16.072` | apply io_write_mib_per_second changed by -16.07% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-061-deepiocoalescingenabled-1.json` |
| `operator96-062-ignorecscompliancecheck-0` | `low_confidence` | `low` | `ok` | `195.350` | apply io_read_mib_per_second changed by +195.35% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-062-ignorecscompliancecheck-0.json` |
| `operator96-062-ignorecscompliancecheck-1` | `low_confidence` | `low` | `ok` | `143.615` | apply io_read_mib_per_second changed by +143.62% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-062-ignorecscompliancecheck-1.json` |
| `operator96-063-dripsswhwdivergenceenablelivedump-0` | `harmful` | `high` | `ok` | `-7.338` | apply cpu_multi_iterations_per_second changed by -7.34% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-063-dripsswhwdivergenceenablelivedump-0.json` |
| `operator96-063-dripsswhwdivergenceenablelivedump-1` | `harmful` | `low` | `ok` | `-10.992` | post_reboot io_write_mib_per_second changed by -10.99% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-063-dripsswhwdivergenceenablelivedump-1.json` |
