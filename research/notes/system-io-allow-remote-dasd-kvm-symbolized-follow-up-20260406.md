# System I/O AllowRemoteDASD KVM Symbolized Follow-up

Date: 2026-04-06
Candidate: `system.io-allow-remote-dasd`
Guest: `regprobe-win11-25h2-session`

## Objective
- prove that the Linux KVM guest lane can produce a real PDB-backed Ghidra export
- replay the `AllowRemoteDASD` / `RemovableStorageDevices` route on the current 25H2 guest
- keep the result bounded and repo-backed without relying on VMware-specific tooling

## Result
- `ntoskrnl.exe` completed a bounded symbolized Ghidra pass inside the Windows 11 guest
- `pdb_loaded` is `true`
- the leading recovered branch stayed inside `IopAllowRemoteDASD`
- `AllowRemoteDASD` produced one `symbolized_branch` match and one `string_only_review` match
- `RemovableStorageDevices` still landed as review-only context in the same function, which keeps the removable-storage collision story intact

## Operational Notes
- guest-side `symchk.exe` only populated the symbol store reliably when the `SRV*...` path used Windows-style backslashes
- `SetPdbSymbolRepository.java` needed to tolerate staged `.pd_` files so the KVM lane could expand compressed PDBs before Ghidra analysis
- guest-to-host artifact transfer worked over the user-mode NIC using `http://10.0.2.2:<port>` uploads from PowerShell

## Artifacts
- `evidence/raw/ghidra/allowremotedasd-kvm-20260406b/evidence.json`
- `evidence/raw/ghidra/allowremotedasd-kvm-20260406b/ghidra-matches.md`
- `evidence/raw/ghidra/allowremotedasd-kvm-20260406b/symchk-ntos.txt`

## Short Take
- the KVM research lane is now good enough for bounded PDB-backed branch probes
- this replay agrees with the earlier VMware-era result: the current-build function identity is `IopAllowRemoteDASD`, and the surrounding string context still points at the removable-storage policy path rather than a clean Session Manager I/O route
