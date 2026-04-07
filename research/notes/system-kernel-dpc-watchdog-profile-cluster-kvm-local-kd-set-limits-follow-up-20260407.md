# system.kernel-dpc-watchdog-profile-cluster KVM local-KD set-limits follow-up - 2026-04-07

## Summary

- `KiSetProcessorDpcLimits` is not the seeding caller for the DPC watchdog profile globals.
- The current-build function writes fields from an input configuration block (`r14`) into per-processor state and swaps the auxiliary profile buffer pointer when needed.
- The function does not directly read `KeDpcWatchdogProfile*` globals and does not perform any registry access in the captured disassembly.

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-set-limits-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-set-limits-kd-20260407a/dpc-watchdog-set-limits-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-set-limits-kd-20260407a/host-review.json`

## Interpretation

- new proof gained:
  - `KiSetProcessorDpcLimits` is downstream apply logic, not a global/registry seeding caller
  - the function consumes a prepared block and writes processor-local watchdog state
- narrowed conclusion:
  - the seeding search is now earlier than `KiValidateDpcWatchdogConfiguration`, `KiApplyProcessorDpcLimits`, and `KiSetProcessorDpcLimits`
  - the next credible search zone is boot/init watchdog configuration assembly, not the apply chain
- next proof path:
  - identify the earlier function that constructs the input configuration block before validation/apply
  - if Ghidra symbol-xref still stalls, use narrower KD symbol chain or address-seeded disassembly against the boot/init watchdog path
