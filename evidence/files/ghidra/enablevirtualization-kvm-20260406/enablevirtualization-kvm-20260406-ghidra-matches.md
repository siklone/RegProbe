# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `enablevirtualization-kvm-20260406`
- Timestamp: `2026-04-06T10:13:42.500937700Z`
- PDB source: `C:\Tools\Symbols\enablevirtualization-kvm-20260406`
- Patterns: `EnableVirtualization`, `EnableLUA`, `EnableInstallerDetection`

## `EnableVirtualization`

### String @ `140af2180`

`EnableVirtualization`

- Function: `PsBootPhaseComplete`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `14077185d`
- Register focus: `R14`, `ESI`, `RAX`, `RBP`, `RSP`
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
140771847  LEA ESI,[R14 + 0x30]
14077184b  LEA RAX,[0x140af2160]
140771852  MOV dword ptr [RBP + -0x54],R14D
140771856  MOV qword ptr [RBP + -0x28],RAX
14077185a  XORPS XMM0,XMM0
; branch_snippet
14077185d  LEA RAX,[0x140af2180]
; context_after
140771864  MOV qword ptr [RSP + 0x70],0x980096
14077186d  MOV qword ptr [RBP + -0x10],RAX
140771871  LEA RAX,[0x140af22f0]
140771878  MOV qword ptr [RBP + 0x8],RAX
14077187c  MOVUPS xmmword ptr [RBP + -0x40],XMM0
```

- Function: `PsBootPhaseComplete`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `14077186d`
- Register focus: `RBP`, `RAX`, `RSP`
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
140771852  MOV dword ptr [RBP + -0x54],R14D
140771856  MOV qword ptr [RBP + -0x28],RAX
14077185a  XORPS XMM0,XMM0
14077185d  LEA RAX,[0x140af2180]
140771864  MOV qword ptr [RSP + 0x70],0x980096
; branch_snippet
14077186d  MOV qword ptr [RBP + -0x10],RAX
; context_after
140771871  LEA RAX,[0x140af22f0]
140771878  MOV qword ptr [RBP + 0x8],RAX
14077187c  MOVUPS xmmword ptr [RBP + -0x40],XMM0
140771880  MOV qword ptr [RBP + -0x80],0x30002e
140771888  MOV dword ptr [RBP + -0x30],0x140012
```

## `EnableLUA`

### String @ `140af2160`

`EnableLUA`

- Function: `PsBootPhaseComplete`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `14077184b`
- Register focus: `RDX`, `RAX`, `RBP`, `R14`, `ESI`, `RSP`
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
140771831  LEA RDX,[0x140af2330]
140771838  LEA RAX,[0x140af2250]
14077183f  MOV dword ptr [RBP + -0x6c],R14D
140771843  MOV qword ptr [RBP + -0x78],RAX
140771847  LEA ESI,[R14 + 0x30]
; branch_snippet
14077184b  LEA RAX,[0x140af2160]
; context_after
140771852  MOV dword ptr [RBP + -0x54],R14D
140771856  MOV qword ptr [RBP + -0x28],RAX
14077185a  XORPS XMM0,XMM0
14077185d  LEA RAX,[0x140af2180]
140771864  MOV qword ptr [RSP + 0x70],0x980096
```

- Function: `PsBootPhaseComplete`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `140771856`
- Register focus: `RBP`, `RAX`, `R14`, `ESI`, `RSP`
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
14077183f  MOV dword ptr [RBP + -0x6c],R14D
140771843  MOV qword ptr [RBP + -0x78],RAX
140771847  LEA ESI,[R14 + 0x30]
14077184b  LEA RAX,[0x140af2160]
140771852  MOV dword ptr [RBP + -0x54],R14D
; branch_snippet
140771856  MOV qword ptr [RBP + -0x28],RAX
; context_after
14077185a  XORPS XMM0,XMM0
14077185d  LEA RAX,[0x140af2180]
140771864  MOV qword ptr [RSP + 0x70],0x980096
14077186d  MOV qword ptr [RBP + -0x10],RAX
140771871  LEA RAX,[0x140af22f0]
```

## `EnableInstallerDetection`

### String @ `140af22f0`

`EnableInstallerDetection`

- Function: `PsBootPhaseComplete`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `140771871`
- Register focus: `RAX`, `RBP`, `RSP`
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
140771856  MOV qword ptr [RBP + -0x28],RAX
14077185a  XORPS XMM0,XMM0
14077185d  LEA RAX,[0x140af2180]
140771864  MOV qword ptr [RSP + 0x70],0x980096
14077186d  MOV qword ptr [RBP + -0x10],RAX
; branch_snippet
140771871  LEA RAX,[0x140af22f0]
; context_after
140771878  MOV qword ptr [RBP + 0x8],RAX
14077187c  MOVUPS xmmword ptr [RBP + -0x40],XMM0
140771880  MOV qword ptr [RBP + -0x80],0x30002e
140771888  MOV dword ptr [RBP + -0x30],0x140012
14077188f  MOV dword ptr [RBP + -0x20],R13D
```

- Function: `PsBootPhaseComplete`
- Function source: `pdb-symbol`
- Function confidence: `string_only_review`
- Address: `140771878`
- Register focus: `RAX`, `RSP`, `RBP`
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
14077185a  XORPS XMM0,XMM0
14077185d  LEA RAX,[0x140af2180]
140771864  MOV qword ptr [RSP + 0x70],0x980096
14077186d  MOV qword ptr [RBP + -0x10],RAX
140771871  LEA RAX,[0x140af22f0]
; branch_snippet
140771878  MOV qword ptr [RBP + 0x8],RAX
; context_after
14077187c  MOVUPS xmmword ptr [RBP + -0x40],XMM0
140771880  MOV qword ptr [RBP + -0x80],0x30002e
140771888  MOV dword ptr [RBP + -0x30],0x140012
14077188f  MOV dword ptr [RBP + -0x20],R13D
140771893  MOV dword ptr [RBP + -0x18],0x2a0028
```
