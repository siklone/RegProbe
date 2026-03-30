# power.disable-cpu-idle-states

- Class: `A`
- Pipeline: `v3.1`
- Official doc: `true`
- Cross-layer: `true`
- Layer set: `static_ghidra, behavior_wpr, behavior_benchmark, runtime_reboot, official_doc`
- Tools: `official-doc, etw, ghidra, wpr, benchmark, reboot`

Validated observed implementation only. This record now has a Defender-excluded canonical VM baseline, a successful stepwise A/B/C1/C2/C3/C4/D runtime package on RegProbe-Baseline-20260328, and a machine-checkable apply, reboot, WPR, ETL, copy-back, and restore pass for the app's current profile. The remaining blocker is no longer guest execution, registry write rights, reboot orchestration, WPR stop, ETL existence, or host collection. Cross-layer evidence now converges strongly enough for Class A even though the bundle remains a raw undocumented power-manager surface.

## Current verdict

The repo power notes and the Win25H2Clean probe line up on the same raw bundle and show both the current observed baseline and the app profile. The current-build Ghidra follow-up still did not surface a direct ntoskrnl lead, but the remaining layers now converge strongly enough for this project: follow-up minimal VMware smoke and direct registry-write diagnostics proved that generic guest execution, host copy-back, and the raw bundle writes themselves were healthy under C:/RegProbe-Diag, and the final excluded-baseline stepwise runtime package on RegProbe-Baseline-20260328 completed baseline apply, rebooted post-boot read, WPR start, WPR stop, ETL existence, host copy-back, restore, and post-restore verification. The remaining issue is not runtime or reversibility. Within RegProbe, undocumented raw control surfaces can still reach Class A when cross-layer evidence is strong enough.
