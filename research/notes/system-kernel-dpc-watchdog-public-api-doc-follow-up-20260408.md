# system.kernel-dpc-watchdog public API doc follow-up - 2026-04-08

## Summary

- Microsoft still publishes the public kernel DPC watchdog query API:
  - `KeQueryDpcWatchdogInformation`
- Microsoft also still publishes the output structure:
  - `KDPC_WATCHDOG_INFORMATION`
- The most useful current-build research consequence is not the function prototype by itself.
- It is the documented zero semantics in the structure:
  - `DpcTimeLimit = 0` when DPC time-out has been disabled
  - `DpcWatchdogLimit = 0` when the DPC watchdog has been disabled
- This does **not** prove the `Session Manager\Kernel` registry reader.
- It does tighten the interpretation of the live zero-valued control cluster:
  - `KeDpcTimeoutMs = 0`
  - `KeDpcSoftTimeoutMs = 0`
  - `KeDpcCumulativeSoftTimeoutMs = 0`
  - `KeDpcWatchdogPeriodMs = 0`
- The current-build zero state is no longer only a conflict against repo-doc defaults.
- It is also compatible with a documented watchdog-disabled or timeout-disabled public API surface.

## Source artifacts

- `https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-kequerydpcwatchdoginformation`
- `https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_kdpc_watchdog_information`
- `research/records/system.kernel-dpc-watchdog-control-cluster.json`
- `research/records/system.kernel-dpc-watchdog-period.review.json`

## Interpretation

- new proof gained:
  - Microsoft officially documents a current kernel DPC watchdog query function and its result structure
  - the structure explicitly assigns zero values to disabled watchdog/timeout states
- narrowed conclusion:
  - the live zero control cluster is no longer just "repo-doc contradiction"
  - it is now "repo-doc contradiction plus public disabled-state-compatible semantics"
  - this still does not identify the exact `ExpQuerySystemInformation` query arm or any persisted registry seeding caller
- next proof path:
  - determine whether the current-build `KeQueryDpcWatchdogConfiguration` / `KeQueryDpcWatchdogInformation` path is the actual source of the public zero semantics
  - keep query-arm isolation and registry-seeding search as the remaining hard blockers
