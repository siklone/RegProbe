# Kernel/Power Existing-Value Static Triage

Date: 2026-03-28

Source files:

- `registry-research-framework/audit/kernel-power-net-new-follow-up-20260328.json`
- `registry-research-framework/audit/kernel-power-existing-static-probe-20260328.json`
- `evidence/files/vm-tooling-staging/registry-batch-string-20260328-003236/summary.json`
- `evidence/files/ghidra/kernel-power-existing-ntoskrnl/ghidra-matches.md`
- `research/notes/kernel-power-96-key-routing-20260327.md`

## Goal

The existence-first batch proved that `9` of Hooke's net-new values already exist on the clean baseline. This pass narrows that set before any heavy runtime lane or record creation.

Questions answered here:

- do exact repo-doc hits already exist?
- do the value names appear in likely binaries?
- if a value name appears in `ntoskrnl.exe`, does Ghidra give us a usable xref or a collision?

## Repo-doc reality

Exact repo-doc coverage for these `9` values is still effectively zero outside the new intake notes. The current `Docs/power/power.md` and `Docs/system/system.md` files contain useful family-level background, but not exact current repo documentation for:

- `CustomizeDuringSetup`
- `SourceSettingsVersion`
- `PowerSettingProfile`
- `WatchdogResumeTimeout`
- `WatchdogSleepTimeout`
- `AdditionalCriticalWorkerThreads`
- `AdditionalDelayedWorkerThreads`
- `UuidSequenceNumber`
- `AllowRemoteDASD`

That means the next routing decision should lean on static binary evidence first, not on repo-doc confidence.

## Static string probe

Batch evidence:

- summary: `evidence/files/vm-tooling-staging/registry-batch-string-20260328-003236/summary.json`
- JSON: `evidence/files/vm-tooling-staging/registry-batch-string-20260328-003236/results.json`
- CSV: `evidence/files/vm-tooling-staging/registry-batch-string-20260328-003236/results.csv`

Result:

- total candidates: `9`
- candidates with exact binary hits: `6`
- candidates without hits: `3`
- shell impact: none

Exact-hit set:

- `WatchdogResumeTimeout` -> `ntoskrnl.exe`
- `WatchdogSleepTimeout` -> `ntoskrnl.exe`
- `AdditionalCriticalWorkerThreads` -> `ntoskrnl.exe`
- `AdditionalDelayedWorkerThreads` -> `ntoskrnl.exe`
- `UuidSequenceNumber` -> `ntoskrnl.exe`
- `AllowRemoteDASD` -> `ntoskrnl.exe`

No-hit set in the first static pass:

- `CustomizeDuringSetup`
- `SourceSettingsVersion`
- `PowerSettingProfile`

## Ghidra triage on ntoskrnl.exe

Imported evidence:

- markdown: `evidence/files/ghidra/kernel-power-existing-ntoskrnl/ghidra-matches.md`
- structured summary: `evidence/files/ghidra/kernel-power-existing-ntoskrnl/evidence.json`
- run log: `evidence/files/ghidra/kernel-power-existing-ntoskrnl/ghidra-run.log`

Key findings:

- `WatchdogResumeTimeout` -> one xref, unresolved block at `140c63608`
- `WatchdogSleepTimeout` -> one xref, unresolved block at `140c635d8`
- `AdditionalCriticalWorkerThreads` -> one xref, unresolved block at `140c62b88`
- `AdditionalDelayedWorkerThreads` -> one xref, unresolved block at `140c62bb8`
- `UuidSequenceNumber` -> string found, but zero direct xrefs resolved by Ghidra
- `AllowRemoteDASD` -> two resolved xrefs, but they map to `\\REGISTRY\\MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\RemovableStorageDevices\\AllowRemoteDASD`

The last bullet is the most important correction in this pass: the `AllowRemoteDASD` hit is not support for the `Session Manager\\I/O System` candidate. It is currently a name collision against a different policy path.

## Host-side triage from Hooke

Hooke's static-only summary lines up with the local evidence:

- `CustomizeDuringSetup`, `SourceSettingsVersion`, and `PowerSettingProfile` still look like bootstrap or implementation-detail values without a trustworthy repo-side semantic map.
- the watchdog pair is the strongest next lane because the names exist in `ntoskrnl.exe` and already have a related repo-side pseudocode lead under `Docs/privacy/assets/sleepstudy-PoFxInitPowerManagement.c`
- the Executive worker-thread pair is promising, but still needs manual fallback work because the first Ghidra pass landed in `<no function>` blocks
- `UuidSequenceNumber` should stay static-only until a concrete reader or branch is found
- `AllowRemoteDASD` should be rerouted away from the current Session Manager candidate lane

## Recommended next gate

Promote to the next Ghidra and runtime-prep lane:

1. `WatchdogResumeTimeout`
2. `WatchdogSleepTimeout`
3. `AdditionalCriticalWorkerThreads`
4. `AdditionalDelayedWorkerThreads`

Keep static-only for now:

- `CustomizeDuringSetup`
- `SourceSettingsVersion`
- `PowerSettingProfile`
- `UuidSequenceNumber`

Demote or reroute:

- `AllowRemoteDASD`

Machine-readable queue update:

- `registry-research-framework/audit/kernel-power-existing-next-gate-20260328.json`
