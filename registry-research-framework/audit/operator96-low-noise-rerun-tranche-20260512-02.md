# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-12T13:57:07Z`
- Status: **ok**
- Planned experiments: `10`
- Completed in this run: `10`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 11 | `operator96-011-disablediskcounters-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\DisableDiskCounters` | `1` | `absent` | `vm-observed` |
| 11 | `operator96-011-disablediskcounters-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\DisableDiskCounters` | `0` | `absent` | `vm-observed` |
| 12 | `operator96-012-ioallowloadcrashdumpdriver-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoAllowLoadCrashDumpDriver` | `0` | `absent` | `vm-observed` |
| 12 | `operator96-012-ioallowloadcrashdumpdriver-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoAllowLoadCrashDumpDriver` | `1` | `absent` | `vm-observed` |
| 13 | `operator96-013-ioenablesessionzeroaccesscheck-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoEnableSessionZeroAccessCheck` | `1` | `absent` | `vm-observed` |
| 13 | `operator96-013-ioenablesessionzeroaccesscheck-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoEnableSessionZeroAccessCheck` | `0` | `absent` | `vm-observed` |
| 14 | `operator96-014-globaltimerresolutionrequests-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\GlobalTimerResolutionRequests` | `1` | `absent` | `vm-observed` |
| 14 | `operator96-014-globaltimerresolutionrequests-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\GlobalTimerResolutionRequests` | `0` | `absent` | `vm-observed` |
| 15 | `operator96-015-forceparkingrequested-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceParkingRequested` | `0` | `absent` | `vm-observed` |
| 15 | `operator96-015-forceparkingrequested-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceParkingRequested` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-011-disablediskcounters-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `108.162` | `3.86` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-011-disablediskcounters-1.json` |
| `operator96-011-disablediskcounters-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-83.067` | `1.95` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-011-disablediskcounters-0.json` |
| `operator96-012-ioallowloadcrashdumpdriver-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-20.111` | `-12.09` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-012-ioallowloadcrashdumpdriver-0.json` |
| `operator96-012-ioallowloadcrashdumpdriver-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-16.043` | `5.65` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-012-ioallowloadcrashdumpdriver-1.json` |
| `operator96-013-ioenablesessionzeroaccesscheck-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-22.106` | `0.72` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-013-ioenablesessionzeroaccesscheck-1.json` |
| `operator96-013-ioenablesessionzeroaccesscheck-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `105.565` | `0.27` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-013-ioenablesessionzeroaccesscheck-0.json` |
| `operator96-014-globaltimerresolutionrequests-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-17.979` | `-2.51` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-014-globaltimerresolutionrequests-1.json` |
| `operator96-014-globaltimerresolutionrequests-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-29.737` | `-5.33` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-014-globaltimerresolutionrequests-0.json` |
| `operator96-015-forceparkingrequested-0` | `cpu_gain` | `medium` | `ok` | `ok` | `True` | `ok`/`0` | `11.611` | `8.65` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-015-forceparkingrequested-0.json` |
| `operator96-015-forceparkingrequested-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `87.757` | `-5.6` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-015-forceparkingrequested-1.json` |
