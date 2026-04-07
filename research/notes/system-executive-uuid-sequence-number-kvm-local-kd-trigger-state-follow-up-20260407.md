# System Executive UuidSequenceNumber KVM Local KD Trigger-State Follow-up

Date: 2026-04-07
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- test whether the existing KVM `uuid-rpc-com-burst` trigger actually perturbs the live UUID state that the local-KD lane can inspect
- measure `ExpUuidSequenceNumber`, `ExpUuidSequenceNumberValid`, and `ExpUuidSequenceNumberNotSaved` immediately before and after the trigger inside one attached debugger session

## Result
- the host-driven local-KD helper attached successfully to the live guest kernel, completed without timing out, and the trigger burst exited cleanly with code `0`
- the first snapshot read `ExpUuidSequenceNumber = 0x002caf1b` (`2928411` decimal), `ExpUuidSequenceNumberValid = 0x01`, and `ExpUuidSequenceNumberNotSaved = 0x00`
- the second snapshot after the `uuid-rpc-com-burst` read the exact same values: `ExpUuidSequenceNumber = 0x002caf1b`, `ExpUuidSequenceNumberValid = 0x01`, and `ExpUuidSequenceNumberNotSaved = 0x00`
- this means the current UUID / RPC / COM burst did execute, but it did not move the persisted UUID state that the live local-KD lane is observing on the working guest

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-uuid-trigger-state-20260407a/local-kd-uuid-trigger-state-20260407a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-uuid-trigger-state-20260407a/local-kd-uuid-trigger-state-20260407a.log`
- `evidence/files/vm-tooling-staging/local-kd-uuid-trigger-state-20260407a/local-kd-uuid-trigger-state-20260407a.txt`

## Short Take
- KVM local KD now shows that the existing `uuid-rpc-com-burst` trigger is not enough by itself to change the live `ExpUuidSequenceNumber` state on the working guest
- that makes the remaining `runtime_no_read` blocker more concrete: the transport is not only missing a direct registry read, it also is not perturbing the observed UUID state variables
- the next exact-read chase should therefore use a different trigger shape rather than just replaying the current UUID / RPC / COM burst
