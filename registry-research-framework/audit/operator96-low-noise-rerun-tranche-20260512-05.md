# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-12T20:59:04Z`
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
| `operator96-027-longdpcqueuethreshold-2` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-9.929` | `-3.2` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-027-longdpcqueuethreshold-2.json` |
| `operator96-027-longdpcqueuethreshold-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `75.243` | `0.24` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-027-longdpcqueuethreshold-0.json` |
| `operator96-028-longdpcruntimethreshold-50` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-66.341` | `-4.14` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-028-longdpcruntimethreshold-50.json` |
| `operator96-028-longdpcruntimethreshold-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `95.173` | `3.25` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-028-longdpcruntimethreshold-0.json` |
| `operator96-029-forcebugcheckfordpcwatchdog-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.638` | `-3.86` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-029-forcebugcheckfordpcwatchdog-0.json` |
| `operator96-029-forcebugcheckfordpcwatchdog-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-76.827` | `-4.34` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-029-forcebugcheckfordpcwatchdog-1.json` |
| `operator96-030-forceforegroundboostdecay-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-22.574` | `-3.46` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-030-forceforegroundboostdecay-0.json` |
| `operator96-030-forceforegroundboostdecay-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.498` | `-4.54` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-030-forceforegroundboostdecay-1.json` |
| `operator96-031-rebalanceminpriority-16` | `harmful` | `medium` | `ok` | `ok` | `True` | `ok`/`0` | `-13.145` | `-7.65` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-031-rebalanceminpriority-16.json` |
| `operator96-031-rebalanceminpriority-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-19.993` | `-1.06` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-031-rebalanceminpriority-0.json` |
