# system.disable-service-splitting

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

- `official-doc` Microsoft Learn: Service host grouping in Windows 10 -> https://learn.microsoft.com/en-us/windows/application-management/svchost-service-refactoring
- `repo-doc` Repo system research notes for service splitting -> Docs/system/system.md
- `decompiled-pseudocode` Decompiled SCM configuration reader for service splitting -> Docs/system/assets/servicesplitting-ScReadSCMConfiguration.c
- `repo-code` Current app implementation -> app/Services/TweakProviders/SystemTweakProvider.cs
