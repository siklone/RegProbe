# Registry Value Experiment Analysis

- Generated UTC: `2026-05-10T23:02:55Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `10`
- Errors: `0`

## Verdict Counts

- `harmful`: `3`
- `low_confidence`: `3`
- `noisy`: `4`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-011-disablediskcounters-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-011-disablediskcounters-0.json` |
| `operator96-011-disablediskcounters-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-011-disablediskcounters-1.json` |
| `operator96-012-ioallowloadcrashdumpdriver-0` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-012-ioallowloadcrashdumpdriver-0.json` |
| `operator96-012-ioallowloadcrashdumpdriver-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-012-ioallowloadcrashdumpdriver-1.json` |
| `operator96-013-ioenablesessionzeroaccesscheck-0` | `harmful` | `low` | `ok` | `-35.149` | post_reboot io_write_mib_per_second changed by -35.15% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-013-ioenablesessionzeroaccesscheck-0.json` |
| `operator96-013-ioenablesessionzeroaccesscheck-1` | `harmful` | `low` | `ok` | `-12.311` | post_reboot io_write_mib_per_second changed by -12.31% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-013-ioenablesessionzeroaccesscheck-1.json` |
| `operator96-014-globaltimerresolutionrequests-0` | `low_confidence` | `low` | `ok` | `493.950` | apply io_write_mib_per_second changed by +493.95% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-014-globaltimerresolutionrequests-0.json` |
| `operator96-014-globaltimerresolutionrequests-1` | `low_confidence` | `low` | `ok` | `81.657` | apply io_read_mib_per_second changed by +81.66% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-014-globaltimerresolutionrequests-1.json` |
| `operator96-015-forceparkingrequested-0` | `harmful` | `low` | `ok` | `-8.129` | post_reboot io_write_mib_per_second changed by -8.13% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-015-forceparkingrequested-0.json` |
| `operator96-015-forceparkingrequested-1` | `low_confidence` | `low` | `ok` | `28.732` | apply io_read_mib_per_second changed by +28.73% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-015-forceparkingrequested-1.json` |
