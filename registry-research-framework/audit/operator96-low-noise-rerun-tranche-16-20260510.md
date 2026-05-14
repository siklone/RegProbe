# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T21:53:52Z`
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
| 89 | `operator96-089-powerwatchdogpocallouttimeoutmsec-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPoCalloutTimeoutMsec` | `0` | `absent` | `vm-observed` |
| 89 | `operator96-089-powerwatchdogpocallouttimeoutmsec-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPoCalloutTimeoutMsec` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-085-heterohgseeperfhintsindependentenabled-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `141.774` | `3.44` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-085-heterohgseeperfhintsindependentenabled-1.json` |
| `operator96-085-heterohgseeperfhintsindependentenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-22.138` | `-7.51` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-085-heterohgseeperfhintsindependentenabled-0.json` |
| `operator96-086-heterohgsplusdisabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.456` | `1.21` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-086-heterohgsplusdisabled-1.json` |
| `operator96-086-heterohgsplusdisabled-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `106.839` | `9.15` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-086-heterohgsplusdisabled-0.json` |
| `operator96-087-ipilastclockownerdisable-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-9.437` | `-7.04` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-087-ipilastclockownerdisable-1.json` |
| `operator96-087-ipilastclockownerdisable-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `100.178` | `6.08` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-087-ipilastclockownerdisable-0.json` |
| `operator96-088-powerwatchdogrequestqueuetimeoutmsec-0` | `cpu_gain` | `medium` | `ok` | `ok` | `True` | `ok`/`0` | `9.228` | `-5.59` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-088-powerwatchdogrequestqueuetimeoutmsec-0.json` |
| `operator96-088-powerwatchdogrequestqueuetimeoutmsec-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `527.388` | `17.48` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-088-powerwatchdogrequestqueuetimeoutmsec-1.json` |
| `operator96-089-powerwatchdogpocallouttimeoutmsec-0` | `rollback_failure` | `high` | `ok` | `ok` | `False` | `ok`/`0` | `None` | `-2.42` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-089-powerwatchdogpocallouttimeoutmsec-0.json` |
| `operator96-089-powerwatchdogpocallouttimeoutmsec-1` | `app_breakage` | `medium` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-7.31` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-089-powerwatchdogpocallouttimeoutmsec-1.json` |
