# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-13T02:42:46Z`
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
| `operator96-043-sleepstudydisabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-13.824` | `-1.88` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-043-sleepstudydisabled-1.json` |
| `operator96-043-sleepstudydisabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-15.858` | `0.31` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-043-sleepstudydisabled-0.json` |
| `operator96-044-class1initialunparkcount-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `106.188` | `-4.49` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-044-class1initialunparkcount-0.json` |
| `operator96-044-class1initialunparkcount-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-24.862` | `2.07` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-044-class1initialunparkcount-1.json` |
| `operator96-045-customizeduringsetup-0` | `harmful` | `medium` | `ok` | `ok` | `True` | `ok`/`0` | `-7.798` | `-0.47` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-045-customizeduringsetup-0.json` |
| `operator96-046-energyestimationenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-17.34` | `-0.3` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-046-energyestimationenabled-0.json` |
| `operator96-047-hiberfilesizepercent-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `120.166` | `24.0` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-047-hiberfilesizepercent-1.json` |
