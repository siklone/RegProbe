# system.kernel-dpc-watchdog-control-cluster WPR boot no-hit follow-up - 2026-04-14

## Summary

- A fresh current-build boot-registry WPR replay was launched through the working QGA lane for the exact `Session Manager\Kernel` DPC watchdog control cluster.
- The replay produced:
  - `dpc-watchdog-control-boot-registry-20260414a.etl` at `1759510528` bytes
  - `dpc-watchdog-control-boot-registry-20260414a.csv` at `5232294334` bytes
- The broad guest normalizer stalled on the very large CSV, so a dedicated exact-value filter wrapper was run against that retained CSV.
- The filter returned zero exact hits for all three target value names:
  - `DPCTimeout`
  - `DpcSoftTimeout`
  - `DpcCumulativeSoftTimeout`

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-control-wpr-filter-20260414b/dpc-watchdog-control-wpr-filter-20260414b-summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-control-wpr-filter-20260414b/dpc-watchdog-control-wpr-filter-20260414b.hits.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-control-wpr-filter-20260414b/host-review.json`
- `scripts/vm/run-dpc-watchdog-control-wpr-filter-guest.ps1`

## Result

This closes the old source-retention excuse for the lane. We now have a fresh current-build boot-registry replay and a retained exact-value filter result, and that result is still a no-hit.

The remaining gap is therefore narrower than before:

- no exact current-build registry read for the three control values
- no named persisted seeding caller above the explicit writer/query family
- no primary Microsoft source for the internal contract

## Why this matters

This no longer looks like a setup problem. The QGA transport worked, the guest rebooted, the boot trace was retained, and the exact-value filter completed. The current environment is now repeating bounded no-hit outcomes for the control cluster, which is strong enough to move the record from active chase to intentional hold until a stronger current-build pivot appears.
