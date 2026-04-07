# power.session-win32-callout-watchdog-bugcheck-enabled KVM local-KD follow-up - 2026-04-07

## Summary

- A live KVM local-KD follow-up disassembled `nt!PopInvokeWin32CalloutWithWatchdog` and queried `nt!PopWin32CalloutWatchdogBugcheckEnabled` directly.
- The current-build symbol family is real:
  - `PopWin32CalloutWatchdogCallback`
  - `PopInvokeWin32Callout`
  - `PopInvokeWin32CalloutWithWatchdog`
  - `PopWin32CalloutWatchdogBugcheckEnabled`
- The live global `PopWin32CalloutWatchdogBugcheckEnabled` currently reads `0`.
- The direct disassembly did not show `PopInvokeWin32CalloutWithWatchdog` reading that global.
- Instead, the named callout watchdog path uses `PopWin32kCalloutWatchdogTimeoutSeconds`, builds a watchdog callback packet, calls `ZwPowerInformation`, and then invokes `PsInvokeWin32Callout`.

## Source artifacts

- `evidence/files/vm-tooling-staging/watchdog-win32-callout-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/watchdog-win32-callout-20260407a/watchdog-win32-callout-20260407a.log`
- `evidence/files/vm-tooling-staging/watchdog-win32-callout-20260407a/watchdog-win32-callout-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/watchdog-win32-callout-20260407a/host-review.json`

## Interpretation

- new proof gained:
  - direct current-build disassembly of the named watchdog-wrapped Win32 callout path
  - live current-build value read of `PopWin32CalloutWatchdogBugcheckEnabled = 0`
- narrowed conclusion:
  - `PopInvokeWin32CalloutWithWatchdog` is not the direct reader of the bugcheck-enabled global on this build
- remaining gap:
  - find the earlier watchdog initialization or policy-loading path that seeds `PopWin32CalloutWatchdogBugcheckEnabled`
