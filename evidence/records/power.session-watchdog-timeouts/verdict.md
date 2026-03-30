# power.session-watchdog-timeouts

- Class: `B`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `runtime_procmon, static_ghidra, behavior_wpr, runtime_reboot`
- Tools: `etw, procmon, ghidra, ghidra_no_function_fallback, wpr, reboot`

Validated decision-gated record. The Session Manager watchdog timeout pair now has baseline existence on Win25H2Clean, exact ntoskrnl string hits plus current-build Ghidra fallback artifacts, repo-side PoFx pseudocode that ties the pair to directed power watchdog timeout globals, a successful reboot-verified boot trace baseline, a host-side ETL registry review that proves repeated boot-time access to Session Manager/Power, a working Procmon boot-log capture that reproduces adjacent Session Manager/Power traffic from System during boot, a DcomLaunch attribution package that narrows the svchost lead to the service host group containing Power, prior S1-specific Procmon follow-ups that failed to leave decisive in-guest artifacts, and a new tools-hardened lightweight ETW S1 follow-up that still lost the guest before a usable exact-value capture could be completed. The pair remains Class B because the current VMware baseline is S1-only and still does not provide a reliable decisive runtime path for the exact watchdog values.

## Current verdict

The watchdog timeout pair now has cross-layer evidence: a clean baseline export, exact current-build ntoskrnl string and Ghidra fallback artifacts, repo-side PoFx pseudocode, a successful reboot-verified boot trace, a host-side ETL registry review, and multiple runtime attempts across Procmon and lightweight ETW. The current blocker is no longer lack of research depth. It is that the current S1-only VMware environment still does not provide a reliable decisive exact-value runtime path for the watchdog pair.
