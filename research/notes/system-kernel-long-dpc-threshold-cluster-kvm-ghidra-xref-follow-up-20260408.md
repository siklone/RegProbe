# system.kernel-long-dpc-threshold-cluster KVM Ghidra xref follow-up - 2026-04-08

## Summary

- The long-DPC threshold cluster now has a direct PDB-backed current-build xref for `KiLongDpcQueueThreshold`, not just docs, strings, live KD values, and adjacent ETW lineage.
- A bounded Ghidra export using `sym:KiLongDpcQueueThreshold` returned `status = ok`, `ghidra_exit_code = 0`, and `match_count = 3`.
- All three references were naturally resolved:
  - `KiExecuteAllDpcs`
  - `KeInitSystem`
  - `KeInitSystem`
- This upgrades the queue-threshold side from adjacent consumer lineage to a direct consumer/init xref.
- It does not yet resolve a matching direct current-build read site for `KiLongDpcRuntimeThreshold`.

## Source artifacts

- `evidence/files/vm-tooling-staging/longdpc-queue-threshold-ghidra-20260408b/longdpc-queue-threshold-ghidra-20260408b-summary.json`
- `evidence/files/vm-tooling-staging/longdpc-queue-threshold-ghidra-20260408b/longdpc-queue-threshold-ghidra-20260408b-evidence.json`
- `evidence/files/vm-tooling-staging/longdpc-queue-threshold-ghidra-20260408b/longdpc-queue-threshold-ghidra-20260408b-ghidra-matches.md`
- `evidence/files/vm-tooling-staging/longdpc-queue-threshold-ghidra-20260408b/longdpc-queue-threshold-ghidra-20260408b-launcher-stage.json`
- `evidence/files/vm-tooling-staging/longdpc-queue-threshold-ghidra-20260408b/longdpc-queue-threshold-ghidra-20260408b-symchk.txt`

## Key observations

- The export was symbol-seeded, not string-seeded:
  - `sym:KiLongDpcQueueThreshold`
- `evidence.json` retained three naturally resolved matches:
  - `140255414` in `KiExecuteAllDpcs`
  - `140c60fc2` in `KeInitSystem`
  - `140c60fcb` in `KeInitSystem`
- All three matches reported:
  - `forced_boundary = false`
  - `naturally_resolved = true`
  - `decompile_success = true`
- The summary retained generic wrapper fields such as `error_kind = ghidra-string-xref-error`, but the actual probe outcome was still canonical-success:
  - `status = ok`
  - `ghidra_exit_code = 0`
  - `match_count = 3`

## Interpretation

- new proof gained:
  - `KiLongDpcQueueThreshold` now has a direct current-build xref in both a runtime consumer (`KiExecuteAllDpcs`) and an init path (`KeInitSystem`)
- narrowed conclusion:
  - the old cluster-wide blocker is no longer a generic "no direct threshold read site"
  - the narrower unresolved point is the direct current-build read site for `KiLongDpcRuntimeThreshold`
- still unresolved:
  - whether either `KeInitSystem` reference represents a persisted registry seeding path rather than plain init-time use
  - whether `KiLongDpcRuntimeThreshold` has a matching direct xref or read site that is simply outside this bounded export
  - whether any exact runtime registry read still exists on current builds
- next proof path:
  - pivot KD/Ghidra toward `KiLongDpcRuntimeThreshold`
  - keep `KiExecuteAllDpcs` and `KeInitSystem` as the new direct queue-threshold anchors rather than revisiting ETW-only adjacency
