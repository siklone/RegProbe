# system.kernel-dpc-watchdog NtSetSystemInformation lineage follow-up - 2026-04-08

## Summary

- The new watchdog caller proof does not stand alone anymore.
- Existing deprecated DPC scheduler records already prove that `NtSetSystemInformation` is a real runtime configuration surface for nearby kernel DPC controls:
  - `AdjustDpcThreshold` decompilation shows `KiAdjustDpcThreshold` being populated from the system-information setter buffer and exported back through the query path.
  - `DpcQueueDepth` decompilation shows `KiMaximumDpcQueueDepth` being populated from the system-information setter buffer and exported back through the query path.
- The new current-build Ghidra caller proof now places `KeUpdateDpcWatchdogConfiguration` under the same syscall family by showing a direct reference from `NtSetSystemInformation`.

## Source artifacts

- `research/records/system.kernel-adjust-dpc-threshold.review.json`
- `research/records/system.kernel-dpc-queue-depth.review.json`
- `research/notes/system-kernel-dpc-watchdog-configuration-kvm-ghidra-caller-follow-up-20260407.md`
- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-ghidra-20260407a/ghidra-matches.md`

## Interpretation

- new proof gained:
  - the DPC watchdog writer is no longer just a generic runtime API hypothesis
  - there is already repo-native decompilation evidence that `NtSetSystemInformation` carries adjacent DPC scheduler configuration values
  - the watchdog writer now sits inside the same syscall family by direct current-build xref
- narrowed conclusion:
  - the strongest current interpretation is runtime system-information lineage, not proven persisted registry seeding
  - the unresolved step is no longer whether the writer has a runtime caller; it is whether any persisted-settings path also funnels into that writer on this build
- next proof path:
  - isolate the exact `NtSetSystemInformation` arm / info-class for the watchdog writer path
  - continue searching for a boot/init or persisted-registry caller above the same writer
