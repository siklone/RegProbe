# System Executive KVM Address-Seeded Follow-up

Date: 2026-04-06
Candidates: `system.executive-additional-worker-threads`
Guest: `regprobe-win11-25h2-session`

## Objective
- replay the old Executive worker-thread fallback addresses directly instead of relying on string discovery
- verify whether the KVM guest can preserve a reviewable block around the historical forced-boundary sites
- see whether full analysis plus PDB staging can turn those sites into real function identities

## Result
- pass `executive-worker-addrseed-kvm-20260406b` reopened both historical fallback addresses: `140c62b88` and `140c62bb8`
- that first `-NoAnalysis` pass still showed forced-boundary unresolved blocks, including a truncated `halt_baddata()` stub at `140c62b88`
- pass `executive-worker-addrseed-kvm-20260406c` then staged `ntkrnlmp.pdb`, ran full analysis, and naturally resolved both addresses inside `IopInitializeSystemDrivers`
- the full-analysis result removed the old forced-boundary ambiguity: both seeds now point into the same driver-initialization function rather than to an Executive-specific helper
- this upgrades the method, but it weakens those old addresses as semantic support for the worker-thread pair

## Artifacts
- `evidence/files/ghidra/executive-worker-addrseed-kvm-20260406b/executive-worker-addrseed-kvm-20260406b-evidence.json`
- `evidence/files/ghidra/executive-worker-addrseed-kvm-20260406b/executive-worker-addrseed-kvm-20260406b-ghidra-matches.md`
- `evidence/files/ghidra/executive-worker-addrseed-kvm-20260406b/executive-worker-addrseed-kvm-20260406b-ghidra-run.log`
- `evidence/files/ghidra/executive-worker-addrseed-kvm-20260406c/executive-worker-addrseed-kvm-20260406c-evidence.json`
- `evidence/files/ghidra/executive-worker-addrseed-kvm-20260406c/executive-worker-addrseed-kvm-20260406c-ghidra-matches.md`
- `evidence/files/ghidra/executive-worker-addrseed-kvm-20260406c/executive-worker-addrseed-kvm-20260406c-ghidra-run.log`
- `evidence/files/ghidra/executive-worker-addrseed-kvm-20260406c/executive-worker-addrseed-kvm-20260406c-symchk.txt`

## Short Take
- address seeds were the right recovery tool for this lane
- once full analysis and PDB staging were applied, the old fallback blocks resolved to `IopInitializeSystemDrivers`, so `140c62b88` and `140c62bb8` should no longer be treated as Executive-specific semantic evidence
