# system.kernel-dpc-watchdog NtSetSystemInformation KVM local-KD follow-up - 2026-04-08

## Summary

- The previously inferred watchdog writer caller is now confirmed in live current-build KD output.
- A class-focused local-KD pass landed directly on the call site:
  - `NtSetSystemInformation+0x1bc9 -> KeUpdateDpcWatchdogConfiguration`
- The same call path is privilege-gated:
  - it loads `SeAliasAdminsSid`
  - calls `RtlCheckTokenMembership`
  - only reaches the watchdog writer on the success path
- The surrounding dispatch window is now also visible in the same live pass:
  - `mov eax,0E9h`
  - `mov ecx,ebx`
  - `sub ecx,0E0h`
  - `sub ecx,1`
  - `sub ecx,1`
  - `sub ecx,2`
  - `je NtSetSystemInformation+0x1b95`
- That dispatch arithmetic implies the watchdog writer arm is reached when `ebx == 0xE4`.
- This `0xE4` result is a live-disassembly inference about the numeric system-information arm, not a confirmed public `SYSTEM_INFORMATION_CLASS` name.
- The family now has a privileged runtime syscall writer arm plus a narrowed numeric arm inference, but it still does **not** prove any persisted registry seeding path.

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-ntsetsysteminfo-class-kd-20260408a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-ntsetsysteminfo-class-kd-20260408a/dpc-watchdog-ntsetsysteminfo-class-kd-20260408a.log`
- `evidence/files/vm-tooling-staging/dpc-watchdog-ntsetsysteminfo-class-kd-20260408a/dpc-watchdog-ntsetsysteminfo-class-kd-20260408a.stdout.txt`
- `evidence/files/vm-tooling-staging/dpc-watchdog-ntsetsysteminfo-class-kd-20260408a/host-review.json`

## Interpretation

- new proof gained:
  - the `NtSetSystemInformation -> KeUpdateDpcWatchdogConfiguration` edge is now live-KD confirmed, not just Ghidra xref
  - the writer path is admin-gated by `SeAliasAdminsSid` membership
  - the exact numeric syscall arm is now inferable from the live dispatch chain as `0xE4`
- narrowed conclusion:
  - the watchdog writer clearly belongs to a privileged runtime syscall configuration path
  - the unresolved step is now narrower than before: any persisted registry lineage remains unproven, and the public/current-build class name for inferred arm `0xE4` is still unresolved
- next proof path:
  - identify whether the inferred `0xE4` arm has a stable public/current-build name
  - keep searching for any boot/init or persisted-settings caller above the same writer
