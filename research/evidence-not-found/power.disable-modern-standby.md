# power.disable-modern-standby

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

- `official-doc` Microsoft Learn: What is Modern Standby -> https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/modern-standby
- `repo-code` Former app implementation -> app/Services/TweakProviders/PowerTweakProvider.cs
- `registry-observation` nohuto power trace for MSDisabled -> research/_source-mirrors/win-registry/records/Power.txt
- `repo-doc` Repo power notes -> Docs/power/power.md
