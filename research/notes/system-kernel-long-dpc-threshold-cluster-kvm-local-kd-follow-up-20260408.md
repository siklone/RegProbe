# system.kernel-long-dpc-threshold-cluster KVM local-KD follow-up - 2026-04-08

## Summary

- The long-DPC threshold cluster now has live current-build local-KD state, not just repo docs, baseline absence, and string hits.
- A dedicated wildcard symbol sweep resolved the current-build symbol family:
  - `nt!KiLongDpcQueueThreshold`
  - `nt!KiLongDpcRuntimeThreshold`
  - `nt!KiLongDpcRuntimeThresholdCycles`
  - `nt!KiEnterLongDpcProcessing`
  - `nt!EtwTraceLongDpcDetectionEvent`
  - `nt!EtwTraceLongDpcMitigationEvent`
- A dedicated value pass then returned:
  - `KiLongDpcQueueThreshold = 3`
  - `KiLongDpcRuntimeThreshold = 100`
  - `KiLongDpcRuntimeThresholdCycles = 0x000493e0`

## Source artifacts

- `evidence/files/vm-tooling-staging/local-kd-longdpc-wildcard-20260408a/local-kd-longdpc-wildcard-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-longdpc-wildcard-20260408a/local-kd-longdpc-wildcard-20260408a.log`
- `evidence/files/vm-tooling-staging/local-kd-longdpc-values-20260408a/local-kd-longdpc-values-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-longdpc-values-20260408a/local-kd-longdpc-values-20260408a.log`

## Interpretation

- new proof gained:
  - the repo-doc defaults are no longer docs-only hypotheses
  - current-build live kernel state currently matches the repo-doc values exactly for the two primary globals
  - the long-DPC family is definitely live in the current build, not just a stale string residue
- narrowed conclusion:
  - `LongDpcQueueThreshold = 3` and `LongDpcRuntimeThreshold = 100` are current-build-consistent live globals
  - this removes the old `live-state-unproven` blocker from the cluster
  - `KiLongDpcRuntimeThresholdCycles = 0x493e0` gives an adjacent live-derived runtime representation for the same family
- still unresolved:
  - this pass did not prove a persisted `Session Manager\Kernel` registry reader or seeding caller
  - it also did not yet prove the exact current-build caller that consumes the two threshold globals
- next proof path:
  - use KD or Ghidra xref/disassembly to isolate the direct reader path for `KiLongDpcQueueThreshold` / `KiLongDpcRuntimeThreshold`
  - treat the Procmon lane as export-blocked and the KD lane as the current winning transport for this family
