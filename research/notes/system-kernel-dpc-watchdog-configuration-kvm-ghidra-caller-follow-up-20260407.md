# system.kernel-dpc-watchdog-configuration KVM Ghidra caller follow-up - 2026-04-07

## Summary

- The previously pending PDB-backed Ghidra symbol-xref pass for the DPC watchdog writer lane completed successfully.
- The current-build `KeUpdateDpcWatchdogConfiguration` path is no longer caller-free:
  - `NtSetSystemInformation` references `KeUpdateDpcWatchdogConfiguration`
  - `KeUpdateDpcWatchdogConfiguration` references `KiCreateDpcLimitsProcessorConfiguration`
  - `KiInitializeProcessor` references `KiCreateDpcLimitsProcessorConfiguration`
- This does not prove a persisted registry seeding path.
- It does prove that the writer path is reachable both from a runtime system-information API surface and from processor initialization consumption of the already-built limits block.

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-ghidra-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-ghidra-20260407a/evidence.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-ghidra-20260407a/ghidra-matches.md`
- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-ghidra-20260407a/symchk.txt`

## Interpretation

- new proof gained:
  - `KeUpdateDpcWatchdogConfiguration` is not just an isolated writer; current-build Ghidra now ties it to `NtSetSystemInformation`
  - `KiCreateDpcLimitsProcessorConfiguration` is used both by the explicit writer path and by `KiInitializeProcessor`
- narrowed conclusion:
  - the unresolved question is no longer "is there any caller?"
  - the unresolved question is "is there a persisted registry seeding caller on this build, or only runtime API and initialization consumers?"
- next proof path:
  - keep searching for a boot/init or registry-backed caller above `KeUpdateDpcWatchdogConfiguration`
  - capture live values for the DPC watchdog control globals so the new caller proof can be paired with current-build state
