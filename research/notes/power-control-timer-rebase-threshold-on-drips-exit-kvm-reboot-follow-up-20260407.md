# Power Control TimerRebaseThresholdOnDripsExit KVM Reboot Follow-up

Date: 2026-04-07
Candidate: `power.control.timer-rebase-threshold-on-drips-exit`
Guest: `regprobe-win11-25h2-session`

## Objective
- verify whether `TimerRebaseThresholdOnDripsExit` stays stable across a real KVM reboot on the current Linux-hosted baseline
- check whether the guest still lacks `Standby (S0 Low Power Idle)` before and after reboot, which would keep the DRIPS-exit trigger environment-limited
- replace the older VMware-only standby limitation wording with a current virtualized baseline statement backed by live KVM evidence

## Result
- the host-driven KVM reboot observation completed end-to-end and confirmed a real boot-time advance from `2026-04-07T10:30:32.5000000Z` to `2026-04-07T10:44:42.5000000Z`
- `TimerRebaseThresholdOnDripsExit` stayed `REG_DWORD 60` before and after reboot; the raw summary reports `value_changed = true` only because the full JSON blob carries new timestamps and boot metadata, while `value_preserved = true` captures the actual registry value result
- `powercfg /a` was identical before and after reboot and continued to report that `Standby (S0 Low Power Idle)` is unavailable, along with hibernation and Fast Startup being unavailable
- this does not create a DRIPS exit trigger, but it does show that the environment limitation is not confined to the older VMware lane; the current KVM guest lacks the same Modern Standby capability

## Artifacts
- `evidence/files/vm-tooling-staging/timerrebasethresholdondripsexit-reboot-kvm-20260407a/timerrebasethresholdondripsexit-reboot-kvm-20260407a-before.json`
- `evidence/files/vm-tooling-staging/timerrebasethresholdondripsexit-reboot-kvm-20260407a/timerrebasethresholdondripsexit-reboot-kvm-20260407a-after.json`
- `evidence/files/vm-tooling-staging/timerrebasethresholdondripsexit-reboot-kvm-20260407a/timerrebasethresholdondripsexit-reboot-kvm-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/timerrebasethresholdondripsexit-reboot-kvm-20260407a/timerrebasethresholdondripsexit-reboot-kvm-20260407a-powercfg-a-before.txt`
- `evidence/files/vm-tooling-staging/timerrebasethresholdondripsexit-reboot-kvm-20260407a/timerrebasethresholdondripsexit-reboot-kvm-20260407a-powercfg-a-after.txt`
- `evidence/files/vm-tooling-staging/timerrebasethresholdondripsexit-reboot-kvm-20260407a/host-review.json`

## Short Take
- `TimerRebaseThresholdOnDripsExit` persists at `60` across a real KVM reboot
- the same guest still reports `Standby (S0 Low Power Idle)` as unavailable before and after reboot
- the remaining gate is therefore a genuine virtualized-baseline Modern Standby limitation, not just stale VMware wording
