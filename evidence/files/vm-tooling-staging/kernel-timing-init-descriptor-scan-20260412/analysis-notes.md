# Kernel Timing INIT Descriptor Scan 2026-04-12

- Purpose: find current-build `ntoskrnl.exe` INIT descriptor rows that bind `Session Manager\Kernel` registry value-name strings to live KD kernel globals.
- Binary: `/tmp/regprobe-kernel-upload/uploads/ntoskrnl.exe`
- Image base: `0x140000000`

## Results
- `TimerCheckFlags` -> `nt!KeTimerCheckFlags`: binding found
  - row `0xbed8d8` / `0xc748d8`; key context `Session Manager\Kernel`
- `ForceBugcheckForDpcWatchdog` -> `nt!KiForceBugcheckForDpcWatchdog`: binding found
  - row `0xbf25b8` / `0xc795b8`; key context `Session Manager\Kernel`
- `GlobalTimerResolutionRequests` -> `nt!KiGlobalTimerResolutionRequests`: binding found
  - row `0xbed488` / `0xc74488`; key context `Session Manager\Kernel`
- `LongDpcQueueThreshold` -> `nt!KiLongDpcQueueThreshold`: binding found
  - row `0xbf2618` / `0xc79618`; key context `Session Manager\Kernel`
- `LongDpcRuntimeThreshold` -> `nt!KiLongDpcRuntimeThreshold`: binding found
  - row `0xbf25e8` / `0xc795e8`; key context `Session Manager\Kernel`
- `DPCTimeout` -> `nt!KeDpcTimeoutMs`: binding found
  - row `0xbed0f8` / `0xc740f8`; key context `Session Manager\Kernel`
- `DpcSoftTimeout` -> `nt!KeDpcSoftTimeoutMs`: binding found
  - row `0xbed128` / `0xc74128`; key context `Session Manager\Kernel`
- `DpcCumulativeSoftTimeout` -> `nt!KeDpcCumulativeSoftTimeoutMs`: binding found
  - row `0xbed158` / `0xc74158`; key context `Session Manager\Kernel`
- `DpcWatchdogProfileBufferSizeBytes` -> `nt!KeDpcWatchdogProfileBufferSizeBytes`: binding found
  - row `0xbed1e8` / `0xc741e8`; key context `Session Manager\Kernel`
- `DpcWatchdogProfileCumulativeDpcThreshold` -> `nt!KeDpcWatchdogProfileCumulativeDpcThresholdMs`: binding found
  - row `0xbed248` / `0xc74248`; key context `Session Manager\Kernel`
- `DpcWatchdogProfileOffset` -> `nt!KeDpcWatchdogProfileOffsetMs`: binding found
  - row `0xbed1b8` / `0xc741b8`; key context `Session Manager\Kernel`
- `DpcWatchdogProfileSingleDpcThreshold` -> `nt!KeDpcWatchdogProfileSingleDpcThresholdMs`: binding found
  - row `0xbed218` / `0xc74218`; key context `Session Manager\Kernel`

## Interpretation

- 12 of 12 target value-name strings are present in the current-build `INIT` section and have a 64-bit pointer reference from an INIT descriptor row.
- Each retained descriptor row points at the same static RVA as the live KD global captured in the earlier VM debugger bundles.
- This strengthens the static registry-seeding/binding layer for the runtime-blocked kernel timing records.
- It does not claim an exact runtime registry read; the post-boot ETW, unseeded boot WPR, and seeded boot WPR lanes still found no exact target value-name hit.
