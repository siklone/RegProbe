# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T00:53:30Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `10`
- Errors: `0`

## Verdict Counts

- `harmful`: `5`
- `low_confidence`: `3`
- `noisy`: `2`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-016-enableweruserreporting-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-016-enableweruserreporting-0.json` |
| `operator96-016-enableweruserreporting-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-016-enableweruserreporting-1.json` |
| `operator96-017-hyperstartdisabled-0` | `harmful` | `medium` | `ok` | `-9.146` | apply cpu_multi_iterations_per_second changed by -9.15% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-017-hyperstartdisabled-0.json` |
| `operator96-017-hyperstartdisabled-1` | `low_confidence` | `low` | `ok` | `180.912` | apply io_read_mib_per_second changed by +180.91% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-017-hyperstartdisabled-1.json` |
| `operator96-018-disablelightweightsuspend-0` | `harmful` | `low` | `ok` | `-16.608` | post_reboot io_write_mib_per_second changed by -16.61% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-018-disablelightweightsuspend-0.json` |
| `operator96-018-disablelightweightsuspend-1` | `harmful` | `medium` | `ok` | `-8.830` | apply cpu_multi_iterations_per_second changed by -8.83% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-018-disablelightweightsuspend-1.json` |
| `operator96-019-timercheckflags-0` | `harmful` | `low` | `ok` | `-10.787` | apply io_write_mib_per_second changed by -10.79% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-019-timercheckflags-0.json` |
| `operator96-019-timercheckflags-1` | `low_confidence` | `low` | `ok` | `133.524` | apply io_read_mib_per_second changed by +133.52% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-019-timercheckflags-1.json` |
| `operator96-020-forceidlegraceperiod-0` | `low_confidence` | `low` | `ok` | `136.539` | apply io_read_mib_per_second changed by +136.54% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-020-forceidlegraceperiod-0.json` |
| `operator96-020-forceidlegraceperiod-1` | `harmful` | `low` | `ok` | `-17.040` | post_reboot io_write_mib_per_second changed by -17.04% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-020-forceidlegraceperiod-1.json` |
