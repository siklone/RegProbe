# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-13T15:11:49Z`
- Status: **ok**
- Planned experiments: `10`
- Completed in this run: `10`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 75 | `operator96-075-msdisabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MSDisabled` | `1` | `absent` | `vm-observed` |
| 75 | `operator96-075-msdisabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MSDisabled` | `0` | `absent` | `vm-observed` |
| 76 | `operator96-076-fxaccountingtelemetrydisabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\FxAccountingTelemetryDisabled` | `1` | `absent` | `vm-observed` |
| 76 | `operator96-076-fxaccountingtelemetrydisabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\FxAccountingTelemetryDisabled` | `0` | `absent` | `vm-observed` |
| 77 | `operator96-077-win32kcalloutwatchdogtimeoutseconds-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\Win32kCalloutWatchdogTimeoutSeconds` | `0` | `absent` | `vm-observed` |
| 77 | `operator96-077-win32kcalloutwatchdogtimeoutseconds-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\Win32kCalloutWatchdogTimeoutSeconds` | `1` | `absent` | `vm-observed` |
| 78 | `operator96-078-enableminimalhiberfile-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnableMinimalHiberFile` | `0` | `absent` | `vm-observed` |
| 78 | `operator96-078-enableminimalhiberfile-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnableMinimalHiberFile` | `1` | `absent` | `vm-observed` |
| 79 | `operator96-079-hiberbootenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberbootEnabled` | `0` | `absent` | `vm-observed` |
| 79 | `operator96-079-hiberbootenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberbootEnabled` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-075-msdisabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.186` | `-7.19` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-075-msdisabled-1.json` |
| `operator96-075-msdisabled-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `65.402` | `-0.73` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-075-msdisabled-0.json` |
| `operator96-076-fxaccountingtelemetrydisabled-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `96.909` | `-3.27` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-076-fxaccountingtelemetrydisabled-1.json` |
| `operator96-076-fxaccountingtelemetrydisabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-81.703` | `0.39` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-076-fxaccountingtelemetrydisabled-0.json` |
| `operator96-077-win32kcalloutwatchdogtimeoutseconds-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-17.133` | `1.41` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-077-win32kcalloutwatchdogtimeoutseconds-0.json` |
| `operator96-077-win32kcalloutwatchdogtimeoutseconds-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.018` | `-7.02` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-077-win32kcalloutwatchdogtimeoutseconds-1.json` |
| `operator96-078-enableminimalhiberfile-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `90.858` | `-3.55` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-078-enableminimalhiberfile-0.json` |
| `operator96-078-enableminimalhiberfile-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `68.456` | `2.42` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-078-enableminimalhiberfile-1.json` |
| `operator96-079-hiberbootenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-18.22` | `-0.75` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-079-hiberbootenabled-0.json` |
| `operator96-079-hiberbootenabled-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `115.123` | `3.72` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-079-hiberbootenabled-1.json` |
