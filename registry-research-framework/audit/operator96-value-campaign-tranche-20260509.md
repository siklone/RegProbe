# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-09T09:26:27Z`
- Status: **ok**
- Planned experiments: `5`
- Completed in this run: `5`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 1 | `operator96-001-enablelocallogonsid-0` | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableLocalLogonSid` | `0` | `absent` | `vm-observed` |
| 1 | `operator96-001-enablelocallogonsid-1` | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableLocalLogonSid` | `1` | `absent` | `vm-observed` |
| 2 | `operator96-002-enablevirtualization-0` | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableVirtualization` | `0` | `1` | `vm-observed` |
| 3 | `operator96-003-additionalcriticalworkerthreads-5` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\AdditionalCriticalWorkerThreads` | `5` | `0` | `vm-observed` |
| 3 | `operator96-003-additionalcriticalworkerthreads-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\AdditionalCriticalWorkerThreads` | `1` | `0` | `vm-observed` |

## Results

| Experiment | Status | Hard smoke | Interactive | Post-reboot CPU single Δ% | Post-reboot CPU multi Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---:|---:|---:|---|
| `operator96-001-enablelocallogonsid-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `2.77` | `25.97` | `-17.58` | `registry-research-framework/audit/registry-value-experiments/operator96-001-enablelocallogonsid-0.json` |
| `operator96-001-enablelocallogonsid-1` | `ok` | `True` | `ok`/`0` | `-11.97` | `-18.74` | `46.31` | `registry-research-framework/audit/registry-value-experiments/operator96-001-enablelocallogonsid-1.json` |
| `operator96-002-enablevirtualization-0` | `ok` | `True` | `ok`/`0` | `0.72` | `16.85` | `121.02` | `registry-research-framework/audit/registry-value-experiments/operator96-002-enablevirtualization-0.json` |
| `operator96-003-additionalcriticalworkerthreads-5` | `ok` | `True` | `ok`/`0` | `3.5` | `6.69` | `2.57` | `registry-research-framework/audit/registry-value-experiments/operator96-003-additionalcriticalworkerthreads-5.json` |
| `operator96-003-additionalcriticalworkerthreads-1` | `ok` | `True` | `ok`/`0` | `6.56` | `0.3` | `-40.0` | `registry-research-framework/audit/registry-value-experiments/operator96-003-additionalcriticalworkerthreads-1.json` |
