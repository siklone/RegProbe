# power.session-watchdog-timeouts stepwise boot trace follow-up - 2026-03-29

## Summary

- The watchdog boot-trace lane is now migrated to the same explicit step-summary shape used by the CPU idle lane.
- A final native run on the canonical excluded baseline `RegProbe-Baseline-20260328` completed all visible steps and produced a full `summary.json` plus `session.json` package.
- The lane still exposes copy-back nuance clearly: the bounded copy helper timed out, but the host ETL still materialized, so the step is now treated as degraded rather than opaque failure.

## Final native stepwise package

- Summary:
  - `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260329-083642/summary.json`
- Session:
  - `evidence/files/vm-tooling-staging/watchdog-timeouts-boottrace-20260329-083642/session.json`
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
- `copy` completed in a degraded form: the bounded helper reported a timeout, but the host ETL still existed by the time the step summary was written

## Why this matters

The old monolithic watchdog boot-trace lane could tell us only that the end-to-end run was flaky. The migrated lane now shows what each primitive did:

- not at snapshot revert
- not at reboot detection
- not at `WPR stop`
- not at guest-side ETL existence
- and not at opaque ETL loss
- but at a bounded, inspectable copy-back edge case when the raw host ETL takes too long to acknowledge completion

That is exactly the propagation goal of the CPU idle stepwise pattern: turn a vague reboot/WPR failure into named primitives that can be fixed independently.

Earlier propagated attempts at `watchdog-timeouts-boottrace-20260329-024632` and `watchdog-timeouts-boottrace-20260329-031816` are still useful historical evidence because they exposed the same lane before the longer host timeout was allowed. The final native run closes that gap and confirms that the stepwise shape itself is sound on the excluded baseline.
