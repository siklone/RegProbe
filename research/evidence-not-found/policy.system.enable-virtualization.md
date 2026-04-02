# policy.system.enable-virtualization

- Class: `B`
- Record status: `draft`
- Tested build: `26100`
- Reason: `no-hit-or-insufficient-proof`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `static_ghidra, behavior_wpr`
- Tools: `etw, ghidra, wpr`

## Why it stays negative

Static routing is promising, but the path-aware ETW lane stayed no-hit and the family still carries a nearby VBS collision in winload.exe.

## Attached references

- `repo-doc` Repo security notes for EnableVirtualization -> Docs/security/security.md
- `registry-observation` Win25H2Clean 96-key phase-0 existence batch -> evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json
- `decompilation` Boot-phase UAC policy cluster lead -> research/_source-mirrors/decompiled-pseudocode/ntoskrnl/PsBootPhaseComplete.c
- `decompilation` Path-aware static probe for EnableVirtualization -> evidence/files/path-aware/path-aware-static-20260330-222908/policy-system-enable-virtualization/summary.json and evidence/files/ghidra/policy-system-enable-virtualization-ntoskrnl-exe-path-aware-20260330-222908/ghidra-matches.md
- `etw-trace` Path-aware lightweight ETW follow-up for EnableVirtualization -> evidence/files/path-aware/path-aware-runtime-20260330-221529/summary.json and evidence/files/path-aware/path-aware-runtime-20260330-221529/policy-system-enable-virtualization/summary.json
- `etw-trace` Secondary-profile path-aware ETW replay for EnableVirtualization -> evidence/files/path-aware/secondary/path-aware-runtime-secondary-20260331-110610/summary.json and evidence/files/path-aware/secondary/path-aware-runtime-secondary-20260331-110610/policy-system-enable-virtualization/summary.json
