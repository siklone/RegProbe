# Static Evidence v3.2 Provisioning Follow-Up

This follow-up covers the first real environment pass after the v3.2 static hardening batch landed.

## Symbol tooling

- Script: `scripts/vm/provision-symbol-tools.ps1`
- Audit: `registry-research-framework/audit/symbol-tools-provisioning-20260331.json`
- Result: `ready`

What happened:

- The guest shell was healthy before and after the run.
- The VM is now treated as offline for symbol provisioning. Guest `winget` and guest bootstrapper retries are disabled by default.
- `symchk.exe` was solved from the host side, not the guest side. The host downloaded the official Windows SDK ISO, extracted the debugger tools offline, and staged the full `x64` debugger root into the VM through the HGFS share.
- The canonical guest tool root is now `C:\Tools\SymbolTools`, and `symchk.exe` resolves there cleanly.
- The shared WinDbg package path is still reachable, but it remains a side channel only. It is not the canonical symbol-tool source because it does not contain `symchk.exe`.

## Ghidra pilot rerun

- Artifact root: `evidence/raw/ghidra-v32/serialize-timer-expiration-ghidra-v32-rerun-20260401-005209`
- Result: no longer blocked by missing symbol tooling

What changed:

- The exact guest binary is copied back to the host.
- `symchk` runs on the host against that exact binary with internet access.
- The downloaded symbol cache is staged back into the guest through the shared folder.
- Ghidra now parses the staged `ntkrnlmp.pdb` locally in the guest. The run log contains both:
  - `PDB analyzer parsing file: ...ntkrnlmp.pdb`
  - `PDB Types and Main Symbols Processing Terminated Normally`

Current remaining blocker:

- The environment blocker is gone, but the bounded branch export is still unresolved for the pilot string.
- The current probe is now `review-only`, not `blocked-pdb-missing`.
- `pdb_loaded = true` is verified from the Ghidra run log, but the exported match still resolves to `<no function>`, so no promotion-grade branch claim is made yet.

## IDA provisioning

- Script: `scripts/vm/provision-ida-headless.ps1`
- Audit: `registry-research-framework/audit/ida-provisioning-20260331.json`
- Result: `provisioned-freeware-gui-only`

What changed:

- The official IDA Free installer is now downloaded on the host and can be staged into the guest from the host side.
- The guest now has `C:\Tools\IDA\Freeware\ida64.exe`.
- The freeware install supports interactive string/xref/disassembly review inside the VM.

Current blocker:

- `idat64.exe` is still absent.
- Hex-Rays decompiler is still absent.
- So the original headless IDAPython lane remains blocked until a licensed/headless-capable build is supplied, or until a real automation-capable acceptance state exists in the guest.

## Net result

This follow-up leaves the retained v3.2 pipeline note more explicit about the environment edge:

- `symchk` and the Windows debugger tools are now downloaded on the host and available in the guest
- the Ghidra pilot is no longer blocked by missing symbol tooling or guest internet assumptions
- `IDA` is present in the guest, but only as Freeware GUI tooling
- the production static pipeline remains Ghidra-first; `IDA` is optional and only participates when a working automation-capable build is actually available
- the remaining gap is no longer “can the tools exist in the VM?” but “can the static lane produce a bounded, symbol-backed branch mapping for the target record?”

## Retained audit artifacts

- Bootstrap scan set:
  - [static-evidence-v32-summary-20260331.md](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-summary-20260331.md)
  - [static-evidence-v32-scan-20260331.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-scan-20260331.json)
  - [static-evidence-v32-pdb-missing-20260331.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-pdb-missing-20260331.json)
  - [static-evidence-v32-ghidra-bloat-20260331.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-ghidra-bloat-20260331.json)
  - [static-evidence-v32-link-audit-20260331.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-link-audit-20260331.json)
  - [static-evidence-v32-link-broken-20260331.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-link-broken-20260331.json)
  - [static-evidence-v32-link-context-review-20260331.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-link-context-review-20260331.json)
- Canonical retained rerun set:
  - [static-evidence-v32-summary-20260401.md](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-summary-20260401.md)
  - [static-evidence-v32-scan-20260401.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-scan-20260401.json)
  - [static-evidence-v32-branch-template-missing-20260401.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-branch-template-missing-20260401.json)
  - [static-evidence-v32-pdb-missing-20260401.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-pdb-missing-20260401.json)
  - [static-evidence-v32-ghidra-bloat-20260401.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-ghidra-bloat-20260401.json)
  - [static-evidence-v32-link-audit-20260401.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-link-audit-20260401.json)
  - [static-evidence-v32-link-broken-20260401.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-link-broken-20260401.json)
  - [static-evidence-v32-link-context-review-20260401.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/static-evidence-v32-link-context-review-20260401.json)

Later suffix-only reruns from the same day were transient scan outputs. They were not referenced by retained notes, scripts, or catalog surfaces, so the canonical `20260401` set remains the checked-in audit baseline for this lane.
