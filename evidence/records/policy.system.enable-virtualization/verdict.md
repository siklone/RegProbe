# policy.system.enable-virtualization

- Class: `B`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `static_ghidra, behavior_wpr`
- Tools: `etw, ghidra, wpr`

Draft candidate package for EnableVirtualization under HKLM/SOFTWARE/Microsoft/Windows/CurrentVersion/Policies/System. The clean Win25H2Clean baseline confirmed the live value, the repo security notes document the value and meanings, the path-aware static pass found exact current-build ntoskrnl.exe value hits plus adjacent EnableLUA and EnableInstallerDetection hits, and the tools-hardened path-aware ETW lane completed shell-safe but stayed a clean no-hit. That keeps the lane at Class B with a runtime_no_read and path-context gate.

## Current verdict

EnableVirtualization is backed by repo documentation, phase-0 baseline existence, a current-build ntoskrnl value hit with adjacent UAC-policy context, and a shell-safe path-aware ETW runtime lane. The runtime lane still stayed a clean no-hit and the family still carries a nearby VBS collision in winload.exe, so the record remains decision-gated at Class B.
