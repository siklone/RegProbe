# Power Control Win32kCalloutWatchdogTimeoutSeconds KVM Reboot Follow-up

Date: 2026-04-12
Candidate: `power.control.win32k-callout-watchdog-timeout-seconds`
Guest: `regprobe-win11-25h2-session`

## Objective
- close the remaining reboot-backed gap for `Win32kCalloutWatchdogTimeoutSeconds`
- verify that the `Control\Power` parent path and missing-value baseline survive a real KVM reboot
- separate reboot uncertainty from the still-open runtime-read and override-semantics questions

## Result
- the host-driven KVM reboot lane completed end to end across a real guest reboot
- the guest boot time advanced from `2026-04-12T04:20:14.5000000Z` to `2026-04-12T04:33:01.5000000Z`
- `HKLM\SYSTEM\CurrentControlSet\Control\Power` still existed before and after reboot
- `Win32kCalloutWatchdogTimeoutSeconds` stayed absent before and after reboot
- `value_preserved = true`, so the candidate no longer has a reboot-diff gap
- the raw snapshot `error` field still contains a `GetValueKind` exception, but that reflects probing a missing value rather than a missing key because `key_exists = true` on both sides of the reboot

## Artifacts
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-reboot-kvm-20260412c/win32k-callout-watchdog-timeout-reboot-kvm-20260412c-before.json`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-reboot-kvm-20260412c/win32k-callout-watchdog-timeout-reboot-kvm-20260412c-after.json`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-reboot-kvm-20260412c/win32k-callout-watchdog-timeout-reboot-kvm-20260412c-summary.json`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-reboot-kvm-20260412c/win32k-callout-watchdog-timeout-reboot-kvm-20260412c-powercfg-a-before.txt`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-reboot-kvm-20260412c/win32k-callout-watchdog-timeout-reboot-kvm-20260412c-powercfg-a-after.txt`
- `evidence/files/vm-tooling-staging/win32k-callout-watchdog-timeout-reboot-kvm-20260412c/host-review.json`

## Short Take
- the reboot story is now closed for `Win32kCalloutWatchdogTimeoutSeconds`
- the candidate still lacks an exact runtime registry read and still lacks proof that a present value overrides the built-in 30-second default
- the next proof path should focus on runtime attribution or non-default semantics, not on reboot stability
