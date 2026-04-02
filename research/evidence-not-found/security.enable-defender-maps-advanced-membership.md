# security.enable-defender-maps-advanced-membership

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

- `official-doc` Microsoft Learn: ADMX_MicrosoftDefenderAntivirus SpynetReporting -> https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-microsoftdefenderantivirus
- `repo-doc` Local Defender MAPS lead note -> research/notes/windows-11-settings-and-privacy-leads.md
- `procmon-trace` Win25H2Clean Procmon baseline for Defender MAPS policy path -> evidence/files/procmon/security.enable-defender-maps-advanced-membership/spynet-ui-baseline.txt
- `procmon-trace` Win25H2Clean Procmon read for SpyNetReporting = 2 -> evidence/files/procmon/security.disable-defender-sample-submission/spynet-ui-state2.txt
- `repo-code` Current security provider MAPS membership write -> app/Services/TweakProviders/SecurityTweakProvider.cs
