# Policy System EnableVirtualization KVM Symbolized Follow-up

Date: 2026-04-06
Candidate: `policy.system.enable-virtualization`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the `EnableVirtualization` policy family in the Linux KVM guest with staged Microsoft symbols
- check whether the 25H2 guest can recover a bounded compare-and-jump path instead of a plain string hit

## Result
- `symchk.exe` staged `ntkrnlmp.pdb` for the current `ntoskrnl.exe` build
- the bounded Ghidra export resolved all observed matches to `PsBootPhaseComplete` with `function_source = pdb-symbol`
- the family remained `string_only_review` only: `EnableLUA`, `EnableVirtualization`, and `EnableInstallerDetection` all stayed in stack-relative setup context without a surviving bounded compare/jump pair
- the KVM pass improves function identity, but it does not remove the existing policy-family ambiguity or the runtime no-hit gate

## Artifacts
- `evidence/files/ghidra/enablevirtualization-kvm-20260406/enablevirtualization-kvm-20260406-evidence.json`
- `evidence/files/ghidra/enablevirtualization-kvm-20260406/enablevirtualization-kvm-20260406-ghidra-matches.md`
- `evidence/files/ghidra/enablevirtualization-kvm-20260406/enablevirtualization-kvm-20260406-symchk.txt`

## Short Take
- this is a stronger static context package than the old raw string hit
- it still does not justify lifting `policy.system.enable-virtualization` beyond the existing Class B / review-only posture
