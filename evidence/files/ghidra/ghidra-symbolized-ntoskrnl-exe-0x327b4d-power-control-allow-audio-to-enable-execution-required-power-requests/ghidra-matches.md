# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `ghidra-symbolized-ntoskrnl-exe-0x327b4d-power-control-allow-audio-to-enable-execution-required-power-requests`
- Timestamp: `2026-04-14T04:29:37.133879800Z`
- PDB source: `C:\Tools\Symbols\ghidra-symbolized-ntoskrnl-exe-0x327b4d-power-control-allow-audio-to-enable-execution-required-power-requests`
- Patterns: `AllowAudioToEnableExecutionRequiredPowerRequests`
- Module offsets: `ntoskrnl.exe+0x327B4D`, `ntoskrnl.exe+0x3ED794`, `ntoskrnl.exe+0x3EDD84`, `ntoskrnl.exe+0x6BE358`, `ntoskrnl.exe+0x87108C`, `ntoskrnl.exe+0xAE49F6`

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

## Caller stack frame `ntoskrnl.exe+0x3ED794`

- Match kind: `caller_stack_frame`
- Function: `EtwpStackTraceDispatcher`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `1403ed794`
- Register focus: `RBP`, `R9`, `RDI`, `R8`, `EBX`, `EDX`, `RCX`, `RSI`, `RSP`, `R13`, `RBX`
- Flag focus: `unclear`
- Compare: `unclear`
- Jump: `unclear`
- Value mapping: `value=0 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `unclear`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `45`
- Heuristic reasons: `pdb-symbol present | value immediate found in bounded block | stack-relative context detected`
- Effect: unclear - keep this as review-only until a PDB-backed branch mapping is available.
- Unclear: `true`

```asm
; context_before
1403ed784  MOV R9,RBP
1403ed787  MOV R8,RDI
1403ed78a  MOV EDX,EBX
1403ed78c  MOV RCX,RSI
1403ed78f  CALL 0x1403edbd0
; branch_snippet
1403ed794  ADD RSP,0x40
; context_after
1403ed798  POP R13
1403ed79a  POP RDI
1403ed79b  POP RSI
1403ed79c  POP RBP
1403ed79d  POP RBX
```

## Caller stack frame `ntoskrnl.exe+0x3EDD84`

- Match kind: `caller_stack_frame`
- Function: `EtwpTraceStackWalk`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `1403edd84`
- Register focus: `R12`, `EAX`, `RSI`, `RSP`, `R13`, `ECX`
- Flag focus: `ZF`, `CF`, `SF`, `OF`
- Compare: `1403edd89  TEST CL,0x2`
- Jump: `1403edd8c  JNZ 0x1403ee160`
- Value mapping: `value=0 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `compare + conditional jump recovered in bounded context.`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `95`
- Heuristic reasons: `pdb-symbol present | compare+jump survived bounded symbolized review | compare/test anchor found | conditional jump found | value immediate found in bounded block | stack-relative context detected`
- Effect: PDB-backed function identity, compare/jump structure, and a bounded value map are present.
- Unclear: `false`

```asm
; context_before
1403edd6a  MOVZX EAX,byte ptr [R12 + 0x7d]
1403edd70  AND EAX,0x1
1403edd73  MOV qword ptr [R12 + 0x6b0],RSI
1403edd7b  MOV dword ptr [RSP + 0x30],EAX
1403edd7f  CALL 0x14027d7a0
; branch_snippet
1403edd84  MOVZX ECX,byte ptr [R13 + 0x7]
1403edd89  TEST CL,0x2
1403edd8c  JNZ 0x1403ee160
1403edd9b  CMP dword ptr [RSP + 0x30],ECX
; context_after
1403edd89  TEST CL,0x2
1403edd8c  JNZ 0x1403ee160
1403edd92  MOVZX ECX,byte ptr [R12 + 0x7d]
1403edd98  AND ECX,0x1
1403edd9b  CMP dword ptr [RSP + 0x30],ECX
```

## Caller stack frame `ntoskrnl.exe+0x6BE358`

