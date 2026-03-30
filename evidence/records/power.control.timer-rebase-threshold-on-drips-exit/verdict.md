# power.control.timer-rebase-threshold-on-drips-exit

- Class: `B`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, behavior_wpr, runtime_reboot`
- Tools: `etw, procmon, ghidra, ghidra_no_function_fallback, wpr, reboot`

Draft candidate package for TimerRebaseThresholdOnDripsExit under HKLM/SYSTEM/CurrentControlSet/Control/Power. The clean Win25H2Clean baseline confirmed the current default, the repo power notes carry an exact docs hit, the shared string batch found an exact current-build ntoskrnl.exe hit, the shared Ghidra batch produced reviewable xref artifacts, and the tools-hardened lightweight ETW follow-up on RegProbe-Baseline-ToolsHardened-20260330 first checked modern standby capability before attempting any DRIPS-exit trigger. That capability gate showed the current VMware baseline only exposes Standby (S1), so a real DRIPS / Modern Standby exit trigger cannot be exercised here and the record remains decision-gated by a VM standby limitation rather than by a dead-flag conclusion.

## Current verdict

TimerRebaseThresholdOnDripsExit is backed by phase-0 baseline existence, exact repo-doc hits, current-build ntoskrnl string corroboration, reviewable Ghidra artifacts, prior shell-safe Procmon lanes, and a tools-hardened DRIPS capability gate on RegProbe-Baseline-ToolsHardened-20260330. The record remains decision-gated because the current VMware baseline does not support Modern Standby / DRIPS exit, so the intended runtime trigger cannot be exercised on this VM.
