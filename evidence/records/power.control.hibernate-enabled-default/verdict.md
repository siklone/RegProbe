# power.control.hibernate-enabled-default

- Class: `C`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, runtime_reboot`
- Tools: `procmon, ghidra, ghidra_no_function_fallback, reboot`

Draft candidate package for HibernateEnabledDefault under HKLM/SYSTEM/CurrentControlSet/Control/Power. The clean Win25H2Clean baseline confirmed the current default, the repo power notes carry an exact docs hit, the shared string batch found an exact current-build ntoskrnl.exe hit, the shared Ghidra batch produced reviewable xref artifacts, and the guest-processed stepwise Procmon boot log on RegProbe-Baseline-Clean-20260329 completed shell-safe but did not capture an exact runtime read for HibernateEnabledDefault. A follow-up guest-processed post-boot Procmon trigger batch using powercfg, powercfg /energy, CPU stress, and profile-specific probes also completed shell-safe but still did not capture an exact runtime read for HibernateEnabledDefault.

## Current verdict

HibernateEnabledDefault is backed by phase-0 baseline existence, exact repo-doc hits, current-build ntoskrnl string corroboration, reviewable Ghidra artifacts, the clean-baseline stepwise Procmon boot log package, and a second guest-processed post-boot Procmon trigger batch. It still remains below Class A because neither runtime lane captured an exact runtime read for this value on the current baseline.
