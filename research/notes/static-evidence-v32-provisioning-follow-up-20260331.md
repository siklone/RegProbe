# Static Evidence v3.2 Provisioning Follow-Up

This follow-up covers the first real environment pass after the v3.2 static hardening batch landed.

## Symbol tooling

- Script: `scripts/vm/provision-symbol-tools.ps1`
- Audit: `registry-research-framework/audit/symbol-tools-provisioning-20260331.json`
- Result: `blocked-symchk-still-missing`

What happened:

- The guest shell was healthy before and after the run.
- No host-side `symchk.exe` fallback root was available.
- Guest `winget` was present, but all three package attempts failed with the same code: `0x8a15000f`
  - `Microsoft.WinDbg`
  - `Microsoft.WindowsSDK.10.0.18362`
  - `Microsoft.WindowsSDK.10.0.17134`
- After the attempts, `symchk.exe` still was not present anywhere in the guest search roots.

## Ghidra pilot rerun

- Artifact root: `evidence/files/ghidra-v32/serialize-timer-expiration-ghidra-v32-rerun-20260331-222508`
- Result: still `blocked-pdb-missing`

This rerun matters because it used the widened `symchk` discovery logic. The blocker is now confirmed as environment provisioning, not a narrow path assumption inside the Ghidra runner.

## IDA provisioning

- Script: `scripts/vm/provision-ida-headless.ps1`
- Audit: `registry-research-framework/audit/ida-provisioning-20260331.json`
- Result: `blocked-installer-missing`

What changed:

- The script no longer relies on `vmrun` stdout to detect guest state.
- Guest detection now writes a JSON probe and copies it back to the host.
- Host discovery now searches common install roots instead of only a manually supplied folder.

Current blocker:

- No guest `idat64.exe`
- No host portable IDA root
- No local license artifact to validate even if a binary were found later

## Net result

The v3.2 pipeline is now more honest about the environment edge:

- `symchk` is blocked by real guest provisioning failure
- `IDA` is blocked by real installer availability, not by a brittle detection script
- The Ghidra runner and the IDA provisioning path both now fail closed with reproducible JSON output
