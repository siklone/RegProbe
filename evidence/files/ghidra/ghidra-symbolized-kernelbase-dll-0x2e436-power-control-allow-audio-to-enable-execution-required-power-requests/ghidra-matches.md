# Ghidra Branch Review

- Program: `KernelBase.dll`
- Probe: `ghidra-symbolized-kernelbase-dll-0x2e436-power-control-allow-audio-to-enable-execution-required-power-requests`
- Timestamp: `2026-04-14T04:11:12.174392600Z`
- PDB source: `C:\Tools\Symbols\ghidra-symbolized-kernelbase-dll-0x2e436-power-control-allow-audio-to-enable-execution-required-power-requests`
- Patterns: `AllowAudioToEnableExecutionRequiredPowerRequests`
- Module offsets: `KernelBase.dll+0x2E436`, `KernelBase.dll+0x2EDAB`, `KernelBase.dll+0x30AAD`

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

## Caller stack frame `KernelBase.dll+0x2EDAB`

- Match kind: `caller_stack_frame`
- Function: `RegQueryValueExW`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `18002edab`
- Register focus: `RDI`, `R9`, `RSP`, `R8`, `RDX`, `RCX`, `R13`, `EAX`, `ESI`, `R15`, `EDX`
- Flag focus: `ZF`
- Compare: `18002edb5  TEST ESI,ESI`
- Jump: `18002edb7  JZ 0x18002ee03`
- Value mapping: `value=1 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `compare + conditional jump recovered in bounded context.`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `95`
- Heuristic reasons: `pdb-symbol present | compare+jump survived bounded symbolized review | compare/test anchor found | conditional jump found | value immediate found in bounded block | stack-relative context detected`
- Effect: PDB-backed function identity, compare/jump structure, and a bounded value map are present.
- Unclear: `false`

```asm
; context_before
18002ed96  MOV R9,RDI
18002ed99  LEA R8,[RSP + 0x30]
18002ed9e  LEA RDX,[RSP + 0x58]
18002eda3  MOV RCX,R13
18002eda6  CALL 0x180030940
; branch_snippet
18002edab  MOV ESI,EAX
18002edb5  TEST ESI,ESI
18002edb7  JZ 0x18002ee03
18002edbd  TEST R15,R15
; context_after
18002edad  MOV R8D,dword ptr [RSP + 0xc0]
18002edb5  TEST ESI,ESI
18002edb7  JZ 0x18002ee03
18002edb9  MOV EDX,dword ptr [RSP + 0x30]
18002edbd  TEST R15,R15
```

## Caller stack frame `KernelBase.dll+0x30AAD`

- Match kind: `caller_stack_frame`
- Function: `BaseRegQueryValueInternal`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `180030aad`
- Register focus: `RAX`, `RSP`, `RDX`, `RSI`, `RCX`, `RDI`, `R13`, `EAX`, `EBX`, `RBP`, `R14`
- Flag focus: `ZF`
- Compare: `180030ab4  TEST R13,R13`
- Jump: `180030ab7  JNZ 0x180030d62`
- Value mapping: `value=1 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `compare + conditional jump recovered in bounded context.`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `95`
- Heuristic reasons: `pdb-symbol present | compare+jump survived bounded symbolized review | compare/test anchor found | conditional jump found | value immediate found in bounded block | stack-relative context detected`
- Effect: PDB-backed function identity, compare/jump structure, and a bounded value map are present.
- Unclear: `false`

```asm
; context_before
180030a96  MOV qword ptr [RSP + 0x28],RAX
180030a9b  MOV RDX,RSI
180030a9e  MOV RCX,RDI
180030aa1  MOV dword ptr [RSP + 0x20],R14D
180030aa6  CALL qword ptr [0x1802909a8]
; branch_snippet
180030aad  NOP dword ptr [RAX + RAX*0x1]
180030ab4  TEST R13,R13
180030ab7  JNZ 0x180030d62
; context_after
180030ab2  MOV EBX,EAX
180030ab4  TEST R13,R13
180030ab7  JNZ 0x180030d62
180030abd  MOV RDI,qword ptr [RBP + -0x68]
180030ac1  LEA R14,[RBP + -0x40]
```

## `AllowAudioToEnableExecutionRequiredPowerRequests`

_No matching strings found._

