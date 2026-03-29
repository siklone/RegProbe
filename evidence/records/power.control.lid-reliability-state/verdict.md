# power.control.lid-reliability-state

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, runtime_reboot`
- Tools: `procmon, ghidra, ghidra_no_function_fallback, reboot`

Draft candidate package for LidReliabilityState under HKLM/SYSTEM/CurrentControlSet/Control/Power. The clean Win25H2Clean baseline confirmed the current default, the repo power notes carry an exact docs hit, the shared string batch found an exact current-build ntoskrnl.exe hit, the shared Ghidra batch produced reviewable xref artifacts, and the guest-processed stepwise Procmon boot log on RegProbe-Baseline-Clean-20260329 captured an exact runtime read for LidReliabilityState. App surfacing remains a separate product decision from evidence classification.

## Current verdict

LidReliabilityState now has converged cross-layer evidence on RegProbe-Baseline-Clean-20260329: phase-0 baseline existence, exact repo-doc hits, current-build ntoskrnl string corroboration, reviewable Ghidra artifacts, and an exact runtime read from System captured by the guest-processed stepwise Procmon boot log. App mapping is tracked separately and does not block evidence classification.
