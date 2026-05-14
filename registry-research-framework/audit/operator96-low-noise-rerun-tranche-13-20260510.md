# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T15:23:00Z`
- Status: **ok**
- Planned experiments: `10`
- Completed in this run: `10`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 70 | `operator96-070-alwayscomputeqoshints-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AlwaysComputeQosHints` | `0` | `absent` | `vm-observed` |
| 70 | `operator96-070-alwayscomputeqoshints-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AlwaysComputeQosHints` | `1` | `absent` | `vm-observed` |
| 71 | `operator96-071-heteromulticoreclassesenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroMultiCoreClassesEnabled` | `0` | `absent` | `vm-observed` |
| 71 | `operator96-071-heteromulticoreclassesenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroMultiCoreClassesEnabled` | `1` | `absent` | `vm-observed` |
| 72 | `operator96-072-heteromulticlassparkingenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroMultiClassParkingEnabled` | `0` | `absent` | `vm-observed` |
| 72 | `operator96-072-heteromulticlassparkingenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroMultiClassParkingEnabled` | `1` | `absent` | `vm-observed` |
| 73 | `operator96-073-disableidlestatesatboot-2` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableIdleStatesAtBoot` | `2` | `absent` | `vm-observed` |
| 73 | `operator96-073-disableidlestatesatboot-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableIdleStatesAtBoot` | `0` | `absent` | `vm-observed` |
| 74 | `operator96-074-perfboostatguaranteed-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfBoostAtGuaranteed` | `1` | `absent` | `vm-observed` |
| 74 | `operator96-074-perfboostatguaranteed-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfBoostAtGuaranteed` | `0` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-070-alwayscomputeqoshints-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-34.94` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-070-alwayscomputeqoshints-0.json` |
| `operator96-070-alwayscomputeqoshints-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-26.58` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-070-alwayscomputeqoshints-1.json` |
| `operator96-071-heteromulticoreclassesenabled-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-46.24` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-071-heteromulticoreclassesenabled-0.json` |
| `operator96-071-heteromulticoreclassesenabled-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-12.52` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-071-heteromulticoreclassesenabled-1.json` |
| `operator96-072-heteromulticlassparkingenabled-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-25.24` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-072-heteromulticlassparkingenabled-0.json` |
| `operator96-072-heteromulticlassparkingenabled-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-2.85` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-072-heteromulticlassparkingenabled-1.json` |
| `operator96-073-disableidlestatesatboot-2` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-28.05` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-073-disableidlestatesatboot-2.json` |
| `operator96-073-disableidlestatesatboot-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-16.91` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-073-disableidlestatesatboot-0.json` |
| `operator96-074-perfboostatguaranteed-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-45.677` | `9.83` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-074-perfboostatguaranteed-1.json` |
| `operator96-074-perfboostatguaranteed-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-14.657` | `2.6` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-074-perfboostatguaranteed-0.json` |
