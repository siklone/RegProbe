# system.kernel-dpc-watchdog-profile-cluster KVM local-KD update-config follow-up - 2026-04-07

## Summary

- `KeUpdateDpcWatchdogConfiguration` is the current-build writer path for the DPC watchdog globals.
- The function:
  - acquires `KiDpcWatchdogConfigurationLock`
  - calls `KiValidateDpcWatchdogConfiguration`
  - uses the returned bitmask to selectively write validated values into:
    - `KeDpcTimeoutMs`
    - `KeDpcWatchdogPeriodMs`
    - `KeDpcSoftTimeoutMs`
    - `KeDpcCumulativeSoftTimeoutMs`
    - `KeDpcWatchdogProfileSingleDpcThresholdMs`
    - `KeDpcWatchdogProfileCumulativeDpcThresholdMs`
    - `KeDpcWatchdogProfileBufferSizeBytes`
- `KeDpcWatchdogProfileBufferSizeBytes` has an additional current-build fallback:
  - if the caller does not provide a new buffer size and both profile thresholds remain `0`, the function preserves `0`
  - if the caller does not provide a new buffer size but one of the profile thresholds is non-zero, the function synthesizes `0x41000`
- After writing the globals, the function builds a processor configuration block with `KiCreateDpcLimitsProcessorConfiguration` and fans it out through `KeGenericProcessorCallback(KiUpdateProcessorDpcWatchdogConfiguration)`.
- `KiCreateDpcLimitsProcessorConfiguration` is a builder, not a registry reader:
  - it converts the already-written global millisecond values into tick-based per-processor limits
  - it does not read the registry

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-kd-20260407a/dpc-watchdog-update-config-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-kd-20260407a/host-review.json`

## Interpretation

- new proof gained:
  - the lane now has an explicit current-build writer path, not just a reader/validator path
  - `KiCreateDpcLimitsProcessorConfiguration` explains how the written globals are transformed into the per-processor configuration block used by the apply chain
  - the live `0` profile values remain semantically meaningful because the writer preserves them unless a caller provides non-zero validated input
- narrowed conclusion:
  - `KeUpdateDpcWatchdogConfiguration` is the first confirmed writer for the family
  - but it is still not a registry seeding proof because it consumes caller-supplied validated input
  - the remaining search target is now the caller that might feed persisted `Session Manager\\Kernel` values into `KeUpdateDpcWatchdogConfiguration`
- next proof path:
  - identify current-build callers of `KeUpdateDpcWatchdogConfiguration`
  - determine whether any of those callers run at boot/init and source values from the registry rather than from a runtime API surface
