# Ghidra Branch Review

- Program: `kernel32.dll`
- Probe: `ghidra-symbolized-kernel32-dll-0x2e8d7-power-control-allow-system-required-power-requests`
- Timestamp: `2026-04-14T02:17:26.519048100Z`
- PDB source: `C:\Tools\Symbols\ghidra-symbolized-kernel32-dll-0x2e8d7-power-control-allow-system-required-power-requests`
- Patterns: `AllowSystemRequiredPowerRequests`
- Module offsets: `kernel32.dll+0x2E8D7`

## Caller stack frame `kernel32.dll+0x2E8D7`

- Match kind: `caller_stack_frame`
- Function: `BaseThreadInitThunk`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `18002e8d7`
- Register focus: `ECX`, `RCX`, `R8`, `RAX`, `RDX`, `EAX`
- Flag focus: `ZF`
- Compare: `18002e8c8  TEST ECX,ECX`
- Jump: `18002e8ca  JNZ 0x18002e8e6`
- Value mapping: `value=1 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `trap/fault-adjacent block detected; control-flow may be misleading.`
- Stack note: `no obvious stack-relative access in the bounded context.`
- Exception gate: `trap-or-fault-adjacent instructions present; control-flow may be misleading.`
- Heuristic score: `45`
- Heuristic reasons: `pdb-symbol present | compare/test anchor found | conditional jump found | value immediate found in bounded block | exception/trap gate forced review-only`
- Effect: unclear - exception-adjacent control flow needs manual review before any semantic claim.
- Unclear: `true`

```asm
; context_before
18002e8c8  TEST ECX,ECX
18002e8ca  JNZ 0x18002e8e6
18002e8cc  MOV RCX,R8
18002e8cf  MOV RAX,RDX
18002e8d2  CALL 0x180086010
; branch_snippet
18002e8d7  MOV ECX,EAX
18002e8ca  JNZ 0x18002e8e6
18002e8c8  TEST ECX,ECX
; context_after
18002e8d9  CALL qword ptr [0x18008b278]
18002e8e0  NOP dword ptr [RAX + RAX*0x1]
18002e8e5  INT3
18002e8e6  CALL qword ptr [0x18008ba18]
18002e8ed  NOP dword ptr [RAX + RAX*0x1]
```

## `AllowSystemRequiredPowerRequests`

_No matching strings found._

