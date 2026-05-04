# power.session-win32-callout-watchdog-bugcheck-enabled triage - 2026-04-07

## Summary

- `Win32CalloutWatchdogBugcheckEnabled` is the strongest un-packaged watchdog sibling under `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`.
- Current baseline evidence says the parent key exists but the value is absent by default on the clean baseline.
- A later broad current-build string pass found an exact Unicode hit for `Win32CalloutWatchdogBugcheckEnabled` in `C:\Windows\System32\ntoskrnl.exe`.
- A later KVM local-KD symbol sweep also resolved a live `nt!PopWin32CalloutWatchdogBugcheckEnabled` global alongside `PopWatchdogResumeTimeout`, `PopWatchdogSleepTimeout`, `PopWatchdogInit`, and `PopInvokeWin32CalloutWithWatchdog`.
- A direct KVM local-KD follow-up then showed that `PopInvokeWin32CalloutWithWatchdog` does not read this global directly; it uses `PopWin32kCalloutWatchdogTimeoutSeconds` instead, while the live `PopWin32CalloutWatchdogBugcheckEnabled` global currently reads `0`.
- The later lightweight watchdog runtime package already carried this value as an adjacent sibling while probing `WatchdogResumeTimeout` and `WatchdogSleepTimeout`.

## Why this candidate wins the next watchdog branch

Compared with the `PowerWatchdog*` cluster under `Control\Power`, this value has a better evidence shape:

- it is under the same `Session Manager\Power` lane as the already-validated watchdog timeout pair
- it has a clean baseline `path exists / value missing` observation
- it has a current-build kernel string hit
- it has a current-build watchdog-family kernel global
- it now has a directly named watchdog-family caller candidate: `PopInvokeWin32CalloutWithWatchdog`
- that caller candidate has already been narrowed: it uses a timeout sibling, not the bugcheck-enabled global itself
- it already appears in the watchdog runtime packaging as an adjacent value

That is enough to open a schema-backed draft record without over-claiming runtime semantics.

## Current evidence chain

1. `kernel-power-net-new-existence-probe-20260328.md`
   - lists `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\Win32CalloutWatchdogBugcheckEnabled` under values whose parent path exists but the value is absent by default
2. `targeted-string-batch-primary-20260331-135356/results.json`
   - records a checked-in-build `ntoskrnl.exe` Unicode hit for `Win32CalloutWatchdogBugcheckEnabled`
3. `local-kd-watchdog-reg-helpers-20260407abc/watchdog-symbol-sweep-20260407a.log`
   - resolves `nt!PopWin32CalloutWatchdogBugcheckEnabled` on the live checked-in build together with the known watchdog symbol family, including `PopInvokeWin32CalloutWithWatchdog`
4. `watchdog-win32-callout-20260407a/watchdog-win32-callout-20260407a.log`
   - shows `PopInvokeWin32CalloutWithWatchdog` using `PopWin32kCalloutWatchdogTimeoutSeconds` and `PopWin32CalloutWatchdogCallback`, while `dd nt!PopWin32CalloutWatchdogBugcheckEnabled L1` returns `0`
5. `watchdog-lightweight-runtime-20260330-131636/power-session-watchdog-timeouts/summary.json`
   - carries `Win32CalloutWatchdogBugcheckEnabled` as an adjacent sibling in the watchdog runtime package

## What is still missing

- no seeding caller chain yet
- no exact live read
- no Microsoft documentation or policy contract
- no confirmed non-default value semantics

## Next proof path

Use the existing KVM local-KD + PDB + Ghidra lane, not a blind write:

1. pivot away from `PopInvokeWin32CalloutWithWatchdog` and toward the earlier watchdog initialization path that seeds `PopWin32CalloutWatchdogBugcheckEnabled`
2. connect that path to the generic helper surface (`PopOpenPowerKey`, `PopReadRegKeyValue`) if the registry read still funnels through the same helper family
3. confirm whether `Win32CalloutWatchdogBugcheckEnabled` is read in the same initialization/update path as the timeout pair
4. only then consider a controlled missing-vs-present runtime experiment
