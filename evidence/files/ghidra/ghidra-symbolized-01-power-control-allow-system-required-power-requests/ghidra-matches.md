# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `ghidra-symbolized-01-power-control-allow-system-required-power-requests`
- Timestamp: `2026-04-14T00:26:33.103233600Z`
- PDB source: `C:\Tools\Symbols\ghidra-symbolized-01-power-control-allow-system-required-power-requests`
- Patterns: `AllowSystemRequiredPowerRequests`

## `AllowSystemRequiredPowerRequests`

### String @ `140c7d5e0`

`AllowSystemRequiredPowerRequests`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c76258`
- Register focus: `EAX`, `RAX`
- Flag focus: `unclear`
- Compare: `unclear`
- Jump: `unclear`
- Value mapping: `unclear`
- Branch effect: `trap/fault-adjacent block detected; control-flow may be misleading.`
- Stack note: `no obvious stack-relative access in the bounded context.`
- Exception gate: `trap-or-fault-adjacent instructions present; control-flow may be misleading.`
- Heuristic score: `0`
- Heuristic reasons: `exception/trap gate forced review-only`
- Effect: unclear - exception-adjacent control flow needs manual review before any semantic claim.
- Unclear: `true`

```asm
; context_before
140c71630  OR EAX,0xa00
140c71635  ADD AH,CL
140c71637  INT3
140c71640  AND byte ptr [RAX],AL
140c71642  ADD byte ptr [RAX],AL
; branch_snippet
140c71644  INT3
; context_after
```

