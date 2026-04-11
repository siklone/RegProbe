# Policy System EnableVirtualization KVM Procmon Bootlog Follow-up

Date: 2026-04-07
Candidate: `policy.system.enable-virtualization`
Guest: `regprobe-win11-25h2-session`

## Objective
- try the missing early-boot Procmon transport for the `EnableVirtualization` policy family on the live Linux KVM guest
- check whether a real reboot-backed Procmon bootlog run can finally surface the `Policies\System` read that ETW, runtime Procmon, and local-KD-adjacent inspection have not captured directly
- separate "we never tried boot logging on KVM" from "the current KVM guest does not actually accept Procmon boot logging"

## Result
- the new host-driven KVM runner completed a real reboot cycle; the guest boot time advanced from `2026-04-07T11:33:55.5000000Z` to `2026-04-07T11:39:11.5000000Z`
- Procmon was present, but both arm variants returned non-zero on the live guest:
  - direct `/EnableBootLogging`: `exit_code = 1`
  - minimized `/EnableBootLogging`: `exit_code = 1`
- Procmon state stayed unchanged across the arm phase and still pointed at the prior runtime capture logfile rather than a bootlog target
- the collect phase therefore skipped `/ConvertBootLog` with `reason = bootlog-enable-nonzero-exit`
- no bootlog `PML`, no `CSV`, and no filtered hits were produced

## Artifacts
- `evidence/files/vm-tooling-staging/enablevirtualization-procmon-kvm-bootlog-20260407h/summary-arm.json`
- `evidence/files/vm-tooling-staging/enablevirtualization-procmon-kvm-bootlog-20260407h/summary-collect.json`
- `evidence/files/vm-tooling-staging/enablevirtualization-procmon-kvm-bootlog-20260407h/summary.json`
- `evidence/files/vm-tooling-staging/enablevirtualization-procmon-kvm-bootlog-20260407h/host-review.json`

## Short Take
- this closes the remaining KVM bootlog tooling question for `EnableVirtualization`
- the lane is no longer missing a bootlog attempt; the current guest simply does not accept Procmon boot logging for this transport
- that does not weaken the path story from local-KD or the earlier runtime no-hit story; it narrows the blocker further toward genuine `runtime_no_read` plus a current KVM Procmon bootlog ceiling
