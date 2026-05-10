# Registry Value Experiment Analysis

- Generated UTC: `2026-05-10T21:00:16Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `8`
- Errors: `0`

## Verdict Counts

- `harmful`: `6`
- `low_confidence`: `1`
- `noisy`: `1`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-001-enablelocallogonsid-0` | `harmful` | `low` | `ok` | `-81.014` | post_reboot io_write_mib_per_second changed by -81.01% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-001-enablelocallogonsid-0.json` |
| `operator96-001-enablelocallogonsid-1` | `harmful` | `low` | `ok` | `-21.701` | post_reboot io_write_mib_per_second changed by -21.70% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-001-enablelocallogonsid-1.json` |
| `operator96-002-enablevirtualization-0` | `harmful` | `low` | `ok` | `-30.428` | post_reboot io_write_mib_per_second changed by -30.43% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-002-enablevirtualization-0.json` |
| `operator96-006-tickcountrolloverdelay-0` | `low_confidence` | `low` | `ok` | `83.936` | apply io_read_mib_per_second changed by +83.94% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-006-tickcountrolloverdelay-0.json` |
| `operator96-006-tickcountrolloverdelay-1` | `harmful` | `low` | `ok` | `-18.155` | post_reboot io_write_mib_per_second changed by -18.16% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-006-tickcountrolloverdelay-1.json` |
| `operator96-009-forceenablemutantautoboost-0` | `harmful` | `low` | `ok` | `-32.108` | post_reboot io_write_mib_per_second changed by -32.11% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-009-forceenablemutantautoboost-0.json` |
| `operator96-009-forceenablemutantautoboost-1` | `harmful` | `low` | `ok` | `-11.535` | post_reboot io_write_mib_per_second changed by -11.54% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-009-forceenablemutantautoboost-1.json` |
| `operator96-010-allowremotedasd-1` | `noisy` | `low` | `noisy` | `` | host preflight marked one or more stages noisy | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-010-allowremotedasd-1.json` |
