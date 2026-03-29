# power.disable-cpu-idle-states

- Class: `B`
- Pipeline: `v3.1`
- Official doc: `true`
- Cross-layer: `true`
- Layer set: `static_ghidra, behavior_benchmark, runtime_reboot, official_doc`
- Tools: `official-doc, etw, ghidra, benchmark, reboot`

Validated observed implementation only. This record has a concrete Win25H2Clean baseline for the raw CPU idle-state bundle, a machine-checkable apply and restore pass for the app's current profile, a current-build Ghidra no-match follow-up on ntoskrnl.exe, older runtime and benchmark failures from pre-excluded baselines, later minimal VMware smoke plus direct registry-write diagnostics that both succeeded, and a final excluded-baseline stepwise A/B/C1/C2/C3/C4/D runtime package that completed candidate write, reboot, WPR start, WPR stop, guest-side ETL existence, host copy-back, restore, and post-restore verification. That removes runtime orchestration and ETL materialization as blockers.

## Current verdict

The repo power notes and the Win25H2Clean probe line up on the same raw bundle and show both the current observed baseline and the app profile. The current-build Ghidra follow-up still did not surface a direct ntoskrnl lead. Earlier write-diagnostics and rebooted runtime failures exposed orchestration gaps on older baselines, but follow-up minimal VMware smoke and direct registry-write diagnostics both succeeded under C:/RegProbe-Diag. The final excluded-baseline stepwise runtime package on RegProbe-Baseline-20260328 then completed candidate write, reboot, WPR start, WPR stop, guest-side ETL existence, host copy-back, restore, and post-restore verification. The record still stays Class B because it is a raw power-manager registry bundle without a supported Microsoft app-ready control surface.
