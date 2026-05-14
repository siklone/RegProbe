# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-10T21:12:52Z`
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
| `operator96-011-disablediskcounters-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-8.27` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-011-disablediskcounters-1.json` |
| `operator96-011-disablediskcounters-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-6.79` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-011-disablediskcounters-0.json` |
| `operator96-012-ioallowloadcrashdumpdriver-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-4.16` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-012-ioallowloadcrashdumpdriver-0.json` |
| `operator96-012-ioallowloadcrashdumpdriver-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `1.89` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-012-ioallowloadcrashdumpdriver-1.json` |
| `operator96-013-ioenablesessionzeroaccesscheck-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-12.311` | `2.03` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-013-ioenablesessionzeroaccesscheck-1.json` |
| `operator96-013-ioenablesessionzeroaccesscheck-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-35.149` | `-8.52` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-013-ioenablesessionzeroaccesscheck-0.json` |
| `operator96-014-globaltimerresolutionrequests-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `81.657` | `5.73` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-014-globaltimerresolutionrequests-1.json` |
| `operator96-014-globaltimerresolutionrequests-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `493.95` | `16.69` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-014-globaltimerresolutionrequests-0.json` |
| `operator96-015-forceparkingrequested-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-8.129` | `4.34` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-015-forceparkingrequested-0.json` |
| `operator96-015-forceparkingrequested-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `28.732` | `-4.21` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-015-forceparkingrequested-1.json` |
