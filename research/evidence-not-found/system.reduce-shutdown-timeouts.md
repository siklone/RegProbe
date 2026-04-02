# system.reduce-shutdown-timeouts

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

- `official-doc` Microsoft Learn: SystemParametersInfoW function -> https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow
- `official-doc` Microsoft Learn: RM_SHUTDOWN_TYPE -> https://learn.microsoft.com/en-us/windows/win32/api/restartmanager/ne-restartmanager-rm_shutdown_type
- `official-doc` Microsoft Learn: Service Control Handler Function -> https://learn.microsoft.com/en-us/windows/win32/services/service-control-handler-function
- `repo-doc` Repo system research notes for shutdown timeouts -> Docs/system/system.md
- `repo-code` Current app implementation -> app/Services/TweakProviders/SystemRegistryTweakProvider.cs
