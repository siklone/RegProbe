# Defender Cloud Demo Sample Metadata

Record: `security.threat-file-hash-logging`

This artifact replaces the legacy staging placeholder for the extracted Microsoft Defender cloud-delivered protection demo sample. The sample executable itself is intentionally not checked in; this repository keeps deterministic metadata plus the paired VM detection outputs.

## Source

- Microsoft demo download: `https://go.microsoft.com/fwlink/?linkid=2298135`
- Microsoft documentation: `https://learn.microsoft.com/en-us/defender-endpoint/defender-endpoint-demonstration-cloud-delivered-protection`

## Extracted File

| Field | Value |
|---|---|
| File name | `microsoft-defender-cloud-demo.exe` |
| File description | `BaFS Sample` |
| Original file name | `BaFS Sample.exe` |
| SHA256 | `670b00e90a7c9eb7ac6674441551e7764a8364c26e44dcc92474a9abcfac4c04` |
| Size | `123904` bytes |

## Scope

This artifact supports sample identity, source-chain reproducibility, and the VM follow-up notes. It does not by itself prove Defender event `1120` or registry value consumption.
