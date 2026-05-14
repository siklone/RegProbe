# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-13T18:30:02Z`
- Status: **ok**
- Planned experiments: `10`
- Completed in this run: `10`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 85 | `operator96-085-heterohgseeperfhintsindependentenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroHgsEePerfHintsIndependentEnabled` | `1` | `absent` | `vm-observed` |
| 85 | `operator96-085-heterohgseeperfhintsindependentenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroHgsEePerfHintsIndependentEnabled` | `0` | `absent` | `vm-observed` |
| 86 | `operator96-086-heterohgsplusdisabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroHgsPlusDisabled` | `1` | `absent` | `vm-observed` |
| 86 | `operator96-086-heterohgsplusdisabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroHgsPlusDisabled` | `0` | `absent` | `vm-observed` |
| 87 | `operator96-087-ipilastclockownerdisable-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IpiLastClockOwnerDisable` | `1` | `absent` | `vm-observed` |
| 87 | `operator96-087-ipilastclockownerdisable-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IpiLastClockOwnerDisable` | `0` | `absent` | `vm-observed` |
| 88 | `operator96-088-powerwatchdogrequestqueuetimeoutmsec-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogRequestQueueTimeoutMsec` | `0` | `absent` | `vm-observed` |
| 88 | `operator96-088-powerwatchdogrequestqueuetimeoutmsec-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogRequestQueueTimeoutMsec` | `1` | `absent` | `vm-observed` |
| 90 | `operator96-090-powerwatchdogpowerongditimeoutmsec-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPowerOnGdiTimeoutMsec` | `0` | `absent` | `vm-observed` |
| 90 | `operator96-090-powerwatchdogpowerongditimeoutmsec-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPowerOnGdiTimeoutMsec` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-085-heterohgseeperfhintsindependentenabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-81.598` | `-23.08` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-085-heterohgseeperfhintsindependentenabled-1.json` |
| `operator96-085-heterohgseeperfhintsindependentenabled-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `error`/`None` | `468.483` | `44.59` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-085-heterohgseeperfhintsindependentenabled-0.json` |
| `operator96-086-heterohgsplusdisabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-85.239` | `-3.57` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-086-heterohgsplusdisabled-1.json` |
| `operator96-086-heterohgsplusdisabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-11.112` | `-0.46` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-086-heterohgsplusdisabled-0.json` |
| `operator96-087-ipilastclockownerdisable-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-11.221` | `-4.3` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-087-ipilastclockownerdisable-1.json` |
| `operator96-087-ipilastclockownerdisable-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.576` | `-1.04` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-087-ipilastclockownerdisable-0.json` |
| `operator96-088-powerwatchdogrequestqueuetimeoutmsec-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `137.356` | `4.66` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-088-powerwatchdogrequestqueuetimeoutmsec-0.json` |
| `operator96-088-powerwatchdogrequestqueuetimeoutmsec-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-77.454` | `-21.29` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-088-powerwatchdogrequestqueuetimeoutmsec-1.json` |
| `operator96-090-powerwatchdogpowerongditimeoutmsec-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-53.839` | `2.02` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-090-powerwatchdogpowerongditimeoutmsec-0.json` |
| `operator96-090-powerwatchdogpowerongditimeoutmsec-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-16.76` | `-9.2` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-090-powerwatchdogpowerongditimeoutmsec-1.json` |
