# system.kernel-dpc-watchdog-control-cluster triage - 2026-04-07

## Summary

- The `Session Manager\Kernel` DPC watchdog control timeout family is now strong enough for a schema-backed draft cluster.
- Repo docs explicitly list:
  - `DPCTimeout = 20000`
  - `DpcSoftTimeout = 20000`
  - `DpcCumulativeSoftTimeout = 120000`
- Current-build KVM local-KD disassembly already proves that these globals are not dead names:
  - `KeQueryDpcWatchdogConfiguration` reads `KeDpcTimeoutMs`, `KeDpcSoftTimeoutMs`, and `KeDpcCumulativeSoftTimeoutMs`
  - `KiValidateDpcWatchdogConfiguration` backfills missing fields from the same globals and applies ordering/range checks
  - `KeUpdateDpcWatchdogConfiguration` is the explicit writer that updates those globals from caller-supplied validated input
- This is stronger than a docs-only hold, but still weaker than the profile cluster because no dedicated live current-build read of these three globals exists yet and no caller has been shown to feed persisted registry data into the writer path.

## Source artifacts

- `Docs/system/system.md`
- `research/notes/system-kernel-dpc-watchdog-profile-cluster-kvm-local-kd-reader-follow-up-20260407.md`
- `research/notes/system-kernel-dpc-watchdog-profile-cluster-kvm-local-kd-update-config-follow-up-20260407.md`
- `evidence/files/vm-tooling-staging/dpc-watchdog-config-readers-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-config-readers-kd-20260407a/dpc-watchdog-config-readers-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-kd-20260407a/dpc-watchdog-update-config-kd-20260407a.stdout.txt`

## Interpretation

- new proof gained:
  - the three timeout names now have a concrete current-build reader path
  - the same family now has an explicit current-build writer path
  - the cluster is no longer just repo-doc folklore
- narrowed conclusion:
  - this is a real DPC watchdog control family
  - it should move into a draft cluster record
  - it still is not ready for validation or tweak exposure because the live current-build values for these three globals have not been directly read and the persisted registry seeding caller remains unknown
- next proof path:
  - capture dedicated live values for `KeDpcTimeoutMs`, `KeDpcSoftTimeoutMs`, and `KeDpcCumulativeSoftTimeoutMs`
  - continue the caller-chain search above `KeUpdateDpcWatchdogConfiguration`
