# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T03:48:56Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `10`
- Errors: `0`

## Verdict Counts

- `harmful`: `9`
- `low_confidence`: `1`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-027-longdpcqueuethreshold-0` | `harmful` | `low` | `ok` | `-21.715` | post_reboot io_write_mib_per_second changed by -21.71% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-027-longdpcqueuethreshold-0.json` |
| `operator96-027-longdpcqueuethreshold-2` | `harmful` | `low` | `ok` | `-14.197` | post_reboot io_write_mib_per_second changed by -14.20% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-027-longdpcqueuethreshold-2.json` |
| `operator96-028-longdpcruntimethreshold-0` | `low_confidence` | `low` | `ok` | `67.774` | apply io_read_mib_per_second changed by +67.77% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-028-longdpcruntimethreshold-0.json` |
| `operator96-028-longdpcruntimethreshold-50` | `harmful` | `low` | `ok` | `-12.390` | apply io_write_mib_per_second changed by -12.39% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-028-longdpcruntimethreshold-50.json` |
| `operator96-029-forcebugcheckfordpcwatchdog-0` | `harmful` | `low` | `ok` | `-77.268` | apply io_write_mib_per_second changed by -77.27% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-029-forcebugcheckfordpcwatchdog-0.json` |
| `operator96-029-forcebugcheckfordpcwatchdog-1` | `harmful` | `low` | `ok` | `-15.241` | apply io_write_mib_per_second changed by -15.24% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-029-forcebugcheckfordpcwatchdog-1.json` |
| `operator96-030-forceforegroundboostdecay-0` | `harmful` | `high` | `ok` | `-8.524` | apply cpu_multi_iterations_per_second changed by -8.52% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-030-forceforegroundboostdecay-0.json` |
| `operator96-030-forceforegroundboostdecay-1` | `harmful` | `low` | `ok` | `-10.170` | post_reboot io_read_mib_per_second changed by -10.17% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-030-forceforegroundboostdecay-1.json` |
| `operator96-031-rebalanceminpriority-0` | `harmful` | `low` | `ok` | `-7.309` | post_reboot io_write_mib_per_second changed by -7.31% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-031-rebalanceminpriority-0.json` |
| `operator96-031-rebalanceminpriority-16` | `harmful` | `low` | `ok` | `-14.873` | apply io_write_mib_per_second changed by -14.87% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-031-rebalanceminpriority-16.json` |
