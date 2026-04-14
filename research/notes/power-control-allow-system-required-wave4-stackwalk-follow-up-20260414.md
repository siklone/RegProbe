# power.control allow-system-required wave4 stackwalk follow-up - 2026-04-14

## Summary

The Wave 4 ETW stackwalk lane closes the old "exact runtime registry read unresolved" gap for `AllowSystemRequiredPowerRequests`.

The retained runtime bundle now contains an explicit `reg.exe query HKLM\SYSTEM\CurrentControlSet\Control\Power /v AllowSystemRequiredPowerRequests` command line plus exact `KeyName = AllowSystemRequiredPowerRequests` hits. Grouped module-offset Ghidra passes then resolved the caller stack into both user-mode and kernel-mode query surfaces.

## Exact runtime query path

The resolved caller-stack chain now includes:

- `reg.exe!QueryValue`
- `reg.exe!QueryRegistry`
- `kernelbase.dll!RegGetValueW`
- `kernelbase.dll!RegQueryValueExW`
- `kernelbase.dll!BaseRegQueryValueInternal`
- `ntdll.dll!NtQueryValueKey`
- `ntoskrnl.exe!NtQueryValueKey`
- `ntoskrnl.exe!EtwpTraceRegistry`
- `ntoskrnl.exe!EtwpTraceStackWalk`
- `ntoskrnl.exe!KiSystemServiceStart`

This is not yet a proof of the earlier boot/init seeding routine, but it is enough to say that the current build can be observed performing an exact runtime query for the value when the explicit query lane is exercised.

## Decision impact

`AllowSystemRequiredPowerRequests` should no longer carry a blocker that says the exact runtime read itself is missing.

The remaining blocker is narrower:

- `system-execution-required-no-current-build-registry-seeding-path`

The old QGA/WPR boot no-hit result still matters as negative evidence for the boot lane, but it no longer describes the whole runtime story because the Wave 4 stackwalk lane now proves an exact current-build query path.