- Match kind: `caller_stack_frame`
- Function: `KiSystemServiceStart`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `1406be358`
- Register focus: `RAX`, `R10`, `RBP`, `RBX`, `RDI`, `RSI`, `R11`
- Flag focus: `ZF`
- Compare: `1406be37e  TEST byte ptr [RBP + 0xf0],0x1`
- Jump: `1406be34a  JNZ 0x1406bebfa`
- Value mapping: `value=0 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `compare + conditional jump recovered in bounded context.`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `95`
- Heuristic reasons: `pdb-symbol present | compare+jump survived bounded symbolized review | compare/test anchor found | conditional jump found | value immediate found in bounded block | stack-relative context detected`
- Effect: PDB-backed function identity, compare/jump structure, and a bounded value map are present.
- Unclear: `false`

```asm
; context_before
1406be340  TEST dword ptr [0x140fc5b08],0x40
1406be34a  JNZ 0x1406bebfa
1406be350  MOV RAX,R10
1406be353  CALL RAX
1406be355  NOP dword ptr [RAX]
; branch_snippet
1406be358  INC dword ptr GS:[0x2eb8]
1406be37e  TEST byte ptr [RBP + 0xf0],0x1
1406be34a  JNZ 0x1406bebfa
1406be340  TEST dword ptr [0x140fc5b08],0x40
; context_after
1406be360  MOV RBX,qword ptr [RBP + 0xc0]
1406be367  MOV RDI,qword ptr [RBP + 0xc8]
1406be36e  MOV RSI,qword ptr [RBP + 0xd0]
1406be375  MOV R11,qword ptr GS:[0x188]
1406be37e  TEST byte ptr [RBP + 0xf0],0x1
```

## Caller stack frame `ntoskrnl.exe+0x87108C`

- Match kind: `caller_stack_frame`
- Function: `EtwpTraceRegistry`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `14087108c`
- Register focus: `RSP`, `RDX`, `EAX`, `R11`, `EBX`, `RDI`, `RSI`
- Flag focus: `ZF`
- Compare: `unclear`
- Jump: `14087109d  JNZ 0x140870fc0`
- Value mapping: `value=0 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `jump recovered, but the compare/test anchor is still unclear.`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `60`
- Heuristic reasons: `pdb-symbol present | conditional jump found | value immediate found in bounded block | stack-relative context detected`
- Effect: unclear - keep this as review-only until a PDB-backed branch mapping is available.
- Unclear: `true`

```asm
; context_before
140871072  MOV qword ptr [RSP + RAX*0x8 + 0x70],0x2
14087107b  LEA EAX,[RDX + 0x1]
14087107e  LEA RDX,[RSP + 0x68]
140871083  MOV dword ptr [RSP + 0x20],EAX
140871087  CALL 0x1403274f0
; branch_snippet
14087108c  MOV R8D,0x900
14087109d  JNZ 0x140870fc0
; context_after
140871092  LEA R11,[0x140010290]
140871099  BSF R10D,EBX
14087109d  JNZ 0x140870fc0
1408710a3  MOV RDI,qword ptr [RSP + 0xe0]
1408710ab  MOV RSI,qword ptr [RSP + 0xd8]
```

## Caller stack frame `ntoskrnl.exe+0xAE49F6`

- Match kind: `caller_stack_frame`
- Function: `NtQueryValueKey`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `140ae49f6`
- Register focus: `EBX`, `RDX`, `RSP`, `RAX`, `RCX`
- Flag focus: `ZF`, `CF`, `SF`, `OF`
- Compare: `140ae49fb  TEST RAX,RAX`
- Jump: `140ae49fe  JZ 0x140ae4a15`
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
140ae49e1  MOV R9D,R15D
140ae49e4  MOV R8D,EBX
140ae49e7  LEA RDX,[RSP + 0x210]
140ae49ef  MOV CL,0x10
140ae49f1  CALL 0x1406b3df0
; branch_snippet
140ae49f6  MOV RAX,qword ptr [RSP + 0x68]
140ae49fb  TEST RAX,RAX
140ae49fe  JZ 0x140ae4a15
140ae4a08  CMP RAX,RCX
140ae4a0b  JZ 0x140ae4a15
; context_after
140ae49fb  TEST RAX,RAX
140ae49fe  JZ 0x140ae4a15
140ae4a00  LEA RCX,[RSP + 0x230]
140ae4a08  CMP RAX,RCX
140ae4a0b  JZ 0x140ae4a15
```

## `AllowAudioToEnableExecutionRequiredPowerRequests`

### String @ `140c7d530`

`AllowAudioToEnableExecutionRequiredPowerRequests`

- Match kind: `registry_string_xref`
- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c76288`
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

