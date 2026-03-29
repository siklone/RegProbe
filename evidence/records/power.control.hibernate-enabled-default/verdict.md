# power.control.hibernate-enabled-default

- Class: `B`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, behavior_wpr, runtime_reboot`
- Tools: `etw, procmon, ghidra, ghidra_no_function_fallback, wpr, reboot`

Draft candidate package for HibernateEnabledDefault under HKLM/SYSTEM/CurrentControlSet/Control/Power. The clean Win25H2Clean baseline confirmed the current default, the repo power notes carry an exact docs hit, the shared string batch found an exact current-build ntoskrnl.exe hit, the shared Ghidra batch produced reviewable xref artifacts, and the guest-processed stepwise Procmon boot log on RegProbe-Baseline-Clean-20260329 completed shell-safe but did not capture an exact runtime read for HibernateEnabledDefault. A follow-up guest-processed post-boot Procmon trigger batch using powercfg, powercfg /energy, CPU stress, and profile-specific probes also completed shell-safe but still did not capture an exact runtime read for HibernateEnabledDefault. A later trigger-based ETW follow-up then confirmed the current VMware firmware does not support hibernation, so a real hibernation trigger could not be exercised on this baseline. The new guest-return ETW lane materialized the trace inside the guest and returned a compact summary without copy-back, but it still only reached an exact line without an exact query read.

## Current verdict

HibernateEnabledDefault is backed by phase-0 baseline existence, exact repo-doc hits, current-build ntoskrnl string corroboration, reviewable Ghidra artifacts, the clean-baseline stepwise Procmon boot log package, a second guest-processed post-boot Procmon trigger batch, and a guest-return ETW follow-up. The ETW guest-return lane produced an exact line hit, but the current VMware firmware still does not support a real hibernation transition, so the record remains decision-gated by an environment limitation rather than by a dead-flag conclusion.
