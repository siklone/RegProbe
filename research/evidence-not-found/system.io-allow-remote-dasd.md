# system.io-allow-remote-dasd

- Class: `B`
- Record status: `draft`
- Tested build: `26100`
- Reason: `no-hit-or-insufficient-proof`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `static_ghidra, behavior_wpr`
- Tools: `etw, ghidra, wpr`

## Why it stays negative

The strongest current-build code route points to the removable-storage policy path, while the intended Session Manager I/O runtime ETW lane stayed no-hit.

## Attached references

- `registry-observation` Win25H2Clean 96-key phase-0 existence batch -> evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json
- `decompilation` Path-aware static probe for AllowRemoteDASD -> evidence/files/path-aware/path-aware-static-20260330-194412/system-io-allow-remote-dasd/summary.json and evidence/files/ghidra/system-io-allow-remote-dasd-ntoskrnl-exe-path-aware-20260330-194412/ghidra-matches.md
- `etw-trace` Path-aware lightweight ETW follow-up for AllowRemoteDASD -> evidence/files/path-aware/path-aware-runtime-20260330-220218/summary.json and evidence/files/path-aware/path-aware-runtime-20260330-220218/system-io-allow-remote-dasd/summary.json
- `repo-doc` Historical collision review for AllowRemoteDASD -> research/notes/kernel-power-existing-static-triage-20260328.md
