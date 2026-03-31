# system.executive-uuid-sequence-number

- Class: `B`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `static_ghidra, behavior_wpr`
- Tools: `etw, ghidra, wpr`

Draft candidate package for UuidSequenceNumber under HKLM/SYSTEM/CurrentControlSet/Control/Session Manager/Executive. The clean Win25H2Clean baseline confirmed the live value, the residual static triage found an exact current-build ntoskrnl.exe Unicode hit, the bounded Executive ETL review proved adjacent query and set activity for the same value during early boot, and a dedicated tools-hardened lightweight ETW follow-up still stayed a clean runtime no-hit. That keeps the lane at Class B with a runtime_no_read and trigger-context gate.

## Current verdict

UuidSequenceNumber is backed by clean baseline existence, an exact current-build ntoskrnl string hit, bounded early-boot ETL evidence that the same value is active under Session Manager Executive, and a dedicated tools-hardened lightweight ETW lane that still stayed a clean runtime no-hit. That is enough for a strong research package, but not enough for Class A on the current VMware baseline.
