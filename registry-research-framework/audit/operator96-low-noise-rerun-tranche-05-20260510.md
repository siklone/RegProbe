# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T02:31:10Z`
- Status: **ok**
- Planned experiments: `10`
- Completed in this run: `10`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 27 | `operator96-027-longdpcqueuethreshold-2` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcQueueThreshold` | `2` | `absent` | `vm-observed` |
| 27 | `operator96-027-longdpcqueuethreshold-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcQueueThreshold` | `0` | `absent` | `vm-observed` |
| 28 | `operator96-028-longdpcruntimethreshold-50` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcRuntimeThreshold` | `50` | `absent` | `vm-observed` |
| 28 | `operator96-028-longdpcruntimethreshold-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcRuntimeThreshold` | `0` | `absent` | `vm-observed` |
| 29 | `operator96-029-forcebugcheckfordpcwatchdog-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceBugcheckForDpcWatchdog` | `0` | `absent` | `vm-observed` |
| 29 | `operator96-029-forcebugcheckfordpcwatchdog-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceBugcheckForDpcWatchdog` | `1` | `absent` | `vm-observed` |
| 30 | `operator96-030-forceforegroundboostdecay-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceForegroundBoostDecay` | `0` | `absent` | `vm-observed` |
| 30 | `operator96-030-forceforegroundboostdecay-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceForegroundBoostDecay` | `1` | `absent` | `vm-observed` |
| 31 | `operator96-031-rebalanceminpriority-16` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\RebalanceMinPriority` | `16` | `absent` | `vm-observed` |
| 31 | `operator96-031-rebalanceminpriority-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\RebalanceMinPriority` | `0` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-027-longdpcqueuethreshold-2` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-14.197` | `-4.36` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-027-longdpcqueuethreshold-2.json` |
| `operator96-027-longdpcqueuethreshold-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-21.715` | `2.09` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-027-longdpcqueuethreshold-0.json` |
| `operator96-028-longdpcruntimethreshold-50` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-12.39` | `8.22` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-028-longdpcruntimethreshold-50.json` |
| `operator96-028-longdpcruntimethreshold-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `67.774` | `-1.3` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-028-longdpcruntimethreshold-0.json` |
| `operator96-029-forcebugcheckfordpcwatchdog-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-77.268` | `7.13` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-029-forcebugcheckfordpcwatchdog-0.json` |
| `operator96-029-forcebugcheckfordpcwatchdog-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-15.241` | `6.72` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-029-forcebugcheckfordpcwatchdog-1.json` |
| `operator96-030-forceforegroundboostdecay-0` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-8.524` | `1.07` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-030-forceforegroundboostdecay-0.json` |
| `operator96-030-forceforegroundboostdecay-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.17` | `-6.03` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-030-forceforegroundboostdecay-1.json` |
| `operator96-031-rebalanceminpriority-16` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-14.873` | `-2.89` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-031-rebalanceminpriority-16.json` |
| `operator96-031-rebalanceminpriority-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.309` | `-2.39` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-05/operator96-031-rebalanceminpriority-0.json` |
