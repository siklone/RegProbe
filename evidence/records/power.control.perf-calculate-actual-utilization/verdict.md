# power.control.perf-calculate-actual-utilization

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, runtime_reboot`
- Tools: `procmon, ghidra, ghidra_no_function_fallback, reboot, etw`

Draft candidate package for PerfCalculateActualUtilization under HKLM\SYSTEM\CurrentControlSet\Control\Power. The clean Win25H2Clean baseline confirmed the current default, the repo power notes carry an exact docs hit, the shared string batch found an exact current-build ntoskrnl.exe hit, the shared Ghidra batch produced reviewable xref artifacts, and the tools-hardened lightweight ETW follow-up on RegProbe-Baseline-ToolsHardened-20260330 captured an exact runtime read for PerfCalculateActualUtilization. App surfacing remains a separate product decision from evidence classification.

## Current verdict

PerfCalculateActualUtilization now has converged cross-layer evidence on RegProbe-Baseline-ToolsHardened-20260330: phase-0 baseline existence, exact repo-doc hits, current-build ntoskrnl string corroboration, reviewable Ghidra artifacts, prior shell-safe Procmon lanes, and an exact runtime read captured by the tools-hardened lightweight ETW follow-up. App mapping is tracked separately and does not block evidence classification.
