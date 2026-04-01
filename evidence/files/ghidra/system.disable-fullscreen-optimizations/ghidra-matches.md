# Ghidra Branch Review

- Program: `ResourcePolicyServer.dll`
- Probe: `resourcepolicysrv-fullscreen-ghidra-branch-template-refresh`
- Timestamp: `2026-04-01T04:51:44.553181300Z`
- PDB source: `C:\Tools\Symbols\resourcepolicysrv-fullscreen-ghidra-branch-template-refresh`
- Patterns: `GameDVR_FSEBehavior, GameDVR_FSEBehaviorMode, GameDVR_HonorUserFSEBehaviorMode, GameDVR_DXGIHonorFSEWindowsCompatible, GameConfigStore`
- Raw matches: `9`
- Committed matches: `2`
- Omitted additional symbolized branch hits: `5`
- Omitted unresolved review hits: `0`
- Omitted lower-confidence review hits: `2`

## `GameDVR_FSEBehavior`

_No matching strings found._

## `GameDVR_FSEBehaviorMode`

_No matching strings found._

## `GameDVR_HonorUserFSEBehaviorMode`

_No matching strings found._

## `GameDVR_DXGIHonorFSEWindowsCompatible`

_No matching strings found._

## `GameConfigStore`

### Branch @ `180006d19`

- Function: `GcsSrv_GetGameConfigSizeForClientProcess`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Register focus: `EAX, EBX, EDX, RCX, RBP, R8`
- Flag focus: `SF, ZF`
- Compare: `180006d0c  TEST EAX,EAX`
- Jump: `180006d0e  JNS 0x180006d28`
- Value mapping: `value=1 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `compare + conditional jump recovered in bounded context.`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `95`
- Heuristic reasons: `pdb-symbol present | compare+jump survived bounded symbolized review | compare/test anchor found | conditional jump found | value immediate found in bounded block | stack-relative context detected`
- Effect: PDB-backed function identity, compare/jump structure, and a bounded value map are present.
- Unclear: `False`

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

### Branch @ `1800074c1`

- Function: `GcsSrv_ModifyGameConfig`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Register focus: `EAX, EBX, EDX, RCX, RBP, R8, RSP`
- Flag focus: `SF, ZF`
- Compare: `1800074b1  TEST EAX,EAX`
- Jump: `1800074b3  JNS 0x1800074d0`
- Value mapping: `value=1 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `compare + conditional jump recovered in bounded context.`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `95`
- Heuristic reasons: `pdb-symbol present | compare+jump survived bounded symbolized review | compare/test anchor found | conditional jump found | value immediate found in bounded block | stack-relative context detected`
- Effect: PDB-backed function identity, compare/jump structure, and a bounded value map are present.
- Unclear: `False`

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

_Omitted 5 additional symbolized branch hit(s), 2 lower-confidence review hit(s), and 0 unresolved hit(s) from the committed surface._
