# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T08:03:37Z`
- Status: **ok**
- Planned experiments: `7`
- Completed in this run: `7`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 43 | `operator96-043-sleepstudydisabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SleepStudyDisabled` | `1` | `absent` | `vm-observed` |
| 43 | `operator96-043-sleepstudydisabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SleepStudyDisabled` | `0` | `absent` | `vm-observed` |
| 44 | `operator96-044-class1initialunparkcount-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\Class1InitialUnparkCount` | `0` | `64` | `vm-observed` |
| 44 | `operator96-044-class1initialunparkcount-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\Class1InitialUnparkCount` | `1` | `64` | `vm-observed` |
| 45 | `operator96-045-customizeduringsetup-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CustomizeDuringSetup` | `0` | `1` | `vm-observed` |
| 46 | `operator96-046-energyestimationenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnergyEstimationEnabled` | `0` | `1` | `vm-observed` |
| 47 | `operator96-047-hiberfilesizepercent-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberFileSizePercent` | `1` | `0` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-043-sleepstudydisabled-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `138.561` | `4.4` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-043-sleepstudydisabled-1.json` |
| `operator96-043-sleepstudydisabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-19.346` | `-0.6` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-043-sleepstudydisabled-0.json` |
| `operator96-044-class1initialunparkcount-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `134.351` | `8.25` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-044-class1initialunparkcount-0.json` |
| `operator96-044-class1initialunparkcount-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-20.963` | `-7.23` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-044-class1initialunparkcount-1.json` |
| `operator96-045-customizeduringsetup-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-22.41` | `-4.0` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-045-customizeduringsetup-0.json` |
| `operator96-046-energyestimationenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-22.716` | `2.6` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-046-energyestimationenabled-0.json` |
| `operator96-047-hiberfilesizepercent-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-20.801` | `4.5` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-047-hiberfilesizepercent-1.json` |
