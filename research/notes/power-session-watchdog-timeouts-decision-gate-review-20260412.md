# power.session-watchdog-timeouts decision gate review - 2026-04-12

## Decision

Keep `power.session-watchdog-timeouts` as an intentional hold.

The record now has baseline values, strings, Ghidra/KD context, boot and Procmon lanes, incident review, live watchdog globals, derived timeout globals, current-build selection-path disassembly, and generic power-manager registry helper evidence. That is a strong Class B research package.

The hold is explicit because the retained runtime failures now read as validation-environment limitation, not simple missing setup. The S1-only VMware lanes repeatedly failed before preserving decisive artifacts, and the KVM fallback still exported zero exact hits. Re-open this only when we have either a more reliable suspend/resume environment or a stronger current-build caller pivot into `PopReadRegKeyValue` / `PopOpenPowerKey`.
