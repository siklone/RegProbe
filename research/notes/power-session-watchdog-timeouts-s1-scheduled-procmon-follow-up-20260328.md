# power.session-watchdog-timeouts S1 scheduled Procmon follow-up - 2026-03-28

## Summary

- A second S1-specific Procmon attempt moved the watchdog payload into a guest-side scheduled task so the host no longer depended on a long-lived `runProgramInGuest` session.
- The scheduled-task runner preserved shell health before and after the attempt, but VMware Tools still dropped from `running` to `installed` for an extended interval during the transition.
- No in-guest probe outputs survived the lane: no summary, no `PML`, no `CSV`, no filtered hits, and no before/after markers were copied back from the guest.
- A live postmortem after recovery then showed that `RegProbeWatchdogS1ScheduledProcmon` was not present in Task Scheduler, `powercfg /lastwake` still reported `Wake History Count - 0`, and there was still no fresh Kernel-Power sleep/resume output for the attempt.

## Source artifacts

- Scheduled-task summary: `evidence/files/vm-tooling-staging/watchdog-s1-scheduled-procmon-20260328-150559/summary.json`
- Scheduled-task live postmortem: `evidence/files/vm-tooling-staging/watchdog-s1-scheduled-procmon-20260328-150559/watchdog-s1-scheduled-procmon-live-postmortem.txt`
- Runner: `scripts/vm/run-session-watchdog-timeouts-s1-scheduled-procmon-probe.ps1`

## Result

- Shell health was clean before the attempt and recovered cleanly afterward.
- The host-side tools monitor again recorded a long `installed` interval instead of a normal quick return to `running`.
- The scheduled-task decoupling did not fix the lane:
  - the guest-side task did not leave behind a usable probe bundle
  - the task itself was not present when queried after recovery
  - wake history still stayed at zero
  - the postmortem still did not show a fresh sleep/resume Kernel-Power trail

## Why this matters

This was the best remaining VMware-safe variant of the S1 lane on the current baseline. The first S1 attempt could still be blamed on the guest-ops session dropping mid-run. This second attempt removes that excuse by moving the payload into a guest-side scheduled task.

That still did not produce usable artifacts or a confirmed OS-level sleep/resume signal. On the current `Win25H2Clean` VMware baseline, the watchdog lane now has two different S1-specific failures:

- direct guest-process payload
- scheduled-task payload

Both fail before yielding a decisive exact-value live read. That makes the current VMware S1 path a poor decisive lane for `WatchdogResumeTimeout` and `WatchdogSleepTimeout`, and it pushes the next real decision toward either a different suspend/resume control path or a different validation environment.
