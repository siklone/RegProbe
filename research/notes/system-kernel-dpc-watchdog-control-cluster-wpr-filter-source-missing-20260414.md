# system.kernel-dpc-watchdog-control-cluster WPR filter source-missing follow-up - 2026-04-14

## Summary

- A dedicated guest-side filter wrapper now exists for the DPC watchdog control cluster:
  - `scripts/vm/run-dpc-watchdog-control-wpr-filter-guest.ps1`
- The wrapper was executed on the live `regprobe-win11-25h2-session` guest through QGA/`guest-exec`.
- The run completed cleanly, but returned `status = source-missing`.
- The expected source path was:
  - `C:\RegProbe-Diag\wpr-boot-registry\kernel-timing-wpr-boot-registry-20260412\kernel-timing-wpr-boot-registry-20260412.manual.csv`
- On the current guest, that retained `manual.csv` is no longer present.

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-control-wpr-filter-20260414a/dpc-watchdog-control-wpr-filter-20260414a-summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-control-wpr-filter-20260414a/host-review.json`
- `scripts/vm/run-dpc-watchdog-control-wpr-filter-guest.ps1`

## Result

- The new filter wrapper itself is valid and runnable.
- The blocker was not a PowerShell failure or QGA transport failure.
- The blocker was artifact retention: the historical `kernel-timing-wpr-boot-registry-20260412.manual.csv` bundle is no longer on the guest.

## Why this matters

This narrows the last active DPC watchdog control lane further.

The missing proof is not "we do not know how to query the CSV." We now do. The missing proof is that the retained boot-registry source bundle needed for an exact-value filter is no longer available on the working guest.

That means the next useful step is one of these:

- recover the original retained CSV from another host-side stash, if it still exists
- rerun a dedicated boot-registry WPR capture for the control cluster and keep the full CSV
- or bypass the CSV lane entirely with a stronger debugger/decompiler pivot for the persisted seeding caller / exact query arm
