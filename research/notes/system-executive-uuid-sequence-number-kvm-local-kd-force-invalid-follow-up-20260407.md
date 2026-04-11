# System Executive UuidSequenceNumber KVM Local KD Force-Invalid Follow-up

Date: 2026-04-07
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- test whether the direct user-mode UUID allocation APIs become a winning trigger if the live UUID validity bit is cleared first
- distinguish a dead-end user-mode burst from a trigger that can drive the current-build UUID reload path when the in-memory cache is invalid

## Result
- the host-driven local-KD helper attached successfully and completed without timing out
- before the trigger, the live guest reported `ExpUuidSequenceNumber = 0x002caf1b`, `ExpUuidSequenceNumberValid = 1`, and `ExpUuidSequenceNumberNotSaved = 0`
- the debugger lane then forced `ExpUuidSequenceNumberValid` to `0` with `eb nt!ExpUuidSequenceNumberValid 00`
- during the same attached session, the guest ran the direct API burst built from repeated `UuidCreateSequential` plus `NtAllocateUuids` calls; the trigger exited cleanly with code `0` and empty stdout/stderr
- after the trigger, the live guest reported `ExpUuidSequenceNumber = 0x002caf1c`, `ExpUuidSequenceNumberValid = 1`, and `ExpUuidSequenceNumberNotSaved = 0`

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-uuid-force-invalid-trigger-20260407f/local-kd-uuid-force-invalid-trigger-20260407f-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-uuid-force-invalid-trigger-20260407f/local-kd-uuid-force-invalid-trigger-20260407f.log`
- `evidence/files/vm-tooling-staging/local-kd-uuid-force-invalid-trigger-20260407f/local-kd-uuid-force-invalid-trigger-20260407f.txt`
- `evidence/files/vm-tooling-staging/local-kd-uuid-force-invalid-trigger-20260407f/local-kd-uuid-force-invalid-trigger-20260407f.trigger.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-uuid-force-invalid-trigger-20260407f/local-kd-uuid-force-invalid-trigger-20260407f.trigger.stderr.txt`

## Short Take
- the direct user-mode UUID allocation APIs are not a winning trigger while the live UUID cache is already valid
- once `ExpUuidSequenceNumberValid` is forced low, the same API burst becomes a reproducible state-transition trigger: the sequence number advances and the validity bit returns to `1`
- this materially narrows the blocker: the missing proof is no longer whether user-mode allocation can drive UUID state, but whether that state transition can be tied to an exact registry read on the working guest
