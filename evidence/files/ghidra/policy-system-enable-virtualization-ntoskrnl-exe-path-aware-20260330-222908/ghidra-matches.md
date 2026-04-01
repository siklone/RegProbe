# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `policy-system-enable-virtualization-ntoskrnl-exe-path-aware-branch-template-refresh`
- Timestamp: `2026-04-01T04:21:36.052901800Z`
- PDB source: `C:\Tools\Symbols\policy-system-enable-virtualization-ntoskrnl-exe-path-aware-branch-template-refresh`
- Patterns: `EnableVirtualization, EnableLUA, EnableInstallerDetection`
- Raw matches: `6`
- Committed matches: `3`
- Omitted additional symbolized branch hits: `0`
- Omitted unresolved review hits: `0`
- Omitted lower-confidence review hits: `3`

## `EnableVirtualization`

### Branch @ `140761e9d`

- Function: `PsBootPhaseComplete`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Register focus: `R14, ESI, RAX, RBP, RSP`
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
- Unclear: `True`

```asm
; context_before
140761e87  LEA ESI,[R14 + 0x30]
140761e8b  LEA RAX,[0x140adf5f0]
140761e92  MOV dword ptr [RBP + -0x54],R14D
140761e96  MOV qword ptr [RBP + -0x28],RAX
140761e9a  XORPS XMM0,XMM0
; branch_snippet
140761e9d  LEA RAX,[0x140adf610]
; context_after
140761ea4  MOV qword ptr [RSP + 0x70],0x980096
140761ead  MOV qword ptr [RBP + -0x10],RAX
140761eb1  LEA RAX,[0x140adf640]
140761eb8  MOV qword ptr [RBP + 0x8],RAX
140761ebc  MOVUPS xmmword ptr [RBP + -0x40],XMM0
```

_Omitted 0 additional symbolized branch hit(s), 1 lower-confidence review hit(s), and 0 unresolved hit(s) from the committed surface._

## `EnableLUA`

### Branch @ `140761e8b`

- Function: `PsBootPhaseComplete`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Register focus: `RDX, RAX, RBP, R14, ESI, RSP`
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
- Unclear: `True`

```asm
; context_before
140761e71  LEA RDX,[0x140adf680]
140761e78  LEA RAX,[0x140adf5c0]
140761e7f  MOV dword ptr [RBP + -0x6c],R14D
140761e83  MOV qword ptr [RBP + -0x78],RAX
140761e87  LEA ESI,[R14 + 0x30]
; branch_snippet
140761e8b  LEA RAX,[0x140adf5f0]
; context_after
140761e92  MOV dword ptr [RBP + -0x54],R14D
140761e96  MOV qword ptr [RBP + -0x28],RAX
140761e9a  XORPS XMM0,XMM0
140761e9d  LEA RAX,[0x140adf610]
140761ea4  MOV qword ptr [RSP + 0x70],0x980096
```

_Omitted 0 additional symbolized branch hit(s), 1 lower-confidence review hit(s), and 0 unresolved hit(s) from the committed surface._

## `EnableInstallerDetection`

### Branch @ `140761eb1`

- Function: `PsBootPhaseComplete`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Register focus: `RAX, RBP, RSP`
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
- Unclear: `True`

```asm
; context_before
140761e96  MOV qword ptr [RBP + -0x28],RAX
140761e9a  XORPS XMM0,XMM0
140761e9d  LEA RAX,[0x140adf610]
140761ea4  MOV qword ptr [RSP + 0x70],0x980096
140761ead  MOV qword ptr [RBP + -0x10],RAX
; branch_snippet
140761eb1  LEA RAX,[0x140adf640]
; context_after
140761eb8  MOV qword ptr [RBP + 0x8],RAX
140761ebc  MOVUPS xmmword ptr [RBP + -0x40],XMM0
140761ec0  MOV qword ptr [RBP + -0x80],0x30002e
140761ec8  MOV dword ptr [RBP + -0x30],0x140012
140761ecf  MOV dword ptr [RBP + -0x20],R13D
```

_Omitted 0 additional symbolized branch hit(s), 1 lower-confidence review hit(s), and 0 unresolved hit(s) from the committed surface._
