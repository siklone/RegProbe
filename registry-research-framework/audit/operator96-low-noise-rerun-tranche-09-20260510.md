# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T08:15:30Z`
- Status: **ok**
- Planned experiments: `7`
- Completed in this run: `7`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 48 | `operator96-048-mfbufferingthreshold-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MfBufferingThreshold` | `1` | `0` | `vm-observed` |
| 49 | `operator96-049-perfcalculateactualutilization-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCalculateActualUtilization` | `0` | `1` | `vm-observed` |
| 50 | `operator96-050-sourcesettingsversion-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\SourceSettingsVersion` | `0` | `4` | `vm-observed` |
| 50 | `operator96-050-sourcesettingsversion-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\SourceSettingsVersion` | `1` | `4` | `vm-observed` |
| 51 | `operator96-051-timerrebasethresholdondripsexit-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\TimerRebaseThresholdOnDripsExit` | `0` | `60` | `vm-observed` |
| 51 | `operator96-051-timerrebasethresholdondripsexit-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\TimerRebaseThresholdOnDripsExit` | `1` | `60` | `vm-observed` |
| 52 | `operator96-052-hibernateenableddefault-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HibernateEnabledDefault` | `0` | `1` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-048-mfbufferingthreshold-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `444.851` | `19.94` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-048-mfbufferingthreshold-1.json` |
| `operator96-049-perfcalculateactualutilization-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-11.344` | `-3.58` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-049-perfcalculateactualutilization-0.json` |
| `operator96-050-sourcesettingsversion-0` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-7.223` | `-3.0` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-050-sourcesettingsversion-0.json` |
| `operator96-050-sourcesettingsversion-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-31.723` | `2.89` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-050-sourcesettingsversion-1.json` |
| `operator96-051-timerrebasethresholdondripsexit-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-15.179` | `-1.18` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-051-timerrebasethresholdondripsexit-0.json` |
| `operator96-051-timerrebasethresholdondripsexit-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-16.427` | `-12.28` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-051-timerrebasethresholdondripsexit-1.json` |
| `operator96-052-hibernateenableddefault-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-17.296` | `-0.31` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-052-hibernateenableddefault-0.json` |
