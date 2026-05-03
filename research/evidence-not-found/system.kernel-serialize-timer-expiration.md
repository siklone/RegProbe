# system.kernel-serialize-timer-expiration

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

- `repo-doc` Repo system research notes for kernel registry values -> Docs/system/system.md
- `repo-code` Current app implementation -> app/Services/TweakProviders/SystemRegistryTweakProvider.cs
- `decompilation` Nohuto's and our Ghidra decompilation - Decompiled timer-serialization gate -> research/_source-mirrors/decompiled-pseudocode/ntoskrnl/KeInitializeTimerTable.c and evidence/raw/ghidra/system.kernel-serialize-timer-expiration/ghidra-matches.md and evidence/raw/ghidra/system.kernel-serialize-timer-expiration/evidence.json
- `etw-trace` Bounded KVM ETW stackwalk captures exact SerializeTimerExpiration helper query -> evidence/raw/etw-stackwalk/system.kernel-serialize-timer-expiration-etw-20260424e/system.kernel-serialize-timer-expiration-etw-20260424e-summary.json and evidence/raw/etw-stackwalk/system.kernel-serialize-timer-expiration-etw-20260424e/system.kernel-serialize-timer-expiration-etw-20260424e-stage.json and evidence/raw/etw-stackwalk/system.kernel-serialize-timer-expiration-etw-20260424e/system.kernel-serialize-timer-expiration-etw-20260424e.etl and evidence/raw/etw-stackwalk/system.kernel-serialize-timer-expiration-etw-20260424e/normalized-registry-bundle.json and evidence/captures/system-kernel-serialize-timer-expiration-etw-stackwalk-20260424.json
