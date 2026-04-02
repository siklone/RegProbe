# developer.windows-dev-mode

- Class: `A`
- Record status: `validated`
- Tested build: `26100`
- Reason: `no-hit-or-insufficient-proof`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `runtime_procmon, official_doc`
- Tools: `official-doc, procmon`

## Why it stays negative

This record is cross-layer verified and also aligned with a shipped one-click surface.

## Attached references

- `official-doc` Microsoft Learn: Enable your device for development -> https://learn.microsoft.com/en-us/windows/advanced-settings/developer-mode
- `procmon-trace` Procmon capture - Developer settings search reads AppModelUnlock baseline -> evidence/files/procmon/developer.windows-dev-mode/devmode_probe2.csv and evidence/files/procmon/developer.windows-dev-mode/devmode_probe2.txt
- `official-doc` Local Microsoft AppxPackageManager.admx mapping -> evidence/files/external/c/Windows/PolicyDefinitions/AppxPackageManager.admx
- `official-doc` Local Microsoft AppxPackageManager.adml help text -> evidence/files/external/c/PolicyDefinitions/en-US/AppxPackageManager.adml
- `repo-code` Current app implementation -> app/Services/TweakProviders/DeveloperTweakProvider.cs
