# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-10T23:20:45Z`
- Status: **ok**
- Planned experiments: `10`
- Completed in this run: `10`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 16 | `operator96-016-enableweruserreporting-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnableWerUserReporting` | `0` | `absent` | `vm-observed` |
| 16 | `operator96-016-enableweruserreporting-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnableWerUserReporting` | `1` | `absent` | `vm-observed` |
| 17 | `operator96-017-hyperstartdisabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\HyperStartDisabled` | `1` | `absent` | `vm-observed` |
| 17 | `operator96-017-hyperstartdisabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\HyperStartDisabled` | `0` | `absent` | `vm-observed` |
| 18 | `operator96-018-disablelightweightsuspend-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableLightWeightSuspend` | `0` | `absent` | `vm-observed` |
| 18 | `operator96-018-disablelightweightsuspend-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableLightWeightSuspend` | `1` | `absent` | `vm-observed` |
| 19 | `operator96-019-timercheckflags-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\TimerCheckFlags` | `0` | `absent` | `vm-observed` |
| 19 | `operator96-019-timercheckflags-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\TimerCheckFlags` | `1` | `absent` | `vm-observed` |
| 20 | `operator96-020-forceidlegraceperiod-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceIdleGracePeriod` | `0` | `absent` | `vm-observed` |
| 20 | `operator96-020-forceidlegraceperiod-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceIdleGracePeriod` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-016-enableweruserreporting-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-2.58` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-016-enableweruserreporting-0.json` |
| `operator96-016-enableweruserreporting-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-0.3` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-016-enableweruserreporting-1.json` |
| `operator96-017-hyperstartdisabled-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `180.912` | `-1.14` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-017-hyperstartdisabled-1.json` |
| `operator96-017-hyperstartdisabled-0` | `harmful` | `medium` | `ok` | `ok` | `True` | `ok`/`0` | `-9.146` | `-0.71` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-017-hyperstartdisabled-0.json` |
| `operator96-018-disablelightweightsuspend-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-16.608` | `1.31` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-018-disablelightweightsuspend-0.json` |
| `operator96-018-disablelightweightsuspend-1` | `harmful` | `medium` | `ok` | `ok` | `True` | `ok`/`0` | `-8.83` | `-3.51` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-018-disablelightweightsuspend-1.json` |
| `operator96-019-timercheckflags-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.787` | `12.32` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-019-timercheckflags-0.json` |
| `operator96-019-timercheckflags-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `133.524` | `8.65` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-019-timercheckflags-1.json` |
| `operator96-020-forceidlegraceperiod-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `136.539` | `3.12` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-020-forceidlegraceperiod-0.json` |
| `operator96-020-forceidlegraceperiod-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-17.04` | `14.0` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-020-forceidlegraceperiod-1.json` |
