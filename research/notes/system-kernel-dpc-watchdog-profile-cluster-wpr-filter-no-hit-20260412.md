# system.kernel-dpc-watchdog-profile-cluster WPR no-hit filter

Collected: 2026-04-12

## Scope

This follow-up reused the retained WPR boot registry CSV from `kernel-timing-wpr-boot-registry-20260412` and filtered it for the four DPC watchdog profile value names:

- `DpcWatchdogProfileBufferSizeBytes`
- `DpcWatchdogProfileCumulativeDpcThreshold`
- `DpcWatchdogProfileOffset`
- `DpcWatchdogProfileSingleDpcThreshold`

The source CSV was `8248730751` bytes and had already been shown to preserve `Session Manager\Kernel` key opens plus adjacent `QueryValue` rows such as `DefaultHeteroCpuPolicy`.

## Result

The targeted filter completed successfully and found zero matching lines for all four DPC watchdog profile value names.

## Interpretation

This is target-specific negative runtime evidence. It does not prove the profile values are never read, but it does show the retained WPR boot CSV did not contain exact rendered value-name hits for the DPC watchdog profile cluster even though that same CSV format can preserve adjacent value-name reads under the same key family.

The exact runtime-read gap therefore remains open and is narrower: the next attempt needs either a different trigger, a more targeted boot/init trace, or KD/static identification of the descriptor consumer.

## Artifacts

Bundle: `evidence/files/vm-tooling-staging/dpc-watchdog-profile-wpr-filter-20260412a/`

- `dpc-watchdog-profile-wpr-filter-20260412a-summary.json`
- `dpc-watchdog-profile-wpr-filter-20260412a.hits.txt`
- `host-review.json`
