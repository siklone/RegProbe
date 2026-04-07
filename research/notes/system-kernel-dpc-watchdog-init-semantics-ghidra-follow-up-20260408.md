# system.kernel-dpc-watchdog init semantics Ghidra follow-up - 2026-04-08

## Summary

- A previously off-repo PDB-backed Ghidra xref run is now canonicalized for the DPC watchdog control/profile family.
- The strongest new structural result is not another registry reader.
- It is the current-build boot/init semantics around the same globals:
  - `KiInitDpcThresholds`
  - `KiInitializeLegacyWatchdogProfileThresholds`
  - `KeInitSystem`
- `KiInitDpcThresholds` shows why the live zero state is not automatically contradictory:
  - `KeDpcWatchdogPeriodMs` is only floored to `2000` when the current value is non-zero but too small
  - `KeDpcTimeoutMs` is only floored to `20` when the current value is non-zero but too small
  - `KeDpcSoftTimeoutMs` and `KeDpcCumulativeSoftTimeoutMs` are only clamped when non-zero
  - zero values can therefore survive this normalization path
- `KiInitializeLegacyWatchdogProfileThresholds` is even narrower:
  - it derives profile thresholds only when the single/cumulative profile globals are sentinel `0xffffffff`
  - it also requires non-zero control globals such as `KeDpcWatchdogProfileOffsetMs`, `KeDpcWatchdogPeriodMs`, and `KeDpcTimeoutMs`
  - if those prerequisites are missing, the legacy derivation path does not seed profile defaults
- This strengthens the current-build interpretation:
  - the mixed live state (`OffsetMs = 10000`, others `0`) no longer has to be read as a failed default load
  - current-build boot/init code explicitly allows zero-valued control/profile globals to persist
  - repo-doc non-zero values still lack a proven persisted registry seeding caller

## Source artifacts

- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-xref-20260407a/summary.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-xref-20260407a/evidence.json`
- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-xref-20260407a/ghidra-matches.md`
- `evidence/files/vm-tooling-staging/dpc-watchdog-profile-xref-20260407a/host-review.json`

## Interpretation

- new proof gained:
  - current-build Ghidra now gives a coherent boot/init semantics chain for the DPC watchdog globals
  - `KiInitDpcThresholds` explains how non-zero low values are normalized without forcing zero values to non-zero defaults
  - `KiInitializeLegacyWatchdogProfileThresholds` explains how legacy sentinel-based profile derivation can exist without implying that current live zero values are wrong
- narrowed conclusion:
  - current-build contradiction lanes for the DPC watchdog cluster are now better explained by initialization semantics rather than by a missing parser or a bad live read
  - the unresolved step remains persisted registry seeding, not whether the globals are structurally real or whether zero can survive boot/init
- next proof path:
  - identify whether any current-build boot/init caller copies persisted `Session Manager\\Kernel` values into the watchdog globals before or instead of the runtime `NtSetSystemInformation` writer path
  - keep the exact `NtSetSystemInformation` writer arm and the boot/init semantics as separate lanes
