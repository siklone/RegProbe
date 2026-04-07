# System Executive UuidSequenceNumber KVM Regstate Force-Invalid Follow-up

Date: 2026-04-07
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- test whether the same forced-invalid UUID trigger that moved live kernel state also moves the persisted `UuidSequenceNumber` registry value
- separate a pure in-memory cache refresh from a save path that reaches the Session Manager Executive registry value itself

## Result
- the guest-side trigger summary completed and uploaded successfully
- the summary observed `HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\UuidSequenceNumber` move from `0x2caf1d` to `0x2caf1e` (`delta = +1`) during the trigger window
- the paired host-driven local-KD wrapper for this same attempt did not upload its final bundle, so this run is recorded as corroborating registry-side movement rather than as a same-run kernel-plus-registry composite proof

## Artifacts
- `evidence/files/vm-tooling-staging/uuidsequence-regstate-kvm-force-invalid-20260407h/uuidsequence-regstate-kvm-force-invalid-20260407h-summary.json`

## Short Take
- under the forced-invalid UUID hypothesis, the persisted `UuidSequenceNumber` value can move by `+1` in the same trigger window
- that makes the lane more specific: the remaining gap is exact observation of the read side, not whether the trigger can reach stored state at all
- because the paired local-KD wrapper stalled before uploading its final bundle, this follow-up should be treated as strong corroboration rather than the definitive same-run proof
