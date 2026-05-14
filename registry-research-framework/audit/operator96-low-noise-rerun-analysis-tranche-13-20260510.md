# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T17:48:50Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `10`
- Errors: `0`

## Verdict Counts

- `harmful`: `2`
- `noisy`: `8`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-070-alwayscomputeqoshints-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-070-alwayscomputeqoshints-0.json` |
| `operator96-070-alwayscomputeqoshints-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-070-alwayscomputeqoshints-1.json` |
| `operator96-071-heteromulticoreclassesenabled-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-071-heteromulticoreclassesenabled-0.json` |
| `operator96-071-heteromulticoreclassesenabled-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-071-heteromulticoreclassesenabled-1.json` |
| `operator96-072-heteromulticlassparkingenabled-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-072-heteromulticlassparkingenabled-0.json` |
| `operator96-072-heteromulticlassparkingenabled-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-072-heteromulticlassparkingenabled-1.json` |
| `operator96-073-disableidlestatesatboot-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-073-disableidlestatesatboot-0.json` |
| `operator96-073-disableidlestatesatboot-2` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-073-disableidlestatesatboot-2.json` |
| `operator96-074-perfboostatguaranteed-0` | `harmful` | `low` | `ok` | `-14.657` | apply io_write_mib_per_second changed by -14.66% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-074-perfboostatguaranteed-0.json` |
| `operator96-074-perfboostatguaranteed-1` | `harmful` | `low` | `ok` | `-45.677` | apply io_write_mib_per_second changed by -45.68% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-074-perfboostatguaranteed-1.json` |
