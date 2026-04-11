# system.kernel-dpc-watchdog-profile-cluster KVM local-KD reader follow-up - 2026-04-07

## Summary

- `KeQueryDpcWatchdogConfiguration` is the current-build reader path for the DPC watchdog configuration family.
- `KiValidateDpcWatchdogConfiguration` is the current-build validator/default-fallback path for the same family.
- The function directly reads:
  - `KeDpcWatchdogProfileSingleDpcThresholdMs`
  - `KeDpcWatchdogProfileCumulativeDpcThresholdMs`
  - `KeDpcWatchdogProfileBufferSizeBytes`
  - `KeDpcTimeoutMs`
  - `KeDpcWatchdogPeriodMs`
  - `KeDpcSoftTimeoutMs`
  - `KeDpcCumulativeSoftTimeoutMs`
- For each field, the function does a `test` and only sets the corresponding output bit / copies the value when the global is non-zero.
- `KiValidateDpcWatchdogConfiguration` then backfills missing fields from those same globals and enforces ordering/range constraints:
  - `SingleDpcThreshold <= DpcSoftTimeout or DpcTimeout`
  - `CumulativeDpcThreshold <= DpcCumulativeSoftTimeout or DpcWatchdogPeriod`
  - `BufferSizeBytes <= 0x1FFF`
- `KiUpdateProcessorDpcWatchdogConfiguration` is not the seeding caller; it only tail-calls `KiApplyProcessorDpcLimits`.
- This explains why the live `0` values in the profile cluster do not merely contradict repo docs; they also mean the current-build reader will omit those fields from the returned watchdog configuration.

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/dpc-watchdog-profile-thresholds-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-config-readers-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-config-readers-kd-20260407a/dpc-watchdog-config-readers-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-config-readers-kd-20260407a/host-review.json`

## Interpretation

- new proof gained:
  - the cluster now has a concrete current-build reader path, not just docs defaults and live globals
  - the cluster also has a concrete validator/default-fallback path
  - `KeQueryDpcWatchdogConfiguration` treats zero as "do not emit this field" for both profile and control watchdog globals
  - `KiValidateDpcWatchdogConfiguration` treats the same globals as fallback defaults when the caller does not provide explicit fields
  - the live `0` readings therefore have direct semantic weight in the current build
- narrowed conclusion:
  - `no-current-build-reader-path` is no longer a blocker for the cluster
  - the remaining gap is the seeding/initializer path that explains why most profile globals remain zero while `KeDpcWatchdogProfileOffsetMs` is `10000`
  - `KiUpdateProcessorDpcWatchdogConfiguration` is not that path
- next proof path:
  - inspect `KiApplyProcessorDpcLimits` and adjacent watchdog init paths for writes into the profile globals
  - locate the initializer or registry reader that sets the `KeDpcWatchdogProfile*` globals before `KeQueryDpcWatchdogConfiguration` consumes them
