# power.session-watchdog-timeouts decision gate review - 2026-04-12

## Decision

Keep `power.session-watchdog-timeouts` blocked on its exact-read and caller-binding gaps.

The record now has baseline values, strings, Ghidra/KD context, boot and Procmon lanes, incident review, live watchdog globals, derived timeout globals, current-build selection-path disassembly, and generic power-manager registry helper evidence. That is a strong Class B research package.

It still cannot be promoted because no retained runtime lane has captured a decisive exact live read of `WatchdogResumeTimeout` or `WatchdogSleepTimeout`, and the watchdog-specific caller into `PopReadRegKeyValue` / `PopOpenPowerKey` remains unresolved.
