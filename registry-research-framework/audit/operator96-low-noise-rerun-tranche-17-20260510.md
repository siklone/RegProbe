# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T23:24:13Z`
- Status: **ok**
- Planned experiments: `10`
- Completed in this run: `10`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 90 | `operator96-090-powerwatchdogpowerongditimeoutmsec-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPowerOnGdiTimeoutMsec` | `0` | `absent` | `vm-observed` |
| 90 | `operator96-090-powerwatchdogpowerongditimeoutmsec-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPowerOnGdiTimeoutMsec` | `1` | `absent` | `vm-observed` |
| 91 | `operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogDwmSyncFlushTimeoutMsec` | `0` | `absent` | `vm-observed` |
| 91 | `operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogDwmSyncFlushTimeoutMsec` | `1` | `absent` | `vm-observed` |
| 92 | `operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogDrvSetMonitorTimeoutMsec` | `0` | `absent` | `vm-observed` |
| 92 | `operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogDrvSetMonitorTimeoutMsec` | `1` | `absent` | `vm-observed` |
| 94 | `operator96-094-enabledsnetrefresh-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnableDsNetRefresh` | `0` | `absent` | `vm-observed` |
| 94 | `operator96-094-enabledsnetrefresh-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnableDsNetRefresh` | `1` | `absent` | `vm-observed` |
| 95 | `operator96-095-enabledactions-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnabledActions` | `0` | `absent` | `vm-observed` |
| 95 | `operator96-095-enabledactions-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnabledActions` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-090-powerwatchdogpowerongditimeoutmsec-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `38.48` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-090-powerwatchdogpowerongditimeoutmsec-0.json` |
| `operator96-090-powerwatchdogpowerongditimeoutmsec-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.671` | `5.11` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-090-powerwatchdogpowerongditimeoutmsec-1.json` |
| `operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-32.312` | `4.4` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-0.json` |
| `operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `96.485` | `17.14` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-1.json` |
| `operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `552.73` | `25.68` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-0.json` |
| `operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-15.388` | `-0.36` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-1.json` |
| `operator96-094-enabledsnetrefresh-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-25.516` | `-2.83` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-094-enabledsnetrefresh-0.json` |
| `operator96-094-enabledsnetrefresh-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `88.855` | `-1.69` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-094-enabledsnetrefresh-1.json` |
| `operator96-095-enabledactions-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `96.495` | `-1.09` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-095-enabledactions-0.json` |
| `operator96-095-enabledactions-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `95.063` | `4.27` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-095-enabledactions-1.json` |
