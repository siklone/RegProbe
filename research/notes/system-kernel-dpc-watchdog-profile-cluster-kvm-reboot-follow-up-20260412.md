# system.kernel-dpc-watchdog-profile-cluster KVM reboot follow-up

Collected: 2026-04-12

## Scope

This follow-up used a write-free KVM guest script to observe the DPC watchdog profile registry cluster before and after a real host-driven reboot. The script did not apply or delete registry values; it only captured observed state.

Observed path:

`HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`

Observed values:

- `DpcWatchdogProfileBufferSizeBytes`
- `DpcWatchdogProfileCumulativeDpcThreshold`
- `DpcWatchdogProfileOffset`
- `DpcWatchdogProfileSingleDpcThreshold`

## Result

The reboot was observed via boot-time change:

- Before boot time: `2026-04-12T04:33:01.5000000Z`
- After boot time: `2026-04-12T14:17:36.5000000Z`

The key remained present, and all four value states were preserved exactly across reboot:

- `DpcWatchdogProfileOffset` remained `REG_DWORD 10000`.
- `DpcWatchdogProfileBufferSizeBytes` remained absent.
- `DpcWatchdogProfileCumulativeDpcThreshold` remained absent.
- `DpcWatchdogProfileSingleDpcThreshold` remained absent.

## Interpretation

This closes the record's reboot-diff observation layer for state preservation. It reinforces the current-build mixed state already seen in KD and INIT descriptor evidence: one profile offset value is present at `10000`, while the other profile values remain absent or zero-backed.

It does not close the exact runtime registry-read layer. The remaining gaps are still the boot/init routine that consumes the INIT descriptor rows, the exact inner `ExpQuerySystemInformation` query arm, and the repo-doc default conflict for the three non-zero documented profile defaults that are absent or zero in this guest.

## Artifacts

Bundle: `evidence/files/vm-tooling-staging/dpc-watchdog-profile-cluster-reboot-kvm-20260412a/`

- `dpc-watchdog-profile-cluster-reboot-kvm-20260412a-before.json`
- `dpc-watchdog-profile-cluster-reboot-kvm-20260412a-after.json`
- `dpc-watchdog-profile-cluster-reboot-kvm-20260412a-summary.json`
- `dpc-watchdog-profile-cluster-reboot-kvm-20260412a-system-events.txt`
- `host-review.json`
- `dpc-profile-reboot-post.png`
