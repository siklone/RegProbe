# system.io-allow-remote-dasd

- Class: `E`
- Record status: `deprecated`
- Tested build: `26100`
- Reason: `class-e`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `none`
- Tools: `none`

## Why it stays negative

Archived audit trail only. Keep this out of the normal tweak surface.

## Attached references

- `registry-observation` Win25H2Clean 96-key phase-0 existence batch -> evidence/files/vm-tooling-staging/registry-batch-existence-96-live-20260329-100629/results.json
- `official-doc` Microsoft Learn ADMX_RemovableStorage mapping for AllowRemoteDASD -> https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-removablestorage and research/notes/system-io-allow-remote-dasd-official-policy-follow-up-20260407.md
- `decompilation` Path-aware static probe for AllowRemoteDASD -> evidence/files/path-aware/path-aware-static-20260330-194412/system-io-allow-remote-dasd/summary.json and evidence/raw/ghidra/system-io-allow-remote-dasd-ntoskrnl-exe-path-aware-20260330-194412/ghidra-matches.md
- `etw-trace` Path-aware lightweight ETW follow-up for AllowRemoteDASD -> evidence/files/path-aware/path-aware-runtime-20260330-220218/summary.json and evidence/files/path-aware/path-aware-runtime-20260330-220218/system-io-allow-remote-dasd/summary.json
- `repo-doc` Historical collision review for AllowRemoteDASD -> research/notes/kernel-power-existing-static-triage-20260328.md
- `procmon-trace` Linux KVM Procmon replay for AllowRemoteDASD -> evidence/files/vm-tooling-staging/allowremotedasd-procmon-kvm-20260406b/allowremotedasd-procmon-kvm-20260406b-summary.json and evidence/files/vm-tooling-staging/allowremotedasd-procmon-kvm-20260406b/host-review.json
