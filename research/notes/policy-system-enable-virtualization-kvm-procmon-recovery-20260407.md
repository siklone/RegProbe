# Policy System EnableVirtualization KVM Procmon Recovery Follow-up

Date: 2026-04-07
Candidate: `policy.system.enable-virtualization`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the `EnableVirtualization` Procmon lane through the hardened KVM runner after explicitly dropping the elevated guest shell
- check whether a recovery-backed host run changes the earlier clean no-hit result for the `Policies\System` value family
- confirm that the new admin-shell recovery path can carry a real policy-family replay end-to-end instead of only a synthetic smoke

## Result
- the hardened runner reopened an elevated guest PowerShell session, uploaded its ready marker, restaged the Procmon helper, and completed a full runtime replay without manual guest interaction
- the replay still stayed a clean `no-hit`: `MATCH_COUNT=0`, `HITSCSV_EXISTS=False`
- the host-side review counted `228130` CSV rows and still found `0` lines containing:
  - `Policies\System`
  - `EnableVirtualization`
  - `EnableLUA`
  - `EnableInstallerDetection`
- this does not weaken the earlier static or local-KD policy-family story; it reduces the chance that the earlier KVM no-hit depended on a manually preserved elevated shell

## Artifacts
- `evidence/files/vm-tooling-staging/enablevirtualization-procmon-kvm-recovery-20260407a/enablevirtualization-procmon-kvm-recovery-20260407a.txt`
- `evidence/files/vm-tooling-staging/enablevirtualization-procmon-kvm-recovery-20260407a/enablevirtualization-procmon-kvm-recovery-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/enablevirtualization-procmon-kvm-recovery-20260407a/host-review.json`

## Short Take
- `EnableVirtualization` remains runtime-negative on KVM even after removing the manual elevated-shell assumption
- the setting-level outcome stayed the same while the transport got healthier
- this pushes the blocker further toward genuine `runtime_no_read` rather than operator-state fragility
