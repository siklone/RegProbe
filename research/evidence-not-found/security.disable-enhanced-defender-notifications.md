# security.disable-enhanced-defender-notifications

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

- `official-doc` WindowsDefenderSecurityCenter.admx enhanced notifications policy -> Docs/system/system.md
- `official-doc` WindowsDefender.admx reporting enhanced notifications policy -> Docs/system/system.md
- `procmon-trace` Win25H2Clean Procmon baseline for Security Center notifications policy -> evidence/files/procmon/security.disable-enhanced-defender-notifications/defender-disable-enhanced-baseline-1.txt
- `procmon-trace` Win25H2Clean Procmon enabled-state read for Security Center notifications policy -> evidence/files/procmon/security.disable-enhanced-defender-notifications/defender-disable-enhanced-securitycenter-1.txt
- `procmon-trace` Win25H2Clean Procmon reporting-path alias check -> evidence/files/procmon/security.disable-enhanced-defender-notifications/defender-disable-enhanced-reporting-1.txt
- `repo-code` Current security provider enhanced notifications write -> app/Services/TweakProviders/SecurityTweakProvider.cs
