# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `system-io-allow-remote-dasd-ntoskrnl-exe-path-aware-branch-template-refresh`
- Timestamp: `2026-04-01T04:50:41.576994900Z`
- PDB source: `C:\Tools\Symbols\system-io-allow-remote-dasd-ntoskrnl-exe-path-aware-branch-template-refresh`
- Patterns: `AllowRemoteDASD, RemovableStorageDevices`
- Raw matches: `4`
- Committed matches: `2`
- Omitted additional symbolized branch hits: `0`
- Omitted unresolved review hits: `0`
- Omitted lower-confidence review hits: `2`

## `AllowRemoteDASD`

### Branch @ `1404cb6b0`

- Function: `IopAllowRemoteDASD`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Register focus: `EAX, RCX, RBP, R9, RDX, EBX`
- Flag focus: `ZF, SF, CF, OF`
- Compare: `1404cb6bc  TEST EAX,EAX`
- Jump: `1404cb6be  JS 0x1404cb6e3`
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
1404cb6a1  TEST EAX,EAX
1404cb6a3  JS 0x1404cb6ec
1404cb6a5  MOV RCX,qword ptr [RBP + 0x10]
1404cb6a9  LEA R9,[RBP + 0x18]
1404cb6ad  XOR R8D,R8D
; branch_snippet
1404cb6b0  LEA RDX,[0x1406b8040]
1404cb6bc  TEST EAX,EAX
1404cb6be  JS 0x1404cb6e3
1404cb6c4  CMP dword ptr [RCX + 0xc],EBX
1404cb6a3  JS 0x1404cb6ec
1404cb6a1  TEST EAX,EAX
; context_after
1404cb6b7  CALL 0x1409b29dc
1404cb6bc  TEST EAX,EAX
1404cb6be  JS 0x1404cb6e3
1404cb6c0  MOV RCX,qword ptr [RBP + 0x18]
1404cb6c4  CMP dword ptr [RCX + 0xc],EBX
```

_Omitted 0 additional symbolized branch hit(s), 1 lower-confidence review hit(s), and 0 unresolved hit(s) from the committed surface._

## `RemovableStorageDevices`

### Branch @ `1404cb661`

- Function: `IopAllowRemoteDASD`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Register focus: `RBX, RBP, RCX, EBX, RAX, RSP, R8`
- Flag focus: `ZF, CF, SF, OF`
- Compare: `1404cb671  CMP RAX,0xfffe`
- Jump: `unclear`
- Value mapping: `value=0 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `comparison recovered, but nearby jump condition is still unclear.`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `60`
- Heuristic reasons: `pdb-symbol present | compare/test anchor found | value immediate found in bounded block | stack-relative context detected`
- Effect: unclear - keep this as review-only until a PDB-backed branch mapping is available.
- Unclear: `True`

```asm
; context_before
1404cb64a  MOV qword ptr [RBP + 0x10],RBX
1404cb64e  MOV qword ptr [RBP + 0x18],RBX
1404cb652  CALL 0x1404fd750
1404cb657  LEA RCX,[0x1406b7fa0]
1404cb65e  MOV dword ptr [RBP + -0xc],EBX
; branch_snippet
1404cb661  MOV qword ptr [RBP + -0x8],RCX
1404cb671  CMP RAX,0xfffe
; context_after
1404cb665  CALL 0x1404fd750
1404cb66a  ADD RAX,RAX
1404cb66d  MOV byte ptr [RSP + 0x20],BL
1404cb671  CMP RAX,0xfffe
1404cb677  LEA R8,[RBP + -0x10]
```

_Omitted 0 additional symbolized branch hit(s), 1 lower-confidence review hit(s), and 0 unresolved hit(s) from the committed surface._
