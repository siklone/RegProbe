# Ghidra Branch Review

- Program: `diagtrack.dll`
- Probe: `diagtrack-reliability-ghidra-branch-template-refresh`
- Timestamp: `2026-04-01T05:11:51.515354700Z`
- PDB source: `C:\Tools\Symbols\diagtrack-reliability-ghidra-branch-template-refresh`
- Patterns: `addr:18038fce0`, `addr:18026bfc0`, `TimeStampInterval`

## `addr:18038fce0`

_No matching strings found._

## `addr:18026bfc0`

_No matching strings found._

## `TimeStampInterval`

### String @ `1803b9778`

`TimeStampInterval`

- Function: `_guard_dispatch_icall$thunk$10345483385596137414`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `18038fce0`
- Register focus: `RBX`, `RCX`
- Flag focus: `unclear`
- Compare: `unclear`
- Jump: `180376010  JMP 0x18034a090`
- Value mapping: `value=1 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `trap/fault-adjacent block detected; control-flow may be misleading.`
- Stack note: `no obvious stack-relative access in the bounded context.`
- Exception gate: `trap-or-fault-adjacent instructions present; control-flow may be misleading.`
- Heuristic score: `30`
- Heuristic reasons: `pdb-symbol present | conditional jump found | value immediate found in bounded block | exception/trap gate forced review-only`
- Effect: unclear - exception-adjacent control flow needs manual review before any semantic claim.
- Unclear: `true`

```asm
; context_before
1803758bd  POP RBX
1803758be  JMP 0x180136910
1803758d0  LEA RCX,[0x180466840]
1803758d7  JMP 0x18008da14
180376000  INT3
; branch_snippet
180376010  JMP 0x18034a090
1803758d7  JMP 0x18008da14
1803758be  JMP 0x180136910
; context_after
```

