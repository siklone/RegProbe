# Ghidra Branch Review

- Program: `cimwin32.dll`
- Probe: `system-priority-control-ghidra-v32`
- Timestamp: `2026-04-01T08:24:34.325437100Z`
- PDB source: `C:\Tools\Symbols\system-priority-control-ghidra-v32`
- Patterns: `Win32PrioritySeparation`
- Raw matches: `3`
- Committed matches: `2`
- Omitted additional symbolized branch hits: `1`
- Omitted unresolved review hits: `0`
- Omitted lower-confidence review hits: `0`

## `Win32PrioritySeparation`

### Branch @ `180046654`

- Function: `GetNTInfo`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Register focus: `RAX, EAX, RSP, R8, RDX, RCX`
- Flag focus: `ZF`
- Compare: `18004666c  TEST EAX,EAX`
- Jump: `18004666e  JNZ 0x1800466ab`
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
18004663f  CALL qword ptr [0x1801434c0]
180046646  NOP dword ptr [RAX + RAX*0x1]
18004664b  TEST EAX,EAX
18004664d  JNZ 0x1800466ab
18004664f  LEA R8,[RSP + 0x20]
; branch_snippet
180046654  LEA RDX,[0x18016b568]
18004666c  TEST EAX,EAX
18004666e  JNZ 0x1800466ab
18004664d  JNZ 0x1800466ab
18004664b  TEST EAX,EAX
; context_after
18004665b  LEA RCX,[RSP + 0x70]
180046660  CALL qword ptr [0x180143778]
180046667  NOP dword ptr [RAX + RAX*0x1]
18004666c  TEST EAX,EAX
18004666e  JNZ 0x1800466ab
```

### Branch @ `1800ca579`

- Function: `PutInstance`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Register focus: `RAX, EAX, RSP, R8, RDX, RCX`
- Flag focus: `ZF`
- Compare: `1800ca591  TEST EAX,EAX`
- Jump: `1800ca593  JNZ 0x1800ca629`
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
1800ca560  CALL qword ptr [0x1801434c0]
1800ca567  NOP dword ptr [RAX + RAX*0x1]
1800ca56c  TEST EAX,EAX
1800ca56e  JNZ 0x1800ca6dd
1800ca574  LEA R8,[RSP + 0x60]
; branch_snippet
1800ca579  LEA RDX,[0x18016b568]
1800ca591  TEST EAX,EAX
1800ca593  JNZ 0x1800ca629
1800ca56e  JNZ 0x1800ca6dd
1800ca56c  TEST EAX,EAX
; context_after
1800ca580  LEA RCX,[RSP + 0x70]
1800ca585  CALL qword ptr [0x180143778]
1800ca58c  NOP dword ptr [RAX + RAX*0x1]
1800ca591  TEST EAX,EAX
1800ca593  JNZ 0x1800ca629
```

_Omitted 1 additional symbolized branch hit(s), 0 lower-confidence review hit(s), and 0 unresolved hit(s) from the committed surface._
