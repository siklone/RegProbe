# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-12T11:07:44Z`
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
| `operator96-001-enablelocallogonsid-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `14.48` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-001-enablelocallogonsid-0.json` |
| `operator96-001-enablelocallogonsid-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `840.52` | `30.23` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-001-enablelocallogonsid-1.json` |
| `operator96-002-enablevirtualization-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `104.124` | `14.33` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-002-enablevirtualization-0.json` |
| `operator96-006-tickcountrolloverdelay-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-13.65` | `2.02` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-006-tickcountrolloverdelay-0.json` |
| `operator96-006-tickcountrolloverdelay-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-31.872` | `-22.1` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-006-tickcountrolloverdelay-1.json` |
| `operator96-009-forceenablemutantautoboost-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `92.613` | `-1.35` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-009-forceenablemutantautoboost-1.json` |
| `operator96-009-forceenablemutantautoboost-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-21.83` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-009-forceenablemutantautoboost-0.json` |
| `operator96-010-allowremotedasd-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `53.95` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-010-allowremotedasd-1.json` |
