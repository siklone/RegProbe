# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-13T21:25:40Z`
- Status: **ok**
- Planned experiments: `8`
- Completed in this run: `8`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
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
| `operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `112.643` | `14.81` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-0.json` |
| `operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-8.373` | `-3.04` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-1.json` |
| `operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-13.696` | `4.98` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-0.json` |
| `operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `375.694` | `26.5` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-1.json` |
| `operator96-094-enabledsnetrefresh-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-26.583` | `-13.23` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-094-enabledsnetrefresh-0.json` |
| `operator96-094-enabledsnetrefresh-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-35.201` | `-20.59` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-094-enabledsnetrefresh-1.json` |
| `operator96-095-enabledactions-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `74.342` | `13.05` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-095-enabledactions-0.json` |
| `operator96-095-enabledactions-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-28.878` | `9.65` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-095-enabledactions-1.json` |
