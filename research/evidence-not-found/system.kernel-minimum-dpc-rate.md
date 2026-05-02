# system.kernel-minimum-dpc-rate

- Class: `E`
- Record status: `deprecated`
- Tested build: `2026-03-21 review snapshot`
- Reason: `class-e`

This record remains negative evidence on build 2026-03-21 review snapshot: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `none`
- Tools: `none`

## Why it stays negative

Archived audit trail only. Keep this out of the normal tweak surface.

## Attached references

- `decompilation` nohuto mirror: minimum DPC rate query path -> research/_source-mirrors/decompiled-pseudocode/ntoskrnl/ExpQuerySystemInformation.c; research/_source-mirrors/win-config/system/desc.md
- `repo-doc` Repo system research notes for kernel registry values -> Docs/system/system.md
- `repo-code` Current app implementation -> app/Services/TweakProviders/SystemRegistryTweakProvider.cs
