# system.kernel-dpc-watchdog-period

- Class: `E`
- Record status: `deprecated`
- Tested build: `current Learn snapshot`
- Reason: `class-e`

This record remains negative evidence on build current Learn snapshot: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `none`
- Tools: `none`

## Why it stays negative

Archived audit trail only. Keep this out of the normal tweak surface.

## Attached references

- `official-doc` Microsoft Learn: KeQueryDpcWatchdogInformation function -> https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-kequerydpcwatchdoginformation
- `official-doc` Microsoft Learn: Avoiding DPC Watchdog timeout problems in StorPort Miniports -> https://learn.microsoft.com/en-us/troubleshoot/windows-hardware/drivers/avoid-dpc-watchdog-timeout-problems
- `official-doc` Microsoft Learn: KDPC_WATCHDOG_INFORMATION structure -> https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_kdpc_watchdog_information
- `decompilation` Decompiled DPC watchdog configuration reader -> research/_source-mirrors/decompiled-pseudocode/ntoskrnl/KeQueryDpcWatchdogConfiguration.c
- `repo-doc` Repo system research notes for kernel registry values -> Docs/system/system.md
- `repo-code` Current app implementation -> app/Services/TweakProviders/SystemRegistryTweakProvider.cs
