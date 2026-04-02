# security.hide-defender-exclusions-from-local-admins

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

- `official-doc` Microsoft Learn: Defender CSP HideExclusionsFromLocalAdmins -> https://learn.microsoft.com/en-us/windows/client-management/mdm/defender-csp
- `official-doc` Microsoft Learn: Configure exclusions in Defender Antivirus -> https://learn.microsoft.com/en-us/defender-endpoint/configure-exclusions-microsoft-defender-antivirus
- `repo-doc` Windows Defender dump list includes root and Policy Manager HideExclusionsFromLocalAdmins -> Docs/security/assets/Windows-Defender.txt
- `vm-test` Win25H2Clean baseline visibility with managed exclusion present -> evidence/files/vm-tooling-staging/hideexclusions-admins-baseline-1-20260325-001524/hideexclusions-admins-baseline-visibility.json
- `procmon-trace` Win25H2Clean root-path read for HideExclusionsFromLocalAdmins = 1 -> evidence/files/procmon/security.hide-defender-exclusions-from-local-admins/hideexclusions-admins-root-1.txt
- `vm-test` Win25H2Clean visibility change with root-path HideExclusionsFromLocalAdmins = 1 -> evidence/files/vm-tooling-staging/hideexclusions-admins-root-1-20260325-002348/hideexclusions-admins-root-1-visibility.json
