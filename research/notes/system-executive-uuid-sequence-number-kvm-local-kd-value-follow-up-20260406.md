# System Executive UuidSequenceNumber KVM Local KD Value Follow-up

Date: 2026-04-06
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- extend the KVM local-KD lane from path discovery into live current-build UUID state inspection
- check whether the running guest exposes a populated `ExpUuidSequenceNumber` state together with coherent validity and dirty-state flags

## Result
- the host-driven local-KD helper attached successfully to the live guest kernel and completed without timing out
- `dd nt!ExpUuidSequenceNumber L1` returned `0x002caf0c` (`2928396` decimal) from the running guest kernel
- `db nt!ExpUuidSequenceNumberValid L1` returned `0x01`
- `db nt!ExpUuidSequenceNumberNotSaved L1` returned `0x00`
- the earlier KVM Procmon runtime replay had observed a live registry-side `UuidSequenceNumber` value of `2928393`, so the later local-KD in-memory state is only `+3` higher on the same working guest

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-uuid-values-20260406b/local-kd-uuid-values-20260406b-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-uuid-values-20260406b/local-kd-uuid-values-20260406b.log`

## Short Take
- KVM local KD now shows that the live current-build UUID state is populated, marked valid, and not pending save on the working guest
- the small delta from the earlier guest-side registry sample fits an actively changing UUID state rather than a stale baseline constant
- this still does not count as a simultaneous direct registry-read proof, so the lane remains gated by `runtime_no_read`
