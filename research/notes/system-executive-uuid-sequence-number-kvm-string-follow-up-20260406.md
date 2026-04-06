# System Executive UuidSequenceNumber KVM String Follow-up

Date: 2026-04-06
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the current-build `ntoskrnl.exe` `UuidSequenceNumber` string hit inside the KVM guest with full analysis and PDB staging
- test whether the raw Unicode hit can be upgraded into a direct code reference or bounded function context

## Result
- `symchk.exe` staged the correct `ntkrnlmp.pdb` payload for the current guest build
- Ghidra still found the exact Unicode string at `140038cd8`
- this full-analysis pass recovered `0` direct references for that string inside `ntoskrnl.exe`
- a later rerun with the updated bounded indirect string-reference search still recovered `0` direct or bounded indirect references
- the result is stronger than the earlier `-NoAnalysis` smoke because it confirms that the current-build string exists even after full analysis, but it still does not yield a branch, function, or code path that can support a stronger static claim

## Artifacts
- `evidence/files/ghidra/uuidsequence-string-kvm-20260406d/uuidsequence-string-kvm-20260406d-evidence.json`
- `evidence/files/ghidra/uuidsequence-string-kvm-20260406d/uuidsequence-string-kvm-20260406d-ghidra-matches.md`
- `evidence/files/ghidra/uuidsequence-string-kvm-20260406d/uuidsequence-string-kvm-20260406d-symchk.txt`
- `evidence/files/ghidra/uuidsequence-string-kvm-20260406d/uuidsequence-string-kvm-20260406d-ghidra-run.log`
- `evidence/files/ghidra/uuidsequence-string-kvm-20260406h/uuidsequence-string-kvm-20260406h-evidence.json`
- `evidence/files/ghidra/uuidsequence-string-kvm-20260406h/uuidsequence-string-kvm-20260406h-ghidra-matches.md`
- `evidence/files/ghidra/uuidsequence-string-kvm-20260406h/uuidsequence-string-kvm-20260406h-symchk.txt`
- `evidence/files/ghidra/uuidsequence-string-kvm-20260406h/uuidsequence-string-kvm-20260406h-ghidra-run.log`

## Short Take
- the KVM PDB-backed lane confirms `UuidSequenceNumber` is still present as a current-build ntoskrnl string
- even after replaying with bounded indirect string-reference search, the string is not wired to a recoverable code reference in this pass
- the lane therefore still depends on baseline existence plus adjacent runtime evidence rather than a bounded static branch
