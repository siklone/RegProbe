# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-12T15:17:56Z`
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
| `operator96-016-enableweruserreporting-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-19.835` | `-2.86` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-016-enableweruserreporting-0.json` |
| `operator96-016-enableweruserreporting-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-21.609` | `-4.71` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-016-enableweruserreporting-1.json` |
| `operator96-017-hyperstartdisabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-12.437` | `-0.91` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-017-hyperstartdisabled-1.json` |
| `operator96-017-hyperstartdisabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-17.042` | `6.48` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-017-hyperstartdisabled-0.json` |
| `operator96-018-disablelightweightsuspend-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-19.506` | `0.24` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-018-disablelightweightsuspend-0.json` |
| `operator96-018-disablelightweightsuspend-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.694` | `-0.74` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-018-disablelightweightsuspend-1.json` |
| `operator96-019-timercheckflags-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-8.939` | `13.81` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-019-timercheckflags-0.json` |
| `operator96-019-timercheckflags-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.021` | `2.68` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-019-timercheckflags-1.json` |
| `operator96-020-forceidlegraceperiod-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-2.88` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-020-forceidlegraceperiod-0.json` |
| `operator96-020-forceidlegraceperiod-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-14.79` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-020-forceidlegraceperiod-1.json` |
