# Executive UuidSequenceNumber KVM Procmon Runtime Follow-up

Date: 2026-04-06
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the older UUID / RPC / COM burst idea on the live KVM guest with a direct Procmon capture
- test whether a current-build guest surfaces any `Session Manager\Executive` or `UuidSequenceNumber` registry traffic under that trigger family

## Result
- the guest-side helper captured a real Procmon session and exported a CSV with `286763` rows
- the filtered probe summary still reported `MATCH_COUNT=0`
- a host-side keyword scan over the exported CSV also found `0` rows mentioning either `Session Manager\Executive` or `UuidSequenceNumber`
- the working KVM guest reported a live `UuidSequenceNumber` value of `2928393` during the run, which means this pass is runtime-only evidence on the current working guest rather than a replacement for the earlier clean-baseline exports

## Artifacts
- `evidence/files/vm-tooling-staging/uuidsequence-procmon-kvm-20260406a/uuidsequence-procmon-kvm-20260406a.txt`
- `evidence/files/vm-tooling-staging/uuidsequence-procmon-kvm-20260406a/uuidsequence-procmon-kvm-20260406a-summary.json`

## Short Take
- the KVM Procmon lane did not outperform the earlier lightweight ETW no-hit result
- on the current working guest, the UUID / RPC / COM burst still failed to surface any direct runtime read of `UuidSequenceNumber`
