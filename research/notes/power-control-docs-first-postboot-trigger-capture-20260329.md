# Power-Control Docs-First Post-Boot Trigger Capture (2026-03-29)

- Snapshot: `RegProbe-Baseline-Clean-20260329`
- Capture lane: guest-processed live Procmon with post-boot trigger profiles
- Trigger families:
  - `processor-load`: `powercfg`, `powercfg /energy`, CPU stress jobs, `winsat cpuformal`
  - `hibernate-query`: `powercfg`, `powercfg /energy`, wake/requests queries
  - `media-perf`: `powercfg`, `powercfg /energy`, CPU/media-style `winsat`
  - `drips-timer`: `powercfg`, `powercfg /energy`, requests/waketimers/lastwake
- Shell health: clean before and after every candidate run
- Probe root: `evidence/files/vm-tooling-staging/power-control-docs-first-postboot-trigger-20260329-161427`

## Exact Runtime Read Result

- Exact-hit candidates: `0/5`
- The follow-up lane produced guest-processed summaries, filtered hit CSVs, powercfg logs, and per-run job logs for all five remaining records.
- Path-level traffic was captured repeatedly under `HKLM\System\CurrentControlSet\Control\Power\User\PowerSchemes`, led by `svchost.exe` reads for `ActivePowerScheme` and `FriendlyName`.
- None of the five remaining docs-first records produced an exact `RegQueryValue` hit for the target value name.

## Remaining No-Exact-Read Records

- `Class1InitialUnparkCount`
- `HibernateEnabledDefault`
- `MfBufferingThreshold`
- `PerfCalculateActualUtilization`
- `TimerRebaseThresholdOnDripsExit`

## Interpretation

- The clean-baseline boot-log lane and this post-boot trigger lane now agree: these five values still lack an exact runtime read on the current VMware baseline.
- This keeps them below `Class A`.
- The post-boot lane is still useful because it removed an obvious blind spot: targeted `powercfg`/energy/stress triggers were exercised and processed inside the guest, with only summaries and filtered hits copied back.

## Key Artifacts

- Batch summary: `evidence/files/vm-tooling-staging/power-control-docs-first-postboot-trigger-20260329-161427/summary.json`
- Batch results: `evidence/files/vm-tooling-staging/power-control-docs-first-postboot-trigger-20260329-161427/results.json`
- Example filtered hit CSV: `evidence/files/vm-tooling-staging/power-control-docs-first-postboot-trigger-20260329-161427/power-control-perf-calculate-actual-utilization/power-control-perf-calculate-actual-utilization-postboot-trigger.hits.csv`
