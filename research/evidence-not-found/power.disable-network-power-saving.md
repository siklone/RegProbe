# power.disable-network-power-saving

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

- `official-doc` Microsoft Learn: Using Registry Values to Enable and Disable Task Offloading -> https://learn.microsoft.com/en-us/windows-hardware/drivers/network/using-registry-values-to-enable-and-disable-task-offloading
- `official-doc` Microsoft Learn: Multimedia Class Scheduler Service -> https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
- `repo-code` Current app implementation -> app/Services/TweakProviders/PowerTweakProvider.cs
- `repo-doc` Repo power notes -> Docs/power/power.md
