# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T18:02:26Z`
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
| `operator96-075-msdisabled-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `4.35` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-075-msdisabled-1.json` |
| `operator96-075-msdisabled-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `9.21` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-075-msdisabled-0.json` |
| `operator96-076-fxaccountingtelemetrydisabled-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-5.09` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-076-fxaccountingtelemetrydisabled-1.json` |
| `operator96-076-fxaccountingtelemetrydisabled-0` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-10.307` | `-1.73` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-076-fxaccountingtelemetrydisabled-0.json` |
| `operator96-077-win32kcalloutwatchdogtimeoutseconds-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-70.005` | `-8.66` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-077-win32kcalloutwatchdogtimeoutseconds-0.json` |
| `operator96-077-win32kcalloutwatchdogtimeoutseconds-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-15.001` | `3.37` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-077-win32kcalloutwatchdogtimeoutseconds-1.json` |
| `operator96-078-enableminimalhiberfile-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.268` | `-4.58` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-078-enableminimalhiberfile-0.json` |
| `operator96-078-enableminimalhiberfile-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-14.94` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-078-enableminimalhiberfile-1.json` |
| `operator96-079-hiberbootenabled-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `10.25` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-079-hiberbootenabled-0.json` |
| `operator96-079-hiberbootenabled-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-10.85` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-079-hiberbootenabled-1.json` |
