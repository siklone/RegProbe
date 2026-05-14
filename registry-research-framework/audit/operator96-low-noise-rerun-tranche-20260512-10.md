# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-13T05:53:06Z`
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
| `operator96-053-eventprocessorenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-75.875` | `-17.46` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-053-eventprocessorenabled-0.json` |
| `operator96-054-lidreliabilitystate-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-9.882` | `-2.32` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-054-lidreliabilitystate-0.json` |
| `operator96-055-hibernateenabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-17.981` | `-1.58` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-055-hibernateenabled-1.json` |
| `operator96-056-disableinboxpepgeneratedconstraints-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `110.644` | `8.17` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-056-disableinboxpepgeneratedconstraints-1.json` |
| `operator96-056-disableinboxpepgeneratedconstraints-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `116.04` | `-2.07` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-056-disableinboxpepgeneratedconstraints-0.json` |
| `operator96-057-disabledisplayburstonpowersourcechange-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-17.368` | `-4.9` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-057-disabledisplayburstonpowersourcechange-1.json` |
| `operator96-057-disabledisplayburstonpowersourcechange-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-19.473` | `-4.61` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-057-disabledisplayburstonpowersourcechange-0.json` |
