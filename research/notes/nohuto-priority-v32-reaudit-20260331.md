# Nohuto Priority Re-Audit v3.2

This note summarizes the first two v3.2 credibility re-audits that were explicitly queued from Nohuto feedback.

## system.priority-control

- PDB loaded: `false`
- Ghidra function: `blocked-pdb-required`
- IDA function: `blocked-ida-missing`
- Branch mapping: `not attached in v3.2 yet`
- Link status: `reachable_but_mismatch`
- Verdict: `A`, but only because runtime Procmon, reboot, benchmark, and official-path evidence are already strong enough without raw bitmask static promotion

What changed from the old evidence:

- The record no longer treats Microsoft's `Win32_OperatingSystem` page as proof of the raw `0x26` bit semantics.
- The Microsoft page is now scoped to path plus high-level WMI mapping only.
- The raw `0x26` interpretation is explicitly labeled as repo interpretation / historical decomp provenance.
- The static section is now fail-closed: no `FUN_` function name, no long pseudocode dump, no synthetic branch prose.

## power.disable-network-power-saving.policy

- PDB loaded: `false`
- Ghidra function: `not-run`
- IDA function: `blocked-ida-missing`
- Branch mapping: `not required for the narrowed child verdict`
- Link status: `reachable_but_mismatch`
- Verdict: `A`, but only for the narrowed child claim covering `DisableTaskOffload` plus the documented `SystemResponsiveness` path and rounding/clamping behavior

What changed from the old evidence:

- The record no longer uses the MMCSS page as proof of `NetworkThrottlingIndex` semantics.
- `SystemResponsiveness` is now framed narrowly: documented path plus rounding/clamping behavior only.
- The claim of one universal stock `SystemResponsiveness` default across all builds was removed.
- The opaque `NetworkThrottlingIndex` write stays outside this child record in the deprecated parent audit trail.

## Pilot infrastructure status

- Ghidra symbolized pilot on `system.kernel-serialize-timer-expiration`: `blocked-pdb-missing`
- IDA headless provisioning: `blocked-installer-missing`
- Cross-verification comparison logic: controlled samples now prove `match`, `conflict`, and `insufficient` output states

## Net effect

The point of this re-audit was not to make the records look stronger. It was to make them narrower, reproducible, and harder to over-read. The old weak static wording has been replaced with explicit source separation, fail-closed static blocks, and cross-verification scaffolding that does not silently pass when the tools are missing.
