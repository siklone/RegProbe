# Executive UuidSequenceNumber KVM Executive-Burst Follow-up

Date: 2026-04-06
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- retry the older thread-burst idea on the live KVM guest with the new host-driven Procmon runner
- reuse the same `executive-worker-burst` profile that exercises service, CIM, event-log, and file pressure around the Session Manager Executive path
- check whether `UuidSequenceNumber` appears under that alternate burst even if the older UUID / RPC / COM lane stayed a clean no-hit

## Result
- the host-driven KVM runner completed a real Procmon capture and exported a CSV with `273477` rows
- the filtered probe summary still reported `MATCH_COUNT=0` and `HITSCSV_EXISTS=False`
- a host-side keyword review over the exported CSV found `0` rows mentioning:
  - `Session Manager\Executive`
  - `UuidSequenceNumber`
  - `AdditionalCriticalWorkerThreads`
  - `AdditionalDelayedWorkerThreads`
- the working KVM guest reported a live `UuidSequenceNumber` value of `2928393` before and after restore, so this remains runtime evidence on the working guest rather than a replacement for the earlier clean-baseline exports
- unlike the older VMware thread-burst attempt, this alternate burst completed cleanly on KVM; it still did not beat the canonical no-hit result

## Artifacts
- `evidence/files/vm-tooling-staging/uuidsequence-procmon-kvm-executive-burst-20260406b/uuidsequence-procmon-kvm-executive-burst-20260406b.txt`
- `evidence/files/vm-tooling-staging/uuidsequence-procmon-kvm-executive-burst-20260406b/uuidsequence-procmon-kvm-executive-burst-20260406b-summary.json`
- `evidence/files/vm-tooling-staging/uuidsequence-procmon-kvm-executive-burst-20260406b/host-review.json`

## Short Take
- the KVM host-driven runner can now execute the alternate Executive-burst UUID lane end to end
- even under that burst, `UuidSequenceNumber` still stays a clean Procmon no-hit on the current KVM guest
- this improves transport confidence, but it does not raise the lane above its current Class B gate
