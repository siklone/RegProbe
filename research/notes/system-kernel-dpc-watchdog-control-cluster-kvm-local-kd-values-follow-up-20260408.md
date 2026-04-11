# system.kernel-dpc-watchdog-control-cluster KVM local-KD values follow-up - 2026-04-08

## Summary

- A dedicated live KVM local-KD value pass now read the current-build DPC watchdog control globals directly.
- All four sampled globals were `0` on the running build:
  - `KeDpcTimeoutMs = 0`
  - `KeDpcSoftTimeoutMs = 0`
  - `KeDpcCumulativeSoftTimeoutMs = 0`
  - control check: `KeDpcWatchdogPeriodMs = 0`
- This removes the last “live values not yet read” gap from the control cluster.
- It also converts the family from a docs-backed hold into a current-build contradiction lane: the repo-doc defaults are non-zero, but the running kernel state is zero across the sampled control globals.

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-control-values-kd-20260408a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-control-values-kd-20260408a/dpc-watchdog-control-values-kd-20260408a.log`
- `evidence/files/vm-tooling-staging/dpc-watchdog-control-values-kd-20260408a/dpc-watchdog-control-values-kd-20260408a.stdout.txt`

## Interpretation

- new proof gained:
  - the three control globals now have direct live current-build value reads
  - the entire sampled control family is currently zero on the running build
  - the zero-state aligns with the previously proven reader gate that only emits these fields when the globals are non-zero
- narrowed conclusion:
  - this family is no longer blocked on missing live values
  - the remaining blocker is narrower: no current-build persisted registry seeding caller has been shown above the explicit writer path
  - repo-doc defaults should not be treated as unconditional live defaults on this build
- next proof path:
  - continue the caller-chain search above `KeUpdateDpcWatchdogConfiguration`
  - look for any boot/init or persisted-settings caller that can explain non-zero values on builds or modes where they are active
