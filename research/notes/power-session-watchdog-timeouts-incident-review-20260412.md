# power.session-watchdog-timeouts incident review - 2026-04-12

## Summary

Incident reviewed: the retained S1 runtime attempts for `power.session-watchdog-timeouts` are now classified as validation-environment failures, not as safety proof for the watchdog pair and not as evidence that the pair was read decisively at runtime.

The affected lanes were the direct S1 Procmon attempt, the scheduled-task S1 Procmon attempt, and the later tools-hardened lightweight ETW S1 attempt. All three were trying to cross the only sleep surface exposed by the current VMware baselines: `Standby (S1)`.

## Reviewed incident IDs

- `20260328082113-power-session-watchdog-timeouts`
- `20260328084313-power-session-watchdog-timeouts`
- `20260328084937-power-session-watchdog-timeouts`
- `20260328090153-power-session-watchdog-timeouts`
- `20260329031714-power-session-watchdog-timeouts`
- `20260329033857-power-session-watchdog-timeouts`

## Findings

The first direct S1 Procmon attempt caused VMware Tools to leave the normal `running` state while the host-side guest process returned `-1`. The visible shell later recovered and stayed healthy, but no usable in-guest Procmon payload survived: no `PML`, no `CSV`, no hit export, and no summary file. Post-checks also showed `Wake History Count - 0` and no fresh Kernel-Power sleep/resume trail.

The scheduled-task S1 Procmon attempt removed the long-lived guest-ops dependency by moving the payload into Task Scheduler. That still did not produce usable guest artifacts, the scheduled task was not present during live postmortem after recovery, wake history stayed at zero, and no fresh sleep/resume Kernel-Power trail appeared.

The tools-hardened lightweight ETW attempt reduced probe weight and split collection into start, trigger, and stop phases. The VM still exposed only S1, and the guest dropped out during the transition before an exact-value ETW bundle could be completed.

## Decision

The incidents are closed as reviewed environment limitations. They explain why the current VMware S1 lane cannot be used as decisive runtime evidence for `WatchdogResumeTimeout` and `WatchdogSleepTimeout`.

This does not clear the candidate for promotion. The remaining blocker is narrower and still real: either capture a decisive exact registry read for the watchdog values in a better suspend/resume environment, or identify the current-build watchdog-specific caller into the generic `PopReadRegKeyValue` / `PopOpenPowerKey` helper path.

## Source artifacts

- `research/notes/power-session-watchdog-timeouts-s1-procmon-follow-up-20260328.md`
- `research/notes/power-session-watchdog-timeouts-s1-scheduled-procmon-follow-up-20260328.md`
- `research/notes/power-session-watchdog-timeouts-lightweight-runtime-20260330.md`
- `evidence/files/vm-tooling-staging/watchdog-s1-procmon-20260328-144402/summary.json`
- `evidence/files/vm-tooling-staging/watchdog-s1-scheduled-procmon-20260328-150559/summary.json`
- `evidence/files/vm-tooling-staging/watchdog-lightweight-runtime-20260330-131636/summary.json`
