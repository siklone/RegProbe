# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-13T01:17:26Z`
- Status: **ok**
- Planned experiments: `10`
- Completed in this run: `10`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 38 | `operator96-038-watchdogresumetimeout-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogResumeTimeout` | `0` | `120` | `vm-observed` |
| 38 | `operator96-038-watchdogresumetimeout-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogResumeTimeout` | `1` | `120` | `vm-observed` |
| 39 | `operator96-039-watchdogsleeptimeout-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogSleepTimeout` | `0` | `300` | `vm-observed` |
| 39 | `operator96-039-watchdogsleeptimeout-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogSleepTimeout` | `1` | `300` | `vm-observed` |
| 40 | `operator96-040-skiptickoverride-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SkipTickOverride` | `0` | `absent` | `vm-observed` |
| 40 | `operator96-040-skiptickoverride-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SkipTickOverride` | `1` | `absent` | `vm-observed` |
| 41 | `operator96-041-win32calloutwatchdogbugcheckenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\Win32CalloutWatchdogBugcheckEnabled` | `0` | `absent` | `vm-observed` |
| 41 | `operator96-041-win32calloutwatchdogbugcheckenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\Win32CalloutWatchdogBugcheckEnabled` | `1` | `absent` | `vm-observed` |
| 42 | `operator96-042-idlescaninterval-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\IdleScanInterval` | `0` | `absent` | `vm-observed` |
| 42 | `operator96-042-idlescaninterval-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\IdleScanInterval` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-038-watchdogresumetimeout-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-33.409` | `0.17` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-038-watchdogresumetimeout-0.json` |
| `operator96-038-watchdogresumetimeout-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.593` | `-7.59` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-038-watchdogresumetimeout-1.json` |
| `operator96-039-watchdogsleeptimeout-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-14.659` | `1.15` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-039-watchdogsleeptimeout-0.json` |
| `operator96-039-watchdogsleeptimeout-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `270.345` | `20.07` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-039-watchdogsleeptimeout-1.json` |
| `operator96-040-skiptickoverride-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `90.695` | `3.82` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-040-skiptickoverride-0.json` |
| `operator96-040-skiptickoverride-1` | `cpu_gain` | `medium` | `ok` | `ok` | `True` | `ok`/`0` | `11.27` | `1.2` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-040-skiptickoverride-1.json` |
| `operator96-041-win32calloutwatchdogbugcheckenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-71.6` | `-3.6` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-041-win32calloutwatchdogbugcheckenabled-0.json` |
| `operator96-041-win32calloutwatchdogbugcheckenabled-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `99.428` | `-5.28` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-041-win32calloutwatchdogbugcheckenabled-1.json` |
| `operator96-042-idlescaninterval-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-27.144` | `-5.61` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-042-idlescaninterval-0.json` |
| `operator96-042-idlescaninterval-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-18.692` | `-0.64` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-042-idlescaninterval-1.json` |
