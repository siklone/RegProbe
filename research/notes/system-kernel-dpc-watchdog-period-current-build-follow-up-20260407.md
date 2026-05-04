# system.kernel-dpc-watchdog-period checked-in-build follow-up - 2026-04-07

## Summary

- The older `DpcWatchdogPeriod = 120000` review lane is now constrained by checked-in-build KD evidence.
- Dedicated live KVM local-KD proof already read `KeDpcWatchdogPeriodMs = 0` on the running checked-in build.
- A later checked-in-build KD disassembly pass showed:
  - `KeQueryDpcWatchdogConfiguration` only copies `KeDpcWatchdogPeriodMs` into the output block when the global is non-zero
  - `KeUpdateDpcWatchdogConfiguration` is the explicit writer that updates `KeDpcWatchdogPeriodMs` from caller-supplied validated configuration
  - `KiCreateDpcLimitsProcessorConfiguration` then consumes the written global to build the processor-local DPC watchdog limits block
- A later class-focused live KD pass narrowed the runtime writer even further:
  - `NtSetSystemInformation+0x1bc9 -> KeUpdateDpcWatchdogConfiguration`
  - the branch is admin-gated via `SeAliasAdminsSid` and `RtlCheckTokenMembership`
  - the surrounding dispatch chain implies numeric system-information arm `0xE4`
- A later PDB-backed Ghidra init-semantics pass also showed that `KiInitDpcThresholds` preserves zero-valued watchdog globals unless they are already non-zero and too small.
- This means the old app baseline `120000` is now in direct tension with observed current-build kernel state, even though the primary Microsoft registry mapping is still uncaptured.

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-thresholds-kd-20260407a/dpc-watchdog-profile-thresholds-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-config-readers-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-config-readers-kd-20260407a/dpc-watchdog-config-readers-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-kd-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-update-config-kd-20260407a/dpc-watchdog-update-config-kd-20260407a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-ntsetsysteminfo-class-kd-20260408a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-ntsetsysteminfo-class-kd-20260408a/dpc-watchdog-ntsetsysteminfo-class-kd-20260408a.log`
- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-xref-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-xref-20260407a/ghidra-matches.md`

## Interpretation

- new proof gained:
  - the period value is no longer only a decompiled-reader breadcrumb
  - the checked-in build exposes both a reader gate and an explicit writer path for `KeDpcWatchdogPeriodMs`
  - the explicit writer now has a live-confirmed privileged `NtSetSystemInformation` caller arm
  - checked-in-build init semantics now explain why a live zero period can survive without being normalized to a non-zero default
  - live checked-in-build kernel state currently reads `0`, not the repo-app baseline `120000`
- narrowed conclusion:
  - the review blocker is no longer just "primary Microsoft source missing"
  - the app baseline now also conflicts with live current-build kernel state and with the current-build init/runtime story
  - the safest interpretation is that `120000` remains a repo/app hypothesis, not a current-build default
- next proof path:
  - determine whether any caller feeds persisted registry data into `KeUpdateDpcWatchdogConfiguration`
  - identify whether inferred runtime arm `0xE4` has a stable public/current-build class name
