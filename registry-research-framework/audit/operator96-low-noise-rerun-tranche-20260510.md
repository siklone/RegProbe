# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-10T19:47:59Z`
- Status: **ok**
- Planned experiments: `8`
- Completed in this run: `8`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 1 | `operator96-001-enablelocallogonsid-0` | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableLocalLogonSid` | `0` | `absent` | `vm-observed` |
| 1 | `operator96-001-enablelocallogonsid-1` | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableLocalLogonSid` | `1` | `absent` | `vm-observed` |
| 2 | `operator96-002-enablevirtualization-0` | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableVirtualization` | `0` | `1` | `vm-observed` |
| 6 | `operator96-006-tickcountrolloverdelay-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\TickcountRolloverDelay` | `0` | `absent` | `vm-observed` |
| 6 | `operator96-006-tickcountrolloverdelay-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\TickcountRolloverDelay` | `1` | `absent` | `vm-observed` |
| 9 | `operator96-009-forceenablemutantautoboost-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\ForceEnableMutantAutoboost` | `1` | `absent` | `vm-observed` |
| 9 | `operator96-009-forceenablemutantautoboost-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\ForceEnableMutantAutoboost` | `0` | `absent` | `vm-observed` |
| 10 | `operator96-010-allowremotedasd-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\AllowRemoteDASD` | `1` | `0` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-001-enablelocallogonsid-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-81.014` | `-23.16` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-001-enablelocallogonsid-0.json` |
| `operator96-001-enablelocallogonsid-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-21.701` | `9.93` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-001-enablelocallogonsid-1.json` |
| `operator96-002-enablevirtualization-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-30.428` | `-21.91` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-002-enablevirtualization-0.json` |
| `operator96-006-tickcountrolloverdelay-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `83.936` | `-3.1` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-006-tickcountrolloverdelay-0.json` |
| `operator96-006-tickcountrolloverdelay-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-18.155` | `14.49` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-006-tickcountrolloverdelay-1.json` |
| `operator96-009-forceenablemutantautoboost-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-11.535` | `-3.78` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-009-forceenablemutantautoboost-1.json` |
| `operator96-009-forceenablemutantautoboost-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-32.108` | `3.46` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-009-forceenablemutantautoboost-0.json` |
| `operator96-010-allowremotedasd-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-3.57` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-010-allowremotedasd-1.json` |
