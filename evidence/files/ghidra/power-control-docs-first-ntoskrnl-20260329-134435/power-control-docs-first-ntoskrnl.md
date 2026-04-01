# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `power-control-docs-first-ntoskrnl-branch-template-refresh`
- Timestamp: `2026-04-01T04:35:51.554517300Z`
- PDB source: `C:\Tools\Symbols\power-control-docs-first-ntoskrnl-branch-template-refresh`
- Patterns: `Class1InitialUnparkCount`, `HibernateEnabled`, `HibernateEnabledDefault`, `LidReliabilityState`, `MfBufferingThreshold`, `PerfCalculateActualUtilization`, `TimerRebaseThresholdOnDripsExit`

## `Class1InitialUnparkCount`

### String @ `140c6b248`

`Class1InitialUnparkCount`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c64148`
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

## `HibernateEnabled`

### String @ `140028948`

`HibernateEnabled`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140e07500`
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

### String @ `14004902b`

`HibernateEnabled`

### String @ `140c6ada0`

`HibernateEnabledDefault`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c63728`
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

## `HibernateEnabledDefault`

### String @ `140c6ada0`

`HibernateEnabledDefault`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c63728`
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

## `LidReliabilityState`

### String @ `140028a00`

`LidReliabilityState`

- Function: `PopLidReliabilityInit`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `1405cf133`
- Register focus: `RBP`, `EAX`, `RDX`, `RCX`, `RAX`, `RSP`
- Flag focus: `SF`, `ZF`
- Compare: `1405cf12f  TEST EAX,EAX`
- Jump: `1405cf131  JS 0x1405cf187`
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
1405cf123  MOV DIL,0x1
1405cf126  MOVUPS xmmword ptr [RBP + -0x20],XMM1
1405cf12a  CALL 0x14073d560
1405cf12f  TEST EAX,EAX
1405cf131  JS 0x1405cf187
; branch_snippet
1405cf133  LEA RDX,[0x140028a00]
1405cf131  JS 0x1405cf187
1405cf12f  TEST EAX,EAX
; context_after
1405cf13a  LEA RCX,[RBP + -0x30]
1405cf13e  CALL 0x14043ffa0
1405cf143  MOV RCX,qword ptr [RBP + -0x40]
1405cf147  LEA RAX,[RBP + -0x38]
1405cf14b  MOV qword ptr [RSP + 0x28],RAX
```

- Function: `PopSaveLidReliabilityState`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `140747fd8`
- Register focus: `RSP`, `EAX`, `RDX`, `RCX`, `RAX`
- Flag focus: `SF`, `ZF`
- Compare: `140747fd4  TEST EAX,EAX`
- Jump: `140747fd6  JS 0x140748024`
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
140747fc7  XORPS XMM0,XMM0
140747fca  MOVUPS xmmword ptr [RSP + 0x30],XMM0
140747fcf  CALL 0x14073d560
140747fd4  TEST EAX,EAX
140747fd6  JS 0x140748024
; branch_snippet
140747fd8  LEA RDX,[0x140028a00]
140747fd6  JS 0x140748024
140747fd4  TEST EAX,EAX
; context_after
140747fdf  LEA RCX,[RSP + 0x30]
140747fe4  CALL 0x14043ffa0
140747fe9  MOV RCX,qword ptr [RSP + 0x50]
140747fee  LEA RAX,[0x140e0b3c8]
140747ff5  MOV R9D,0x4
```

## `MfBufferingThreshold`

### String @ `140c6b548`

`MfBufferingThreshold`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c63ed8`
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

## `PerfCalculateActualUtilization`

### String @ `140c6b2e0`

`PerfCalculateActualUtilization`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c640b8`
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

## `TimerRebaseThresholdOnDripsExit`

### String @ `140c6c980`

`TimerRebaseThresholdOnDripsExit`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c64ec8`
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

