# power.control.mf-buffering-threshold

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, behavior_wpr, runtime_reboot`
- Tools: `etw, procmon, ghidra, ghidra_no_function_fallback, wpr, reboot`

Draft candidate package for MfBufferingThreshold under HKLM/SYSTEM/CurrentControlSet/Control/Power. The clean Win25H2Clean baseline confirmed the current default, the repo power notes carry an exact docs hit, the shared string batch found an exact current-build ntoskrnl.exe hit, the shared Ghidra batch produced reviewable xref artifacts, and the tools-hardened lightweight ETW follow-up on RegProbe-Baseline-ToolsHardened-20260330 captured an exact runtime read for MfBufferingThreshold through the new disk I/O burst trigger. App surfacing remains a separate product decision from evidence classification.

## Current verdict

MfBufferingThreshold now has converged cross-layer evidence on RegProbe-Baseline-ToolsHardened-20260330: phase-0 baseline existence, exact repo-doc hits, current-build ntoskrnl string corroboration, reviewable Ghidra artifacts, prior shell-safe Procmon lanes, and an exact runtime read captured by the tools-hardened lightweight ETW I/O burst follow-up. App mapping is tracked separately and does not block evidence classification.
