# System I/O AllowRemoteDASD Path-Aware Follow-up

Date: 2026-03-30
Candidate: `system.io-allow-remote-dasd`

## Objective
- disambiguate the residual `AllowRemoteDASD` value under `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System`
- prove whether the current-build string hit belongs to the intended Session Manager I/O path or to the removable-storage policy collision
- require exact-match static routing and a path-aware runtime ETW lane before packaging

## Static Path-Aware Result
- exact value hit: `ntoskrnl.exe`
- exact intended path string hit: none
- exact removable-storage path string hit: none in the narrow exact-string pass
- current-build Ghidra decompilation on `ntoskrnl.exe` is the decisive context: the naturally resolved function opens `\REGISTRY\MACHINE\SOFTWARE\Policies\Microsoft\Windows\RemovableStorageDevices` and queries `AllowRemoteDASD`
- canonical static artifact: `evidence/files/path-aware/path-aware-static-20260330-194412/system-io-allow-remote-dasd/summary.json`
- canonical ghidra artifact: `evidence/files/ghidra/system-io-allow-remote-dasd-ntoskrnl-exe-path-aware-20260330-194412/ghidra-matches.md`

## Runtime Path-Aware Result
- tools-hardened lightweight ETW lane completed shell-safe on `RegProbe-Baseline-ToolsHardened-20260330`
- both the short trigger and split trace phases produced ETL/CSV outputs with a real I/O burst trigger
- exact runtime reads: `0`
- exact value-name lines: `0`
- intended path lines: `0`
- collision-path lines: `0`
- canonical runtime artifact: `evidence/files/path-aware/path-aware-runtime-20260330-220218/system-io-allow-remote-dasd/summary.json`
- lane root summary: `evidence/files/path-aware/path-aware-runtime-20260330-220218/summary.json`

## Verdict
- keep as `Class B`
- reason: `runtime_no_read + path_context_unclear`
- the exact current-build value-name hit is real, but the strongest current-build code route points at the removable-storage policy path rather than the intended Session Manager I/O path, and the intended runtime ETW lane remained a clean `no-hit`
