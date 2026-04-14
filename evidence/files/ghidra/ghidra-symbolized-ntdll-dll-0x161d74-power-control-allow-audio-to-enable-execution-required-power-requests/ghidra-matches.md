# Ghidra Branch Review

- Program: `ntdll.dll`
- Probe: `ghidra-symbolized-ntdll-dll-0x161d74-power-control-allow-audio-to-enable-execution-required-power-requests`
- Timestamp: `2026-04-14T04:15:42.351766300Z`
- PDB source: `C:\Tools\Symbols\ghidra-symbolized-ntdll-dll-0x161d74-power-control-allow-audio-to-enable-execution-required-power-requests`
- Patterns: `AllowAudioToEnableExecutionRequiredPowerRequests`
- Module offsets: `ntdll.dll+0x161D74`, `ntdll.dll+0x8C48C`

## Caller stack frame `ntdll.dll+0x161D74`

- Match kind: `caller_stack_frame`
- Function: `NtQueryValueKey`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `180161d74`
- Register focus: `RCX`, `R10`, `EAX`
- Flag focus: `ZF`
- Compare: `180161d88  TEST byte ptr [0x7ffe0308],0x1`
- Jump: `180161d70  JNZ 0x180161d75`
- Value mapping: `value=0 participates in this conditional block; opposite branch still needs explicit review.`
- Branch effect: `compare + conditional jump recovered in bounded context.`
- Stack note: `no obvious stack-relative access in the bounded context.`
- Exception gate: `none`
- Heuristic score: `90`
- Heuristic reasons: `pdb-symbol present | compare+jump survived bounded symbolized review | compare/test anchor found | conditional jump found | value immediate found in bounded block`
- Effect: PDB-backed function identity, compare/jump structure, and a bounded value map are present.
- Unclear: `false`

```asm
; context_before
180161d60  MOV R10,RCX
180161d63  MOV EAX,0x17
180161d68  TEST byte ptr [0x7ffe0308],0x1
180161d70  JNZ 0x180161d75
180161d72  SYSCALL
; branch_snippet
180161d74  RET
180161d88  TEST byte ptr [0x7ffe0308],0x1
180161d70  JNZ 0x180161d75
180161d68  TEST byte ptr [0x7ffe0308],0x1
; context_after
180161d75  INT 0x2e
180161d77  RET
180161d80  MOV R10,RCX
180161d83  MOV EAX,0x18
180161d88  TEST byte ptr [0x7ffe0308],0x1
```

## Caller stack frame `ntdll.dll+0x8C48C`

- Match kind: `caller_stack_frame`
- Function: `RtlUserThreadStart`
- Function source: `pdb-symbol`
- Function confidence: `symbolized_branch`
- Address: `18008c48c`
- Register focus: `RDX`, `R9`, `RAX`, `RCX`, `ECX`
- Flag focus: `ZF`, `CF`, `SF`, `OF`
- Compare: `18008c480  CMP RAX,RCX`
- Jump: `18008c48c  JMP 0x18008c4b6`
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
18008c47d  MOV RDX,R9
18008c480  CMP RAX,RCX
18008c483  JZ 0x18008c48e
18008c485  XOR ECX,ECX
18008c487  CALL 0x180172020
; branch_snippet
18008c48c  JMP 0x18008c4b6
18008c495  JMP 0x18008c48c
18008c483  JZ 0x18008c48e
18008c480  CMP RAX,RCX
; context_after
18008c48e  XOR ECX,ECX
18008c490  CALL 0x18000d900
18008c495  JMP 0x18008c48c
18008c497  MOV RCX,RDX
18008c49a  MOV RAX,R9
```

## `AllowAudioToEnableExecutionRequiredPowerRequests`

_No matching strings found._

