# privacy.disable-cross-device-experiences

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

- `policy-csp` Microsoft ADMX_GroupPolicy Policy CSP: EnableCDP -> https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-grouppolicy#enablecdp
- `official-doc` Local Microsoft GroupPolicy.admx EnableCDP mapping -> evidence/files/external/c/Windows/PolicyDefinitions/GroupPolicy.admx
- `official-doc` Local Microsoft GroupPolicy.adml EnableCDP help text -> evidence/files/external/c/PolicyDefinitions/en-US/GroupPolicy.adml
- `repo-code` Current app implementation -> app/Services/TweakProviders/PrivacyTweakProvider.cs
- `decompiled-pseudocode` Decompiled Shared Experiences singleton -> Docs/privacy/assets/crossdev-SharedExperiencesSingleton.c
- `vm-test` Guest launch of CrossDeviceResume -> evidence/files/vm-tooling-staging/crossdevice_resume_probe.csv
