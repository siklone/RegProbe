# system.kernel-dpc-watchdog-period current-build follow-up - 2026-04-07

## Summary

- The older `DpcWatchdogPeriod = 120000` review lane is now constrained by current-build KD evidence.
- Dedicated live KVM local-KD proof already read `KeDpcWatchdogPeriodMs = 0` on the running current build.
- A later current-build KD disassembly pass showed:
  - `KeQueryDpcWatchdogConfiguration` only copies `KeDpcWatchdogPeriodMs` into the output block when the global is non-zero
  - `KeUpdateDpcWatchdogConfiguration` is the explicit writer that updates `KeDpcWatchdogPeriodMs` from caller-supplied validated configuration
  - `KiCreateDpcLimitsProcessorConfiguration` then consumes the written global to build the processor-local DPC watchdog limits block
- This means the old app baseline `120000` is now in direct tension with observed current-build kernel state, even though the primary Microsoft registry mapping is still uncaptured.

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/dpc-watchdog-profile-thresholds-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-config-readers-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-config-readers-kd-20260407a/dpc-watchdog-config-readers-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-kd-20260407a/dpc-watchdog-update-config-kd-20260407a.stdout.txt`

## Interpretation

- new proof gained:
  - the period value is no longer only a decompiled-reader breadcrumb
  - the current build exposes both a reader gate and an explicit writer path for `KeDpcWatchdogPeriodMs`
  - live current-build kernel state currently reads `0`, not the repo-app baseline `120000`
- narrowed conclusion:
  - the review blocker is no longer just "primary Microsoft source missing"
  - the app baseline now also conflicts with live current-build kernel state
  - the safest interpretation is that `120000` remains a repo/app hypothesis, not a current-build default
- next proof path:
  - capture live values for the rest of the DPC watchdog control globals
  - determine whether any caller feeds persisted registry data into `KeUpdateDpcWatchdogConfiguration`
