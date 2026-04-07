# system.kernel-dpc-watchdog-profile-cluster KVM local-KD apply-limits follow-up - 2026-04-07

## Summary

- `KiApplyProcessorDpcLimits` is not the seeding caller for the DPC watchdog profile globals.
- The current-build function:
  - takes a configuration block in `rdx`
  - optionally allocates a buffer when `rdx+0x18` and `rdx+0x1c` differ
  - forwards the request to `KiSetProcessorDpcLimits`
- The function does not directly read `KeDpcWatchdogProfile*` globals and does not perform any registry access in the captured disassembly.

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-apply-limits-kd-20260407b/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-apply-limits-kd-20260407b/dpc-watchdog-apply-limits-kd-20260407b.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-apply-limits-kd-20260407b/host-review.json`

## Interpretation

- new proof gained:
  - `KiApplyProcessorDpcLimits` is downstream from configuration assembly
  - it is not the writer or registry seeding path for `KeDpcWatchdogProfile*`
- narrowed conclusion:
  - the seeding search should move to `KiSetProcessorDpcLimits` or to earlier watchdog init / registry-reader code
  - `KiApplyProcessorDpcLimits` can be treated as a negative control, not an open hypothesis
- next proof path:
  - disassemble `KiSetProcessorDpcLimits`
  - if that still only consumes a config block, pivot to boot/init paths that populate the block before the apply phase
