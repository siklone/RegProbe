# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `kernel-serialize-timer-expiration-ghidra-branch-template-refresh`
- Timestamp: `2026-04-01T05:05:39.689851400Z`
- PDB source: `C:\Tools\Symbols\kernel-serialize-timer-expiration-ghidra-branch-template-refresh`
- Patterns: `addr:140c63068`, `SerializeTimerExpiration`

## `addr:140c63068`

_No matching strings found._

## `SerializeTimerExpiration`

### String @ `140c69fc8`

`SerializeTimerExpiration`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c63068`
- Register focus: `RAX`
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
140c618b1  HLT
140c619a8  ADD byte ptr CS:[RAX],DH
140c619ab  ADD byte ptr [RAX],AL
140c619ad  ADD byte ptr [RAX],AL
140c619af  ADD byte ptr [RAX],DL
; branch_snippet
140c619b1  INT1
; context_after
```

