# power.disable-cpu-idle-states

- Class: `B`
- Pipeline: `v3.1`
- Official doc: `true`
- Cross-layer: `true`
- Layer set: `static_ghidra, behavior_benchmark, runtime_reboot, official_doc`
- Tools: `official-doc, etw, ghidra, benchmark, reboot`

Validated observed implementation only. This record has a concrete Win25H2Clean baseline for the raw CPU idle-state bundle, a machine-checkable apply and restore pass for the app's current profile, a current-build Ghidra no-match follow-up on ntoskrnl.exe, a fresh v3.1 runtime rerun on baseline-20260327-regprobe-visible-shell-stable that still fails during the set-candidate phase before reboot, a corrected benchmark rerun on the same baseline that still fails before any CPU or memory workload starts because Explorer does not come back in time during the snapshot-restore boot path, an earlier write-diagnostics lane that still could not emit a guest-side result file or wrapper error, a later minimal VMware smoke plus direct registry write diagnostic that both succeeded, and a final stepwise A/B/C/D runtime package that fixed the candidate-write and reboot substeps. That leaves a narrower remaining blocker: WPR stop returns success, but the expected ETL file still does not materialize for host collection.

## Current verdict

The repo power notes and the Win25H2Clean probe line up on the same raw bundle and show both the current observed baseline and the app profile. The current-build Ghidra follow-up still did not surface a direct ntoskrnl lead. An earlier dedicated write-diagnostics lane produced no guest-side result file, but follow-up minimal VMware smoke and direct registry-write diagnostics both succeeded under C:/RegProbe-Diag. A later stepwise runtime package then fixed the candidate-write and reboot substeps too: the candidate bundle now survives the rebooted runtime pass, and the restore phase returns the machine to baseline. That means the unresolved blocker is no longer generic guest execution, registry write rights, set-candidate, or reboot. The remaining blocker is now specifically WPR or ETL materialization on this baseline, and the record still is not an app-ready supported control surface.
