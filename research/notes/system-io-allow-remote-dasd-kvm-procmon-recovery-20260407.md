# System I/O AllowRemoteDASD KVM Procmon Recovery Follow-up

Date: 2026-04-07
Candidate: `system.io-allow-remote-dasd`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the `AllowRemoteDASD` Procmon lane through the hardened KVM runner after explicitly dropping the elevated guest shell
- check whether a recovery-backed host run changes the earlier clean no-hit result for either the intended Session Manager I/O path or the removable-storage collision path
- confirm that the new admin-shell recovery helper is sufficient for a real setting-level Procmon follow-up, not just a synthetic smoke

## Result
- the hardened runner reopened an elevated guest PowerShell session, uploaded its ready marker, restaged the Procmon helper, and completed a full runtime replay without manual guest interaction
- the replay still stayed a clean `no-hit`: `MATCH_COUNT=0`, `HITSCSV_EXISTS=False`
- the host-side review counted `235794` CSV rows and still found `0` lines containing:
  - `Session Manager\I/O System`
  - `AllowRemoteDASD`
  - `RemovableStorageDevices`
- this does not weaken the earlier static or local-KD collision story; it reduces the chance that the previous no-hit was caused by missing elevated-shell state on the KVM transport

## Artifacts
- `evidence/files/vm-tooling-staging/allowremotedasd-procmon-kvm-recovery-20260407a/allowremotedasd-procmon-kvm-recovery-20260407a.txt`
- `evidence/files/vm-tooling-staging/allowremotedasd-procmon-kvm-recovery-20260407a/allowremotedasd-procmon-kvm-recovery-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/allowremotedasd-procmon-kvm-recovery-20260407a/host-review.json`

## Short Take
- `AllowRemoteDASD` remains runtime-negative on KVM even after removing the manual elevated-shell assumption
- the setting-level outcome stayed the same while the transport got healthier
- this pushes the blocker further toward genuine `runtime_no_read` rather than operator-state fragility
