# system.executive-additional-worker-threads Procmon boot log - 2026-03-28

## Summary

- The Executive worker-thread lane now has a real Procmon boot-log run on the clean `RegProbe` baseline.
- The lane completed a host-driven boot cycle, produced a real boot-log `PML`, and successfully converted it to `CSV`.
- The capture stayed shell-safe before and after reboot.
- Even with a full boot-log capture, the filtered lane still returned `MATCH_COUNT=0` for:
  - `AdditionalCriticalWorkerThreads`
  - `AdditionalDelayedWorkerThreads`
  - `UuidSequenceNumber`

## Source artifacts

- Procmon summary: `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-bootlog-20260328-172645/summary.json`
- Procmon arm summary: `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-bootlog-20260328-172645/summary-arm.json`
- Procmon collect summary: `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-bootlog-20260328-172645/summary-collect.json`
- Raw placeholder: `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-bootlog-20260328-172645/executive-worker-threads-procmon-bootlog.pml.md`

## Result

- `PML_EXISTS=True`
- `CSV_EXISTS=True`
- `MATCH_COUNT=0`
- Shell health stayed clean before and after the run.
- The boot cycle advanced successfully with a soft stop.

## Why this matters

This closes the remaining question about whether the Executive lane could produce a real exact-value Procmon boot-log run on the current `Win25H2Clean` baseline. It can.

That matters because the old gap is no longer "Procmon boot logging might be broken." The gap is now narrower and more useful: even a successful boot-log capture still does not show direct runtime reads for `AdditionalCriticalWorkerThreads` or `AdditionalDelayedWorkerThreads`.

That keeps the lane active as a draft candidate with stronger runtime evidence, but it still does not justify an app mapping or a shipped end-user tweak.
