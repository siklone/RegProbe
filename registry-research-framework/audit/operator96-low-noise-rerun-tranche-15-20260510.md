# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T20:15:54Z`
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
| `operator96-080-maximumfrequencyoverride-100` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-5.99` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-080-maximumfrequencyoverride-100.json` |
| `operator96-080-maximumfrequencyoverride-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `1.29` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-080-maximumfrequencyoverride-0.json` |
| `operator96-081-pofxsystemirpwaitforreportdevicepowered-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-12.331` | `12.25` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-081-pofxsystemirpwaitforreportdevicepowered-0.json` |
| `operator96-081-pofxsystemirpwaitforreportdevicepowered-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-8.025` | `-0.31` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-081-pofxsystemirpwaitforreportdevicepowered-1.json` |
| `operator96-082-allowsystemrequiredpowerrequests-0` | `harmful` | `low` | `ok` | `ok` | `True` | `error`/`None` | `-37.592` | `10.34` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-082-allowsystemrequiredpowerrequests-0.json` |
| `operator96-082-allowsystemrequiredpowerrequests-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-20.94` | `-4.17` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-082-allowsystemrequiredpowerrequests-1.json` |
| `operator96-083-coalescingflushinterval-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `94.925` | `2.21` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-083-coalescingflushinterval-0.json` |
| `operator96-083-coalescingflushinterval-1` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-7.904` | `12.05` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-083-coalescingflushinterval-1.json` |
| `operator96-084-coalescingtimerinterval-0` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-7.518` | `-3.32` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-084-coalescingtimerinterval-0.json` |
| `operator96-084-coalescingtimerinterval-1` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-8.244` | `-4.81` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-084-coalescingtimerinterval-1.json` |
