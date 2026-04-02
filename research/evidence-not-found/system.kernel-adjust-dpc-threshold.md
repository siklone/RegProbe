# system.kernel-adjust-dpc-threshold

- Class: `E`
- Record status: `deprecated`
- Tested build: `26100`
- Reason: `class-e`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `none`
- Tools: `none`

## Why it stays negative

Archived audit trail only. Keep this out of the normal tweak surface.

## Attached references

- `repo-doc` Repo system research notes for kernel registry values -> Docs/system/system.md
- `repo-code` Current app implementation -> app/Services/TweakProviders/SystemRegistryTweakProvider.cs
- `decompilation` Decompiled DPC threshold system-information handler -> research/_source-mirrors/decompiled-pseudocode/ntoskrnl/NtSetSystemInformation.c; research/_source-mirrors/decompiled-pseudocode/ntoskrnl/ExpQuerySystemInformation.c
