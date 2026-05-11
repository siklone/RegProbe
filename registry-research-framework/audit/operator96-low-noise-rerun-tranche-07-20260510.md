# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T05:11:02Z`
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
| `operator96-038-watchdogresumetimeout-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `97.747` | `-0.9` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-038-watchdogresumetimeout-0.json` |
| `operator96-038-watchdogresumetimeout-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `96.42` | `1.53` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-038-watchdogresumetimeout-1.json` |
| `operator96-039-watchdogsleeptimeout-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-8.054` | `-4.46` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-039-watchdogsleeptimeout-0.json` |
| `operator96-039-watchdogsleeptimeout-1` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-7.719` | `7.48` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-039-watchdogsleeptimeout-1.json` |
| `operator96-040-skiptickoverride-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.452` | `11.95` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-040-skiptickoverride-0.json` |
| `operator96-040-skiptickoverride-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-14.131` | `-5.98` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-040-skiptickoverride-1.json` |
| `operator96-041-win32calloutwatchdogbugcheckenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-50.307` | `-16.08` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-041-win32calloutwatchdogbugcheckenabled-0.json` |
| `operator96-041-win32calloutwatchdogbugcheckenabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-78.213` | `-16.18` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-041-win32calloutwatchdogbugcheckenabled-1.json` |
| `operator96-042-idlescaninterval-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-27.841` | `-3.95` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-042-idlescaninterval-0.json` |
| `operator96-042-idlescaninterval-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-23.205` | `0.47` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-07/operator96-042-idlescaninterval-1.json` |
