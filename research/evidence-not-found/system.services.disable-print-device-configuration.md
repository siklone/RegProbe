# system.services.disable-print-device-configuration

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

- `official-doc` Microsoft Learn: Guidance on configuring system services - Print Device Configuration Service -> https://learn.microsoft.com/en-us/windows/iot/iot-enterprise/optimize/services
- `repo-doc` Local SCM snapshot - PrintDeviceConfigurationService -> research/notes/service-snapshots/printdeviceconfigurationservice-sc-qc-2026-03-14.txt
- `repo-code` Current app implementation -> app/Services/TweakProviders/SystemTweakProvider.cs
