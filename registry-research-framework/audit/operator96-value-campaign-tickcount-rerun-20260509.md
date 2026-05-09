# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-09T11:30:21Z`
- Status: **ok**
- Planned experiments: `2`
- Completed in this run: `2`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 6 | `operator96-006-tickcountrolloverdelay-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\TickcountRolloverDelay` | `0` | `absent` | `vm-observed` |
| 6 | `operator96-006-tickcountrolloverdelay-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\TickcountRolloverDelay` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Status | Hard smoke | Interactive | Post-reboot CPU single Δ% | Post-reboot CPU multi Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---:|---:|---:|---|
| `operator96-006-tickcountrolloverdelay-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `7.52` | `1.84` | `5.47` | `registry-research-framework/audit/registry-value-experiments/operator96-006-tickcountrolloverdelay-0.json` |
| `operator96-006-tickcountrolloverdelay-1` | `ok` | `True` | `ok`/`0` | `-14.33` | `-17.49` | `-75.12` | `registry-research-framework/audit/registry-value-experiments/operator96-006-tickcountrolloverdelay-1.json` |
