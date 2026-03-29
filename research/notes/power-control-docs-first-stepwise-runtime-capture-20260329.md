# Power-Control Docs-First Stepwise Runtime Capture (2026-03-29)

- Snapshot: `RegProbe-Baseline-Clean-20260329`
- Capture lane: guest-processed stepwise Procmon boot log
- Shell health: clean before and after reboot
- Stepwise session: `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515`
- Exact-hit candidates: 2/7

## Exact Runtime Reads
- `HibernateEnabled`: exact `RegQueryValue` captured from `smss.exe`.
- `LidReliabilityState`: exact `RegQueryValue` captured from `System`.

## No Exact Runtime Read Yet
- `Class1InitialUnparkCount`: stepwise boot log completed but no exact runtime read was captured on the current clean baseline.
- `HibernateEnabledDefault`: stepwise boot log completed but no exact runtime read was captured on the current clean baseline.
- `MfBufferingThreshold`: stepwise boot log completed but no exact runtime read was captured on the current clean baseline.
- `PerfCalculateActualUtilization`: stepwise boot log completed but no exact runtime read was captured on the current clean baseline.
- `TimerRebaseThresholdOnDripsExit`: stepwise boot log completed but no exact runtime read was captured on the current clean baseline.

## Committed Runtime Artifacts
- Summary: `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/summary.json`
- Results: `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/results.json`
- Exact hits: `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/exact-hits.csv`
- Path hits: `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/path-hits.csv`
- Placeholder: `evidence/files/vm-tooling-staging/power-control-docs-first-stepwise-runtime-20260329-143515/power-control-docs-first-runtime.pml.md`
