# security.disable-defender-sample-submission

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

- `official-doc` Microsoft Learn: Defender Policy CSP SubmitSamplesConsent -> https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-defender
- `official-doc` Microsoft Learn: Block at First Sight dependency on sample submission -> https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-microsoftdefenderantivirus
- `repo-doc` Local Defender sample-submission lead note -> research/notes/windows-11-settings-and-privacy-leads.md
- `repo-doc` Windows Defender dump list includes SubmitSamplesConsent -> Docs/security/assets/Windows-Defender.txt
- `procmon-trace` Win25H2Clean absent-value check for Defender sample submission -> evidence/files/procmon/security.disable-defender-sample-submission/spynet-ui-state2.txt
- `procmon-trace` Win25H2Clean Procmon read for SubmitSamplesConsent = 2 -> evidence/files/procmon/security.disable-defender-sample-submission/submitsamples-ui-state2.txt
