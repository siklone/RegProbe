# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-09T15:50:03Z`
- Status: **ok**
- Planned experiments: `1`
- Completed in this run: `1`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 22 | `operator96-022-maxdynamictickduration-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaxDynamicTickDuration` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Status | Hard smoke | Interactive | Post-reboot CPU single Δ% | Post-reboot CPU multi Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---:|---:|---:|---|
| `operator96-022-maxdynamictickduration-1` | `ok` | `True` | `ok`/`0` | `15.01` | `-31.64` | `331.06` | `registry-research-framework/audit/registry-value-experiments/operator96-022-maxdynamictickduration-1.json` |
