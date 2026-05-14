# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T05:02:40Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `8`
- Errors: `0`

## Verdict Counts

- `harmful`: `7`
- `low_confidence`: `1`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-032-interruptsteeringflags-0` | `harmful` | `low` | `ok` | `-15.778` | post_reboot io_write_mib_per_second changed by -15.78% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-032-interruptsteeringflags-0.json` |
| `operator96-032-interruptsteeringflags-1` | `harmful` | `low` | `ok` | `-8.008` | apply io_write_mib_per_second changed by -8.01% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-032-interruptsteeringflags-1.json` |
| `operator96-033-alwaystrackioboosting-0` | `harmful` | `medium` | `ok` | `-8.402` | apply cpu_multi_iterations_per_second changed by -8.40% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-033-alwaystrackioboosting-0.json` |
| `operator96-033-alwaystrackioboosting-1` | `harmful` | `high` | `ok` | `-7.474` | apply cpu_multi_iterations_per_second changed by -7.47% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-033-alwaystrackioboosting-1.json` |
| `operator96-035-maximumcooperativeidlesearchwidth-0` | `harmful` | `high` | `ok` | `-8.016` | apply cpu_multi_iterations_per_second changed by -8.02% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-035-maximumcooperativeidlesearchwidth-0.json` |
| `operator96-035-maximumcooperativeidlesearchwidth-1` | `harmful` | `low` | `ok` | `-20.440` | post_reboot io_write_mib_per_second changed by -20.44% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-035-maximumcooperativeidlesearchwidth-1.json` |
| `operator96-036-hiberbootenabled-0` | `harmful` | `low` | `ok` | `-30.536` | post_reboot io_write_mib_per_second changed by -30.54% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-036-hiberbootenabled-0.json` |
| `operator96-037-powersettingprofile-1` | `low_confidence` | `low` | `ok` | `113.242` | apply io_read_mib_per_second changed by +113.24% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-06/operator96-037-powersettingprofile-1.json` |
