# power.session-watchdog-timeouts S1 Procmon follow-up - 2026-03-28

## Summary

- An S1-specific Procmon runner now exists for the watchdog lane.
- The guest payload tried `rundll32.exe powrprof.dll,SetSuspendState 0,1,0` on the current `Win25H2Clean` baseline.
- During that attempt, VMware Tools dropped from `running` to `installed`, and the host-side `runProgramInGuest` process came back with exit code `-1`.
- The guest shell recovered and stayed healthy, but the in-guest Procmon payload did not leave behind a usable `PML`, `CSV`, or summary export.
- Immediate post-checks then showed `Wake History Count - 0` and no fresh Kernel-Power sleep/resume entries for this run.

## Source artifacts

- S1 summary: `evidence/files/vm-tooling-staging/watchdog-s1-procmon-20260328-144402/summary.json`
- Post-check last wake: `evidence/files/vm-tooling-staging/watchdog-s1-procmon-20260328-144402/watchdog-s1-procmon-lastwake-postcheck.txt`
- Post-check Kernel-Power events: `evidence/files/vm-tooling-staging/watchdog-s1-procmon-20260328-144402/watchdog-s1-procmon-kernelpower-postcheck.txt`
- Runner: `scripts/vm/run-session-watchdog-timeouts-s1-procmon-probe.ps1`

## Result

- Shell health was clean before the attempt.
- The host-side tools-state monitor recorded a transition away from the normal `running` state during the lane.
- The VM came back to a healthy visible-shell state without needing a manual snapshot revert.
- No guest-side probe files survived the transition:
  - no `watchdog-s1-procmon.txt`
  - no `watchdog-s1-procmon.csv`
  - no `watchdog-s1-procmon.hits.csv`
  - no `watchdog-s1-procmon.pml`
- `powercfg /lastwake` after recovery reported:
  - `Wake History Count - 0`
- The post-check Kernel-Power export only showed older reboot-oriented entries and did not surface a fresh sleep/resume record for this attempt.

## Why this matters

This is the first direct attempt to force the watchdog lane through the only sleep state exposed by the current VMware baseline: `Standby (S1)`. The result did not produce an exact-value live read, but it did answer an important environment question.

On `Win25H2Clean` under the current VMware setup, an in-guest `S1` Procmon lane is not yet reliable enough to use as decisive evidence. The guest-ops channel destabilized during the transition, and the post-checks did not confirm a clean OS-level sleep/resume trail. That pushes the next decision point away from "try more generic probes" and toward either:

- a more VM-specific suspend/resume control path, or
- a different validation environment if the decisive trigger truly depends on sleep-state behavior.
