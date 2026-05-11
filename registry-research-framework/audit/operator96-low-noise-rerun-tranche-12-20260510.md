# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T12:19:48Z`
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
| `operator96-064-disablevsynclatencyupdate-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-52.3` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-064-disablevsynclatencyupdate-1.json` |
| `operator96-064-disablevsynclatencyupdate-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `10.44` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-064-disablevsynclatencyupdate-0.json` |
| `operator96-066-enableinputsuppression-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-2.69` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-066-enableinputsuppression-1.json` |
| `operator96-066-enableinputsuppression-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-16.85` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-066-enableinputsuppression-0.json` |
| `operator96-067-perfchecktimerimplementation-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-45.46` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-067-perfchecktimerimplementation-0.json` |
| `operator96-067-perfchecktimerimplementation-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-19.71` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-067-perfchecktimerimplementation-1.json` |
| `operator96-068-standbyconnectivitygraceperiod-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-12.82` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-068-standbyconnectivitygraceperiod-0.json` |
| `operator96-068-standbyconnectivitygraceperiod-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `8.56` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-068-standbyconnectivitygraceperiod-1.json` |
| `operator96-069-enforceausteritymode-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-34.03` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-069-enforceausteritymode-0.json` |
| `operator96-069-enforceausteritymode-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-19.89` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-069-enforceausteritymode-1.json` |
