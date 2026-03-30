# system.io-allow-remote-dasd

- Class: `B`
- Pipeline: `v3.1`
- Official doc: `false`
- Cross-layer: `true`
- Layer set: `static_ghidra, behavior_wpr`
- Tools: `etw, ghidra, wpr`

Draft candidate package for AllowRemoteDASD under HKLM/SYSTEM/CurrentControlSet/Control/Session Manager/I/O System. The clean Win25H2Clean baseline confirmed the live value, the path-aware static pass found an exact current-build ntoskrnl.exe value hit, and a naturally resolved current-build Ghidra export showed that the strongest code route opens the removable-storage policy path instead of the intended Session Manager I/O path. The tools-hardened path-aware ETW lane completed shell-safe with a real I/O burst but stayed a clean no-hit for the intended path. That keeps the lane at Class B with runtime_no_read and path-context uncertainty.

## Current verdict

AllowRemoteDASD is backed by phase-0 baseline existence, an exact current-build ntoskrnl value-name hit, a naturally resolved current-build Ghidra route to the removable-storage policy path, and a shell-safe intended-path ETW lane that remained a clean no-hit. That is enough to keep the candidate visible, but not enough to treat the intended Session Manager I/O path as cross-layer verified.
