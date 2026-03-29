# power.session-watchdog-timeouts stepwise boot trace follow-up - 2026-03-29

## Summary

- The watchdog boot-trace lane is now migrated to the same explicit step-summary shape used by the CPU idle lane.
- The first propagated run used the canonical excluded baseline `RegProbe-Baseline-20260328`.
- The run isolated its first failing primitive clearly: guest-side ETL creation succeeded, but raw ETL copy-back to the host did not complete.

## Stepwise evidence package

- Summary:
  - `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260329-024632/summary.json`
- Session:
  - `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260329-024632/session.json`
- Step summaries:
  - `step-arm-summary.json`
  - `step-boot-summary.json`
  - `step-stop-summary.json`
  - `step-etl-summary.json`
  - `step-copy-summary.json`

## What the migrated lane proved

- `arm` completed successfully on `RegProbe-Baseline-20260328`
- `boot` completed successfully with a host-driven soft stop/start cycle
- `stop` completed successfully
- `etl` completed successfully and confirmed that the watchdog ETL existed in the guest
- `copy` is the first failing primitive for this migrated lane

## Why this matters

The old monolithic watchdog boot-trace lane could tell us only that the end-to-end run was flaky. The migrated lane now shows where it breaks:

- not at snapshot revert
- not at reboot detection
- not at `WPR stop`
- not at guest-side ETL existence
- but at raw ETL copy-back

That is exactly the propagation goal of the CPU idle stepwise pattern: turn a vague reboot/WPR failure into a named failing primitive that can be fixed independently.

A second rerun on the same excluded baseline also stayed within the stepwise shape but timed out one step earlier, during `stop`. That reinforces the same conclusion: the migrated lane no longer fails as an opaque monolith, and the remaining volatility is now visible at named substeps.
