# System Executive UuidSequenceNumber KVM Local KD API Follow-up

Date: 2026-04-07
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- inspect the live current-build UUID allocation entry points after the trigger-state no-delta result
- determine which kernel APIs are most likely to mutate `ExpUuidSequenceNumber` and `ExpUuidSequenceNumberNotSaved`, so the next runtime-read chase can use a better trigger than the generic UUID / RPC / COM burst

## Result
- the host-driven local-KD helper attached successfully and completed without timing out
- `uf nt!ExUuidCreate` showed the API reading cached UUID fields, calling `nt!ExpUuidGetValues`, and then calling `nt!ExpUuidSaveSequenceNumberIf`
- `uf nt!NtAllocateUuids` showed the syscall path taking the UUID lock, calling `nt!ExpAllocateUuids`, and then calling `nt!ExpUuidSaveSequenceNumberIf`
- `uf nt!ExpAllocateUuids` showed the actual sequence-state machinery: when `ExpUuidSequenceNumberValid` is clear it calls `nt!ExpUuidLoadSequenceNumber`, later writes back `nt!ExpUuidSequenceNumber`, sets `nt!ExpUuidSequenceNumberValid = 1`, sets `nt!ExpUuidSequenceNumberNotSaved = 1`, and on the rollback branch it can `inc dword ptr [nt!ExpUuidSequenceNumber]`
- that makes direct UUID allocation APIs a better future trigger family than the current generic UUID / RPC / COM burst, which already executed cleanly without changing the observed UUID state

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-uuid-api-disasm-20260407c/local-kd-uuid-api-disasm-20260407c-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-uuid-api-disasm-20260407c/local-kd-uuid-api-disasm-20260407c.log`
- `evidence/files/vm-tooling-staging/local-kd-uuid-api-disasm-20260407c/local-kd-uuid-api-disasm-20260407c.txt`

## Short Take
- KVM local KD now identifies the current-build UUID allocation APIs that actually sit on top of the mutable sequence-state machinery
- `ExUuidCreate` and especially `NtAllocateUuids` are stronger next-step trigger candidates than the existing UUID / RPC / COM burst
- the remaining blocker is still exact runtime read proof, but the next chase no longer needs to guess which API family to stress
