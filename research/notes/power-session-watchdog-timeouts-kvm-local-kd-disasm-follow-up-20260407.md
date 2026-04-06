# power.session-watchdog-timeouts KVM local-KD disassembly follow-up - 2026-04-07

## Summary

- A Linux KVM local-KD disassembly follow-up attached to the live `regprobe-win11-25h2-session` guest and disassembled the current-build `PopUpdatePowerActionWatchdogTimeouts` and `PopComputeWatchdogTimeout` routines.
- `PopComputeWatchdogTimeout` now shows a direct current-build branch between the two tracked registry-backed globals:
  - it returns `PopWatchdogSleepTimeout` on the non-resume path
  - it returns `PopWatchdogResumeTimeout` on the resume path
- `PopUpdatePowerActionWatchdogTimeouts` shows the adjacent power-action watchdog globals being refreshed from either:
  - built-in diagnostic override constants `0x14a` and `0x96`, or
  - the default globals `PopPowerActionTransitioningWatchdogTimeoutDefault` and `PopPowerActionResumingWatchdogTimeoutDefault`

## Source artifacts

- `evidence/files/vm-tooling-staging/local-kd-watchdog-disasm-20260407a/local-kd-watchdog-disasm-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-watchdog-disasm-20260407a/local-kd-watchdog-disasm-20260407a.log`

## Why this matters

The earlier KVM local-KD value follow-up proved that the running guest still carries the live watchdog timeout globals and the derived directed-power millisecond globals. This disassembly layer adds the missing current-build code-path proof.

It now shows that the running kernel still:

- reads directly from `PopWatchdogSleepTimeout` and `PopWatchdogResumeTimeout` in `PopComputeWatchdogTimeout`
- keeps a neighboring watchdog-update routine that rewrites the power-action watchdog globals from the default or diagnostic paths

That is not yet a direct registry-read capture, but it is much stronger than relying on pseudocode or fallback artifacts alone.
