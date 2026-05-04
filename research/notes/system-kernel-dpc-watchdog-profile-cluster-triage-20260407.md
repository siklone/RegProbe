# system.kernel-dpc-watchdog-profile-cluster triage - 2026-04-07

## Summary

- The `Session Manager\Kernel` `DpcWatchdogProfile*` family is now a live-structural draft, not just a docs-first list.
- Repo docs explicitly list:
  - `DpcWatchdogProfileBufferSizeBytes = 266240`
  - `DpcWatchdogProfileCumulativeDpcThreshold = 110000`
  - `DpcWatchdogProfileOffset = 10000`
  - `DpcWatchdogProfileSingleDpcThreshold = 18333`
- A dedicated live KVM local-KD bundle resolved the current-build globals:
  - `KeDpcWatchdogProfileOffsetMs = 10000`
  - `KeDpcWatchdogProfileSingleDpcThresholdMs = 0`
  - `KeDpcWatchdogProfileCumulativeDpcThresholdMs = 0`
  - `KeDpcWatchdogProfileBufferSizeBytes = 0`
- The same KD pass also read the control global `KeDpcWatchdogPeriodMs = 0`, which keeps the older `DpcWatchdogPeriod` review lane relevant as a contradiction/control rather than as an exact live-default proof.

## Source artifacts

- `Docs/system/system.md`
- `research/records/system.kernel-dpc-watchdog-period.review.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/dpc-watchdog-profile-thresholds-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/host-review.json`

## Interpretation

- new proof gained:
  - the checked-in build still exports a coherent `KeDpcWatchdogProfile*` symbol cluster
  - `KeDpcWatchdogProfileOffsetMs` matches the repo-doc default of `10000`
  - the other profile globals are live `0`, not their repo-doc non-zero defaults
  - the nearby control global `KeDpcWatchdogPeriodMs` is also live `0`
- narrowed conclusion:
  - this family is real and active enough for a schema-backed draft
  - the repo-doc defaults can no longer be treated as unconditional checked-in-build live defaults
  - the family now needs a reader/initializer proof more than another broad string pass
- next proof path:
  - locate the checked-in-build initializer or reader for `KeDpcWatchdogProfile*`
  - determine whether the zero values are conditional, late-initialized, or evidence that the repo-doc defaults are speculative for the present build
