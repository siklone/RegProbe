# power.session-win32-callout-watchdog-bugcheck-enabled KVM local-KD seed follow-up - 2026-04-07

## Summary

- A canonical KVM local-KD follow-up pivoted from the earlier named wrapper candidate to a broader watchdog/bugcheck symbol sweep and helper disassembly pass.
- The current build still resolves a live `nt!PopWin32CalloutWatchdogBugcheckEnabled` global together with `PopWin32CalloutWatchdogCallback`, `PopWin32kCalloutWatchdogTimeoutSeconds`, and `PopInvokeWin32CalloutWithWatchdog`.
- The live target global currently reads `0`.
- The wider watchdog/bugcheck symbol sweep did not reveal a target-specific setter, copy helper, or registry-seeding caller for the global.
- The proven current-build registry helper path in this neighborhood remains the generic timeout/control reader chain `PopReadUlongPowerKey -> PopReadRegKeyValue -> ZwOpenKey / ZwQueryValueKey`, and the directly observed timeout reader still uses `PopWatchdogSleepTimeout` and `PopWatchdogResumeTimeout`, not the bugcheck-enabled sibling.

## Source artifacts

- `evidence/files/vm-tooling-staging/watchdog-win32-callout-seed-kd-20260407d/summary.json`
- `evidence/files/vm-tooling-staging/watchdog-win32-callout-seed-kd-20260407d/watchdog-win32-callout-seed-kd-20260407d.log`
- `evidence/files/vm-tooling-staging/watchdog-win32-callout-seed-kd-20260407d/watchdog-win32-callout-seed-kd-20260407d.stdout.txt`
- `evidence/files/vm-tooling-staging/watchdog-win32-callout-seed-kd-20260407d/watchdog-win32-callout-seed-kd-20260407d.stderr.txt`
- `evidence/files/vm-tooling-staging/watchdog-win32-callout-seed-kd-20260407d/watchdog-win32-callout-seed-kd-20260407d.txt`
- `evidence/files/vm-tooling-staging/watchdog-win32-callout-seed-kd-20260407d/host-review.json`

## Interpretation

- new proof gained:
  - direct current-build watchdog/bugcheck symbol sweep centered on the target global
  - live current-build value read of `PopWin32CalloutWatchdogBugcheckEnabled = 0`
  - explicit negative result for nearby watchdog/bugcheck symbol pivots as target-specific seeding callers
- narrowed conclusion:
  - the candidate is now better described as an adjacent watchdog-family global with unproven registry semantics on the current build
- remaining gap:
  - either a boot-only init path outside this symbol cluster seeds the global, or the sibling is legacy/dead-path and should stop being escalated without a new runtime or documentation lead
