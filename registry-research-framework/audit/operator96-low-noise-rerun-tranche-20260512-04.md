# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-12T17:06:59Z`
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
| `operator96-022-maxdynamictickduration-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-17.796` | `1.6` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-022-maxdynamictickduration-1.json` |
| `operator96-022-maxdynamictickduration-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-9.049` | `-4.06` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-022-maxdynamictickduration-0.json` |
| `operator96-023-enabletickaccumulationfromaccountingperiods-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-79.058` | `-9.07` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-023-enabletickaccumulationfromaccountingperiods-1.json` |
| `operator96-023-enabletickaccumulationfromaccountingperiods-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `102.476` | `-3.26` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-023-enabletickaccumulationfromaccountingperiods-0.json` |
| `operator96-024-enablepercpuclocktickscheduling-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-13.571` | `-2.95` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-024-enablepercpuclocktickscheduling-1.json` |
| `operator96-024-enablepercpuclocktickscheduling-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-86.969` | `3.58` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-024-enablepercpuclocktickscheduling-0.json` |
| `operator96-025-serializetimerexpiration-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `114.257` | `-2.27` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-025-serializetimerexpiration-0.json` |
| `operator96-025-serializetimerexpiration-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-13.087` | `7.35` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-025-serializetimerexpiration-1.json` |
| `operator96-026-xstatecontextlookasideperprocmaxdepth-1024` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-11.594` | `-1.88` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-026-xstatecontextlookasideperprocmaxdepth-1024.json` |
| `operator96-026-xstatecontextlookasideperprocmaxdepth-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-28.357` | `-10.0` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-026-xstatecontextlookasideperprocmaxdepth-0.json` |
