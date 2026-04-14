# Ghidra Branch Review

- Program: `reg.exe`
- Probe: `ghidra-symbolized-reg-exe-0x128b-power-control-allow-system-required-power-requests`
- Timestamp: `2026-04-14T02:40:04.628243100Z`
- PDB source: `C:\Tools\Symbols\ghidra-symbolized-reg-exe-0x128b-power-control-allow-system-required-power-requests`
- Patterns: `AllowSystemRequiredPowerRequests`
- Module offsets: `reg.exe+0x128B`, `reg.exe+0x379D`, `reg.exe+0x65C6`, `reg.exe+0x6775`

## Caller stack frame `reg.exe+0x128B`

- Match kind: `caller_stack_frame`
- Function: `__scrt_common_main_seh`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `14000128b`
- Register focus: `RDI`, `R8`, `RBX`, `RDX`, `RAX`, `ECX`, `EAX`, `EBX`
- Flag focus: `ZF`
- Compare: `140001292  TEST AL,AL`
- Jump: `140001294  JZ 0x1400012eb`
- Value mapping: `value=1 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `compare + conditional jump recovered in bounded context.`
- Stack note: `no obvious stack-relative access in the bounded context.`
- Exception gate: `none`
- Heuristic score: `90`
- Heuristic reasons: `pdb-symbol present | compare+jump survived bounded symbolized review | compare/test anchor found | conditional jump found | value immediate found in bounded block`
- Effect: PDB-backed function identity, compare/jump structure, and a bounded value map are present.
- Unclear: `false`

```asm
; context_before
140001279  CALL 0x140001f0e
14000127e  MOV R8,RDI
140001281  MOV RDX,RBX
140001284  MOV ECX,dword ptr [RAX]
140001286  CALL 0x14000364c
; branch_snippet
14000128b  MOV EBX,EAX
140001292  TEST AL,AL
140001294  JZ 0x1400012eb
140001296  TEST SIL,SIL
140001299  JNZ 0x1400012a0
; context_after
14000128d  CALL 0x140001b94
140001292  TEST AL,AL
140001294  JZ 0x1400012eb
140001296  TEST SIL,SIL
140001299  JNZ 0x1400012a0
```

## Caller stack frame `reg.exe+0x379D`

- Match kind: `caller_stack_frame`
- Function: `wmain`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `14000379d`
- Register focus: `RDX`, `RSI`, `ECX`, `EBP`, `EAX`, `EDI`
- Flag focus: `unclear`
- Compare: `unclear`
- Jump: `14000379f  JMP 0x140003829`
- Value mapping: `value=1 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `jump recovered, but the compare/test anchor is still unclear.`
- Stack note: `no obvious stack-relative access in the bounded context.`
- Exception gate: `none`
- Heuristic score: `55`
- Heuristic reasons: `pdb-symbol present | conditional jump found | value immediate found in bounded block`
- Effect: unclear - keep this as review-only until a PDB-backed branch mapping is available.
- Unclear: `true`

```asm
; context_before
14000378c  CALL 0x140006c48
140003791  JMP 0x14000379d
140003793  MOV RDX,RSI
140003796  MOV ECX,EBP
140003798  CALL 0x140006404
; branch_snippet
14000379d  MOV EDI,EAX
14000379f  JMP 0x140003829
1400037ae  JMP 0x14000379d
140003791  JMP 0x14000379d
; context_after
14000379f  JMP 0x140003829
1400037a4  MOV RDX,RSI
1400037a7  MOV ECX,EBP
1400037a9  CALL 0x14000450c
1400037ae  JMP 0x14000379d
```

## Caller stack frame `reg.exe+0x65C6`

- Match kind: `caller_stack_frame`
- Function: `QueryRegistry`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `1400065c6`
- Register focus: `RDX`, `RBP`, `R9`, `RCX`, `RSP`, `RAX`, `ECX`, `ESI`, `EAX`, `EDI`, `EBX`
- Flag focus: `unclear`
- Compare: `unclear`
- Jump: `unclear`
- Value mapping: `unclear`
- Branch effect: `unclear`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `35`
- Heuristic reasons: `pdb-symbol present | stack-relative context detected`
- Effect: unclear - keep this as review-only until a PDB-backed branch mapping is available.
- Unclear: `true`

```asm
; context_before
1400065af  MOV RDX,qword ptr [RBP + -0x21]
1400065b3  LEA R9,[RBP + -0x79]
1400065b7  MOV RCX,qword ptr [RSP + 0x38]
1400065bc  MOV qword ptr [RSP + 0x20],RAX
1400065c1  CALL 0x1400066e4
; branch_snippet
1400065c6  MOV ECX,ESI
; context_after
1400065c8  MOV EDI,EAX
1400065ca  XOR EBX,EBX
1400065cc  CALL 0x140001f02
1400065d1  MOV RCX,RAX
1400065d4  LEA RDX,[0x1400129a0]
```

## Caller stack frame `reg.exe+0x6775`

- Match kind: `caller_stack_frame`
- Function: `QueryValue`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `140006775`
- Register focus: `RAX`, `RSP`, `R15`, `EAX`, `EDI`
- Flag focus: `ZF`, `CF`, `SF`, `OF`
- Compare: `14000677c  TEST EAX,EAX`
- Jump: `14000677e  JZ 0x1400067cf`
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
140006759  MOV qword ptr [RSP + 0x30],RAX
14000675e  MOV R9D,0xffff
140006764  MOV qword ptr [RSP + 0x28],R15
140006769  MOV qword ptr [RSP + 0x20],R15
14000676e  CALL qword ptr [0x1400124d8]
; branch_snippet
140006775  NOP dword ptr [RAX + RAX*0x1]
14000677c  TEST EAX,EAX
14000677e  JZ 0x1400067cf
140006780  CMP EAX,0x2
140006783  JNZ 0x14000697f
; context_after
14000677a  MOV EDI,EAX
14000677c  TEST EAX,EAX
14000677e  JZ 0x1400067cf
140006780  CMP EAX,0x2
140006783  JNZ 0x14000697f
```

## `AllowSystemRequiredPowerRequests`

_No matching strings found._

