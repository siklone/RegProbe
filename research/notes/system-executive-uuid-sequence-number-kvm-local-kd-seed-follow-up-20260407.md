# System Executive UuidSequenceNumber KVM Local KD Seed Follow-up

Date: 2026-04-07
Candidate: `system.executive-uuid-sequence-number`
Guest: `regprobe-win11-25h2-session`

## Objective
- inspect nearby UUID seed and Config Manager paths after the direct user-mode allocation burst still produced no delta
- separate true `UuidSequenceNumber` mutation paths from adjacent UUID consumers or privileged seed-management surfaces

## Result
- the host-driven local-KD helper attached successfully and completed without timing out
- `x nt!*Uuid*Seed*` resolved only `nt!ZwSetUuidSeed`, `nt!NtSetUuidSeed`, and `nt!ExpUuidSeedGenericMapping`
- `uf nt!NtSetUuidSeed` showed a privileged setter path that builds SID and ACL state, runs `SeAccessCheck`, updates `nt!ExpUuidCachedValues`, and marks `nt!ExpUuidCacheValid`
- `uf nt!ZwSetUuidSeed` only showed the syscall stub into `KiServiceInternal`, so the meaningful logic is in `NtSetUuidSeed`
- `uf nt!CmpUuidCreate` showed the Config Manager helper as a simple consumer wrapper around `nt!ExUuidCreate`; on `STATUS_RETRY` / `0xC000022D` it sleeps briefly and retries, but it does not expose a separate registry-reader path for `UuidSequenceNumber`

## Artifacts
- `evidence/files/vm-tooling-staging/local-kd-uuid-seed-disasm-20260407e/local-kd-uuid-seed-disasm-20260407e-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-uuid-seed-disasm-20260407e/local-kd-uuid-seed-disasm-20260407e.log`
- `evidence/files/vm-tooling-staging/local-kd-uuid-seed-disasm-20260407e/local-kd-uuid-seed-disasm-20260407e.txt`

## Short Take
- `NtSetUuidSeed` is a high-privilege UUID seed write surface, not the missing runtime-read proof for `Session Manager\\Executive\\UuidSequenceNumber`
- `CmpUuidCreate` is only an adjacent consumer that loops on `ExUuidCreate`; it does not add a new persisted-state reader path
- this narrows the next proof path toward forcing the current-build load path or trapping the actual read side, not toward seed setters or Config Manager wrappers
