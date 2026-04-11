# Executive Additional Worker Threads KVM Procmon Hardened-Runner Follow-up

Date: 2026-04-07
Candidate: `system.executive-additional-worker-threads`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the Executive worker-thread pair through the hardened KVM runner that now ensures an elevated guest PowerShell session before staging the helper
- retest the same built-in `executive-worker-burst` trigger family under the healthier KVM transport
- check whether the earlier KVM no-hit was mostly operator-state noise rather than a real Procmon blind spot on the working guest

## Result
- the hardened runner ensured an elevated guest PowerShell session, uploaded its ready marker, restaged the Procmon helper, and completed a full runtime replay without manual guest interaction
- the replay still stayed a clean `no-hit`: `MATCH_COUNT=0`, `HITSCSV_EXISTS=False`
- the host-side review counted `247372` CSV rows and still found `0` lines containing:
  - `Session Manager\Executive`
  - `AdditionalCriticalWorkerThreads`
  - `AdditionalDelayedWorkerThreads`
  - `UuidSequenceNumber`
- the working guest kept `AdditionalCriticalWorkerThreads=0` before and after restore, so the hardened KVM transport remained active even though Procmon still did not surface the intended reads

## Artifacts
- `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-kvm-hardened-20260407a/executive-worker-threads-procmon-kvm-hardened-20260407a.txt`
- `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-kvm-hardened-20260407a/executive-worker-threads-procmon-kvm-hardened-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/executive-worker-threads-procmon-kvm-hardened-20260407a/host-review.json`

## Short Take
- the Executive worker-thread pair remains Procmon-negative on KVM even under the hardened runner
- this does not weaken the earlier exact lightweight ETW proof; it reduces the chance that the KVM no-hit was just a missing elevated-shell artifact
- for this lane, KVM Procmon is now better understood as a non-winning corroboration transport rather than the decisive promotion lane
