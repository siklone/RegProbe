# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T08:03:56Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `7`
- Errors: `0`

## Verdict Counts

- `harmful`: `5`
- `low_confidence`: `2`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-043-sleepstudydisabled-0` | `harmful` | `low` | `ok` | `-19.346` | post_reboot io_write_mib_per_second changed by -19.35% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-043-sleepstudydisabled-0.json` |
| `operator96-043-sleepstudydisabled-1` | `low_confidence` | `low` | `ok` | `138.561` | apply io_read_mib_per_second changed by +138.56% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-043-sleepstudydisabled-1.json` |
| `operator96-044-class1initialunparkcount-0` | `low_confidence` | `low` | `ok` | `134.351` | apply io_read_mib_per_second changed by +134.35% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-044-class1initialunparkcount-0.json` |
| `operator96-044-class1initialunparkcount-1` | `harmful` | `low` | `ok` | `-20.963` | post_reboot io_write_mib_per_second changed by -20.96% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-044-class1initialunparkcount-1.json` |
| `operator96-045-customizeduringsetup-0` | `harmful` | `low` | `ok` | `-22.410` | post_reboot io_write_mib_per_second changed by -22.41% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-045-customizeduringsetup-0.json` |
| `operator96-046-energyestimationenabled-0` | `harmful` | `low` | `ok` | `-22.716` | post_reboot io_write_mib_per_second changed by -22.72% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-046-energyestimationenabled-0.json` |
| `operator96-047-hiberfilesizepercent-1` | `harmful` | `low` | `ok` | `-20.801` | post_reboot io_write_mib_per_second changed by -20.80% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-08/operator96-047-hiberfilesizepercent-1.json` |
