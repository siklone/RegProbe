# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `ghidra-symbolized-07-power-control-allow-system-required-power-requests`
- Timestamp: `2026-04-14T00:51:19.285502300Z`
- PDB source: `C:\Tools\Symbols\ghidra-symbolized-07-power-control-allow-system-required-power-requests`
- Patterns: `AllowSystemRequiredPowerRequests`
- Module offsets: `ntoskrnl.exe+0x327B4D`

## Caller stack frame `ntoskrnl.exe+0x327B4D`

- Match kind: `caller_stack_frame`
- Function: `FUN_1403278a4`
- Function source: `auto-analysis-fallback`
- Function confidence: `string_only_review`
- Address: `140327b4d`
- Register focus: `EDI`, `RSI`, `R8`, `RDX`, `RBP`, `RCX`, `R13`, `ECX`, `EAX`
- Flag focus: `unclear`
- Compare: `unclear`
- Jump: `140327b52  JMP 0x140327924`
- Value mapping: `value=1 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `jump recovered, but the compare/test anchor is still unclear.`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `30`
- Heuristic reasons: `conditional jump found | value immediate found in bounded block | stack-relative context detected`
- Effect: unclear - keep this as review-only until a PDB-backed branch mapping is available.
- Unclear: `true`

```asm
; context_before
140327b3b  MOV R9D,EDI
140327b3e  MOV R8,RSI
140327b41  LEA RDX,[RBP + 0x30]
140327b45  MOV RCX,R13
140327b48  CALL 0x1403ed650
; branch_snippet
140327b4d  MOVZX R8D,word ptr [RBP]
140327b52  JMP 0x140327924
140327b5b  JMP 0x14032790d
; context_after
140327b52  JMP 0x140327924
140327b57  DEC.LOCK dword ptr [RDX + 0xc]
140327b5b  JMP 0x14032790d
140327b60  XOR ECX,ECX
140327b62  MOV EAX,dword ptr [R13 + 0x504]
```

## `AllowSystemRequiredPowerRequests`

### String @ `140c7d5e0`

`AllowSystemRequiredPowerRequests`

- Match kind: `registry_string_xref`
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

