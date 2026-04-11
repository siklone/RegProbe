# system.kernel-long-dpc-threshold-cluster KVM local-KD consumer follow-up - 2026-04-08

## Summary

- The long-DPC threshold cluster now has a bounded live consumer-lineage proof, not just docs, string hits, live globals, and Procmon export blockers.
- A full-function KVM local-KD pass disassembled:
  - `nt!KiEnterLongDpcProcessing`
  - `nt!EtwTraceLongDpcDetectionEvent`
  - `nt!EtwTraceLongDpcMitigationEvent`
- The retained current-build bundle showed a direct call from `KiEnterLongDpcProcessing` into `EtwTraceLongDpcMitigationEvent`.
- The same retained bundle did not expose a direct symbolized read of:
  - `KiLongDpcQueueThreshold`
  - `KiLongDpcRuntimeThreshold`
  - `KiLongDpcRuntimeThresholdCycles`

## Source artifacts

- `evidence/files/vm-tooling-staging/local-kd-longdpc-consumer-20260408a/local-kd-longdpc-consumer-20260408a-summary.json`
- `evidence/files/vm-tooling-staging/local-kd-longdpc-consumer-20260408a/local-kd-longdpc-consumer-20260408a.log`
- `evidence/files/vm-tooling-staging/local-kd-longdpc-consumer-20260408a/local-kd-longdpc-consumer-20260408a.stdout.txt`
- `evidence/files/vm-tooling-staging/local-kd-longdpc-consumer-20260408a/local-kd-longdpc-consumer-20260408a.stderr.txt`
- `evidence/files/vm-tooling-staging/local-kd-longdpc-consumer-20260408a/local-kd-longdpc-consumer-20260408a.txt`

## Key observations

- `KiEnterLongDpcProcessing` contains a concrete live callsite:
  - `fffff806\`e7098e6e call nt!EtwTraceLongDpcMitigationEvent`
- `EtwTraceLongDpcDetectionEvent` behaves like a thin ETW wrapper and funnels into `EtwTraceKernelEvent`.
- `EtwTraceLongDpcMitigationEvent` reads caller-supplied `dword ptr [rcx+510h]` and optional `dword ptr [rdx+510h]` fields before packaging the ETW event.
- No retained line in the canonical bundle resolved a direct operand against `KiLongDpcQueueThreshold`, `KiLongDpcRuntimeThreshold`, or `KiLongDpcRuntimeThresholdCycles`.

## Interpretation

- new proof gained:
  - the long-DPC family now has a concrete current-build consumer lineage anchored in `KiEnterLongDpcProcessing`
  - the retained ETW helper path is adjacent telemetry packaging, not obviously the threshold-reader itself
- narrowed conclusion:
  - the old blocker is no longer a generic "no current-build caller path"
  - the tighter blocker is "no direct threshold read site yet"
- still unresolved:
  - the exact current-build instruction or helper that reads the `KiLongDpc*` globals
  - whether any persisted `Session Manager\Kernel` seeding path still exists upstream of that direct reader
- next proof path:
  - keep KD/Ghidra focused on the direct threshold read site rather than the already-resolved ETW mitigation wrapper
  - keep Procmon framed as export-blocked rather than unattempted for this family
