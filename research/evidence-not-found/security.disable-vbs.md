# security.disable-vbs

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

- `official-doc` Local Microsoft DeviceGuard.admx mapping -> evidence/files/external/c/Windows/PolicyDefinitions/DeviceGuard.admx
- `official-doc` Local Microsoft DeviceGuard.adml help text -> evidence/files/external/c/PolicyDefinitions/en-US/DeviceGuard.adml
- `repo-code` Current app implementation -> app/Services/TweakProviders/SecurityTweakProvider.cs
- `etw-trace` KVM ETW summary receipt for EnableVirtualizationBasedSecurity -> evidence/captures/security-disable-vbs-etw-stackwalk-attempt-20260427.json and evidence/raw/etw-stackwalk/security-disable-vbs-etw-20260427a/security-disable-vbs-etw-20260427a-summary.json
- `vm-test` Guest Ghidra launch receipt for EnableVirtualizationBasedSecurity -> evidence/raw/ghidra/ghidra-security-disable-vbs-20260427a/summary.json
