# power.disable-cpu-idle-states

- Class: `B`
- Pipeline: `v3.1`
- Official doc: `true`
- Cross-layer: `true`
- Layer set: `static_ghidra, behavior_benchmark, runtime_reboot, official_doc`
- Tools: `official-doc, ghidra, benchmark, reboot`

Validated observed implementation only. This record has a concrete Win25H2Clean baseline for the raw CPU idle-state bundle, a machine-checkable apply and restore pass for the app's current profile, a current-build Ghidra no-match follow-up on ntoskrnl.exe, and a rebooted benchmark attempt that broke shell availability before workloads started and required a snapshot recovery.

## Current verdict

The repo power notes and the Win25H2Clean probe line up on the same raw bundle and show both the current observed baseline and the app profile. The current-build Ghidra follow-up still did not surface a direct ntoskrnl lead, and the first rebooted benchmark lane broke shell availability before any workload ran. That keeps the record active and evidence-backed, but it still is not an app-ready supported control surface.
