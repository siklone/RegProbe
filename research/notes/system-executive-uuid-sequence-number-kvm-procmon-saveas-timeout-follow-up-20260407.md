# System Executive UuidSequenceNumber KVM Procmon SaveAs Timeout Follow-up

Date: 2026-04-07
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- tighten the remaining direct-read gap with a bounded exact-read Procmon lane
- separate trigger ambiguity from Procmon export failure on the working KVM guest

## Result
- the KVM guest runner now exposes deterministic failure details even when the guest wrapper summary does not upload
- a generic control replay (`u6`) used the intended `Session Manager\Executive` path and value name with a trivial `Write-Output ok` trigger
- a candidate-specific replay (`u7`) reused the stronger direct UUID API burst built from repeated `UuidCreateSequential` plus `NtAllocateUuids` calls
- both runs reached `probe-stage = exception` and recorded the same terminal error: `Procmon SaveAs timed out after 60 second(s).`
- both runs produced a `.txt` result artifact and no `.csv` or `.hits.csv`
- the host runner synthesized a fallback summary from `probe-stage.json + .txt` for the UUID-specific replay, so the lane now fails as an explicit export blocker instead of a silent host-side timeout

## Artifacts
- generic control replay:
  - `evidence/files/vm-tooling-staging/procmon-saveas-timeout-kvm-20260407u6/u6-summary.json`
  - `evidence/files/vm-tooling-staging/procmon-saveas-timeout-kvm-20260407u6/u6-probe-stage.json`
  - `evidence/files/vm-tooling-staging/procmon-saveas-timeout-kvm-20260407u6/u6.txt`
  - `evidence/files/vm-tooling-staging/procmon-saveas-timeout-kvm-20260407u6/host-review.json`
- UUID-specific replay:
  - `evidence/files/vm-tooling-staging/uuidsequence-procmon-saveas-timeout-kvm-20260407u7/u7-summary.json`
  - `evidence/files/vm-tooling-staging/uuidsequence-procmon-saveas-timeout-kvm-20260407u7/u7-probe-stage.json`
  - `evidence/files/vm-tooling-staging/uuidsequence-procmon-saveas-timeout-kvm-20260407u7/u7.txt`
  - `evidence/files/vm-tooling-staging/uuidsequence-procmon-saveas-timeout-kvm-20260407u7/host-review.json`

## Short Take
- the remaining UUID gap is no longer “find a stronger obvious user-mode trigger”
- the stronger trigger already exists and still reaches the same Procmon export wall as the trivial control replay
- on this working KVM guest, the exact-read Procmon lane is currently blocked by `Procmon SaveAs`, not by trigger identity, shell state, or summary plumbing ambiguity
- the lane still needs direct read observation, but the next proof path should branch away from Procmon export rather than retrying the same transport blindly
