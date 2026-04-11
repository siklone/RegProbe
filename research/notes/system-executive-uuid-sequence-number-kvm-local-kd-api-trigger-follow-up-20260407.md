# System Executive UuidSequenceNumber KVM Local KD API Trigger Follow-up

Date: 2026-04-07
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- execute a stronger user-mode UUID allocation burst after the API disassembly identified `ExUuidCreate` and `NtAllocateUuids` as the most promising current-build mutation paths
- check whether repeated direct allocation calls can move the live `ExpUuidSequenceNumber` state that earlier generic UUID / RPC / COM bursts failed to perturb

## Result
- the host-driven local-KD helper attached successfully and the trigger command exited cleanly with code `0`
- the trigger downloaded and ran a PowerShell helper that repeatedly called both `UuidCreateSequential` from `rpcrt4.dll` and `NtAllocateUuids` from `ntdll.dll`
- the first snapshot read `ExpUuidSequenceNumber = 0x002caf1b` (`2928411` decimal), `ExpUuidSequenceNumberValid = 0x01`, and `ExpUuidSequenceNumberNotSaved = 0x00`
- the second snapshot after the direct API burst read the exact same values: `ExpUuidSequenceNumber = 0x002caf1b`, `ExpUuidSequenceNumberValid = 0x01`, and `ExpUuidSequenceNumberNotSaved = 0x00`
- trigger stdout and stderr were both empty, so this is a clean no-delta result rather than a failed trigger invocation

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-uuid-api-trigger-state-20260407d/local-kd-uuid-api-trigger-state-20260407d-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-uuid-api-trigger-state-20260407d/local-kd-uuid-api-trigger-state-20260407d.log`
- `evidence/files/vm-tooling-staging/local-kd-uuid-api-trigger-state-20260407d/local-kd-uuid-api-trigger-state-20260407d.txt`

## Short Take
- even a direct user-mode UUID allocation burst built on top of `UuidCreateSequential` and `NtAllocateUuids` did not move the observed persisted UUID sequence state on the working guest
- that sharply narrows the next runtime-read chase: the missing proof is not just “use a better UUID trigger”, because the obvious current-build user-mode allocation APIs also stay no-delta here
- the next winning lane likely needs a different timing window, a kernel-adjacent caller, or a more exact reader-side trap rather than more of the same user-mode allocation pressure
