# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T01:02:25Z`
- Status: **ok**
- Planned experiments: `10`
- Completed in this run: `10`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 22 | `operator96-022-maxdynamictickduration-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaxDynamicTickDuration` | `1` | `absent` | `vm-observed` |
| 22 | `operator96-022-maxdynamictickduration-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaxDynamicTickDuration` | `0` | `absent` | `vm-observed` |
| 23 | `operator96-023-enabletickaccumulationfromaccountingperiods-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnableTickAccumulationFromAccountingPeriods` | `1` | `absent` | `vm-observed` |
| 23 | `operator96-023-enabletickaccumulationfromaccountingperiods-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnableTickAccumulationFromAccountingPeriods` | `0` | `absent` | `vm-observed` |
| 24 | `operator96-024-enablepercpuclocktickscheduling-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnablePerCpuClockTickScheduling` | `1` | `absent` | `vm-observed` |
| 24 | `operator96-024-enablepercpuclocktickscheduling-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnablePerCpuClockTickScheduling` | `0` | `absent` | `vm-observed` |
| 25 | `operator96-025-serializetimerexpiration-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\SerializeTimerExpiration` | `0` | `absent` | `vm-observed` |
| 25 | `operator96-025-serializetimerexpiration-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\SerializeTimerExpiration` | `1` | `absent` | `vm-observed` |
| 26 | `operator96-026-xstatecontextlookasideperprocmaxdepth-1024` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\XStateContextLookasidePerProcMaxDepth` | `1024` | `absent` | `vm-observed` |
| 26 | `operator96-026-xstatecontextlookasideperprocmaxdepth-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\XStateContextLookasidePerProcMaxDepth` | `0` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-022-maxdynamictickduration-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `80.289` | `-1.63` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-022-maxdynamictickduration-1.json` |
| `operator96-022-maxdynamictickduration-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.205` | `3.18` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-022-maxdynamictickduration-0.json` |
| `operator96-023-enabletickaccumulationfromaccountingperiods-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-25.496` | `-0.09` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-023-enabletickaccumulationfromaccountingperiods-1.json` |
| `operator96-023-enabletickaccumulationfromaccountingperiods-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `100.042` | `-4.7` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-023-enabletickaccumulationfromaccountingperiods-0.json` |
| `operator96-024-enablepercpuclocktickscheduling-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-11.311` | `-11.31` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-024-enablepercpuclocktickscheduling-1.json` |
| `operator96-024-enablepercpuclocktickscheduling-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-28.047` | `6.98` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-024-enablepercpuclocktickscheduling-0.json` |
| `operator96-025-serializetimerexpiration-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `147.051` | `-2.89` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-025-serializetimerexpiration-0.json` |
| `operator96-025-serializetimerexpiration-1` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-7.103` | `-1.08` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-025-serializetimerexpiration-1.json` |
| `operator96-026-xstatecontextlookasideperprocmaxdepth-1024` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `95.154` | `-5.72` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-026-xstatecontextlookasideperprocmaxdepth-1024.json` |
| `operator96-026-xstatecontextlookasideperprocmaxdepth-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-9.435` | `-5.95` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-04/operator96-026-xstatecontextlookasideperprocmaxdepth-0.json` |
