# Watchdog Timeouts Lightweight Runtime Follow-Up (2026-03-30)

The tools-hardened follow-up for `power.session-watchdog-timeouts` ran on `RegProbe-Baseline-ToolsHardened-20260330`.

Goal:
- retry the watchdog lane with lightweight ETW instead of heavy Procmon
- keep the trace in split `start -> trigger -> stop` phases
- use the only power-transition surface available on the current VM: `Standby (S1)`

What happened:
- the capability phase confirmed that the current VMware baseline still exposes only `Standby (S1)`
- the lightweight runtime lane reached the watchdog trigger stage
- the guest then dropped out during the S1 transition before a usable exact-value ETW bundle could be completed
- this happened even after the VM tools hardening work and after moving to a lighter ETW path

Why this matters:
- the blocker is now sharper than before
- the watchdog pair is not blocked because the research layers are weak
- it is blocked because the current S1-only VMware environment still cannot carry a decisive exact-value runtime lane through suspend/resume without losing guest state

Decision:
- keep `power.session-watchdog-timeouts` at `Class B`
- mark the blocker as `vm_s1_only_limitation`
- do not demote to `E`, because this is still an environment limitation rather than a dead-flag result
