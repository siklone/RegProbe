# developer.windows-dev-mode

- Class: `A`
- Record status: `validated`
- Tested build: `26100`
- Reason: `no-hit-or-insufficient-proof`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `runtime_procmon, behavior_wpr, official_doc`
- Tools: `official-doc, etw, procmon, wpr`

## Why it stays negative

This record is cross-layer verified and also aligned with a shipped one-click surface.

## Attached references

- `official-doc` Microsoft Learn: Enable your device for development -> https://learn.microsoft.com/en-us/windows/advanced-settings/developer-mode
- `procmon-trace` Procmon capture - Developer settings search reads AppModelUnlock baseline -> evidence/raw/procmon/developer.windows-dev-mode/devmode_probe2.csv and evidence/raw/procmon/developer.windows-dev-mode/devmode_probe2.txt
- `official-doc` Local Microsoft AppxPackageManager.admx mapping -> evidence/files/external/c/Windows/PolicyDefinitions/AppxPackageManager.admx
- `official-doc` Local Microsoft AppxPackageManager.adml help text -> evidence/files/external/c/PolicyDefinitions/en-US/AppxPackageManager.adml
- `repo-code` Current app research-surface implementation -> Docs/research/app-surface/validated-registry-values.json and app/Services/TweakProviders/ResearchAppSurfaceTweakProvider.cs
- `etw-trace` KVM ETW stage receipt for AllowDevelopmentWithoutDevLicense -> evidence/captures/developer-windows-dev-mode-etw-stackwalk-attempt-20260424.json and evidence/raw/etw-stackwalk/developer-windows-dev-mode-etw-20260424-batch1/developer-windows-dev-mode-etw-20260424-batch1-stage.json
