# Ghidra Branch Review

- Program: `ResourcePolicyServer.dll`
- Probe: `resourcepolicysrv-fullscreen-ghidra-branch-template-refresh`
- Timestamp: `2026-04-01T04:51:44.553181300Z`
- PDB source: `C:\Tools\Symbols\resourcepolicysrv-fullscreen-ghidra-branch-template-refresh`
- Patterns: `GameDVR_FSEBehavior`, `GameDVR_FSEBehaviorMode`, `GameDVR_HonorUserFSEBehaviorMode`, `GameDVR_DXGIHonorFSEWindowsCompatible`, `GameConfigStore`

## `GameDVR_FSEBehavior`

### String @ `18001dcd8`

`GameDVR_FSEBehavior`

### String @ `18001dd00`

`GameDVR_FSEBehaviorMode`

## `GameDVR_FSEBehaviorMode`

### String @ `18001dd00`

`GameDVR_FSEBehaviorMode`

## `GameDVR_HonorUserFSEBehaviorMode`

### String @ `18001de10`

`GameDVR_HonorUserFSEBehaviorMode`

## `GameDVR_DXGIHonorFSEWindowsCompatible`

### String @ `18001de60`

`GameDVR_DXGIHonorFSEWindowsCompatible`

## `GameConfigStore`

### String @ `18001d730`

`onecore\base\appmodel\resourcepolicy\gameconfigstore\server\gameconfigstorerpcserver.cpp`

- Function: `GcsSrv_ModifyGameConfig`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `1800074c1`
- Register focus: `EAX`, `EBX`, `EDX`, `RCX`, `RBP`, `R8`, `RSP`
- Flag focus: `SF`, `ZF`
- Compare: `1800074b1  TEST EAX,EAX`
- Jump: `1800074b3  JNS 0x1800074d0`
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
1800074af  MOV EBX,EAX
1800074b1  TEST EAX,EAX
1800074b3  JNS 0x1800074d0
1800074b5  MOV EDX,0x17e
1800074ba  MOV RCX,qword ptr [RBP + 0x398]
; branch_snippet
1800074c1  LEA R8,[0x18001d730]
1800074b3  JNS 0x1800074d0
1800074b1  TEST EAX,EAX
; context_after
1800074c8  MOV R9D,EBX
1800074cb  CALL 0x180006678
1800074d0  MOV RCX,qword ptr [RSP + 0x20]
1800074d5  CALL 0x180005984
1800074da  MOV EAX,EBX
```

- Function: `GcsSrv_GetGameConfigSizeForClientProcess`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `180006d19`
- Register focus: `EAX`, `EBX`, `EDX`, `RCX`, `RBP`, `R8`
- Flag focus: `SF`, `ZF`
- Compare: `180006d0c  TEST EAX,EAX`
- Jump: `180006d0e  JNS 0x180006d28`
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
180006d0a  MOV EBX,EAX
180006d0c  TEST EAX,EAX
180006d0e  JNS 0x180006d28
180006d10  MOV EDX,0x150
180006d15  MOV RCX,qword ptr [RBP + 0x18]
; branch_snippet
180006d19  LEA R8,[0x18001d730]
180006d0e  JNS 0x180006d28
180006d0c  TEST EAX,EAX
; context_after
180006d20  MOV R9D,EAX
180006d23  CALL 0x180006678
180006d28  MOV RCX,qword ptr [RBP + -0x38]
180006d2c  CALL 0x180005984
180006d31  MOV EAX,EBX
```

- Function: `GetGameConfigStoreServer`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `1800060dc`
- Register focus: `EAX`, `EBX`, `RCX`, `RBP`, `R8`, `EDX`, `ECX`
- Flag focus: `SF`, `ZF`
- Compare: `1800060d4  TEST EAX,EAX`
- Jump: `1800060f0  JMP 0x180006191`
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
1800060cd  CALL 0x18000ce90
1800060d2  MOV EBX,EAX
1800060d4  TEST EAX,EAX
1800060d6  JNS 0x1800060f5
1800060d8  MOV RCX,qword ptr [RBP + 0x28]
; branch_snippet
1800060dc  LEA R8,[0x18001d730]
1800060f0  JMP 0x180006191
1800060d6  JNS 0x1800060f5
1800060d4  TEST EAX,EAX
; context_after
1800060e3  MOV R9D,EAX
1800060e6  MOV EDX,0x62
1800060eb  CALL 0x180006678
1800060f0  JMP 0x180006191
1800060f5  MOV ECX,0x28
```

- Function: `GetGameConfigStoreServer`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `180006136`
- Register focus: `EAX`, `EBX`, `RCX`, `RBP`, `R8`, `EDX`, `RDI`
- Flag focus: `SF`, `ZF`
- Compare: `18000612e  TEST EAX,EAX`
- Jump: `180006130  JNS 0x180006154`
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
180006127  CALL 0x18000a630
18000612c  MOV EBX,EAX
18000612e  TEST EAX,EAX
180006130  JNS 0x180006154
180006132  MOV RCX,qword ptr [RBP + 0x28]
; branch_snippet
180006136  LEA R8,[0x18001d730]
180006130  JNS 0x180006154
18000612e  TEST EAX,EAX
; context_after
18000613d  MOV R9D,EAX
180006140  MOV EDX,0x67
180006145  CALL 0x180006678
18000614a  MOV RCX,RDI
18000614d  CALL 0x180005968
```

