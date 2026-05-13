# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-13T03:42:00Z`
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
| `operator96-048-mfbufferingthreshold-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-26.407` | `-5.76` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-048-mfbufferingthreshold-1.json` |
| `operator96-049-perfcalculateactualutilization-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `123.617` | `-2.84` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-049-perfcalculateactualutilization-0.json` |
| `operator96-050-sourcesettingsversion-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-33.321` | `1.86` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-050-sourcesettingsversion-0.json` |
| `operator96-050-sourcesettingsversion-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.15` | `1.03` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-050-sourcesettingsversion-1.json` |
| `operator96-051-timerrebasethresholdondripsexit-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-18.638` | `6.36` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-051-timerrebasethresholdondripsexit-0.json` |
| `operator96-051-timerrebasethresholdondripsexit-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-21.359` | `4.03` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-051-timerrebasethresholdondripsexit-1.json` |
| `operator96-052-hibernateenableddefault-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-9.568` | `-5.53` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-052-hibernateenableddefault-0.json` |
