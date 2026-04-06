# System Executive UuidSequenceNumber KVM Local KD Symbol Follow-up

Date: 2026-04-06
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- verify that the host-driven KVM local-KD lane can resolve the live UUID-related symbol family that earlier static and ReactOS-adjacent work suggested
- reduce symbol-family uncertainty around `UuidSequenceNumber` without overstating the result as a direct registry-read proof

## Result
- the host-driven local-KD helper attached successfully to the live guest kernel and completed without timing out
- `symchk.exe` returned `0`, the debugger query completed, and `x nt!*Exp*Uuid*` resolved a coherent UUID load/save/state symbol family in the running guest
- the live kernel reported `ExpUuidLoadSequenceNumber`, `ExpUuidSaveSequenceNumber`, `ExpUuidSaveSequenceNumberIf`, `ExpUuidGetValues`, and `ExpAllocateUuids`
- the same live query also resolved `ExpUuidSequenceNumber`, `ExpUuidSequenceNumberValid`, `ExpUuidSequenceNumberNotSaved`, and `ExpUuidSequenceNumberRegName`

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-uuid-wildcard-20260406a/local-kd-uuid-wildcard-20260406a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-uuid-wildcard-20260406a/local-kd-uuid-wildcard-20260406a.log`
- `evidence/files/vm-tooling-staging/local-kd-uuid-wildcard-20260406a/local-kd-uuid-wildcard-20260406a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-uuid-wildcard-20260406a/local-kd-uuid-wildcard-20260406a.txt`

## Short Take
- KVM local KD now confirms the current-build UUID load/save/state symbol family in the live guest kernel, which strengthens the Rpc- and UUID-adjacent symbol story behind `UuidSequenceNumber`
- this still does not prove that `Session Manager\\Executive\\UuidSequenceNumber` is the direct current-build runtime reader or writer
- the lane therefore stays gated by runtime no-hit and trigger-context uncertainty