- Function: `GetGameConfigStoreServer`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `1800061ad`
- Register focus: `EAX`, `EBX`, `RCX`, `RBP`, `R8`, `EDX`, `RDI`, `R14`
- Flag focus: `SF`, `ZF`
- Compare: `1800061a5  TEST EAX,EAX`
- Jump: `1800061c1  JMP 0x1800061c6`
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
18000619e  CALL 0x180008590
1800061a3  MOV EBX,EAX
1800061a5  TEST EAX,EAX
1800061a7  JNS 0x1800061c3
1800061a9  MOV RCX,qword ptr [RBP + 0x28]
; branch_snippet
1800061ad  LEA R8,[0x18001d730]
1800061c1  JMP 0x1800061c6
1800061a7  JNS 0x1800061c3
1800061a5  TEST EAX,EAX
; context_after
1800061b4  MOV R9D,EAX
1800061b7  MOV EDX,0x75
1800061bc  CALL 0x180006678
1800061c1  JMP 0x1800061c6
1800061c3  MOV qword ptr [R14],RDI
```

- Function: `ShutdownGameConfigRpcServer`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `1800078b7`
- Register focus: `EAX`, `EBX`, `EDX`, `RCX`, `RSP`, `R8`
- Flag focus: `SF`, `ZF`
- Compare: `1800078a9  TEST EAX,EAX`
- Jump: `1800078d0  JMP 0x180007901`
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
1800078a7  MOV EBX,EAX
1800078a9  TEST EAX,EAX
1800078ab  JNS 0x1800078d2
1800078ad  MOV EDX,0xf8
1800078b2  MOV RCX,qword ptr [RSP + 0x38]
; branch_snippet
1800078b7  LEA R8,[0x18001d730]
1800078d0  JMP 0x180007901
1800078ab  JNS 0x1800078d2
1800078a9  TEST EAX,EAX
; context_after
1800078be  MOV R9D,EBX
1800078c1  CALL 0x180006678
1800078c6  LEA RCX,[RSP + 0x20]
1800078cb  CALL 0x180005c60
1800078d0  JMP 0x180007901
```

### String @ `18001d790`

`gameConfigStoreManagement`

- Function: `GcsSrv_Preamble`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `180005e47`
- Register focus: `RCX`, `RBP`, `R8`, `RDX`, `EAX`, `RAX`
- Flag focus: `ZF`, `SF`, `CF`, `OF`
- Compare: `180005e5e  TEST EAX,EAX`
- Jump: `180005e60  JNS 0x180005e7e`
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
180005e34  JMP 0x180005f23
180005e39  CMP SIL,0x1
180005e3d  JNZ 0x180005ea0
180005e3f  MOV RCX,qword ptr [RBP + -0x28]
180005e43  LEA R8,[RBP + -0x30]
; branch_snippet
180005e47  LEA RDX,[0x18001d790]
180005e5e  TEST EAX,EAX
180005e60  JNS 0x180005e7e
180005e3d  JNZ 0x180005ea0
180005e39  CMP SIL,0x1
180005e34  JMP 0x180005f23
; context_after
180005e4e  MOV byte ptr [RBP + -0x30],0x0
180005e52  CALL qword ptr [0x1800270a0]
180005e59  NOP dword ptr [RAX + RAX*0x1]
180005e5e  TEST EAX,EAX
180005e60  JNS 0x180005e7e
```

### String @ `18001d9c0`

`System\GameConfigStore\Parents`

- Function: `GetGameConfigStoreParentsPath`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `180008a60`
- Register focus: `EAX`, `RAX`, `RBP`, `RBX`
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
180008a31  RET
180008a40  MOV EAX,0x80004001
180008a45  RET
180008a50  LEA RAX,[0x18001da00]
180008a57  RET
; branch_snippet
180008a60  LEA RAX,[0x18001d9c0]
; context_after
180008a67  RET
180008a70  LEA RAX,[0x18001da40]
180008a77  RET
180008a80  PUSH RBP
180008a82  PUSH RBX
```

### String @ `18001da00`

`System\GameConfigStore\Children`

- Function: `GetGameConfigStoreChildrenPath`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `180008a50`
- Register focus: `RBP`, `RBX`, `EAX`, `RAX`
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
180008a2f  POP RBP
180008a30  POP RBX
180008a31  RET
180008a40  MOV EAX,0x80004001
180008a45  RET
; branch_snippet
180008a50  LEA RAX,[0x18001da00]
; context_after
180008a57  RET
180008a60  LEA RAX,[0x18001d9c0]
180008a67  RET
180008a70  LEA RAX,[0x18001da40]
180008a77  RET
```

_Stopped after 4 matching strings to keep the export bounded._

