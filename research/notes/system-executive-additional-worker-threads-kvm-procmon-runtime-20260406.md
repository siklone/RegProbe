# Executive Additional Worker Threads KVM Procmon Runtime Follow-up

Date: 2026-04-06
Candidate: `system.executive-additional-worker-threads`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the Executive worker-thread pair on the live KVM guest with the new host-driven Procmon runner
- reuse the older concurrent service, event-log, CIM, and file-burst idea as a built-in `executive-worker-burst` trigger profile
- check whether KVM Procmon can surface `AdditionalCriticalWorkerThreads`, `AdditionalDelayedWorkerThreads`, or adjacent `UuidSequenceNumber` traffic under that burst

## Result
- the host-driven KVM runner staged the guest scripts through the bridge and completed a real Procmon capture without manual guest typing
- the replay exported a CSV with `297774` rows
- the filtered probe summary still reported `MATCH_COUNT=0` and `HITSCSV_EXISTS=False`
- a host-side keyword review over the exported CSV also found `0` rows mentioning:
  - `Session Manager\Executive`
  - `AdditionalCriticalWorkerThreads`
  - `AdditionalDelayedWorkerThreads`
  - `UuidSequenceNumber`
- this does not weaken the earlier exact-hit lightweight ETW lane; it only shows that the current KVM Procmon transport still misses this Executive lane on the working guest

## Artifacts
- `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-kvm-20260406e/executive-worker-threads-procmon-kvm-20260406e.txt`
- `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-kvm-20260406e/executive-worker-threads-procmon-kvm-20260406e-summary.json`
- `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-kvm-20260406e/host-review.json`

## Short Take
- the new host-driven KVM runner is strong enough to reproduce a real Executive Procmon replay end to end
- on the current KVM guest, that Procmon lane still stays a clean no-hit for the Executive worker-thread pair
- the canonical promotion proof for this candidate remains the earlier exact-hit lightweight ETW package
