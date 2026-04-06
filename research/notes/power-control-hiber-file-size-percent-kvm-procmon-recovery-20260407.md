# Power Control HiberFileSizePercent KVM Procmon Recovery Follow-up

Date: 2026-04-07
Candidate: `power.control.hiber-file-size-percent`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the `HiberFileSizePercent` Procmon lane through the hardened KVM runner after explicitly dropping the elevated guest shell
- check whether a recovery-backed host run changes the earlier adjacent-only KVM result under `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- confirm that the new admin-shell recovery path can carry a real power-family replay end-to-end instead of only a smoke or a policy-family probe

## Result
- the hardened runner reopened an elevated guest PowerShell session, uploaded its ready marker, restaged the Procmon helper, and completed a full runtime replay without manual guest interaction
- the replay was not a clean `no-hit`: it surfaced `4` matched lines and `2` host-reviewed `Control\Power` path fragments across a `245402`-row CSV
- the matched activity stayed adjacent rather than decisive: `powercfg.exe` opened `HKLM\System\CurrentControlSet\Control\Power`, wrote `HibernateEnabled=0`, and closed the same key
- the replay still surfaced `0` direct `HiberFileSizePercent` path or value hits, so the lane remains blocked on `runtime_no_read`

## Artifacts
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-procmon-kvm-recovery-20260407a/hiberfilesizepercent-procmon-kvm-recovery-20260407a.txt`
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-procmon-kvm-recovery-20260407a/hiberfilesizepercent-procmon-kvm-recovery-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-procmon-kvm-recovery-20260407a/hiberfilesizepercent-procmon-kvm-recovery-20260407a.hits.csv`
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-procmon-kvm-recovery-20260407a/host-review.json`

## Short Take
- `HiberFileSizePercent` remains runtime-negative on KVM even after removing the manual elevated-shell assumption
- the recovery-backed replay reproduced the same adjacent-only `Control\Power` pattern with slightly less noise than the earlier run
- this pushes the blocker further toward genuine `runtime_no_read` rather than transport fragility
