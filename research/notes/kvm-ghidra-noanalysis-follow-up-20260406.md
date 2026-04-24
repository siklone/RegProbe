# KVM Ghidra NoAnalysis Follow-up

Date: 2026-04-06
Guest: `regprobe-win11-25h2-session`

## Objective
- test whether the KVM guest can use `run-ghidra-symbolized-probe.ps1 -NoAnalysis` as a faster fallback when full PDB-backed Ghidra analysis stalls
- distinguish transport and symbol-store health from evidence-grade bounded branch recovery

## Result
- guest-side `symchk.exe` continued to stage the correct `ntkrnlmp.pdb` payload and the helper completed cleanly in `-NoAnalysis` mode
- `ThreadDpcEnable` finished quickly but produced `_No matching strings found_`, which is acceptable as a negative candidate result
- `UuidSequenceNumber` also finished quickly but still produced `_No matching strings found_` even though earlier full-analysis triage had already shown a current-build `ntoskrnl.exe` string hit for that value family
- that mismatch shows the practical limit of this fallback: `-NoAnalysis` is good for transport, symbol-store, and script smoke, but it is not evidence-grade for the repo's string-first bounded branch lane

## Artifacts
- `evidence/raw/ghidra/threaddpcenable-kvm-20260406b/threaddpcenable-kvm-20260406b-evidence.json`
- `evidence/raw/ghidra/threaddpcenable-kvm-20260406b/threaddpcenable-kvm-20260406b-ghidra-matches.md`
- `evidence/raw/ghidra/threaddpcenable-kvm-20260406b/threaddpcenable-kvm-20260406b-symchk.txt`
- `evidence/raw/ghidra/uuidsequence-kvm-20260406b/uuidsequence-kvm-20260406b-evidence.json`
- `evidence/raw/ghidra/uuidsequence-kvm-20260406b/uuidsequence-kvm-20260406b-ghidra-matches.md`
- `evidence/raw/ghidra/uuidsequence-kvm-20260406b/uuidsequence-kvm-20260406b-symchk.txt`

## Short Take
- keep `-NoAnalysis` as a smoke fallback only
- use full analysis or a debugger/address-seeded lane for any static claim that depends on recovered strings, function identity, or bounded branch semantics
