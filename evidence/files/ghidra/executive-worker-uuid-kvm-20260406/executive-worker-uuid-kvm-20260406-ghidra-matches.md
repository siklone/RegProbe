# Ghidra Branch Review

- Program: `ntoskrnl.exe`
- Probe: `executive-worker-uuid-kvm-20260406`
- Timestamp: `2026-04-06T09:57:25.854091700Z`
- PDB source: `C:\Tools\Symbols\executive-worker-uuid-kvm-20260406`
- Patterns: `AdditionalCriticalWorkerThreads`, `AdditionalDelayedWorkerThreads`, `UuidSequenceNumber`

## `AdditionalCriticalWorkerThreads`

### String @ `140c7ad60`

`AdditionalCriticalWorkerThreads`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c73f78`
- Register focus: `EAX`, `RAX`
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
140c71630  OR EAX,0xa00
140c71635  ADD AH,CL
140c71637  INT3
140c71640  AND byte ptr [RAX],AL
140c71642  ADD byte ptr [RAX],AL
; branch_snippet
140c71644  INT3
; context_after
```

## `AdditionalDelayedWorkerThreads`

### String @ `140c7ada0`

`AdditionalDelayedWorkerThreads`

- Function: `<no function>`
- Function source: `unresolved`
- Function confidence: `string_only_review`
- Address: `140c73fa8`
- Register focus: `EAX`, `RAX`
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
140c71630  OR EAX,0xa00
140c71635  ADD AH,CL
140c71637  INT3
140c71640  AND byte ptr [RAX],AL
140c71642  ADD byte ptr [RAX],AL
; branch_snippet
140c71644  INT3
; context_after
```

## `UuidSequenceNumber`

### String @ `140038cd8`

`UuidSequenceNumber`
