# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `ghidra-power-kernel-static-batch-20260423`
- Timestamp: `2026-04-23T05:03:05.328256500Z`
- PDB source: `C:\Tools\Symbols\ghidra-power-kernel-static-batch-20260423`
- Patterns: `Win32kCalloutWatchdogTimeoutSeconds`, `ForceBugcheckForDpcWatchdog`, `GlobalTimerResolutionRequests`, `TimerCheckFlags`
- Module offsets: ``

## `Win32kCalloutWatchdogTimeoutSeconds`

### String @ `140c7ba60`

`Win32kCalloutWatchdogTimeoutSeconds`

- Match kind: `registry_string_xref`
- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c74148`
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
140c71b48  AND AL,byte ptr [RAX]
140c71b4a  AND AL,0x0
140c71b4c  ADD byte ptr [RAX],AL
140c71b4e  ADD byte ptr [RAX],AL
140c71b50  PUSH RAX
; branch_snippet
140c71b51  HLT
; context_after
```

## `ForceBugcheckForDpcWatchdog`

### String @ `140c7f340`

`ForceBugcheckForDpcWatchdog`

- Match kind: `registry_string_xref`
- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c78618`
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
140c71b48  AND AL,byte ptr [RAX]
140c71b4a  AND AL,0x0
140c71b4c  ADD byte ptr [RAX],AL
140c71b4e  ADD byte ptr [RAX],AL
140c71b50  PUSH RAX
; branch_snippet
140c71b51  HLT
; context_after
```

## `GlobalTimerResolutionRequests`

### String @ `140c7ae00`

`GlobalTimerResolutionRequests`

- Match kind: `registry_string_xref`
- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c734e8`
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
140c71b48  AND AL,byte ptr [RAX]
140c71b4a  AND AL,0x0
140c71b4c  ADD byte ptr [RAX],AL
140c71b4e  ADD byte ptr [RAX],AL
140c71b50  PUSH RAX
; branch_snippet
140c71b51  HLT
; context_after
```

## `TimerCheckFlags`

### String @ `140c7a940`

`TimerCheckFlags`

- Match kind: `registry_string_xref`
- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c73938`
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
140c71b48  AND AL,byte ptr [RAX]
140c71b4a  AND AL,0x0
140c71b4c  ADD byte ptr [RAX],AL
140c71b4e  ADD byte ptr [RAX],AL
140c71b50  PUSH RAX
; branch_snippet
140c71b51  HLT
; context_after
```

