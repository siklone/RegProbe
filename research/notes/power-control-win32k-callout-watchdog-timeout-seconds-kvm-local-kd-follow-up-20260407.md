# power.control.win32k-callout-watchdog-timeout-seconds KVM local-KD follow-up - 2026-04-07

## Summary

- A live KVM local-KD follow-up queried `nt!PopWin32kCalloutWatchdogTimeoutSeconds` directly and disassembled `nt!PopInvokeWin32CalloutWithWatchdog`.
- The checked-in build keeps the live global at `0x1e` (`30`) on the observed clean baseline.
- The same checked-in-build wrapper directly multiplies the global by `1000` before issuing the watchdog packet through `ZwPowerInformation`, then returns to `PsInvokeWin32Callout`.
- This makes `Win32kCalloutWatchdogTimeoutSeconds` a stronger watchdog-family lead than the bugcheck-enabled sibling: the live caller path is already real and direct.

## Source artifacts

- `evidence/files/vm-tooling-staging/watchdog-win32k-callout-timeout-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/watchdog-win32k-callout-timeout-kd-20260407a/watchdog-win32k-callout-timeout-kd-20260407a.log`
- `evidence/files/vm-tooling-staging/watchdog-win32k-callout-timeout-kd-20260407a/watchdog-win32k-callout-timeout-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/watchdog-win32k-callout-timeout-kd-20260407a/watchdog-win32k-callout-timeout-kd-20260407a.stderr.txt`
- `evidence/files/vm-tooling-staging/watchdog-win32k-callout-timeout-kd-20260407a/watchdog-win32k-callout-timeout-kd-20260407a.txt`
- `evidence/files/vm-tooling-staging/watchdog-win32k-callout-timeout-kd-20260407a/host-review.json`

## Interpretation

- new proof gained:
  - live checked-in-build value read of `PopWin32kCalloutWatchdogTimeoutSeconds = 30`
  - direct checked-in-build wrapper usage in `PopInvokeWin32CalloutWithWatchdog`
- narrowed conclusion:
  - the missing `HKLM\SYSTEM\CurrentControlSet\Control\Power\Win32kCalloutWatchdogTimeoutSeconds` registry value likely falls back to a built-in checked-in-build default of `30` seconds
- remaining gap:
  - prove whether the registry value seeds this global through a dedicated power helper path or whether the value is only an optional override for the built-in default
