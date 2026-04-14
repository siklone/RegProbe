# Ghidra Branch Review

- Program: `KernelBase.dll`
- Probe: `ghidra-symbolized-01-kernelbase-dll-0x2e436-power-control-allow-system-required-power-requests`
- Timestamp: `2026-04-14T01:01:38.272962300Z`
- PDB source: `C:\Tools\Symbols\ghidra-symbolized-01-kernelbase-dll-0x2e436-power-control-allow-system-required-power-requests`
- Patterns: `AllowSystemRequiredPowerRequests`
- Module offsets: `KernelBase.dll+0x2E436`

## Caller stack frame `KernelBase.dll+0x2E436`

- Match kind: `caller_stack_frame`
- Function: `RegGetValueW`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `18002e436`
- Register focus: `RDI`, `R13`, `RSP`, `R14`, `R12`, `ECX`, `RAX`, `EAX`, `EBX`, `EDX`
- Flag focus: `ZF`, `CF`, `SF`, `OF`
- Compare: `18002e447  CMP R13,RAX`
- Jump: `unclear`
- Value mapping: `unclear`
- Branch effect: `comparison recovered, but nearby jump condition is still unclear.`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `50`
- Heuristic reasons: `pdb-symbol present | compare/test anchor found | stack-relative context detected`
- Effect: unclear - keep this as review-only until a PDB-backed branch mapping is available.
- Unclear: `true`

```asm
; context_before
18002e420  CMOVNZ R13,RDI
18002e424  XOR R8D,R8D
18002e427  MOV qword ptr [RSP + 0x28],R13
18002e42c  MOV qword ptr [RSP + 0x20],R14
18002e431  CALL 0x18002ecb0
; branch_snippet
18002e436  MOV ECX,dword ptr [R12]
18002e447  CMP R13,RAX
18002e420  CMOVNZ R13,RDI
; context_after
18002e43a  XOR R11D,R11D
18002e43d  MOV EBX,EAX
18002e43f  MOV EDX,R11D
18002e442  LEA RAX,[RSP + 0x44]
18002e447  CMP R13,RAX
```

## `AllowSystemRequiredPowerRequests`

_No matching strings found._

