# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T09:22:33Z`
- Status: **ok**
- Planned experiments: `7`
- Completed in this run: `7`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 53 | `operator96-053-eventprocessorenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EventProcessorEnabled` | `0` | `1` | `vm-observed` |
| 54 | `operator96-054-lidreliabilitystate-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\LidReliabilityState` | `0` | `1` | `vm-observed` |
| 55 | `operator96-055-hibernateenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HibernateEnabled` | `1` | `0` | `vm-observed` |
| 56 | `operator96-056-disableinboxpepgeneratedconstraints-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableInboxPepGeneratedConstraints` | `1` | `absent` | `vm-observed` |
| 56 | `operator96-056-disableinboxpepgeneratedconstraints-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableInboxPepGeneratedConstraints` | `0` | `absent` | `vm-observed` |
| 57 | `operator96-057-disabledisplayburstonpowersourcechange-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableDisplayBurstOnPowerSourceChange` | `1` | `absent` | `vm-observed` |
| 57 | `operator96-057-disabledisplayburstonpowersourcechange-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableDisplayBurstOnPowerSourceChange` | `0` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-053-eventprocessorenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-15.306` | `-15.31` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-053-eventprocessorenabled-0.json` |
| `operator96-054-lidreliabilitystate-0` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-7.427` | `-1.13` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-054-lidreliabilitystate-0.json` |
| `operator96-055-hibernateenabled-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `101.66` | `11.0` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-055-hibernateenabled-1.json` |
| `operator96-056-disableinboxpepgeneratedconstraints-1` | `harmful` | `medium` | `ok` | `ok` | `True` | `ok`/`0` | `-7.25` | `3.92` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-056-disableinboxpepgeneratedconstraints-1.json` |
| `operator96-056-disableinboxpepgeneratedconstraints-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-33.999` | `8.09` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-056-disableinboxpepgeneratedconstraints-0.json` |
| `operator96-057-disabledisplayburstonpowersourcechange-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `80.97` | `3.96` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-057-disabledisplayburstonpowersourcechange-1.json` |
| `operator96-057-disabledisplayburstonpowersourcechange-0` | `cpu_gain` | `medium` | `ok` | `ok` | `True` | `ok`/`0` | `8.392` | `23.02` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-057-disabledisplayburstonpowersourcechange-0.json` |
