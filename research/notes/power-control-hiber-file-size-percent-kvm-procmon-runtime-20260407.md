# Power Control HiberFileSizePercent KVM Procmon Runtime Follow-up

Date: 2026-04-07
Candidate: `power.control.hiber-file-size-percent`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the `HiberFileSizePercent` power lane through the Linux KVM Procmon transport with a dedicated hibernate toggle burst
- check whether a live capture surfaces a direct path or value hit under `HKLM\SYSTEM\CurrentControlSet\Control\Power`
- verify that the newer host-driven KVM bridge can stage the guest helper and collect artifacts without a manual guest copy step

## Result
- the KVM guest helper produced a real Procmon capture and uploaded a text summary plus raw CSV-backed host review
- the run was not a clean `no-hit`: it surfaced `8` matched lines and `6` host-reviewed `Control\Power` path fragments across a `267382`-row CSV
- the matched activity stayed adjacent rather than decisive: `powercfg.exe` opened `HKLM\System\CurrentControlSet\Control\Power`, wrote `HibernateEnabled=0`, and the `System` process queried nearby `PowerButtonBugcheck`, `OneSettingPowerButtonBugcheck`, `PowerButtonLiveDump`, and `OneSettingPowerButtonLiveDump`
- the replay still surfaced `0` `HiberFileSizePercent` path or value hits, so the lane remains blocked on `runtime_no_read`

## Artifacts
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-procmon-kvm-20260407c/hiberfilesizepercent-procmon-kvm-20260407c.txt`
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-procmon-kvm-20260407c/hiberfilesizepercent-procmon-kvm-20260407c-summary.json`
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-procmon-kvm-20260407c/host-review.json`

## Lane Notes
- this follow-up was the first runtime proof that the hardened KVM guest bridge can auto-start from the host runners instead of relying on a manually prepared bridge process
- the guest replay also confirmed that once an elevated PowerShell is present, the host-driven Procmon helper can stage, execute, and upload artifacts end-to-end on the KVM guest

## Short Take
- KVM runtime replay now agrees with the local-KD path story: the lane really does touch `Control\Power` on the current build
- the transport is healthier because it captured real live power-key activity instead of another empty `0`-hit run
- it still does not show a direct `HiberFileSizePercent` read, so the record stays review-only under `runtime_no_read`
