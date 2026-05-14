# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T09:13:25Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `7`
- Errors: `0`

## Verdict Counts

- `harmful`: `6`
- `low_confidence`: `1`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-048-mfbufferingthreshold-1` | `low_confidence` | `low` | `ok` | `444.851` | post_reboot io_write_mib_per_second changed by +444.85% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-048-mfbufferingthreshold-1.json` |
| `operator96-049-perfcalculateactualutilization-0` | `harmful` | `low` | `ok` | `-11.344` | apply io_write_mib_per_second changed by -11.34% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-049-perfcalculateactualutilization-0.json` |
| `operator96-050-sourcesettingsversion-0` | `harmful` | `high` | `ok` | `-7.223` | apply cpu_multi_iterations_per_second changed by -7.22% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-050-sourcesettingsversion-0.json` |
| `operator96-050-sourcesettingsversion-1` | `harmful` | `low` | `ok` | `-31.723` | post_reboot io_write_mib_per_second changed by -31.72% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-050-sourcesettingsversion-1.json` |
| `operator96-051-timerrebasethresholdondripsexit-0` | `harmful` | `low` | `ok` | `-15.179` | apply io_write_mib_per_second changed by -15.18% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-051-timerrebasethresholdondripsexit-0.json` |
| `operator96-051-timerrebasethresholdondripsexit-1` | `harmful` | `low` | `ok` | `-16.427` | post_reboot io_write_mib_per_second changed by -16.43% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-051-timerrebasethresholdondripsexit-1.json` |
| `operator96-052-hibernateenableddefault-0` | `harmful` | `low` | `ok` | `-17.296` | apply cpu_single_iterations_per_second changed by -17.30% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-09/operator96-052-hibernateenableddefault-0.json` |
