# security.threat-file-hash-logging

- Class: `A`
- Record status: `validated`
- Tested build: `26100`
- Reason: `no-hit-or-insufficient-proof`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `runtime_procmon, runtime_reboot, official_doc`
- Tools: `official-doc, procmon, reboot`

## Why it stays negative

This record is cross-layer verified and also aligned with a shipped one-click surface.

## Attached references

- `official-doc` Microsoft Support: March 2016 anti-malware platform update for Endpoint Protection clients -> https://support.microsoft.com/en-gb/topic/march-2016-anti-malware-platform-update-for-endpoint-protection-clients-d99f5dc9-b7a0-bdb2-5161-3efc43d889fa
- `official-doc` Microsoft Learn: Create indicators for files -> https://learn.microsoft.com/en-us/defender-endpoint/indicator-file
- `official-doc` Microsoft Support: March 2016 anti-malware platform update for Endpoint Protection clients -> https://support.microsoft.com/en-gb/topic/march-2016-anti-malware-platform-update-for-endpoint-protection-clients-d99f5dc9-b7a0-bdb2-5161-3efc43d889fa
- `official-doc` Microsoft Learn: Demonstrate cloud-delivered protection -> https://learn.microsoft.com/en-us/defender-endpoint/defender-endpoint-demonstration-cloud-delivered-protection
- `repo-doc` Local Defender dumps and traces for ThreatFileHashLogging and EnableFileHashComputation -> Docs/security/assets/Windows-Defender.txt
- `vm-test` Original high-risk snapshot had Defender disabled -> evidence/files/vm-tooling-staging/defender-runtime-repair.json
