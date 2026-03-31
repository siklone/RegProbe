# system.kernel.disable-exception-chain-validation

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `static_ghidra, behavior_wpr, runtime_reboot`
- Tools: `etw, ghidra, wpr, reboot`

Validated cross-layer record for DisableExceptionChainValidation under HKLM/SYSTEM/CurrentControlSet/Control/Session Manager/Kernel. The broad 96-key phase-0 batch confirmed the baseline path and value absence, repo system notes already tracked the value by name, broad current-build static triage found an exact ntoskrnl.exe hit, and the tools-hardened Session Manager Kernel lightweight ETW batch wrote the candidate, rebooted once, and captured exact RegQueryValue hits for DisableExceptionChainValidation while the other 20 sibling kernel candidates stayed no-hit. That is enough for Class A in this project even though the value remains research-only and not app-mapped.

## Current verdict

DisableExceptionChainValidation now has converged project evidence on the tools-hardened baseline: broad phase-0 existence, repo system notes, a current-build ntoskrnl static hit, and an exact early-boot runtime read from the single-reboot Session Manager Kernel ETW batch. The value remains research-only and not app-mapped, but that no longer blocks Class A.
