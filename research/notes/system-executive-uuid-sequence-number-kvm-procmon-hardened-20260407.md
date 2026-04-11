# Executive UuidSequenceNumber KVM Procmon Hardened-Runner Follow-up

Date: 2026-04-07
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the `UuidSequenceNumber` Procmon lane through the hardened KVM runner that now ensures an elevated guest PowerShell session before staging the helper
- retest the stronger `executive-worker-burst` trigger family under the healthier KVM transport
- check whether the earlier KVM Executive-burst no-hit was mostly operator-state noise or a real runtime blind spot

## Result
- the hardened runner ensured an elevated guest PowerShell session, uploaded its ready marker, restaged the Procmon helper, and completed a full runtime replay without manual guest interaction
- the replay still stayed a clean `no-hit`: `MATCH_COUNT=0`, `HITSCSV_EXISTS=False`
- the host-side review counted `240500` CSV rows and still found `0` lines containing:
  - `Session Manager\Executive`
  - `UuidSequenceNumber`
  - `AdditionalCriticalWorkerThreads`
  - `AdditionalDelayedWorkerThreads`
- the working KVM guest reported a live `UuidSequenceNumber` value of `2928397` both before and after restore, so the runtime lane remained active even though Procmon still did not surface the intended read

## Artifacts
- `evidence/files/vm-tooling-staging/uuidsequence-procmon-kvm-hardened-20260407a/uuidsequence-procmon-kvm-hardened-20260407a.txt`
- `evidence/files/vm-tooling-staging/uuidsequence-procmon-kvm-hardened-20260407a/uuidsequence-procmon-kvm-hardened-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/uuidsequence-procmon-kvm-hardened-20260407a/host-review.json`

## Short Take
- `UuidSequenceNumber` remains runtime-negative on KVM even under the hardened runner and the alternate Executive burst
- this makes the KVM runtime gap look more like genuine `runtime_no_read` than a missing elevated-shell or staging problem
- the lane now has stronger current-build path and state proof than runtime proof
