# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-13T00:07:20Z`
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
| `operator96-032-interruptsteeringflags-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-11.799` | `-11.8` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-032-interruptsteeringflags-1.json` |
| `operator96-032-interruptsteeringflags-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `80.096` | `1.27` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-032-interruptsteeringflags-0.json` |
| `operator96-033-alwaystrackioboosting-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-58.514` | `4.32` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-033-alwaystrackioboosting-0.json` |
| `operator96-033-alwaystrackioboosting-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-8.415` | `0.94` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-033-alwaystrackioboosting-1.json` |
| `operator96-035-maximumcooperativeidlesearchwidth-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-30.782` | `-3.15` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-035-maximumcooperativeidlesearchwidth-0.json` |
| `operator96-035-maximumcooperativeidlesearchwidth-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-39.269` | `-6.56` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-035-maximumcooperativeidlesearchwidth-1.json` |
| `operator96-036-hiberbootenabled-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `108.997` | `-0.75` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-036-hiberbootenabled-0.json` |
| `operator96-037-powersettingprofile-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.609` | `-3.38` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-037-powersettingprofile-1.json` |
