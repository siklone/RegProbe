# power.disable-cpu-idle-states write diagnostics - 2026-03-28

## Summary

- A dedicated write-diagnostics lane was added to isolate the `set-candidate` failure in the CPU idle-state runtime probe.
- The runner attempted to separate raw registry write behavior from the heavier reboot and benchmark lanes.
- Four guest runs were executed on the clean visible-shell baseline:
  - three under the dedicated `C:\Tools\ValidationController\cpu-idle-write-diag` output root while the wrapper and guest-root handling were tightened
  - one under the already existing `C:\Tools\ValidationController\controller` root
- Both runs failed in the same way:
  - shell health was clean before the attempt
  - no guest result file was produced
  - no wrapper error file was produced
  - snapshot recovery brought the shell back cleanly

## Source artifacts

- Attempt 1 summary: `evidence/files/vm-tooling-staging/cpu-idle-write-diagnostics-20260328-191358/summary.json`
- Attempt 2 summary: `evidence/files/vm-tooling-staging/cpu-idle-write-diagnostics-20260328-193355/summary.json`
- Attempt 3 summary: `evidence/files/vm-tooling-staging/cpu-idle-write-diagnostics-20260328-194225/summary.json`
- Attempt 4 summary: `evidence/files/vm-tooling-staging/cpu-idle-write-diagnostics-20260328-194947/summary.json`
- Runner: `scripts/vm/run-cpu-idle-states-write-diagnostics.ps1`

## Result

- The diagnostics lane did not reach a write-level result matrix for:
  - `provider-no-wpr`
  - `regexe-no-wpr`
  - `provider-after-wpr`
  - `regexe-after-wpr`
- The failure is narrower than the full runtime and benchmark lanes:
  - this is no longer only "Explorer did not come back after reboot"
  - it is now also "guest execution did not yield a diagnostics result file even before a rebooted runtime result existed"

## Why this matters

This does not promote the bundle and it does not resolve the last `Class B` blocker.

It does make the blocker more honest. The current baseline now has:

- a failed v3.1 runtime lane during `set-candidate`
- a failed benchmark lane before workload start
- a dedicated write-diagnostics lane that still could not emit a guest-side result file or wrapper error, even when the output root changed

That points the remaining ambiguity away from "maybe we just need one more ordinary runtime rerun" and toward a narrower tooling or environment problem in the guest execution chain. The next meaningful escalation is likely a different guest execution primitive such as `runScriptInGuest`, an interactive in-guest command path, or a dedicated debug environment, not another identical reboot-and-retry pass.
