# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-13T13:46:14Z`
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
| `operator96-070-alwayscomputeqoshints-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `428.176` | `26.34` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-070-alwayscomputeqoshints-0.json` |
| `operator96-070-alwayscomputeqoshints-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.626` | `1.47` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-070-alwayscomputeqoshints-1.json` |
| `operator96-071-heteromulticoreclassesenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-21.213` | `7.19` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-071-heteromulticoreclassesenabled-0.json` |
| `operator96-071-heteromulticoreclassesenabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-33.363` | `2.67` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-071-heteromulticoreclassesenabled-1.json` |
| `operator96-072-heteromulticlassparkingenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-8.691` | `2.55` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-072-heteromulticlassparkingenabled-0.json` |
| `operator96-072-heteromulticlassparkingenabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-15.816` | `-4.5` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-072-heteromulticlassparkingenabled-1.json` |
| `operator96-073-disableidlestatesatboot-2` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-14.471` | `3.59` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-073-disableidlestatesatboot-2.json` |
| `operator96-073-disableidlestatesatboot-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `120.176` | `7.26` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-073-disableidlestatesatboot-0.json` |
| `operator96-074-perfboostatguaranteed-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-9.297` | `-4.57` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-074-perfboostatguaranteed-1.json` |
| `operator96-074-perfboostatguaranteed-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `92.784` | `-3.91` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-074-perfboostatguaranteed-0.json` |
