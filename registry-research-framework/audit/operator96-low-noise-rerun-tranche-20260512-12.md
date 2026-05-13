# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-13T12:24:08Z`
- Status: **ok**
- Planned experiments: `10`
- Completed in this run: `10`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 64 | `operator96-064-disablevsynclatencyupdate-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableVsyncLatencyUpdate` | `1` | `absent` | `vm-observed` |
| 64 | `operator96-064-disablevsynclatencyupdate-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableVsyncLatencyUpdate` | `0` | `absent` | `vm-observed` |
| 66 | `operator96-066-enableinputsuppression-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnableInputSuppression` | `1` | `absent` | `vm-observed` |
| 66 | `operator96-066-enableinputsuppression-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnableInputSuppression` | `0` | `absent` | `vm-observed` |
| 67 | `operator96-067-perfchecktimerimplementation-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCheckTimerImplementation` | `0` | `absent` | `vm-observed` |
| 67 | `operator96-067-perfchecktimerimplementation-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCheckTimerImplementation` | `1` | `absent` | `vm-observed` |
| 68 | `operator96-068-standbyconnectivitygraceperiod-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\StandbyConnectivityGracePeriod` | `0` | `absent` | `vm-observed` |
| 68 | `operator96-068-standbyconnectivitygraceperiod-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\StandbyConnectivityGracePeriod` | `1` | `absent` | `vm-observed` |
| 69 | `operator96-069-enforceausteritymode-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnforceAusterityMode` | `0` | `absent` | `vm-observed` |
| 69 | `operator96-069-enforceausteritymode-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnforceAusterityMode` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-064-disablevsynclatencyupdate-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `102.112` | `0.12` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-064-disablevsynclatencyupdate-1.json` |
| `operator96-064-disablevsynclatencyupdate-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-81.135` | `-1.34` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-064-disablevsynclatencyupdate-0.json` |
| `operator96-066-enableinputsuppression-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-22.098` | `-2.11` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-066-enableinputsuppression-1.json` |
| `operator96-066-enableinputsuppression-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-25.153` | `-6.93` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-066-enableinputsuppression-0.json` |
| `operator96-067-perfchecktimerimplementation-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-80.432` | `-20.1` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-067-perfchecktimerimplementation-0.json` |
| `operator96-067-perfchecktimerimplementation-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.487` | `7.36` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-067-perfchecktimerimplementation-1.json` |
| `operator96-068-standbyconnectivitygraceperiod-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-23.96` | `-9.54` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-068-standbyconnectivitygraceperiod-0.json` |
| `operator96-068-standbyconnectivitygraceperiod-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-82.477` | `-20.14` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-068-standbyconnectivitygraceperiod-1.json` |
| `operator96-069-enforceausteritymode-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-21.586` | `-6.57` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-069-enforceausteritymode-0.json` |
| `operator96-069-enforceausteritymode-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `89.241` | `2.13` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-069-enforceausteritymode-1.json` |
