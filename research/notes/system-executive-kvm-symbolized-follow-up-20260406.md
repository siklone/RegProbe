# System Executive KVM Symbolized Follow-up

Date: 2026-04-06
Candidates: `system.executive-uuid-sequence-number`, `system.executive-additional-worker-threads`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the Executive value family with the KVM PDB-backed Ghidra helper
- see whether the old forced-boundary worker-thread artifacts can be upgraded to a bounded symbolized branch result

## Result
- `symchk.exe` staged `ntkrnlmp.pdb` for the current `ntoskrnl.exe` build
- `AdditionalCriticalWorkerThreads` and `AdditionalDelayedWorkerThreads` still landed in exception-adjacent `<no function>` `INT3` blocks, so both remain `string_only_review`
- the bounded export did not recover a useful `UuidSequenceNumber` branch artifact in this pass
- a later address-seeded replay on the same guest showed why the old fallback addresses were misleading: with full analysis and PDB staging, both `140c62b88` and `140c62bb8` naturally resolved inside `IopInitializeSystemDrivers`
- this keeps the Executive record quality anchored to the earlier runtime-backed evidence package; the KVM helper run is a corrective/static follow-up, not a classifier upgrade

## Artifacts
- `evidence/raw/ghidra/executive-worker-uuid-kvm-20260406/executive-worker-uuid-kvm-20260406-evidence.json`
- `evidence/raw/ghidra/executive-worker-uuid-kvm-20260406/executive-worker-uuid-kvm-20260406-ghidra-matches.md`
- `evidence/raw/ghidra/executive-worker-uuid-kvm-20260406/executive-worker-uuid-kvm-20260406-symchk.txt`

## Short Take
- the new KVM helper is working, but the recovered address-seeded result points to `IopInitializeSystemDrivers` rather than a cleaner Executive-specific semantic map
- treat the runtime-backed evidence as primary and stop leaning on the old forced-boundary address pair as static support
