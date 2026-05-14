# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T02:20:30Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `10`
- Errors: `0`

## Verdict Counts

- `harmful`: `6`
- `low_confidence`: `4`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-022-maxdynamictickduration-0` | `harmful` | `low` | `ok` | `-10.205` | post_reboot io_write_mib_per_second changed by -10.21% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-022-maxdynamictickduration-0.json` |
| `operator96-022-maxdynamictickduration-1` | `low_confidence` | `low` | `ok` | `80.289` | apply io_read_mib_per_second changed by +80.29% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-022-maxdynamictickduration-1.json` |
| `operator96-023-enabletickaccumulationfromaccountingperiods-0` | `low_confidence` | `low` | `ok` | `100.042` | apply io_read_mib_per_second changed by +100.04% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-023-enabletickaccumulationfromaccountingperiods-0.json` |
| `operator96-023-enabletickaccumulationfromaccountingperiods-1` | `harmful` | `low` | `ok` | `-25.496` | apply io_write_mib_per_second changed by -25.50% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-023-enabletickaccumulationfromaccountingperiods-1.json` |
| `operator96-024-enablepercpuclocktickscheduling-0` | `harmful` | `low` | `ok` | `-28.047` | apply io_write_mib_per_second changed by -28.05% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-024-enablepercpuclocktickscheduling-0.json` |
| `operator96-024-enablepercpuclocktickscheduling-1` | `harmful` | `low` | `ok` | `-11.311` | post_reboot io_write_read_mib_per_second changed by -11.31% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-024-enablepercpuclocktickscheduling-1.json` |
| `operator96-025-serializetimerexpiration-0` | `low_confidence` | `low` | `ok` | `147.051` | apply io_read_mib_per_second changed by +147.05% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-025-serializetimerexpiration-0.json` |
| `operator96-025-serializetimerexpiration-1` | `harmful` | `high` | `ok` | `-7.103` | apply cpu_multi_iterations_per_second changed by -7.10% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-025-serializetimerexpiration-1.json` |
| `operator96-026-xstatecontextlookasideperprocmaxdepth-0` | `harmful` | `low` | `ok` | `-9.435` | post_reboot io_write_mib_per_second changed by -9.44% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-026-xstatecontextlookasideperprocmaxdepth-0.json` |
| `operator96-026-xstatecontextlookasideperprocmaxdepth-1024` | `low_confidence` | `low` | `ok` | `95.154` | apply io_read_mib_per_second changed by +95.15% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-026-xstatecontextlookasideperprocmaxdepth-1024.json` |
