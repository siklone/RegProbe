# Policy System EnableVirtualization KVM Procmon Runtime Follow-up

Date: 2026-04-06
Candidate: `policy.system.enable-virtualization`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the `EnableVirtualization` policy family in the Linux KVM guest with a dedicated UAC/policy surface burst
- check whether a live Procmon capture surfaces a direct path or value hit under `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System`
- reuse the KVM guest helper path instead of the older VMware-only ETW orchestration

## Result
- the KVM guest helper produced a real Procmon capture and uploaded both a text summary and a raw CSV
- the run stayed a clean `no-hit`: `MATCH_COUNT=0`, `HITSCSV_EXISTS=False`
- a host-side review of the uploaded CSV counted `300881` rows and still found `0` lines containing `Policies\System`, `EnableVirtualization`, `EnableLUA`, or `EnableInstallerDetection`
- this does not overturn the earlier static context package; it reinforces the runtime gate from a second transport family

## Artifacts
- `evidence/files/vm-tooling-staging/enablevirtualization-procmon-kvm-20260406b/enablevirtualization-procmon-kvm-20260406b.txt`
- `evidence/files/vm-tooling-staging/enablevirtualization-procmon-kvm-20260406b/enablevirtualization-procmon-kvm-20260406b-summary.json`
- `evidence/files/vm-tooling-staging/enablevirtualization-procmon-kvm-20260406b/host-review.json`

## Lane Notes
- this follow-up surfaced two helper issues while bringing the KVM runtime lane forward:
  - `run-registry-policy-probe.ps1` was passing empty `string[]` arguments in a way that PowerShell treated as a missing parameter value
  - the working guest did not yet have `registry-policy-probe.ps1` in `C:\Tools\Scripts`, so the helper was staged from the host bridge into the temporary bootstrap root for this replay
- after the helper fix, the KVM runtime lane completed cleanly enough to produce a full capture without manual artifact extraction inside the guest

## Short Take
- `EnableVirtualization` still has a stronger static story than runtime story
- the KVM Procmon replay agrees with the earlier primary and secondary ETW lanes: the intended live read remains unresolved
- keep the record review-only and continue to treat `runtime_no_read` as a real blocker rather than a transport artifact
