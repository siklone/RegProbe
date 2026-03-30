# Policy System EnableVirtualization Path-Aware Follow-up

Date: 2026-03-30
Candidate: `policy.system.enable-virtualization`

## Objective
- disambiguate the residual `EnableVirtualization` value under `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System`
- separate it from the unrelated `EnableVirtualizationBasedSecurity` surface
- require exact-match static routing and a path-aware runtime ETW lane before packaging

## Static Path-Aware Result
- exact value hit: `ntoskrnl.exe`
- exact adjacent context hits in the same binary: `EnableLUA`, `EnableInstallerDetection`
- collision hit lives in a different binary: `winload.exe` carries `EnableVirtualizationBasedSecurity`
- exact path string did not survive the current-build static pass, but the current-build Ghidra export keeps `EnableVirtualization`, `EnableLUA`, and `EnableInstallerDetection` in the same naturally resolved `ntoskrnl.exe` function
- canonical static artifact: `evidence/files/path-aware/path-aware-static-20260330-222908/policy-system-enable-virtualization/summary.json`
- canonical ghidra artifact: `evidence/files/ghidra/policy-system-enable-virtualization-ntoskrnl-exe-path-aware-20260330-222908/ghidra-matches.md`

## Runtime Path-Aware Result
- tools-hardened lightweight ETW lane completed shell-safe on `RegProbe-Baseline-ToolsHardened-20260330`
- both the short trigger and split trace phases produced ETL/CSV outputs
- exact runtime reads: `0`
- exact value-name lines: `0`
- intended path lines: `0`
- collision-path lines: `0`
- canonical runtime artifact: `evidence/files/path-aware/path-aware-runtime-20260330-221529/policy-system-enable-virtualization/summary.json`
- lane root summary: `evidence/files/path-aware/path-aware-runtime-20260330-221529/summary.json`

## Verdict
- keep as `Class B`
- reason: `runtime_no_read + path_context_unclear`
- current-build static evidence is promising but still not decisive for the intended policy path because the live runtime lane remained a clean `no-hit` and the family still has a nearby VBS collision in `winload.exe`
