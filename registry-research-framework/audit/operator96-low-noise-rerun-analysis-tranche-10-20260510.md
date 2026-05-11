# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T10:17:20Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `7`
- Errors: `0`

## Verdict Counts

- `cpu_gain`: `1`
- `harmful`: `4`
- `low_confidence`: `2`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-053-eventprocessorenabled-0` | `harmful` | `low` | `ok` | `-15.306` | post_reboot io_write_read_mib_per_second changed by -15.31% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-053-eventprocessorenabled-0.json` |
| `operator96-054-lidreliabilitystate-0` | `harmful` | `high` | `ok` | `-7.427` | apply cpu_multi_iterations_per_second changed by -7.43% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-054-lidreliabilitystate-0.json` |
| `operator96-055-hibernateenabled-1` | `low_confidence` | `low` | `ok` | `101.660` | apply io_read_mib_per_second changed by +101.66% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-055-hibernateenabled-1.json` |
| `operator96-056-disableinboxpepgeneratedconstraints-0` | `harmful` | `low` | `ok` | `-33.999` | apply io_write_mib_per_second changed by -34.00% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-056-disableinboxpepgeneratedconstraints-0.json` |
| `operator96-056-disableinboxpepgeneratedconstraints-1` | `harmful` | `medium` | `ok` | `-7.250` | apply cpu_multi_iterations_per_second changed by -7.25% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-056-disableinboxpepgeneratedconstraints-1.json` |
| `operator96-057-disabledisplayburstonpowersourcechange-0` | `cpu_gain` | `medium` | `ok` | `8.392` | apply cpu_single_iterations_per_second changed by +8.39% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-057-disabledisplayburstonpowersourcechange-0.json` |
| `operator96-057-disabledisplayburstonpowersourcechange-1` | `low_confidence` | `low` | `ok` | `80.970` | apply io_read_mib_per_second changed by +80.97% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-10/operator96-057-disabledisplayburstonpowersourcechange-1.json` |
