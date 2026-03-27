# power.disable-cpu-idle-states

- Class: `B`
- Pipeline: `v3.1`
- Official doc: `true`
- Cross-layer: `true`
- Layer set: `static_ghidra, behavior_benchmark, runtime_reboot, official_doc`
- Tools: `official-doc, etw, ghidra, benchmark, reboot`

Validated observed implementation only. This record has a concrete Win25H2Clean baseline for the raw CPU idle-state bundle, a machine-checkable apply and restore pass for the app's current profile, a current-build Ghidra no-match follow-up on ntoskrnl.exe, a fresh v3.1 runtime rerun on baseline-20260327-regprobe-visible-shell-stable that still fails during the set-candidate phase before reboot, and a corrected benchmark rerun on the same baseline that still fails before any CPU or memory workload starts because Explorer does not come back in time during the snapshot-restore boot path.

## Current verdict

The repo power notes and the Win25H2Clean probe line up on the same raw bundle and show both the current observed baseline and the app profile. The current-build Ghidra follow-up still did not surface a direct ntoskrnl lead. On the fresh visible-shell baseline, the v3.1 runtime lane still failed during the set-candidate stage before reboot, and the corrected benchmark lane still failed before any workload started because Explorer did not come back in time during the snapshot-restore boot path. That keeps the record active and evidence-backed, but it still is not an app-ready supported control surface.
