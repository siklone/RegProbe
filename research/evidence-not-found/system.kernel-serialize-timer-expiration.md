# system.kernel-serialize-timer-expiration

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
- `decompilation` Nohuto's and our Ghidra decompilation - Decompiled timer-serialization gate -> research/_source-mirrors/decompiled-pseudocode/ntoskrnl/KeInitializeTimerTable.c and evidence/files/ghidra/system.kernel-serialize-timer-expiration/ghidra-matches.md and evidence/files/ghidra/system.kernel-serialize-timer-expiration/evidence.json
