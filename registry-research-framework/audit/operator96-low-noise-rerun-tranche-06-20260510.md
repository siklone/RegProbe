# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T04:00:19Z`
- Status: **ok**
- Planned experiments: `8`
- Completed in this run: `8`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 32 | `operator96-032-interruptsteeringflags-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\InterruptSteeringFlags` | `1` | `absent` | `vm-observed` |
| 32 | `operator96-032-interruptsteeringflags-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\InterruptSteeringFlags` | `0` | `absent` | `vm-observed` |
| 33 | `operator96-033-alwaystrackioboosting-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\AlwaysTrackIoBoosting` | `0` | `absent` | `vm-observed` |
| 33 | `operator96-033-alwaystrackioboosting-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\AlwaysTrackIoBoosting` | `1` | `absent` | `vm-observed` |
| 35 | `operator96-035-maximumcooperativeidlesearchwidth-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaximumCooperativeIdleSearchWidth` | `0` | `absent` | `vm-observed` |
| 35 | `operator96-035-maximumcooperativeidlesearchwidth-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaximumCooperativeIdleSearchWidth` | `1` | `absent` | `vm-observed` |
| 36 | `operator96-036-hiberbootenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\HiberbootEnabled` | `0` | `1` | `vm-observed` |
| 37 | `operator96-037-powersettingprofile-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\PowerSettingProfile` | `1` | `0` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-032-interruptsteeringflags-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-8.008` | `-2.39` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-032-interruptsteeringflags-1.json` |
| `operator96-032-interruptsteeringflags-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-15.778` | `-1.05` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-032-interruptsteeringflags-0.json` |
| `operator96-033-alwaystrackioboosting-0` | `harmful` | `medium` | `ok` | `ok` | `True` | `ok`/`0` | `-8.402` | `-4.94` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-033-alwaystrackioboosting-0.json` |
| `operator96-033-alwaystrackioboosting-1` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-7.474` | `-0.04` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-033-alwaystrackioboosting-1.json` |
| `operator96-035-maximumcooperativeidlesearchwidth-0` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-8.016` | `7.15` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-035-maximumcooperativeidlesearchwidth-0.json` |
| `operator96-035-maximumcooperativeidlesearchwidth-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-20.44` | `-6.01` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-035-maximumcooperativeidlesearchwidth-1.json` |
| `operator96-036-hiberbootenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-30.536` | `-8.17` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-036-hiberbootenabled-0.json` |
| `operator96-037-powersettingprofile-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `113.242` | `7.43` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-037-powersettingprofile-1.json` |
