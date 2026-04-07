# Power Control HiberFileSizePercent KVM Reboot Follow-up

Date: 2026-04-07
Candidate: `power.control.hiber-file-size-percent`
Guest: `regprobe-win11-25h2-session`

## Objective
- close the last early-boot style gap on `HiberFileSizePercent` by observing the same value before and after a real KVM reboot
- check whether the current KVM guest changes `HiberFileSizePercent` across boot even though `powercfg /a` reports that hibernation is unsupported
- harden the new host-managed reboot observation lane so that reboot-backed evidence no longer depends on an in-guest scheduled task succeeding on its own

## Result
- the new host-managed KVM reboot observation lane completed end-to-end after a real guest reboot instead of relying on the guest helper to self-restart
- the guest boot time advanced from `2026-04-07T10:01:16.5000000Z` to `2026-04-07T10:09:21.5000000Z`, so the reboot was actually observed
- `HiberFileSizePercent` stayed `REG_DWORD 0` before and after reboot; the raw snapshot summary reports `value_changed = true` only because the full JSON blob includes new timestamps and boot metadata, while `value_preserved = true` captures the actual registry value result
- `powercfg /a` was identical before and after reboot and continued to report that hibernation is unsupported, along with Fast Startup being unavailable because hibernation is unavailable
- this closes the remaining `reboot-diff` gap for the candidate and leaves the lane blocked only on `runtime_no_read`

## Artifacts
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-reboot-kvm-20260407b/hiberfilesizepercent-reboot-kvm-20260407b-before.json`
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-reboot-kvm-20260407b/hiberfilesizepercent-reboot-kvm-20260407b-after.json`
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-reboot-kvm-20260407b/hiberfilesizepercent-reboot-kvm-20260407b-summary.json`
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-reboot-kvm-20260407b/hiberfilesizepercent-reboot-kvm-20260407b-powercfg-a-before.txt`
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-reboot-kvm-20260407b/hiberfilesizepercent-reboot-kvm-20260407b-powercfg-a-after.txt`
- `evidence/files/vm-tooling-staging/hiberfilesizepercent-reboot-kvm-20260407b/host-review.json`

## Short Take
- the KVM lane now has a real reboot-backed observation for `HiberFileSizePercent`, not just static/local-KD/runtime-adjacent evidence
- the value stayed stable at `0` across the reboot and the guest continued to expose no hibernation support before or after boot
- this means the remaining gate is no longer reboot uncertainty; it is strictly the lack of a decisive exact runtime read
