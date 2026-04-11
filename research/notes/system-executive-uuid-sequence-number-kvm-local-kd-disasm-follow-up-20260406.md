# System Executive UuidSequenceNumber KVM Local KD Disassembly Follow-up

Date: 2026-04-06
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- move the KVM local-KD lane beyond wildcard symbol discovery and inspect the live current-build UUID load/save routines directly
- check whether the running guest kernel still points `UuidSequenceNumber` at the Session Manager Executive path or at some other adjacent location

## Result
- the host-driven local-KD helper attached successfully to the live guest kernel and completed without timing out
- `uf nt!ExpUuidLoadSequenceNumber` showed a live current-build path that calls `RtlGetPersistedStateLocation`, `RtlInitUnicodeString`, `ZwOpenKey`, and `ZwQueryValueKey`, then stores the returned DWORD into `nt!ExpUuidSequenceNumber`
- `uf nt!ExpUuidSaveSequenceNumber` showed the matching write path through `RtlGetPersistedStateLocation`, `RtlInitUnicodeString`, `ZwOpenKey`, and `ZwSetValueKey`
- `uf nt!ExpUuidSaveSequenceNumberIf` showed that the save path is gated by `nt!ExpUuidSequenceNumberNotSaved` and clears that flag after a successful save
- `du 0xfffff800\`976f6390` resolved the path-builder string to `\Registry\Machine\System\CurrentControlSet\Control\Session Manager\Executive`
- `du 0xfffff800\`976f6430` resolved the companion qualifier string to `KernelExecutive`
- `du poi(nt!ExpUuidSequenceNumberRegName+0x8)` resolved the live value-name buffer to `UuidSequenceNumber`

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-uuid-disasm-20260406a/local-kd-uuid-disasm-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-uuid-disasm-20260406a/local-kd-uuid-disasm-20260406a.log`
- `evidence/files/vm-tooling-staging/local-kd-uuid-strings-20260406a/local-kd-uuid-strings-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-uuid-strings-20260406a/local-kd-uuid-strings-20260406a.log`

## Short Take
- KVM local KD now ties the current-build live UUID load/save path to a Session Manager Executive persisted-state location plus the exact `UuidSequenceNumber` value name
- this materially reduces the older path ambiguity around the lane and is stronger than the earlier ReactOS-adjacent guesswork
- the remaining blocker is still direct runtime observation on the live guest, not current-build path identity
