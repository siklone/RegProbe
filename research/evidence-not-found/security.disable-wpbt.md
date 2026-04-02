# security.disable-wpbt

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

- `official-doc` Microsoft Learn DFCI WPBT setting reference -> https://learn.microsoft.com/en-us/intune/intune-service/configuration/device-firmware-configuration-interface-windows-settings
- `repo-doc` Repo security notes for WPBT -> Docs/security/security.md
- `repo-code` Current app implementation -> app/Services/TweakProviders/SecurityTweakProvider.cs
- `registry-observation` nohuto regkit trace for Session Manager -> research/_source-mirrors/regkit/assets/traces/23H2.txt; research/_source-mirrors/regkit/assets/traces/24H2.txt; research/_source-mirrors/regkit/assets/traces/25H2.txt; research/_source-mirrors/win-registry/records/Session-Manager.txt
