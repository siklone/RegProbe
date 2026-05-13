# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-13T16:33:25Z`
- Status: **ok**
- Planned experiments: `10`
- Completed in this run: `10`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 80 | `operator96-080-maximumfrequencyoverride-100` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MaximumFrequencyOverride` | `100` | `absent` | `vm-observed` |
| 80 | `operator96-080-maximumfrequencyoverride-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MaximumFrequencyOverride` | `0` | `absent` | `vm-observed` |
| 81 | `operator96-081-pofxsystemirpwaitforreportdevicepowered-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PoFxSystemIrpWaitForReportDevicePowered` | `0` | `absent` | `vm-observed` |
| 81 | `operator96-081-pofxsystemirpwaitforreportdevicepowered-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PoFxSystemIrpWaitForReportDevicePowered` | `1` | `absent` | `vm-observed` |
| 82 | `operator96-082-allowsystemrequiredpowerrequests-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowSystemRequiredPowerRequests` | `0` | `absent` | `vm-observed` |
| 82 | `operator96-082-allowsystemrequiredpowerrequests-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowSystemRequiredPowerRequests` | `1` | `absent` | `vm-observed` |
| 83 | `operator96-083-coalescingflushinterval-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingFlushInterval` | `0` | `absent` | `vm-observed` |
| 83 | `operator96-083-coalescingflushinterval-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingFlushInterval` | `1` | `absent` | `vm-observed` |
| 84 | `operator96-084-coalescingtimerinterval-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingTimerInterval` | `0` | `absent` | `vm-observed` |
| 84 | `operator96-084-coalescingtimerinterval-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingTimerInterval` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-080-maximumfrequencyoverride-100` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `100.025` | `-2.16` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-080-maximumfrequencyoverride-100.json` |
| `operator96-080-maximumfrequencyoverride-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `81.345` | `-3.64` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-080-maximumfrequencyoverride-0.json` |
| `operator96-081-pofxsystemirpwaitforreportdevicepowered-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-75.887` | `-1.02` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-081-pofxsystemirpwaitforreportdevicepowered-0.json` |
| `operator96-081-pofxsystemirpwaitforreportdevicepowered-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `147.617` | `6.2` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-081-pofxsystemirpwaitforreportdevicepowered-1.json` |
| `operator96-082-allowsystemrequiredpowerrequests-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.836` | `-2.35` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-082-allowsystemrequiredpowerrequests-0.json` |
| `operator96-082-allowsystemrequiredpowerrequests-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-9.496` | `-1.79` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-082-allowsystemrequiredpowerrequests-1.json` |
| `operator96-083-coalescingflushinterval-0` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-10.571` | `-4.13` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-083-coalescingflushinterval-0.json` |
| `operator96-083-coalescingflushinterval-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.896` | `0.89` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-083-coalescingflushinterval-1.json` |
| `operator96-084-coalescingtimerinterval-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `103.62` | `2.57` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-084-coalescingtimerinterval-0.json` |
| `operator96-084-coalescingtimerinterval-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-87.75` | `-18.72` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-084-coalescingtimerinterval-1.json` |
