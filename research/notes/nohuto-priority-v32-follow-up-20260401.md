# Nohuto Priority Re-Audit Follow-Up v3.2

This follow-up closes the last remaining `partial` item in the Nohuto priority queue.

## system.priority-control

- PDB loaded: `true`
- Ghidra function: `GetNTInfo; PutInstance`
- IDA function: `blocked-ida-missing`
- Branch mapping: `value=1 participates in bounded GetNTInfo and PutInstance conditional blocks; opposite branches still need explicit review.`
- Link status: `reachable_manual_review`
- Verdict: `A`

What changed from the previous v3.2 pass:

- The record no longer carries a `blocked-pdb-required` static state.
- A bounded PDB-backed Ghidra artifact is now attached at [system-priority-control-ghidra-v32-20260401-112136](/H:/D/Dev/RegProbe/evidence/files/ghidra-v32/system-priority-control-ghidra-v32-20260401-112136).
- The primary static proof now comes from current-build `cimwin32.dll` branch output instead of historical decomp prose.
- The raw `0x26` semantics still stay labeled as repo interpretation rather than Microsoft contract.

## power.disable-network-power-saving.policy

- PDB loaded: `false`
- Ghidra function: `not-run`
- IDA function: `blocked-ida-missing`
- Branch mapping: `not required for the narrowed child verdict`
- Link status: `reachable_manual_review`
- Verdict: `A`

What changed from the previous v3.2 pass:

- The narrowed child record remains resolved.
- The MMCSS page is still scoped to `SystemResponsiveness` path plus rounding/clamping behavior only.
- The unresolved `NetworkThrottlingIndex` claim stays outside this child record.

## Queue result

The current scanner pass now reports both priority items as `resolved` in [nohuto-priority-queue-20260401g.json](/H:/D/Dev/RegProbe/registry-research-framework/audit/nohuto-priority-queue-20260401g.json).
