# Registry Value Experiment Analysis

- Generated UTC: `2026-05-11T23:14:44Z`
- Input: `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16`
- Pattern: `operator96-*.json`
- Artifacts analyzed: `10`
- Errors: `0`

## Verdict Counts

- `app_breakage`: `1`
- `cpu_gain`: `1`
- `harmful`: `3`
- `low_confidence`: `4`
- `rollback_failure`: `1`

## Results

| Experiment | Verdict | Confidence | Host noise | Δ% | Reason | Artifact |
|---|---|---|---|---:|---|---|
| `operator96-085-heterohgseeperfhintsindependentenabled-0` | `harmful` | `low` | `ok` | `-22.138` | apply io_write_mib_per_second changed by -22.14% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-085-heterohgseeperfhintsindependentenabled-0.json` |
| `operator96-085-heterohgseeperfhintsindependentenabled-1` | `low_confidence` | `low` | `ok` | `141.774` | apply io_read_mib_per_second changed by +141.77% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-085-heterohgseeperfhintsindependentenabled-1.json` |
| `operator96-086-heterohgsplusdisabled-0` | `low_confidence` | `low` | `ok` | `106.839` | apply io_read_mib_per_second changed by +106.84% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-086-heterohgsplusdisabled-0.json` |
| `operator96-086-heterohgsplusdisabled-1` | `harmful` | `low` | `ok` | `-10.456` | post_reboot io_write_mib_per_second changed by -10.46% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-086-heterohgsplusdisabled-1.json` |
| `operator96-087-ipilastclockownerdisable-0` | `low_confidence` | `low` | `ok` | `100.178` | apply io_read_mib_per_second changed by +100.18% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-087-ipilastclockownerdisable-0.json` |
| `operator96-087-ipilastclockownerdisable-1` | `harmful` | `low` | `ok` | `-9.437` | post_reboot io_read_mib_per_second changed by -9.44% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-087-ipilastclockownerdisable-1.json` |
| `operator96-088-powerwatchdogrequestqueuetimeoutmsec-0` | `cpu_gain` | `medium` | `ok` | `9.228` | post_reboot cpu_multi_iterations_per_second changed by +9.23% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-088-powerwatchdogrequestqueuetimeoutmsec-0.json` |
| `operator96-088-powerwatchdogrequestqueuetimeoutmsec-1` | `low_confidence` | `low` | `ok` | `527.388` | apply io_write_mib_per_second changed by +527.39% | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-088-powerwatchdogrequestqueuetimeoutmsec-1.json` |
| `operator96-089-powerwatchdogpocallouttimeoutmsec-0` | `rollback_failure` | `high` | `ok` | `` | post-rollback-stage-failed | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-089-powerwatchdogpocallouttimeoutmsec-0.json` |
| `operator96-089-powerwatchdogpocallouttimeoutmsec-1` | `app_breakage` | `medium` | `noisy` | `` | post_rollback interactive smoke failures increased from 0 to 1 | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-089-powerwatchdogpocallouttimeoutmsec-1.json` |
