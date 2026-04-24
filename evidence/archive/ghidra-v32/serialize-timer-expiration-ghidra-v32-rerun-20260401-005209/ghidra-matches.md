# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `serialize-timer-expiration-ghidra-v32-rerun`
- Timestamp: `2026-03-31T22:05:42.408600100Z`
- PDB source: `C:\Tools\Symbols\serialize-timer-expiration-ghidra-v32-rerun`
- Patterns: `SerializeTimerExpiration`

## `SerializeTimerExpiration`

### String @ `140c69fc8`

`SerializeTimerExpiration`

- Function: `<no function>`
- Function source: `unresolved`
- Address: `140c63068`
- Value mapping: `unclear`
- Effect: unclear - keep this as review-only until a PDB-backed branch mapping is available.
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

