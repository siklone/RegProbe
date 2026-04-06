# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `allowremotedasd-kvm-20260406b`
- Timestamp: `2026-04-06T09:04:09.953940500Z`
- PDB source: `C:\Tools\Symbols\allowremotedasd-kvm-20260406`
- Patterns: `AllowRemoteDASD`, `RemovableStorageDevices`

## `AllowRemoteDASD`

### String @ `1406c3340`

`AllowRemoteDASD`

- Function: `IopAllowRemoteDASD`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `1404cb37f`
- Register focus: `RBX`, `RSP`, `RBP`, `EBX`, `RCX`
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
1404cb370  MOV qword ptr [RSP + 0x18],RBX
1404cb375  PUSH RBP
1404cb376  MOV RBP,RSP
1404cb379  SUB RSP,0x40
1404cb37d  XOR EBX,EBX
; branch_snippet
1404cb37f  LEA RCX,[0x1406c3340]
; context_after
1404cb386  MOV qword ptr [RBP + 0x10],RBX
1404cb38a  MOV qword ptr [RBP + 0x18],RBX
1404cb38e  CALL 0x1404ffed0
1404cb393  LEA RCX,[0x1406c32a0]
1404cb39a  MOV dword ptr [RBP + -0xc],EBX
```

- Function: `IopAllowRemoteDASD`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `1404cb3ec`
- Register focus: `EAX`, `RCX`, `RBP`, `R9`, `RDX`, `EBX`
- Flag focus: `ZF`, `SF`, `CF`, `OF`
- Compare: `1404cb3f8  TEST EAX,EAX`
- Jump: `1404cb3fa  JS 0x1404cb41f`
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
1404cb3dd  TEST EAX,EAX
1404cb3df  JS 0x1404cb428
1404cb3e1  MOV RCX,qword ptr [RBP + 0x10]
1404cb3e5  LEA R9,[RBP + 0x18]
1404cb3e9  XOR R8D,R8D
; branch_snippet
1404cb3ec  LEA RDX,[0x1406c3340]
1404cb3f8  TEST EAX,EAX
1404cb3fa  JS 0x1404cb41f
1404cb400  CMP dword ptr [RCX + 0xc],EBX
1404cb3df  JS 0x1404cb428
1404cb3dd  TEST EAX,EAX
; context_after
1404cb3f3  CALL 0x1409cad5c
1404cb3f8  TEST EAX,EAX
1404cb3fa  JS 0x1404cb41f
1404cb3fc  MOV RCX,qword ptr [RBP + 0x18]
1404cb400  CMP dword ptr [RCX + 0xc],EBX
```

## `RemovableStorageDevices`

### String @ `1406c32a0`

`\REGISTRY\MACHINE\SOFTWARE\Policies\Microsoft\Windows\RemovableStorageDevices`

- Function: `IopAllowRemoteDASD`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `1404cb393`
- Register focus: `EBX`, `RCX`, `RBX`, `RBP`, `RAX`, `RSP`
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
1404cb37d  XOR EBX,EBX
1404cb37f  LEA RCX,[0x1406c3340]
1404cb386  MOV qword ptr [RBP + 0x10],RBX
1404cb38a  MOV qword ptr [RBP + 0x18],RBX
1404cb38e  CALL 0x1404ffed0
; branch_snippet
1404cb393  LEA RCX,[0x1406c32a0]
; context_after
1404cb39a  MOV dword ptr [RBP + -0xc],EBX
1404cb39d  MOV qword ptr [RBP + -0x8],RCX
1404cb3a1  CALL 0x1404ffed0
1404cb3a6  ADD RAX,RAX
1404cb3a9  MOV byte ptr [RSP + 0x20],BL
```

- Function: `IopAllowRemoteDASD`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `1404cb39d`
- Register focus: `RBX`, `RBP`, `RCX`, `EBX`, `RAX`, `RSP`, `R8`
- Flag focus: `ZF`, `CF`, `SF`, `OF`
- Compare: `1404cb3ad  CMP RAX,0xfffe`
- Jump: `unclear`
- Value mapping: `value=0 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `comparison recovered, but nearby jump condition is still unclear.`
- Stack note: `stack-relative access is visible in the bounded context; review local variables and home-space assumptions before claiming semantics.`
- Exception gate: `none`
- Heuristic score: `60`
- Heuristic reasons: `pdb-symbol present | compare/test anchor found | value immediate found in bounded block | stack-relative context detected`
- Effect: unclear - keep this as review-only until a PDB-backed branch mapping is available.
- Unclear: `true`

```asm
; context_before
1404cb386  MOV qword ptr [RBP + 0x10],RBX
1404cb38a  MOV qword ptr [RBP + 0x18],RBX
1404cb38e  CALL 0x1404ffed0
1404cb393  LEA RCX,[0x1406c32a0]
1404cb39a  MOV dword ptr [RBP + -0xc],EBX
; branch_snippet
1404cb39d  MOV qword ptr [RBP + -0x8],RCX
1404cb3ad  CMP RAX,0xfffe
; context_after
1404cb3a1  CALL 0x1404ffed0
1404cb3a6  ADD RAX,RAX
1404cb3a9  MOV byte ptr [RSP + 0x20],BL
1404cb3ad  CMP RAX,0xfffe
1404cb3b3  LEA R8,[RBP + -0x10]
```
