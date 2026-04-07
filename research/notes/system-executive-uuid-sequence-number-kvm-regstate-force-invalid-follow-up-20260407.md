# System Executive UuidSequenceNumber KVM Regstate Force-Invalid Follow-up

Date: 2026-04-07
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- test whether the same forced-invalid UUID trigger that moved live kernel state also moves the persisted `UuidSequenceNumber` registry value
- separate a pure in-memory cache refresh from a save path that reaches the Session Manager Executive registry value itself

## Result
- the guest-side trigger summary completed and uploaded successfully
- the paired host-driven local-KD wrapper also completed and uploaded its final bundle
- the same trigger window observed `HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\UuidSequenceNumber` move from `0x2caf1d` to `0x2caf1e` (`delta = +1`)
- the same local-KD bundle observed `ExpUuidSequenceNumber` move from `0x002caf1d` to `0x002caf1e`, `ExpUuidSequenceNumberValid` flip `1 -> 0 -> 1`, and `ExpUuidSequenceNumberNotSaved` remain `0`

## Artifacts
- `evidence/files/vm-tooling-staging/uuidsequence-regstate-kvm-force-invalid-20260407h/uuidsequence-regstate-kvm-force-invalid-20260407h-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-uuid-force-invalid-regstate-20260407h/local-kd-uuid-force-invalid-regstate-20260407h-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-uuid-force-invalid-regstate-20260407h/local-kd-uuid-force-invalid-regstate-20260407h.log`
- `evidence/files/vm-tooling-staging/local-kd-uuid-force-invalid-regstate-20260407h/local-kd-uuid-force-invalid-regstate-20260407h.txt`

## Short Take
- under the forced-invalid UUID hypothesis, the same trigger window now shows both the live kernel state and the persisted `UuidSequenceNumber` value moving by `+1`
- that upgrades the lane from “state-transition trigger” to “same-run kernel-plus-registry state-transition proof”
- the remaining gap is now narrower: exact observation of the read side itself, not whether the trigger can reach stored state
